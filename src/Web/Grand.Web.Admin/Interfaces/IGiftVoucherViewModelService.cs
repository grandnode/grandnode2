using Grand.Domain.Orders;
using Grand.Web.Admin.Models.Orders;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Interfaces
{
    public interface IGiftVoucherViewModelService
    {
        GiftVoucherListModel PrepareGiftVoucherListModel();
        Task<GiftVoucherModel> PrepareGiftVoucherModel();
        Task<GiftVoucherModel> PrepareGiftVoucherModel(GiftVoucherModel model);
        Task<(IEnumerable<GiftVoucherModel> giftVoucherModels, int totalCount)> PrepareGiftVoucherModel(GiftVoucherListModel model, int pageIndex, int pageSize);
        Task<Order> FillGiftVoucherModel(GiftVoucher giftVoucher, GiftVoucherModel model);
        Task NotifyRecipient(GiftVoucher giftVoucher, GiftVoucherModel model);
        Task<GiftVoucher> InsertGiftVoucherModel(GiftVoucherModel model);
        Task<GiftVoucher> UpdateGiftVoucherModel(GiftVoucher giftVoucher, GiftVoucherModel model);
        Task DeleteGiftVoucher(GiftVoucher giftVoucher);
        Task<GiftVoucherModel> PrepareGiftVoucherModel(GiftVoucher giftVoucher);
        Task<(IEnumerable<GiftVoucherModel.GiftVoucherUsageHistoryModel> giftVoucherUsageHistoryModels, int totalCount)> PrepareGiftVoucherUsageHistoryModels(GiftVoucher giftVoucher, int pageIndex, int pageSize);
    }
}
