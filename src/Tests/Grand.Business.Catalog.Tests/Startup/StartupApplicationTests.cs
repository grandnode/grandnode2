using Microsoft.VisualStudio.TestTools.UnitTesting;
using Grand.Business.Catalog.Startup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Grand.Business.Catalog.Startup.Tests
{
    [TestClass()]
    public class StartupApplicationTests
    {
        StartupApplication _application;
        ServiceCollection _serviceCollection;
        IConfiguration _configuration;

        [TestInitialize]
        public void Init()
        {
            _serviceCollection = new ServiceCollection();
            _configuration = new ConfigurationBuilder().Build();
            _application = new StartupApplication();

        }
        [TestMethod()]
        public void ConfigureServicesTest()
        {
            //Act
            _application.ConfigureServices(_serviceCollection, _configuration);
            //Assert
            Assert.IsTrue(_serviceCollection.Count > 0);
        }
    }
}