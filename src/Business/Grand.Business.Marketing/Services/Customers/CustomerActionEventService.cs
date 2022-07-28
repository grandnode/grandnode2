﻿using Grand.Business.Core.Commands.Marketing;
using Grand.Business.Core.Interfaces.Marketing.Customers;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Domain.Orders;
using MediatR;

namespace Grand.Business.Marketing.Services.Customers
{
    public partial class CustomerActionEventService : ICustomerActionEventService
    {
        #region Fields

        private readonly IRepository<CustomerAction> _customerActionRepository;
        private readonly IRepository<CustomerActionHistory> _customerActionHistoryRepository;
        private readonly IRepository<CustomerActionType> _customerActionTypeRepository;
        private readonly ICacheBase _cacheBase;
        private readonly IMediator _mediator;

        #endregion

        #region Ctor

        public CustomerActionEventService(
            IRepository<CustomerAction> customerActionRepository,
            IRepository<CustomerActionType> customerActionTypeRepository,
            IRepository<CustomerActionHistory> customerActionHistoryRepository,
            ICacheBase cacheBase,
            IMediator mediator)
        {
            _customerActionRepository = customerActionRepository;
            _customerActionTypeRepository = customerActionTypeRepository;
            _customerActionHistoryRepository = customerActionHistoryRepository;
            _cacheBase = cacheBase;
            _mediator = mediator;
        }

        #endregion

        #region Utilities

        protected async Task<IList<CustomerActionType>> GetAllCustomerActionType()
        {
            return await _cacheBase.GetAsync(CacheKey.CUSTOMER_ACTION_TYPE, async () =>
            {
                return await Task.FromResult(_customerActionTypeRepository.Table.ToList());
            });
        }

        protected bool UsedAction(string actionId, string customerId)
        {
            var query = from u in _customerActionHistoryRepository.Table
                        where u.CustomerId == customerId && u.CustomerActionId == actionId
                        select u.Id;
            if (query.Count() > 0)
                return true;

            return false;
        }

        #endregion

        #region Methods

        public virtual async Task AddToCart(ShoppingCartItem cart, Product product, Customer customer)
        {
            var actiontypes = await GetAllCustomerActionType();
            var actionType = actiontypes.Where(x => x.SystemKeyword == CustomerActionTypeEnum.AddToCart.ToString()).FirstOrDefault();
            if (actionType?.Enabled == true)
            {
                var datetimeUtcNow = DateTime.UtcNow;
                var query = from a in _customerActionRepository.Table
                            where a.Active == true && a.ActionTypeId == actionType.Id
                                    && datetimeUtcNow >= a.StartDateTimeUtc && datetimeUtcNow <= a.EndDateTimeUtc
                            select a;

                foreach (var item in query.ToList())
                {
                    if (!UsedAction(item.Id, customer.Id))
                    {
                        if (await _mediator.Send(new CustomerActionEventConditionCommand()
                        {
                            CustomerActionTypes = actiontypes,
                            Action = item,
                            ProductId = product.Id,
                            Attributes = cart.Attributes,
                            CustomerId = customer.Id
                        }))
                        {
                            await _mediator.Send(new CustomerActionEventReactionCommand()
                            {
                                CustomerActionTypes = actiontypes,
                                Action = item,
                                CartItem = cart,
                                CustomerId = customer.Id
                            });
                        }
                    }
                }
            }
        }

        public virtual async Task AddOrder(Order order, CustomerActionTypeEnum customerActionType)
        {
            var actiontypes = await GetAllCustomerActionType();
            var actionType = actiontypes.Where(x => x.SystemKeyword == customerActionType.ToString()).FirstOrDefault();
            if (actionType?.Enabled == true)
            {
                var datetimeUtcNow = DateTime.UtcNow;
                var query = from a in _customerActionRepository.Table
                            where a.Active == true && a.ActionTypeId == actionType.Id
                                    && datetimeUtcNow >= a.StartDateTimeUtc && datetimeUtcNow <= a.EndDateTimeUtc
                            select a;

                foreach (var item in query.ToList())
                {
                    if (!UsedAction(item.Id, order.CustomerId))
                    {
                        foreach (var orderItem in order.OrderItems)
                        {
                            if (await _mediator.Send(new CustomerActionEventConditionCommand()
                            {
                                CustomerActionTypes = actiontypes,
                                Action = item,
                                ProductId = orderItem.ProductId,
                                CustomerId = order.CustomerId,
                                Attributes = orderItem.Attributes
                            }))
                            {
                                await _mediator.Send(new CustomerActionEventReactionCommand()
                                {
                                    CustomerActionTypes = actiontypes,
                                    Action = item,
                                    Order = order,
                                    CustomerId = order.CustomerId,
                                });
                                break;
                            }
                        }
                    }
                }

            }

        }

        public virtual async Task Url(Customer customer, string currentUrl, string previousUrl)
        {
            if (!customer.IsSystemAccount)
            {
                var actiontypes = await GetAllCustomerActionType();
                var actionType = actiontypes.FirstOrDefault(x => x.SystemKeyword == CustomerActionTypeEnum.Url.ToString());
                if (actionType?.Enabled == true)
                {
                    var datetimeUtcNow = DateTime.UtcNow;
                    var query = from a in _customerActionRepository.Table
                                where a.Active == true && a.ActionTypeId == actionType.Id
                                        && datetimeUtcNow >= a.StartDateTimeUtc && datetimeUtcNow <= a.EndDateTimeUtc
                                select a;

                    foreach (var item in query.ToList())
                    {
                        if (!UsedAction(item.Id, customer.Id))
                        {
                            if (await _mediator.Send(new CustomerActionEventConditionCommand()
                            {
                                CustomerActionTypes = actiontypes,
                                Action = item,
                                CustomerId = customer.Id,
                                CurrentUrl = currentUrl,
                                PreviousUrl = previousUrl
                            }))
                            {
                                await _mediator.Send(new CustomerActionEventReactionCommand()
                                {
                                    CustomerActionTypes = actiontypes,
                                    Action = item,
                                    CustomerId = customer.Id
                                });
                            }
                        }
                    }
                }
            }
        }

        public virtual async Task Viewed(Customer customer, string currentUrl, string previousUrl)
        {
            if (!customer.IsSystemAccount)
            {
                var actiontypes = await GetAllCustomerActionType();
                var actionType = actiontypes.Where(x => x.SystemKeyword == CustomerActionTypeEnum.Viewed.ToString()).FirstOrDefault();
                if (actionType?.Enabled == true)
                {
                    var datetimeUtcNow = DateTime.UtcNow;
                    var query = from a in _customerActionRepository.Table
                                where a.Active == true && a.ActionTypeId == actionType.Id
                                        && datetimeUtcNow >= a.StartDateTimeUtc && datetimeUtcNow <= a.EndDateTimeUtc
                                select a;

                    foreach (var item in query.ToList())
                    {
                        if (!UsedAction(item.Id, customer.Id))
                        {
                            if (await _mediator.Send(new CustomerActionEventConditionCommand()
                            {
                                CustomerActionTypes = actiontypes,
                                Action = item,
                                CustomerId = customer.Id,
                                CurrentUrl = currentUrl,
                                PreviousUrl = previousUrl
                            }))
                            {
                                await _mediator.Send(new CustomerActionEventReactionCommand()
                                {
                                    CustomerActionTypes = actiontypes,
                                    Action = item,
                                    CustomerId = customer.Id
                                });
                            }
                        }
                    }

                }
            }

        }

        public virtual async Task Registration(Customer customer)
        {
            var actiontypes = await GetAllCustomerActionType();
            var actionType = actiontypes.Where(x => x.SystemKeyword == CustomerActionTypeEnum.Registration.ToString()).FirstOrDefault();
            if (actionType?.Enabled == true)
            {
                var datetimeUtcNow = DateTime.UtcNow;
                var query = from a in _customerActionRepository.Table
                            where a.Active == true && a.ActionTypeId == actionType.Id
                                    && datetimeUtcNow >= a.StartDateTimeUtc && datetimeUtcNow <= a.EndDateTimeUtc
                            select a;

                foreach (var item in query.ToList())
                {
                    if (!UsedAction(item.Id, customer.Id))
                    {
                        if (await _mediator.Send(new CustomerActionEventConditionCommand()
                        {
                            CustomerActionTypes = actiontypes,
                            Action = item,
                            CustomerId = customer.Id
                        }))
                        {
                            await _mediator.Send(new CustomerActionEventReactionCommand()
                            {
                                CustomerActionTypes = actiontypes,
                                Action = item,
                                CustomerId = customer.Id
                            });
                        }
                    }
                }

            }
        }

        #endregion
    }
}
