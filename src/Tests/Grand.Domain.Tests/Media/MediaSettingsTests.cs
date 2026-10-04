using Grand.Domain.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Domain.Tests.Media;

[TestClass]
public class MediaSettingsTests
{
    [TestMethod]
    public void ImageQuality_DefaultsTo80()
    {
        //thumbnails are encoded with this JPEG quality; 100 made a 450 px thumbnail 120-200 KB
        //against 30-70 KB at 80, with no visible difference
        Assert.AreEqual(80, new MediaSettings().ImageQuality);
    }
}
