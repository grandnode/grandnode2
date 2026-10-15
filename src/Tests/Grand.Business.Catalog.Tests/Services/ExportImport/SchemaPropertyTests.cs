using Grand.Business.Catalog.Services.ExportImport;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Utilities.ExportImport;
using Grand.Domain.Catalog;
using Grand.Domain.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Services.ExportImport;

[TestClass]
public class SchemaPropertyTests
{
    private Mock<IPictureService> _pictureService;

    [TestInitialize]
    public void Init()
    {
        _pictureService = new Mock<IPictureService>();
        _pictureService.Setup(x => x.GetPictureById(It.IsAny<string>()))
            .ReturnsAsync((string id) => new Picture { Id = id });
        _pictureService.Setup(x => x.GetThumbPhysicalPath(It.IsAny<Picture>(), It.IsAny<int>(), It.IsAny<bool>()))
            .ReturnsAsync((Picture p, int _, bool _) => $"/thumbs/{p.Id}.jpg");
    }

    private static object Value<T>(PropertyByName<T>[] properties, string name, T item)
    {
        return properties.Single(x => x.PropertyName == name).GetProperty(item);
    }

    [TestMethod]
    public async Task Product_PictureColumns_ReadPreloadedPictures()
    {
        var product = new Product();
        product.ProductPictures.Add(new ProductPicture { PictureId = "p1" });
        product.ProductPictures.Add(new ProductPicture { PictureId = "p2" });
        var noPictures = new Product();

        var properties = await new ProductSchemaProperty(_pictureService.Object).GetProperties([product, noPictures]);

        Assert.AreEqual("/thumbs/p1.jpg", Value(properties, "Picture1", product));
        Assert.AreEqual("/thumbs/p2.jpg", Value(properties, "Picture2", product));
        Assert.IsNull(Value(properties, "Picture3", product));
        Assert.IsNull(Value(properties, "Picture1", noPictures));
        //pictures are loaded once per product, not once per picture column
        _pictureService.Verify(x => x.GetPictureById(It.IsAny<string>()), Times.Exactly(2));
    }

    [TestMethod]
    public async Task Product_PictureColumns_DoNotDependOnRowInstances()
    {
        var product = new Product();
        product.ProductPictures.Add(new ProductPicture { PictureId = "p1" });
        var sameProduct = new Product();
        sameProduct.ProductPictures.Add(new ProductPicture { PictureId = "p1" });

        var properties = await new ProductSchemaProperty(_pictureService.Object).GetProperties([product, sameProduct]);

        Assert.AreEqual("/thumbs/p1.jpg", Value(properties, "Picture1", new Product {
            ProductPictures = { new ProductPicture { PictureId = "p1" } }
        }));
        //a picture shared by several rows is loaded once
        _pictureService.Verify(x => x.GetPictureById("p1"), Times.Once);
    }

    [TestMethod]
    public async Task Brand_PictureColumn_ReadsPreloadedPicture()
    {
        var brand = new Brand { PictureId = "b1" };

        var properties = await new BrandSchemaProperty(_pictureService.Object).GetProperties([brand]);

        Assert.AreEqual("/thumbs/b1.jpg", Value(properties, "Picture", brand));
        Assert.AreEqual("/thumbs/b1.jpg", Value(properties, "Picture", new Brand { PictureId = "b1" }));
    }

    [TestMethod]
    public async Task Brand_PictureColumn_WithoutPicture_UsesPictureServiceResult()
    {
        _pictureService.Setup(x => x.GetPictureById(""))
            .ReturnsAsync((Picture)null);
        _pictureService.Setup(x => x.GetThumbPhysicalPath(null, It.IsAny<int>(), It.IsAny<bool>()))
            .ReturnsAsync("/thumbs/default.jpg");
        var brand = new Brand { PictureId = null };

        var properties = await new BrandSchemaProperty(_pictureService.Object).GetProperties([brand]);

        Assert.AreEqual("/thumbs/default.jpg", Value(properties, "Picture", brand));
    }

    [TestMethod]
    public async Task Category_PictureColumn_ReadsPreloadedPicture()
    {
        var category = new Category { PictureId = "c1" };

        var properties = await new CategorySchemaProperty(_pictureService.Object).GetProperties([category]);

        Assert.AreEqual("/thumbs/c1.jpg", Value(properties, "Picture", category));
    }

    [TestMethod]
    public async Task Collection_PictureColumn_ReadsPreloadedPicture()
    {
        var collection = new Collection { PictureId = "k1" };

        var properties = await new CollectionSchemaProperty(_pictureService.Object).GetProperties([collection]);

        Assert.AreEqual("/thumbs/k1.jpg", Value(properties, "Picture", collection));
    }
}
