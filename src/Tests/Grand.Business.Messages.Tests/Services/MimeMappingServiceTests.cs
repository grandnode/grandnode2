using Grand.Business.Messages.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Messages.Tests.Services
{
    [TestClass]
    public class MimeMappingServiceTests
    {
        private MimeMappingService _service;

        [TestInitialize]
        public void Init()
        {
            _service = new MimeMappingService(new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider());
        }

        [TestMethod]
        public void Map_ReturnExpectedValue()
        {
            
            Assert.AreEqual("text/plain", _service.Map("file.txt"));
            Assert.AreEqual("video/x-msvideo", _service.Map("file.avi"));
            Assert.AreEqual("application/vnd.microsoft.portable-executable", _service.Map("file.exe"));
            Assert.AreEqual("image/jpeg", _service.Map("file.jpg"));
            Assert.AreEqual("image/gif", _service.Map("file.gif"));
            Assert.AreEqual("image/png", _service.Map("file.png"));
            Assert.AreEqual("application/octet-stream", _service.Map("file.wirdtype"));
        }
    }
}
