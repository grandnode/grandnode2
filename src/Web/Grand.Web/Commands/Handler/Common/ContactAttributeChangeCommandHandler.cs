using Grand.Business.Marketing.Interfaces.Contacts;
using Grand.Business.Storage.Interfaces;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Web.Commands.Models.Common;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Commands.Handler.Common
{
    public class ContactAttributeChangeCommandHandler : IRequestHandler<ContactAttributeChangeCommand, (IList<string> enabledAttributeIds, IList<string> disabledAttributeIds)>
    {
        private readonly IContactAttributeService _contactAttributeService;
        private readonly IContactAttributeParser _contactAttributeParser;
        private readonly IDownloadService _downloadService;

        public ContactAttributeChangeCommandHandler(IContactAttributeService contactAttributeService, IContactAttributeParser contactAttributeParser,
            IDownloadService downloadService)
        {
            _contactAttributeService = contactAttributeService;
            _contactAttributeParser = contactAttributeParser;
            _downloadService = downloadService;
        }

        public async Task<(IList<string> enabledAttributeIds, IList<string> disabledAttributeIds)> Handle(ContactAttributeChangeCommand request, CancellationToken cancellationToken)
        {
            var customAttributes = await ParseContactAttributes(request);

            var enabledAttributeIds = new List<string>();
            var disabledAttributeIds = new List<string>();
            var attributes = await _contactAttributeService.GetAllContactAttributes(request.Store.Id);
            foreach (var attribute in attributes)
            {
                var conditionMet = await _contactAttributeParser.IsConditionMet(attribute, customAttributes);
                if (conditionMet.HasValue)
                {
                    if (conditionMet.Value)
                        enabledAttributeIds.Add(attribute.Id);
                    else
                        disabledAttributeIds.Add(attribute.Id);
                }
            }
            return (enabledAttributeIds, disabledAttributeIds);
        }

        private async Task<IList<CustomAttribute>> ParseContactAttributes(ContactAttributeChangeCommand request)
        {
            var customAttributes = new List<CustomAttribute>();
            var contactAttributes = await _contactAttributeService.GetAllContactAttributes(request.Store.Id);
            foreach (var attribute in contactAttributes)
            {
                string controlId = string.Format("contact_attribute_{0}", attribute.Id);
                switch (attribute.AttributeControlType)
                {
                    case AttributeControlType.DropdownList:
                    case AttributeControlType.RadioList:
                    case AttributeControlType.ColorSquares:
                    case AttributeControlType.ImageSquares:
                        {
                            request.Form.TryGetValue(controlId, out var ctrlAttributes);
                            if (!string.IsNullOrEmpty(ctrlAttributes))
                            {
                                customAttributes = _contactAttributeParser.AddContactAttribute(customAttributes,
                                        attribute, ctrlAttributes).ToList();

                            }
                        }
                        break;
                    case AttributeControlType.Checkboxes:
                        {
                            request.Form.TryGetValue(controlId, out var cblAttributes);
                            if (!string.IsNullOrEmpty(cblAttributes))
                            {
                                foreach (var item in cblAttributes)
                                {
                                    customAttributes = _contactAttributeParser.AddContactAttribute(customAttributes, attribute, item).ToList();
                                }
                            }
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
                            {
                                customAttributes = _contactAttributeParser.AddContactAttribute(customAttributes,
                                            attribute, selectedAttributeId.ToString()).ToList();
                            }
                        }
                        break;
                    case AttributeControlType.TextBox:
                    case AttributeControlType.MultilineTextbox:
                        {
                            request.Form.TryGetValue(controlId, out var ctrlAttributes);
                            if (!string.IsNullOrEmpty(ctrlAttributes))
                            {
                                string enteredText = ctrlAttributes.ToString().Trim();
                                customAttributes = _contactAttributeParser.AddContactAttribute(customAttributes,
                                    attribute, enteredText).ToList();
                            }
                        }
                        break;
                    case AttributeControlType.Datepicker:
                        {
                            request.Form.TryGetValue(controlId + "_day", out var date);
                            request.Form.TryGetValue(controlId + "_month", out var month);
                            request.Form.TryGetValue(controlId + "_year", out var year);
                            DateTime? selectedDate = null;
                            try
                            {
                                selectedDate = new DateTime(Int32.Parse(year), Int32.Parse(month), Int32.Parse(date));
                            }
                            catch { }
                            if (selectedDate.HasValue)
                            {
                                customAttributes = _contactAttributeParser.AddContactAttribute(customAttributes,
                                    attribute, selectedDate.Value.ToString("D")).ToList();
                            }
                        }
                        break;
                    case AttributeControlType.FileUpload:
                        {
                            Guid downloadGuid;
                            request.Form.TryGetValue(controlId, out var guid);
                            Guid.TryParse(guid, out downloadGuid);
                            var download = await _downloadService.GetDownloadByGuid(downloadGuid);
                            if (download != null)
                            {
                                customAttributes = _contactAttributeParser.AddContactAttribute(customAttributes,
                                           attribute, download.DownloadGuid.ToString()).ToList();
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            //validate conditional attributes (if specified)
            foreach (var attribute in contactAttributes)
            {
                var conditionMet = await _contactAttributeParser.IsConditionMet(attribute, customAttributes);
                if (conditionMet.HasValue && !conditionMet.Value)
                    customAttributes = _contactAttributeParser.RemoveContactAttribute(customAttributes, attribute).ToList();
            }

            return customAttributes;
        }
    }
}
