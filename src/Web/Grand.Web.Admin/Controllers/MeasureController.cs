using Grand.Business.Common.Interfaces.Configuration;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Services.Security;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Security.Authorization;
using Grand.Domain.Directory;
using Grand.Infrastructure.Caching;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Directory;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.Measures)]
    public partial class MeasureController : BaseAdminController
    {
        #region Fields

        private readonly IMeasureService _measureService;
        private readonly ISettingService _settingService;
        private readonly ITranslationService _translationService;
        private readonly MeasureSettings _measureSettings;
        private readonly ICacheBase _cacheBase;

        #endregion

        #region Constructors

        public MeasureController(IMeasureService measureService,
            ISettingService settingService,
            ITranslationService translationService,
            MeasureSettings measureSettings,
            ICacheBase cacheBase)
        {
            _measureService = measureService;
            _settingService = settingService;
            _translationService = translationService;
            _measureSettings = measureSettings;
            _cacheBase = cacheBase;
        }

        #endregion

        #region Methods
        protected async Task ClearCache()
        {
            await _cacheBase.Clear();
        }

        public IActionResult Index() => View();

        #region Weights

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Weights_List)]
        public async Task<IActionResult> Weights(DataSourceRequest command)
        {
            var weightsModel = (await _measureService.GetAllMeasureWeights())
                .Select(x => x.ToModel())
                .ToList();
            foreach (var wm in weightsModel)
                wm.IsPrimaryWeight = wm.Id == _measureSettings.BaseWeightId;
            var gridModel = new DataSourceResult
            {
                Data = weightsModel,
                Total = weightsModel.Count
            };

            return Json(gridModel);
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Weights_Edit)]
        public async Task<IActionResult> WeightUpdate(MeasureWeightModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new DataSourceResult { Errors = ModelState.SerializeErrors() });
            }

            var weight = await _measureService.GetMeasureWeightById(model.Id);
            weight = model.ToEntity(weight);
            await _measureService.UpdateMeasureWeight(weight);

            return new JsonResult("");
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Weights_Add)]
        public async Task<IActionResult> WeightAdd(MeasureWeightModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new DataSourceResult { Errors = ModelState.SerializeErrors() });
            }

            var weight = new MeasureWeight();
            weight = model.ToEntity(weight);
            await _measureService.InsertMeasureWeight(weight);

            return new JsonResult("");
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Weights_Delete)]
        public async Task<IActionResult> WeightDelete(string id)
        {
            var weight = await _measureService.GetMeasureWeightById(id);
            if (weight == null)
                throw new ArgumentException("No weight found with the specified id");

            if (weight.Id == _measureSettings.BaseWeightId)
            {
                return Json(new DataSourceResult { Errors = _translationService.GetResource("Admin.Configuration.Measures.Weights.CantDeletePrimary") });
            }

            await _measureService.DeleteMeasureWeight(weight);

            return new JsonResult("");
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Weights_Edit)]
        public async Task<IActionResult> MarkAsPrimaryWeight(string id)
        {
            var primaryWeight = await _measureService.GetMeasureWeightById(id);
            if (primaryWeight != null)
            {
                _measureSettings.BaseWeightId = primaryWeight.Id;
                await _settingService.SaveSetting(_measureSettings);
            }

            //now clear cache
            await ClearCache();

            return Json(new { result = true });
        }

        #endregion

        #region Dimensions

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Dimensions_List)]
        public async Task<IActionResult> Dimensions(DataSourceRequest command)
        {
            var dimensionsModel = (await _measureService.GetAllMeasureDimensions())
                .Select(x => x.ToModel())
                .ToList();
            foreach (var wm in dimensionsModel)
                wm.IsPrimaryDimension = wm.Id == _measureSettings.BaseDimensionId;
            var gridModel = new DataSourceResult
            {
                Data = dimensionsModel,
                Total = dimensionsModel.Count
            };

            return Json(gridModel);
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Dimensions_Edit)]
        public async Task<IActionResult> DimensionUpdate(MeasureDimensionModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new DataSourceResult { Errors = ModelState.SerializeErrors() });
            }

            var dimension = await _measureService.GetMeasureDimensionById(model.Id);
            dimension = model.ToEntity(dimension);
            await _measureService.UpdateMeasureDimension(dimension);

            return new JsonResult("");
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Dimensions_Add)]
        public async Task<IActionResult> DimensionAdd(MeasureDimensionModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new DataSourceResult { Errors = ModelState.SerializeErrors() });
            }

            var dimension = new MeasureDimension();
            dimension = model.ToEntity(dimension);
            await _measureService.InsertMeasureDimension(dimension);

            return new JsonResult("");
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Dimensions_Delete)]
        public async Task<IActionResult> DimensionDelete(string id)
        {
            var dimension = await _measureService.GetMeasureDimensionById(id);
            if (dimension == null)
                throw new ArgumentException("No dimension found with the specified id");

            if (dimension.Id == _measureSettings.BaseDimensionId)
            {
                return Json(new DataSourceResult { Errors = _translationService.GetResource("Admin.Configuration.Measures.Dimensions.CantDeletePrimary") });
            }

            await _measureService.DeleteMeasureDimension(dimension);

            return new JsonResult("");
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Dimensions_Edit)]
        public async Task<IActionResult> MarkAsPrimaryDimension(string id)
        {
            var primaryDimension = await _measureService.GetMeasureDimensionById(id);
            if (primaryDimension != null)
            {
                _measureSettings.BaseDimensionId = id;
                await _settingService.SaveSetting(_measureSettings);
            }

            //now clear cache
            await ClearCache();

            return Json(new { result = true });
        }
        #endregion


        #region Units

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Units_List)]
        public async Task<IActionResult> Units(DataSourceRequest command)
        {
            var unitsModel = (await _measureService.GetAllMeasureUnits())
                .Select(x => x.ToModel())
                .ToList();

            var gridModel = new DataSourceResult
            {
                Data = unitsModel,
                Total = unitsModel.Count
            };

            return Json(gridModel);
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Units_Edit)]
        public async Task<IActionResult> UnitUpdate(MeasureUnitModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new DataSourceResult { Errors = ModelState.SerializeErrors() });
            }

            var unit = await _measureService.GetMeasureUnitById(model.Id);
            unit = model.ToEntity(unit);
            await _measureService.UpdateMeasureUnit(unit);

            return new JsonResult("");
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Units_Add)]
        public async Task<IActionResult> UnitAdd(MeasureUnitModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new DataSourceResult { Errors = ModelState.SerializeErrors() });
            }

            var unit = new MeasureUnit();
            unit = model.ToEntity(unit);
            await _measureService.InsertMeasureUnit(unit);

            return new JsonResult("");
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Units_Delete)]
        public async Task<IActionResult> UnitDelete(string id)
        {
            var unit = await _measureService.GetMeasureUnitById(id);
            if (unit == null)
                throw new ArgumentException("No unit found with the specified id");

            await _measureService.DeleteMeasureUnit(unit);

            return new JsonResult("");
        }

        #endregion

        #endregion
    }
}
