using Microsoft.VisualStudio.TestTools.UnitTesting;
using Grand.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Domain.Common.Tests
{
    [TestClass()]
    public class AddressExtensionsTests
    {
        [TestMethod()]
        public void FindAddressTest_NotNull()
        {
            var addresses = new List<Address>() {
                new Address(){ Address1 = "address1", Address2 = "address11", FirstName = "name1", LastName = "namelast1" },
                new Address(){ Address1 = "address2", Address2 = "address12", FirstName = "name2", LastName = "namelast2" },
                new Address(){ Address1 = "address3", Address2 = "address13", FirstName = "name3", LastName = "namelast3" },
                new Address(){ Address1 = "address4", Address2 = "address14", FirstName = "name4", LastName = "namelast4" },
            };
            var address = addresses.FindAddress("name1", "namelast1", string.Empty, string.Empty, string.Empty, 
                string.Empty, "address1", "address11", string.Empty, string.Empty, string.Empty, string.Empty);
            Assert.IsNotNull(address);
        }
        [TestMethod()]
        public void FindAddressTest_IsNull()
        {
            var addresses = new List<Address>() {
                new Address(){ Address1 = "address1", Address2 = "address11", FirstName = "name1", LastName = "namelast1" },
                new Address(){ Address1 = "address2", Address2 = "address12", FirstName = "name2", LastName = "namelast2" },
                new Address(){ Address1 = "address3", Address2 = "address13", FirstName = "name3", LastName = "namelast3" },
                new Address(){ Address1 = "address4", Address2 = "address14", FirstName = "name4", LastName = "namelast4" },
            };
            var address = addresses.FindAddress("name1", "namelast1", string.Empty, string.Empty, string.Empty,
                string.Empty, "address1", "address2", "city", string.Empty, string.Empty, string.Empty);
            Assert.IsNull(address);
        }
    }
}