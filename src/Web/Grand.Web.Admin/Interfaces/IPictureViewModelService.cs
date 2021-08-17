using Grand.Web.Admin.Models.Common;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Interfaces
{
    public interface IPictureViewModelService
    {
        Task<PictureModel> PreparePictureModel(string pictureId, string objectId);
        Task UpdatePicture(PictureModel model);
    }
}
