using Grand.Business.Marketing.Commands.Models;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Marketing.Commands.Handlers
{
    /// <summary>
    /// Update Customer Reminder History
    /// </summary>
    public class UpdateCustomerReminderHistoryCommandHandler : IRequestHandler<UpdateCustomerReminderHistoryCommand, bool>
    {
        private readonly IRepository<CustomerReminderHistory> _customerReminderHistory;

        public UpdateCustomerReminderHistoryCommandHandler(IRepository<CustomerReminderHistory> customerReminderHistory)
        {
            _customerReminderHistory = customerReminderHistory;
        }

        public async Task<bool> Handle(UpdateCustomerReminderHistoryCommand request, CancellationToken cancellationToken)
        {
            var update = UpdateBuilder<CustomerReminderHistory>.Create()
                .Set(x => x.EndDate, DateTime.UtcNow)
                .Set(x => x.Status, CustomerReminderHistoryStatusEnum.CompletedOrdered)
                .Set(x => x.OrderId, request.OrderId);

            await _customerReminderHistory.UpdateManyAsync(x => x.CustomerId == request.CustomerId && x.Status == CustomerReminderHistoryStatusEnum.Started, update);

            update = UpdateBuilder<CustomerReminderHistory>.Create()
                .Set(x => x.Status, CustomerReminderHistoryStatusEnum.CompletedOrdered)
                .Set(x => x.OrderId, request.OrderId);

            await _customerReminderHistory.UpdateManyAsync(x => x.CustomerId == request.CustomerId 
                    && x.Status == CustomerReminderHistoryStatusEnum.CompletedOrdered && x.EndDate == DateTime.UtcNow.AddHours(-36), update);

            return true;
        }
    }
}
