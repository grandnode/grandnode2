using Grand.Domain.Messages;
using Grand.Web.AdminShared.Models.Messages;

namespace Grand.Web.AdminShared.Interfaces;

public interface ICampaignViewModelService
{
    Task<CampaignModel> PrepareCampaignModel();
    Task<CampaignModel> PrepareCampaignModel(CampaignModel model);
    Task<CampaignModel> PrepareCampaignModel(Campaign campaign);
    Task<(IEnumerable<CampaignModel> campaignModels, int totalCount)> PrepareCampaignModels();
    Task<Campaign> InsertCampaignModel(CampaignModel model);
    Task<Campaign> UpdateCampaignModel(Campaign campaign, CampaignModel model);
}