using Microsoft.VisualStudio.TestTools.UnitTesting;
using Grand.SharedKernel.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.SharedKernel.Extensions.Tests
{
    [TestClass()]
    public class FormatTextTests
    {
       
        [TestMethod()]
        [DataRow("sample <br /> sample ")]
        [DataRow("&nbsp;&nbsp; sample <br /> sample &nbsp;&nbsp;")]
        public void ConvertTextTest(string text)
        {
            Assert.IsFalse(FormatText.ConvertText(text).Contains("<br"));
            Assert.IsFalse(FormatText.ConvertText(text).Contains("&nbsp;&nbsp;"));
        }

    }
}