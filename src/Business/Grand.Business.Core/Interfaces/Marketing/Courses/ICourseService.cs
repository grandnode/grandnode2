﻿using Grand.Domain;
using Grand.Domain.Courses;
using Grand.Domain.Customers;

namespace Grand.Business.Core.Interfaces.Marketing.Courses
{
    public interface ICourseService
    {
        Task<Course> GetById(string id);
        Task<IPagedList<Course>> GetAll(int pageIndex = 0, int pageSize = int.MaxValue);
        Task<IList<Course>> GetByCustomer(Customer customer, string storeId);
        Task<Course> Update(Course course);
        Task<Course> Insert(Course course);
        Task Delete(Course course);
    }
}
