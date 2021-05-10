using Grand.Business.Storage.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                var result=file.Object.GetDownloadBits();
                Assert.IsTrue(result.SequenceEqual(expected));
            }
        }
    }
}
