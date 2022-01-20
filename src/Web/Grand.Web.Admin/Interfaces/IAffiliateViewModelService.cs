﻿using Grand.Domain.Affiliates;
using Grand.Web.Admin.Models.Affiliates;

namespace Grand.Web.Admin.Interfaces
{
    public interface IAffiliateViewModelService
    {
        Task PrepareAffiliateModel(AffiliateModel model, Affiliate affiliate, bool excludeProperties,
            bool prepareEntireAddressModel = true);
        Task<(IEnumerable<AffiliateModel> affiliateModels, int totalCount)> PrepareAffiliateModelList(AffiliateListModel model, int pageIndex, int pageSize);
        Task<Affiliate> InsertAffiliateModel(AffiliateModel model);
        Task<Affiliate> UpdateAffiliateModel(AffiliateModel model, Affiliate affiliate);
        Task<(IEnumerable<AffiliateModel.AffiliatedOrderModel> affiliateOrderModels, int totalCount)> PrepareAffiliatedOrderList(Affiliate affiliate, AffiliatedOrderListModel model, int pageIndex, int pageSize);
        Task<(IEnumerable<AffiliateModel.AffiliatedCustomerModel> affiliateCustomerModels, int totalCount)> PrepareAffiliatedCustomerList(Affiliate affiliate, int pageIndex, int pageSize);
    }
}
