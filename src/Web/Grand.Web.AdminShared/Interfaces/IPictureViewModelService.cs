using Grand.Web.AdminShared.Models.Common;

namespace Grand.Web.AdminShared.Interfaces;

public interface IPictureViewModelService
{
    Task<PictureModel> PreparePictureModel(string pictureId, string objectId);
    Task UpdatePicture(PictureModel model);
}