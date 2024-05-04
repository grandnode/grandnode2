using Grand.Business.Core.Interfaces.Catalog.Directory;
using Grand.Data;
using Grand.Domain.Directory;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using Grand.SharedKernel;
using MediatR;

namespace Grand.Business.Catalog.Services.Directory;

/// <summary>
///     Measure dimension service
/// </summary>
public class MeasureService : IMeasureService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    /// <param name="cacheBase">Cache manager</param>
    /// <param name="measureDimensionRepository">Dimension repository</param>
    /// <param name="measureWeightRepository">Weight repository</param>
    /// <param name="measureUnitRepository">Unit repository</param>
    /// <param name="measureSettings">Measure settings</param>
    /// <param name="mediator">Mediator</param>
    public MeasureService(ICacheBase cacheBase,
        IRepository<MeasureDimension> measureDimensionRepository,
        IRepository<MeasureWeight> measureWeightRepository,
        IRepository<MeasureUnit> measureUnitRepository,
        MeasureSettings measureSettings,
        IMediator mediator)
    {
        _cacheBase = cacheBase;
        _measureDimensionRepository = measureDimensionRepository;
        _measureWeightRepository = measureWeightRepository;
        _measureUnitRepository = measureUnitRepository;
        _measureSettings = measureSettings;
        _mediator = mediator;
    }

    #endregion

    #region Fields

    private readonly IRepository<MeasureDimension> _measureDimensionRepository;
    private readonly IRepository<MeasureWeight> _measureWeightRepository;
    private readonly IRepository<MeasureUnit> _measureUnitRepository;
    private readonly ICacheBase _cacheBase;
    private readonly MeasureSettings _measureSettings;
    private readonly IMediator _mediator;

    #endregion

    #region Methods

    #region Dimensions

    /// <summary>
    ///     Gets a measure dimension by identifier
    /// </summary>
    /// <param name="measureDimensionId">Measure dimension identifier</param>
    /// <returns>Measure dimension</returns>
    public virtual Task<MeasureDimension> GetMeasureDimensionById(string measureDimensionId)
    {
        var key = string.Format(CacheKey.MEASUREDIMENSIONS_BY_ID_KEY, measureDimensionId);
        return _cacheBase.GetAsync(key, () => _measureDimensionRepository.GetByIdAsync(measureDimensionId));
    }

    /// <summary>
    ///     Gets a measure dimension by system keyword
    /// </summary>
    /// <param name="systemKeyword">The system keyword</param>
    /// <returns>Measure dimension</returns>
    public virtual async Task<MeasureDimension> GetMeasureDimensionBySystemKeyword(string systemKeyword)
    {
        if (string.IsNullOrEmpty(systemKeyword))
            return null;

        var measureDimensions = await GetAllMeasureDimensions();
        return measureDimensions.FirstOrDefault(measureDimension => string.Equals(measureDimension.SystemKeyword,
            systemKeyword, StringComparison.InvariantCultureIgnoreCase));
    }

    /// <summary>
    ///     Gets all measure dimensions
    /// </summary>
    /// <returns>Measure dimensions</returns>
    public virtual async Task<IList<MeasureDimension>> GetAllMeasureDimensions()
    {
        var key = CacheKey.MEASUREDIMENSIONS_ALL_KEY;
        return await _cacheBase.GetAsync(key, async () =>
        {
            var query = from md in _measureDimensionRepository.Table
                orderby md.DisplayOrder
                select md;
            return await Task.FromResult(query.ToList());
        });
    }

    /// <summary>
    ///     Inserts a measure dimension
    /// </summary>
    /// <param name="measure">Measure dimension</param>
    public virtual async Task InsertMeasureDimension(MeasureDimension measure)
    {
        ArgumentNullException.ThrowIfNull(measure);

        await _measureDimensionRepository.InsertAsync(measure);

        await _cacheBase.RemoveByPrefix(CacheKey.MEASUREDIMENSIONS_PATTERN_KEY);

        //event notification
        await _mediator.EntityInserted(measure);
    }

    /// <summary>
    ///     Updates the measure dimension
    /// </summary>
    /// <param name="measure">Measure dimension</param>
    public virtual async Task UpdateMeasureDimension(MeasureDimension measure)
    {
        ArgumentNullException.ThrowIfNull(measure);

        await _measureDimensionRepository.UpdateAsync(measure);

        await _cacheBase.RemoveByPrefix(CacheKey.MEASUREDIMENSIONS_PATTERN_KEY);

        //event notification
        await _mediator.EntityUpdated(measure);
    }

    /// <summary>
    ///     Deletes measure dimension
    /// </summary>
    /// <param name="measureDimension">Measure dimension</param>
    public virtual async Task DeleteMeasureDimension(MeasureDimension measureDimension)
    {
        ArgumentNullException.ThrowIfNull(measureDimension);

        await _measureDimensionRepository.DeleteAsync(measureDimension);

        await _cacheBase.RemoveByPrefix(CacheKey.MEASUREDIMENSIONS_PATTERN_KEY);

        //event notification
        await _mediator.EntityDeleted(measureDimension);
    }

    /// <summary>
    ///     Converts dimension
    /// </summary>
    /// <param name="value">Value to convert</param>
    /// <param name="sourceMeasureDimension">Source dimension</param>
    /// <param name="targetMeasureDimension">Target dimension</param>
    /// <param name="round">A value indicating whether a result should be rounded</param>
    /// <returns>Converted value</returns>
    public virtual async Task<double> ConvertDimension(double value,
        MeasureDimension sourceMeasureDimension, MeasureDimension targetMeasureDimension, bool round = true)
    {
        ArgumentNullException.ThrowIfNull(sourceMeasureDimension);
        ArgumentNullException.ThrowIfNull(targetMeasureDimension);

        var result = value;
        if (result != 0 && sourceMeasureDimension.Id != targetMeasureDimension.Id)
        {
            result = await ConvertToPrimaryMeasureDimension(result, sourceMeasureDimension);
            result = await ConvertFromPrimaryMeasureDimension(result, targetMeasureDimension);
        }

        if (round)
            result = Math.Round(result, 2);
        return result;
    }

    /// <summary>
    ///     Converts to primary measure dimension
    /// </summary>
    /// <param name="value">Value to convert</param>
    /// <param name="sourceMeasureDimension">Source dimension</param>
    /// <returns>Converted value</returns>
    public virtual async Task<double> ConvertToPrimaryMeasureDimension(double value,
        MeasureDimension sourceMeasureDimension)
    {
        ArgumentNullException.ThrowIfNull(sourceMeasureDimension);

        var result = value;
        var baseDimensionIn = await GetMeasureDimensionById(_measureSettings.BaseDimensionId);
        if (result == 0 || sourceMeasureDimension.Id == baseDimensionIn.Id) return result;
        var exchangeRatio = sourceMeasureDimension.Ratio;
        if (exchangeRatio == 0)
            throw new GrandException($"Exchange ratio not set for dimension [{sourceMeasureDimension.Name}]");
        result /= exchangeRatio;
        return result;
    }

    /// <summary>
    ///     Converts from primary dimension
    /// </summary>
    /// <param name="value">Value to convert</param>
    /// <param name="targetMeasureDimension">Target dimension</param>
    /// <returns>Converted value</returns>
    public virtual async Task<double> ConvertFromPrimaryMeasureDimension(double value,
        MeasureDimension targetMeasureDimension)
    {
        ArgumentNullException.ThrowIfNull(targetMeasureDimension);

        var result = value;
        var baseDimensionIn = await GetMeasureDimensionById(_measureSettings.BaseDimensionId);
        if (result == 0 || targetMeasureDimension.Id == baseDimensionIn.Id) return result;
        var exchangeRatio = targetMeasureDimension.Ratio;
        if (exchangeRatio == 0)
            throw new GrandException($"Exchange ratio not set for dimension [{targetMeasureDimension.Name}]");
        result *= exchangeRatio;
        return result;
    }

    #endregion

    #region Weights

    /// <summary>
    ///     Gets a measure weight by identifier
    /// </summary>
    /// <param name="measureWeightId">Measure weight identifier</param>
    /// <returns>Measure weight</returns>
    public virtual Task<MeasureWeight> GetMeasureWeightById(string measureWeightId)
    {
        var key = string.Format(CacheKey.MEASUREWEIGHTS_BY_ID_KEY, measureWeightId);
        return _cacheBase.GetAsync(key, () => _measureWeightRepository.GetByIdAsync(measureWeightId));
    }

    /// <summary>
    ///     Gets a measure weight by system keyword
    /// </summary>
    /// <param name="systemKeyword">The system keyword</param>
    /// <returns>Measure weight</returns>
    public virtual async Task<MeasureWeight> GetMeasureWeightBySystemKeyword(string systemKeyword)
    {
        if (string.IsNullOrEmpty(systemKeyword))
            return null;

        var measureWeights = await GetAllMeasureWeights();
        return measureWeights.FirstOrDefault(measureWeight => string.Equals(measureWeight.SystemKeyword, systemKeyword,
            StringComparison.InvariantCultureIgnoreCase));
    }

    /// <summary>
    ///     Gets all measure weights
    /// </summary>
    /// <returns>Measure weights</returns>
    public virtual async Task<IList<MeasureWeight>> GetAllMeasureWeights()
    {
        var key = CacheKey.MEASUREWEIGHTS_ALL_KEY;
        return await _cacheBase.GetAsync(key, async () =>
        {
            var query = from mw in _measureWeightRepository.Table
                orderby mw.DisplayOrder
                select mw;
            return await Task.FromResult(query.ToList());
        });
    }

    /// <summary>
    ///     Inserts a measure weight
    /// </summary>
    /// <param name="measure">Measure weight</param>
    public virtual async Task InsertMeasureWeight(MeasureWeight measure)
    {
        ArgumentNullException.ThrowIfNull(measure);

        await _measureWeightRepository.InsertAsync(measure);

        await _cacheBase.RemoveByPrefix(CacheKey.MEASUREWEIGHTS_PATTERN_KEY);

        //event notification
        await _mediator.EntityInserted(measure);
    }

    /// <summary>
    ///     Updates the measure weight
    /// </summary>
    /// <param name="measure">Measure weight</param>
    public virtual async Task UpdateMeasureWeight(MeasureWeight measure)
    {
        ArgumentNullException.ThrowIfNull(measure);

        await _measureWeightRepository.UpdateAsync(measure);

        await _cacheBase.RemoveByPrefix(CacheKey.MEASUREWEIGHTS_PATTERN_KEY);

        //event notification
        await _mediator.EntityUpdated(measure);
    }

    /// <summary>
    ///     Deletes measure weight
    /// </summary>
    /// <param name="measureWeight">Measure weight</param>
    public virtual async Task DeleteMeasureWeight(MeasureWeight measureWeight)
    {
        ArgumentNullException.ThrowIfNull(measureWeight);

        await _measureWeightRepository.DeleteAsync(measureWeight);

        await _cacheBase.RemoveByPrefix(CacheKey.MEASUREWEIGHTS_PATTERN_KEY);

        //event notification
        await _mediator.EntityDeleted(measureWeight);
    }

    /// <summary>
    ///     Converts weight
    /// </summary>
    /// <param name="value">Value to convert</param>
    /// <param name="sourceMeasureWeight">Source weight</param>
    /// <param name="targetMeasureWeight">Target weight</param>
    /// <param name="round">A value indicating whether a result should be rounded</param>
    /// <returns>Converted value</returns>
    public virtual async Task<double> ConvertWeight(double value,
        MeasureWeight sourceMeasureWeight, MeasureWeight targetMeasureWeight, bool round = true)
    {
        ArgumentNullException.ThrowIfNull(sourceMeasureWeight);
        ArgumentNullException.ThrowIfNull(targetMeasureWeight);

        var result = value;
        if (result != 0 && sourceMeasureWeight.Id != targetMeasureWeight.Id)
        {
            result = await ConvertToPrimaryMeasureWeight(result, sourceMeasureWeight);
            result = await ConvertFromPrimaryMeasureWeight(result, targetMeasureWeight);
        }

        if (round)
            result = Math.Round(result, 2);
        return result;
    }

    /// <summary>
    ///     Converts to primary measure weight
    /// </summary>
    /// <param name="value">Value to convert</param>
    /// <param name="sourceMeasureWeight">Source weight</param>
    /// <returns>Converted value</returns>
    public virtual async Task<double> ConvertToPrimaryMeasureWeight(double value, MeasureWeight sourceMeasureWeight)
    {
        ArgumentNullException.ThrowIfNull(sourceMeasureWeight);

        var result = value;
        var baseWeightIn = await GetMeasureWeightById(_measureSettings.BaseWeightId);
        if (result == 0 || sourceMeasureWeight.Id == baseWeightIn.Id) return result;
        var exchangeRatio = sourceMeasureWeight.Ratio;
        if (exchangeRatio == 0)
            throw new GrandException($"Exchange ratio not set for weight [{sourceMeasureWeight.Name}]");
        result /= exchangeRatio;
        return result;
    }

    /// <summary>
    ///     Converts from primary weight
    /// </summary>
    /// <param name="value">Value to convert</param>
    /// <param name="targetMeasureWeight">Target weight</param>
    /// <returns>Converted value</returns>
    public virtual async Task<double> ConvertFromPrimaryMeasureWeight(double value,
        MeasureWeight targetMeasureWeight)
    {
        ArgumentNullException.ThrowIfNull(targetMeasureWeight);

        var result = value;
        var baseWeightIn = await GetMeasureWeightById(_measureSettings.BaseWeightId);
        if (result == 0 || targetMeasureWeight.Id == baseWeightIn.Id) return result;
        var exchangeRatio = targetMeasureWeight.Ratio;
        if (exchangeRatio == 0)
            throw new GrandException($"Exchange ratio not set for weight [{targetMeasureWeight.Name}]");
        result *= exchangeRatio;
        return result;
    }

    #endregion

    #region MeasureUnit

    /// <summary>
    ///     Gets a measure unit by identifier
    /// </summary>
    /// <param name="measureUnitId">Measure unit identifier</param>
    /// <returns>Measure dimension</returns>
    public virtual Task<MeasureUnit> GetMeasureUnitById(string measureUnitId)
    {
        var key = string.Format(CacheKey.MEASUREUNITS_BY_ID_KEY, measureUnitId);
        return _cacheBase.GetAsync(key, () => _measureUnitRepository.GetByIdAsync(measureUnitId));
    }


    /// <summary>
    ///     Gets all measure units
    /// </summary>
    /// <returns>Measure unit</returns>
    public virtual async Task<IList<MeasureUnit>> GetAllMeasureUnits()
    {
        var key = CacheKey.MEASUREUNITS_ALL_KEY;
        return await _cacheBase.GetAsync(key, async () =>
        {
            var query = from md in _measureUnitRepository.Table
                orderby md.DisplayOrder
                select md;
            return await Task.FromResult(query.ToList());
        });
    }

    /// <summary>
    ///     Inserts a measure unit
    /// </summary>
    /// <param name="measure">Measure unit</param>
    public virtual async Task InsertMeasureUnit(MeasureUnit measure)
    {
        ArgumentNullException.ThrowIfNull(measure);

        await _measureUnitRepository.InsertAsync(measure);

        await _cacheBase.RemoveByPrefix(CacheKey.MEASUREUNITS_PATTERN_KEY);

        //event notification
        await _mediator.EntityInserted(measure);
    }

    /// <summary>
    ///     Updates the measure unit
    /// </summary>
    /// <param name="measure">Measure unit</param>
    public virtual async Task UpdateMeasureUnit(MeasureUnit measure)
    {
        ArgumentNullException.ThrowIfNull(measure);

        await _measureUnitRepository.UpdateAsync(measure);

        await _cacheBase.RemoveByPrefix(CacheKey.MEASUREUNITS_PATTERN_KEY);

        //event notification
        await _mediator.EntityUpdated(measure);
    }

    /// <summary>
    ///     Deletes measure unit
    /// </summary>
    /// <param name="measureUnit">Measure unit</param>
    public virtual async Task DeleteMeasureUnit(MeasureUnit measureUnit)
    {
        ArgumentNullException.ThrowIfNull(measureUnit);

        //delete
        await _measureUnitRepository.DeleteAsync(measureUnit);

        //clear cache
        await _cacheBase.RemoveByPrefix(CacheKey.MEASUREUNITS_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);

        //event notification
        await _mediator.EntityDeleted(measureUnit);
    }

    #endregion

    #endregion
}