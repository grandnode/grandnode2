using Grand.Business.Common.Services.Directory;
using Grand.Domain.Data;
using Grand.Domain.Directory;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Events;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Common.Tests.Services.Directory
{
    [TestClass()]
    public class MeasureServiceTests
    {
        private Mock<ICacheBase> _cacheMock;
        private Mock<IRepository<MeasureDimension>> _mdRepositoryMock;
        private Mock<IRepository<MeasureWeight>> _mwRepositoryMock;
        private Mock<IRepository<MeasureUnit>> _muRepositoryMock;
        private MeasureSettings _settings;
        private Mock<IMediator> _mediatorMock;
        private MeasureService _measureService;

        [TestInitialize()]
        public void Init()
        {
            _cacheMock = new Mock<ICacheBase>();
            _mdRepositoryMock = new Mock<IRepository<MeasureDimension>>();
            _mwRepositoryMock = new Mock<IRepository<MeasureWeight>>();
            _muRepositoryMock = new Mock<IRepository<MeasureUnit>>();
            _settings = new MeasureSettings();
            _mediatorMock = new Mock<IMediator>();
            _measureService = new MeasureService(_cacheMock.Object,_mdRepositoryMock.Object,_mwRepositoryMock.Object,
                _muRepositoryMock.Object,_settings,_mediatorMock.Object);
        }

        [TestMethod()]
        public async Task InsertMeasureDimension_ValidArgument()
        {
            await _measureService.InsertMeasureDimension(new MeasureDimension());
            _mdRepositoryMock.Verify(c => c.InsertAsync(It.IsAny<MeasureDimension>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<MeasureDimension>>(), default), Times.Once);
        }

        [TestMethod()]
        public async Task UpdateMeasureDimension_ValidArgument()
        {
            await _measureService.UpdateMeasureDimension(new MeasureDimension());
            _mdRepositoryMock.Verify(c => c.UpdateAsync(It.IsAny<MeasureDimension>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<MeasureDimension>>(), default), Times.Once);
        }

        [TestMethod()]
        public async Task DeleteMeasureDimensiony_ValidArgument()
        {
            await _measureService.DeleteMeasureDimension(new MeasureDimension());
            _mdRepositoryMock.Verify(c => c.DeleteAsync(It.IsAny<MeasureDimension>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<MeasureDimension>>(), default), Times.Once);
        }

        [TestMethod()]
        public async Task InsertMeasureWeight_ValidArgument()
        {
            await _measureService.InsertMeasureWeight(new MeasureWeight());
            _mwRepositoryMock.Verify(c => c.InsertAsync(It.IsAny<MeasureWeight>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<MeasureWeight>>(), default), Times.Once);
        }

        [TestMethod()]
        public async Task UpdateMeasureWeight_ValidArgument()
        {
            await _measureService.UpdateMeasureWeight(new MeasureWeight());
            _mwRepositoryMock.Verify(c => c.UpdateAsync(It.IsAny<MeasureWeight>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<MeasureWeight>>(), default), Times.Once);
        }

        [TestMethod()]
        public async Task DeleteMeasureWeight_ValidArgument()
        {
            await _measureService.DeleteMeasureWeight(new MeasureWeight());
            _mwRepositoryMock.Verify(c => c.DeleteAsync(It.IsAny<MeasureWeight>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<MeasureWeight>>(), default), Times.Once);
        }

        [TestMethod()]
        public async Task InsertMeasureUnit_ValidArgument()
        {
            await _measureService.InsertMeasureUnit(new MeasureUnit());
            _muRepositoryMock.Verify(c => c.InsertAsync(It.IsAny<MeasureUnit>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<MeasureUnit>>(), default), Times.Once);
        }

        [TestMethod()]
        public async Task UpdateMeasureUnit_ValidArgument()
        {
            await _measureService.UpdateMeasureUnit(new MeasureUnit());
            _muRepositoryMock.Verify(c => c.UpdateAsync(It.IsAny<MeasureUnit>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<MeasureUnit>>(), default), Times.Once);
        }

        [TestMethod()]
        public async Task DeleteMeasureUnit_ValidArgument()
        {
            await _measureService.DeleteMeasureWeight(new MeasureWeight());
            _mwRepositoryMock.Verify(c => c.DeleteAsync(It.IsAny<MeasureWeight>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<MeasureWeight>>(), default), Times.Once);
        }
    }
}
