using Grand.Business.Core.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Storage.Tests.Extensions
{
    [TestClass]
    public class ExtensionsTests
    {
        [TestMethod]
        public void GetDownloadBits_ReturnExpectedValue()
        {
            var file = new Mock<IFormFile>();
            MemoryStream test_Stream = null;
            byte[] expected = null;
            using (test_Stream = new MemoryStream(Encoding.UTF8.GetBytes("whatever")))
            {
                expected = test_Stream.ToArray();
                file.Setup(c => c.OpenReadStream()).Returns(test_Stream);
                var result = file.Object.GetDownloadBits();
                Assert.IsTrue(result.SequenceEqual(expected));
            }
        }
    }
}
