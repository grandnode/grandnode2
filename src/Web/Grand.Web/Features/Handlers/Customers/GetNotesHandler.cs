using Grand.Business.Customers.Interfaces;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Web.Features.Models.Customers;
using Grand.Web.Models.Customer;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Customers
{
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
            var model = new CustomerNotesModel();
            model.CustomerId = request.Customer.Id;
            var notes = await _customerNoteService.GetCustomerNotes(request.Customer.Id, true);
            foreach (var item in notes)
            {
                var mm = new CustomerNote();
                mm.NoteId = item.Id;
                mm.CreatedOn = _dateTimeService.ConvertToUserTime(item.CreatedOnUtc, DateTimeKind.Utc);
                mm.Note = item.Note;
                mm.Title = item.Title;
                mm.DownloadId = item.DownloadId;
                model.CustomerNoteList.Add(mm);
            }
            return model;
        }
    }
}
