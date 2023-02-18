﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Messages.Startup.Tests
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