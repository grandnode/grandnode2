using Grand.Business.Core.Interfaces.Marketing.Contacts;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Web.Commands.Models.Contact;
using MediatR;

namespace Grand.Web.Commands.Handler.Contact;

public class ContactAttributeChangeCommandHandler : IRequestHandler<ContactAttributeChangeCommand, (IList<string>
    enabledAttributeIds, IList<string> disabledAttributeIds)>
{
    private readonly IContactAttributeParser _contactAttributeParser;
    private readonly IContactAttributeService _contactAttributeService;
    private readonly IDownloadService _downloadService;

    public ContactAttributeChangeCommandHandler(IContactAttributeService contactAttributeService,
        IContactAttributeParser contactAttributeParser,
        IDownloadService downloadService)
    {
        _contactAttributeService = contactAttributeService;
        _contactAttributeParser = contactAttributeParser;
        _downloadService = downloadService;
    }

    public async Task<(IList<string> enabledAttributeIds, IList<string> disabledAttributeIds)> Handle(
        ContactAttributeChangeCommand request, CancellationToken cancellationToken)
    {
        var customAttributes = await ParseContactAttributes(request);

        var enabledAttributeIds = new List<string>();
        var disabledAttributeIds = new List<string>();
        var attributes = await _contactAttributeService.GetAllContactAttributes(request.Store.Id);
        foreach (var attribute in attributes)
        {
            var conditionMet = await _contactAttributeParser.IsConditionMet(attribute, customAttributes);
            if (!conditionMet.HasValue) continue;
            if (conditionMet.Value)
                enabledAttributeIds.Add(attribute.Id);
            else
                disabledAttributeIds.Add(attribute.Id);
        }

        return (enabledAttributeIds, disabledAttributeIds);
    }

    private async Task<IList<CustomAttribute>> ParseContactAttributes(ContactAttributeChangeCommand request)
    {
        var customAttributes = new List<CustomAttribute>();
        var contactAttributes = await _contactAttributeService.GetAllContactAttributes(request.Store.Id);
        foreach (var attribute in contactAttributes)
            switch (attribute.AttributeControlType)
            {
                case AttributeControlType.DropdownList:
                case AttributeControlType.RadioList:
                case AttributeControlType.ColorSquares:
                case AttributeControlType.ImageSquares:
                {
                    var ctrlAttributes = request.Attributes.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                    if (!string.IsNullOrEmpty(ctrlAttributes))
                        customAttributes = _contactAttributeParser.AddContactAttribute(customAttributes,
                            attribute, ctrlAttributes).ToList();
                }
                    break;
                case AttributeControlType.Checkboxes:
                {
                    var cblAttributes = request.Attributes.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                    if (!string.IsNullOrEmpty(cblAttributes))
                        foreach (var item in cblAttributes.Split(','))
                            customAttributes = _contactAttributeParser
                                .AddContactAttribute(customAttributes, attribute, item).ToList();
                }
                    break;
                case AttributeControlType.ReadonlyCheckboxes:
                {
                    //load read-only (already server-side selected) values
                    var attributeValues = attribute.ContactAttributeValues;
                    foreach (var selectedAttributeId in attributeValues
                                 .Where(v => v.IsPreSelected)
                                 .Select(v => v.Id)
                                 .ToList())
                        customAttributes = _contactAttributeParser.AddContactAttribute(customAttributes,
                            attribute, selectedAttributeId).ToList();
                }
                    break;
                case AttributeControlType.TextBox:
                case AttributeControlType.MultilineTextbox:
                {
                    var ctrlAttributes = request.Attributes.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                    if (!string.IsNullOrEmpty(ctrlAttributes))
                    {
                        var enteredText = ctrlAttributes.Trim();
                        customAttributes = _contactAttributeParser.AddContactAttribute(customAttributes,
                            attribute, enteredText).ToList();
                    }
                }
                    break;
                case AttributeControlType.Datepicker:
                {
                    var date = request.Attributes.FirstOrDefault(x => x.Key == attribute.Id + "_day")?.Value;
                    var month = request.Attributes.FirstOrDefault(x => x.Key == attribute.Id + "_month")?.Value;
                    var year = request.Attributes.FirstOrDefault(x => x.Key == attribute.Id + "_year")?.Value;
                    DateTime? selectedDate = null;
                    try
                    {
                        selectedDate = new DateTime(int.Parse(year), int.Parse(month), int.Parse(date));
                    }
                    catch
                    {
                        // ignored
                    }

                    if (selectedDate.HasValue)
                        customAttributes = _contactAttributeParser.AddContactAttribute(customAttributes,
                            attribute, selectedDate.Value.ToString("D")).ToList();
                }
                    break;
                case AttributeControlType.FileUpload:
                {
                    var guid = request.Attributes.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                    Guid.TryParse(guid, out var downloadGuid);
                    var download = await _downloadService.GetDownloadByGuid(downloadGuid);
                    if (download != null)
                        customAttributes = _contactAttributeParser.AddContactAttribute(customAttributes,
                            attribute, download.DownloadGuid.ToString()).ToList();
                }
                    break;
            }

        //validate conditional attributes (if specified)
        foreach (var attribute in contactAttributes)
        {
            var conditionMet = await _contactAttributeParser.IsConditionMet(attribute, customAttributes);
            if (conditionMet.HasValue && !conditionMet.Value)
                customAttributes = _contactAttributeParser.RemoveContactAttribute(customAttributes, attribute)
                    .ToList();
        }

        return customAttributes;
    }
}