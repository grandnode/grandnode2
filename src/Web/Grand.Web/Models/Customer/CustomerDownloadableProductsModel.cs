﻿using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Customer
{
    public class CustomerDownloadableProductsModel : BaseModel
    {
        public CustomerDownloadableProductsModel()
        {
            Items = new List<DownloadableProductsModel>();
        }

        public IList<DownloadableProductsModel> Items { get; set; }

        #region Nested classes
        public class DownloadableProductsModel : BaseModel
        {
            public Guid OrderItemGuid { get; set; }

            public string OrderId { get; set; }
            public int OrderNumber { get; set; }

            public string ProductId { get; set; }
            public string ProductName { get; set; }
            public string ProductSeName { get; set; }
            public string ProductAttributes { get; set; }

            public string DownloadId { get; set; }
            public string LicenseId { get; set; }

            public DateTime CreatedOn { get; set; }
        }
        #endregion
    }

    public class UserAgreementModel : BaseModel
    {
        public Guid OrderItemGuid { get; set; }
        public string UserAgreementText { get; set; }
    }
}