using Grand.Business.System.Interfaces.Installation;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService : IInstallationService
    {
        protected virtual async Task InstallCustomersAndUsers(string defaultUserEmail, string defaultUserPassword)
        {
            var crAdministrators = new CustomerGroup
            {
                Name = "Administrators",
                Active = true,
                IsSystem = true,
                SystemName = SystemCustomerGroupNames.Administrators,
            };
            await _customerGroupRepository.InsertAsync(crAdministrators);

            var crRegistered = new CustomerGroup
            {
                Name = "Registered",
                Active = true,
                IsSystem = true,
                SystemName = SystemCustomerGroupNames.Registered,
            };
            await _customerGroupRepository.InsertAsync(crRegistered);

            var crGuests = new CustomerGroup
            {
                Name = "Guests",
                Active = true,
                IsSystem = true,
                SystemName = SystemCustomerGroupNames.Guests,
            };
            await _customerGroupRepository.InsertAsync(crGuests);

            var crVendors = new CustomerGroup
            {
                Name = "Vendors",
                Active = true,
                IsSystem = true,
                SystemName = SystemCustomerGroupNames.Vendors,
            };
            await _customerGroupRepository.InsertAsync(crVendors);

            var crStaff = new CustomerGroup
            {
                Name = "Staff",
                Active = true,
                IsSystem = true,
                SystemName = SystemCustomerGroupNames.Staff,
            };
            await _customerGroupRepository.InsertAsync(crStaff);

            var crSalesManager = new CustomerGroup
            {
                Name = "Sales manager",
                Active = true,
                IsSystem = true,
                SystemName = SystemCustomerGroupNames.SalesManager,
            };
            await _customerGroupRepository.InsertAsync(crSalesManager);

            //admin user
            var adminUser = new Customer
            {
                CustomerGuid = Guid.NewGuid(),
                Email = defaultUserEmail,
                Username = defaultUserEmail,
                Password = defaultUserPassword,
                PasswordFormatId = PasswordFormat.Clear,
                PasswordSalt = "",
                Active = true,
                CreatedOnUtc = DateTime.UtcNow,
                LastActivityDateUtc = DateTime.UtcNow,
                PasswordChangeDateUtc = DateTime.UtcNow,
            };
            var country = _countryRepository.Table.FirstOrDefault(c => c.ThreeLetterIsoCode == "USA");
            var defaultAdminUserAddress = new Address
            {
                FirstName = "John",
                LastName = "Smith",
                PhoneNumber = "12345678",
                Email = "admin@yourstore.com",
                FaxNumber = "",
                Company = "GrandNode LTD",
                Address1 = "21 West 52nd Street",
                Address2 = "",
                City = "New York",
                StateProvinceId = country?.StateProvinces.FirstOrDefault(sp => sp.Name == "New York")?.Id,
                CountryId = country?.Id,
                ZipPostalCode = "10021",
                CreatedOnUtc = DateTime.UtcNow,
            };
            adminUser.Addresses.Add(defaultAdminUserAddress);
            adminUser.BillingAddress = defaultAdminUserAddress;
            adminUser.ShippingAddress = defaultAdminUserAddress;
            adminUser.Groups.Add(crAdministrators.Id);
            adminUser.Groups.Add(crRegistered.Id);
            adminUser.UserFields.Add(new UserField() { Key = SystemCustomerFieldNames.FirstName, Value = "John" });
            adminUser.UserFields.Add(new UserField() { Key = SystemCustomerFieldNames.LastName, Value = "Smith" });
            await _customerRepository.InsertAsync(adminUser);

            //Anonymous user
            var anonymousUser = new Customer
            {
                Email = "builtin@anonymous.com",
                CustomerGuid = Guid.NewGuid(),
                PasswordFormatId = PasswordFormat.Clear,
                AdminComment = "Built-in system guest record used for anonymous requests.",
                Active = true,
                IsSystemAccount = true,
                SystemName = SystemCustomerNames.Anonymous,
                CreatedOnUtc = DateTime.UtcNow,
                LastActivityDateUtc = DateTime.UtcNow,
            };
            anonymousUser.Groups.Add(crGuests.Id);
            await _customerRepository.InsertAsync(anonymousUser);

            //search engine (crawler) built-in user
            var searchEngineUser = new Customer
            {
                Email = "builtin@search_engine_record.com",
                CustomerGuid = Guid.NewGuid(),
                PasswordFormatId = PasswordFormat.Clear,
                AdminComment = "Built-in system guest record used for requests from search engines.",
                Active = true,
                IsSystemAccount = true,
                SystemName = SystemCustomerNames.SearchEngine,
                CreatedOnUtc = DateTime.UtcNow,
                LastActivityDateUtc = DateTime.UtcNow,
            };
            searchEngineUser.Groups.Add(crGuests.Id);
            await _customerRepository.InsertAsync(searchEngineUser);


            //built-in user for background tasks
            var backgroundTaskUser = new Customer
            {
                Email = "builtin@background-task-record.com",
                CustomerGuid = Guid.NewGuid(),
                PasswordFormatId = PasswordFormat.Clear,
                AdminComment = "Built-in system record used for background tasks.",
                Active = true,
                IsSystemAccount = true,
                SystemName = SystemCustomerNames.BackgroundTask,
                CreatedOnUtc = DateTime.UtcNow,
                LastActivityDateUtc = DateTime.UtcNow,
            };
            backgroundTaskUser.Groups.Add(crGuests.Id);
            await _customerRepository.InsertAsync(backgroundTaskUser);

        }
    }
}
