using Grand.Domain.Media;
using Grand.Domain.Orders;
using Grand.SharedKernel.Extensions;
using Grand.Web.Extensions;
using Grand.Web.Models.Orders;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Tests.Extensions;

[TestClass]
public class MerchandiseReturnUploadsTests
{
    private static readonly IList<string> Images = FileExtensions.GetAllowedMediaFileTypes(null);

    private readonly List<MerchandiseReturnReason> _reasons = [
        new() { Id = "photo", PhotoRequired = true },
        new() { Id = "plain" }
    ];

    private static IFormFile File(string name, long length = 10, string contentType = "image/png")
    {
        return new FormFile(new MemoryStream(new byte[length]), 0, length, "photos", name) {
            Headers = new HeaderDictionary(), ContentType = contentType
        };
    }

    private static List<MerchandiseReturnModel.OrderItemModel> Items(params (string Id, int Quantity, string Reason)[] items)
    {
        return items.Select(x => new MerchandiseReturnModel.OrderItemModel
            { Id = x.Id, Quantity = x.Quantity, MerchandiseReturnReasonId = x.Reason }).ToList();
    }

    private string Error(List<MerchandiseReturnModel.OrderItemModel> items,
        Dictionary<string, IList<IFormFile>> photos)
    {
        return MerchandiseReturnUploads.PhotosError(items, photos, _reasons, Images);
    }

    [TestMethod]
    public void PhotosError_RequiredReasonWithoutPhoto_Required()
    {
        var error = Error(Items(("a", 1, "photo")), []);

        Assert.AreEqual("MerchandiseReturns.Photos.Required", error);
    }

    [TestMethod]
    public void PhotosError_RequiredReasonWithPhoto_Null()
    {
        var error = Error(Items(("a", 1, "photo"), ("b", 1, "plain")), new() { ["a"] = [File("a.jpg")] });

        Assert.IsNull(error);
    }

    [TestMethod]
    public void PhotosError_PhotoOfItemNotReturned_Invalid()
    {
        //a quantity of 0 returns nothing, so a photo of it belongs to no return
        var error = Error(Items(("a", 0, "plain")), new() { ["a"] = [File("a.jpg")] });

        Assert.AreEqual("MerchandiseReturns.Photos.Invalid", error);
    }

    [TestMethod]
    public void PhotosError_PdfExtension_Invalid()
    {
        var error = Error(Items(("a", 1, "photo")), new() { ["a"] = [File("a.pdf", contentType: "image/png")] });

        Assert.AreEqual("MerchandiseReturns.Photos.Invalid", error);
    }

    [TestMethod]
    public void PhotosError_TooLarge_Invalid()
    {
        var error = Error(Items(("a", 1, "plain")),
            new() { ["a"] = [File("a.png", MerchandiseReturnUploads.MaxFileBytes + 1)] });

        Assert.AreEqual("MerchandiseReturns.Photos.Invalid", error);
    }

    [TestMethod]
    public void PhotosError_FourForOneItem_Invalid()
    {
        var error = Error(Items(("a", 1, "plain")),
            new() { ["a"] = [File("1.png"), File("2.png"), File("3.png"), File("4.png")] });

        Assert.AreEqual("MerchandiseReturns.Photos.Invalid", error);
    }

    [TestMethod]
    public void PhotosError_ElevenInAll_Invalid()
    {
        var items = Items(("a", 1, "plain"), ("b", 1, "plain"), ("c", 1, "plain"), ("d", 1, "plain"));
        var photos = new Dictionary<string, IList<IFormFile>> {
            ["a"] = [File("1.png"), File("2.png"), File("3.png")],
            ["b"] = [File("4.png"), File("5.png"), File("6.png")],
            ["c"] = [File("7.png"), File("8.png"), File("9.png")],
            ["d"] = [File("10.png"), File("11.png")]
        };

        Assert.AreEqual("MerchandiseReturns.Photos.Invalid", Error(items, photos));
    }

    [TestMethod]
    public void PhotosError_EmptyFile_Invalid()
    {
        var error = Error(Items(("a", 1, "plain")), new() { ["a"] = [File("a.png", 0)] });

        Assert.AreEqual("MerchandiseReturns.Photos.Invalid", error);
    }

    [TestMethod]
    public void ToDownload_ContentTypeFromExtension()
    {
        //what the browser says the file is counts for nothing
        var download = MerchandiseReturnUploads.ToDownload(File("photo.png", contentType: "text/html"), "c1", "r1");

        Assert.AreEqual("image/png", download.ContentType);
        Assert.AreEqual(".png", download.Extension);
        Assert.AreEqual("photo", download.Filename);
        Assert.AreEqual("r1", download.ReferenceId);
        Assert.AreEqual("c1", download.CustomerId);
        Assert.AreEqual(DownloadType.MerchandiseReturn, download.DownloadType);
        Assert.AreEqual(10, download.DownloadBinary.Length);
    }
}
