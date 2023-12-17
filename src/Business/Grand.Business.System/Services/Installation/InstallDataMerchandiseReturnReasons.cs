﻿using Grand.Domain.Orders;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService
    {
        protected virtual Task InstallMerchandiseReturnReasons()
        {
            var merchandiseReturnReasons = new List<MerchandiseReturnReason>
                                {
                                    new MerchandiseReturnReason
                                        {
                                            Name = "Received Wrong Product",
                                            DisplayOrder = 1
                                        },
                                    new MerchandiseReturnReason
                                        {
                                            Name = "Wrong Product Ordered",
                                            DisplayOrder = 2
                                        },
                                    new MerchandiseReturnReason
                                        {
                                            Name = "There Was A Problem With The Product",
                                            DisplayOrder = 3
                                        }
                                };

            merchandiseReturnReasons.ForEach(x=>_merchandiseReturnReasonRepository.Insert(x));
            return Task.CompletedTask;
        }
    }
}
