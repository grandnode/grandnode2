using Grand.Web.Admin.Models.Common;

namespace Grand.Web.Admin.Interfaces
{
    public interface IPictureViewModelService
    {
        Task<PictureModel> PreparePictureModel(string pictureId, string objectId);
        Task UpdatePicture(PictureModel model);
    }
}
