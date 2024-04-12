using Grand.Infrastructure.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;

namespace Grand.Infrastructure.Tests.TypeConverters.Converter;

[TestClass]
public class CaptchaInterfaceConverter
{
    [TestMethod]
    public void ConvertFrom_CaptchaObject_To_Json()
    {
        //Arrange
        var sample = new SampleClass {
            CaptchaValidModel = new CaptchaModel {
                ReCaptchaResponse = "xxx",
                ReCaptchaResponseField = "yyy",
                ReCaptchaChallengeField = "zzz",
                ReCaptchaResponseValue = "uuu"
            }
        };
        //Act
        var jsonString = JsonSerializer.Serialize(sample);
        //Assert
        var expJson =
            "{\"CaptchaValidModel\":{\"ReCaptchaChallengeField\":\"zzz\",\"ReCaptchaResponseField\":\"yyy\",\"ReCaptchaResponseValue\":\"uuu\",\"ReCaptchaResponse\":\"xxx\"}}";

        Assert.AreEqual(expJson, jsonString);
    }

    [TestMethod]
    public void ConvertFrom_CaptchaNullObject_To_Json()
    {
        //Arrange
        var sample = new SampleClass();
        //Act
        var jsonString = JsonSerializer.Serialize(sample);
        //Assert
        var expJson =
            "{\"CaptchaValidModel\":{\"ReCaptchaChallengeField\":null,\"ReCaptchaResponseField\":null,\"ReCaptchaResponseValue\":null,\"ReCaptchaResponse\":null}}";
        Assert.AreEqual(expJson, jsonString);
    }

    [TestMethod]
    public void ConvertFrom_Json_To_CaptchaObject()
    {
        //Arrange
        var json =
            "{\"CaptchaValidModel\":{\"ReCaptchaChallengeField\":\"zzz\",\"ReCaptchaResponseField\":\"yyy\",\"ReCaptchaResponseValue\":\"uuu\",\"ReCaptchaResponse\":\"xxx\"}}";

        //Act
        var sample = JsonSerializer.Deserialize<SampleClass>(json);
        //Assert
        Assert.IsNotNull(sample);
        Assert.AreEqual("zzz", sample.CaptchaValidModel.ReCaptchaChallengeField);
        Assert.AreEqual("yyy", sample.CaptchaValidModel.ReCaptchaResponseField);
        Assert.AreEqual("uuu", sample.CaptchaValidModel.ReCaptchaResponseValue);
        Assert.AreEqual("xxx", sample.CaptchaValidModel.ReCaptchaResponse);
    }

    [TestMethod]
    public void ConvertFrom_JsonEmptyCaptcha_To_CaptchaObject()
    {
        //Arrange
        var json = "{}";

        //Act
        var sample = JsonSerializer.Deserialize<SampleClass>(json);
        //Assert
        Assert.IsNotNull(sample);
        Assert.IsNull(sample.CaptchaValidModel.ReCaptchaChallengeField);
        Assert.IsNull(sample.CaptchaValidModel.ReCaptchaResponseField);
        Assert.IsNull(sample.CaptchaValidModel.ReCaptchaResponseValue);
        Assert.IsNull(sample.CaptchaValidModel.ReCaptchaResponse);
    }

    public class SampleClass
    {
        public ICaptchaValidModel CaptchaValidModel { get; set; } = new CaptchaModel();
    }
}