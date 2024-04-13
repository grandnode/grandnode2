using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Web.Features.Models.Customers;
using Grand.Web.Models.Customer;
using MediatR;

namespace Grand.Web.Features.Handlers.Customers;

public class GetNotesHandler : IRequestHandler<GetNotes, CustomerNotesModel>
{
    private readonly ICustomerNoteService _customerNoteService;
    private readonly IDateTimeService _dateTimeService;

    public GetNotesHandler(ICustomerNoteService customerNoteService,
        IDateTimeService dateTimeService)
    {
        _customerNoteService = customerNoteService;
        _dateTimeService = dateTimeService;
    }

    public async Task<CustomerNotesModel> Handle(GetNotes request, CancellationToken cancellationToken)
    {
        var model = new CustomerNotesModel {
            CustomerId = request.Customer.Id
        };
        var notes = await _customerNoteService.GetCustomerNotes(request.Customer.Id, true);
        foreach (var item in notes)
        {
            var mm = new CustomerNote {
                NoteId = item.Id,
                CreatedOn = _dateTimeService.ConvertToUserTime(item.CreatedOnUtc, DateTimeKind.Utc),
                Note = item.Note,
                Title = item.Title,
                DownloadId = item.DownloadId
            };
            model.CustomerNoteList.Add(mm);
        }

        return model;
    }
}