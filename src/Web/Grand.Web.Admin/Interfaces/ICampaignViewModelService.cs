﻿using Grand.Domain.Messages;
using Grand.Web.Admin.Models.Messages;

namespace Grand.Web.Admin.Interfaces
{
    public interface ICampaignViewModelService
    {
        Task<CampaignModel> PrepareCampaignModel();
        Task<CampaignModel> PrepareCampaignModel(CampaignModel model);
        Task<CampaignModel> PrepareCampaignModel(Campaign campaign);
        Task<(IEnumerable<CampaignModel> campaignModels, int totalCount)> PrepareCampaignModels();
        Task<Campaign> InsertCampaignModel(CampaignModel model);
        Task<Campaign> UpdateCampaignModel(Campaign campaign, CampaignModel model);
    }
}
