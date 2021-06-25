#! "net5.0"
#r "Grand.Infrastructure"
#r "Grand.Business.Messages"
#r "Grand.Web"

using Grand.Business.Messages.Events;
using Grand.Domain.Orders;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using System;

/* Sample code to use INotificationHandler - mediatr pattern - add new token to the message template */

public class OrderTokenTest : INotificationHandler<EntityTokensAddedEvent<Order>>
{
    public Task Handle(EntityTokensAddedEvent<Order> eventMessage, CancellationToken cancellationToken)
    {
        //in message templates you can put new token {{AdditionalTokens["NewOrderNumber"]}}

        eventMessage.LiquidObject.AdditionalTokens.Add("NewOrderNumber", $"{eventMessage.Entity.CreatedOnUtc.Year}/{eventMessage.Entity.OrderNumber}");

        return Task.CompletedTask;
    }

}
