using Grand.Business.Marketing.Interfaces.Courses;
using Grand.Domain;
using Grand.Domain.Courses;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Domain.Orders;
using Grand.Infrastructure.Extensions;
using Grand.SharedKernel.Extensions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Marketing.Services.Courses
{
    public class CourseService : ICourseService
    {
        private readonly IRepository<Course> _courseRepository;
        private readonly IRepository<Order> _orderRepository;
        private readonly IMediator _mediator;

        public CourseService(IRepository<Course> courseRepository,
            IRepository<Order> orderRepository,
            IMediator mediator)
        {
            _courseRepository = courseRepository;
            _orderRepository = orderRepository;
            _mediator = mediator;
        }

        public virtual async Task Delete(Course course)
        {
            if (course == null)
                throw new ArgumentNullException(nameof(course));

            await _courseRepository.DeleteAsync(course);

            //event notification
            await _mediator.EntityDeleted(course);
        }

        public virtual async Task<IPagedList<Course>> GetAll(int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = from q in _courseRepository.Table
                        orderby q.DisplayOrder
                        select q;

            return await PagedList<Course>.Create(query, pageIndex, pageSize);
        }
        public virtual async Task<IList<Course>> GetByCustomer(Customer customer, string storeId)
        {
            var query = from c in _courseRepository.Table
                        select c;

            query = query.Where(c => c.Published);

            if ((!CommonHelper.IgnoreAcl || (!string.IsNullOrEmpty(storeId) && !CommonHelper.IgnoreStoreLimitations)))
            {
                if (!CommonHelper.IgnoreAcl)
                {
                    //ACL (access control list)
                    var allowedCustomerGroupsIds = customer.GetCustomerGroupIds();
                    query = from p in query
                            where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                            select p;
                }
                if (!string.IsNullOrEmpty(storeId) && !CommonHelper.IgnoreStoreLimitations)
                {
                    //Store acl
                    query = from p in query
                            where !p.LimitedToStores || p.Stores.Contains(storeId)
                            select p;
                }
            }

            //courses without assigned product
            var q1 = query.Where(x => string.IsNullOrEmpty(x.ProductId)).ToList();

            //get products from orders - paid/not deleted/for customer/store
            var pl = _orderRepository.Table.Where(x => x.CustomerId == customer.Id && !x.Deleted
                            && x.PaymentStatusId == Domain.Payments.PaymentStatus.Paid
                            && x.StoreId == storeId).SelectMany(x => x.OrderItems, (p, pr) => pr.ProductId).Distinct().ToList();

            //courses assigned to products
            var q2 = query.Where(x => pl.Contains(x.ProductId)).ToList();

            return await Task.FromResult(q1.Concat(q2).ToList());
        }

        public virtual Task<Course> GetById(string id)
        {
            return _courseRepository.GetByIdAsync(id);
        }

        public virtual async Task<Course> Insert(Course course)
        {
            if (course == null)
                throw new ArgumentNullException(nameof(course));

            await _courseRepository.InsertAsync(course);

            //event notification
            await _mediator.EntityInserted(course);

            return course;
        }

        public virtual async Task<Course> Update(Course course)
        {
            if (course == null)
                throw new ArgumentNullException(nameof(course));

            await _courseRepository.UpdateAsync(course);

            //event notification
            await _mediator.EntityUpdated(course);

            return course;
        }
    }
}
