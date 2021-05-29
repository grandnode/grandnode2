using Grand.Domain.Directory;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Common.Interfaces.Directory
{
    /// <summary>
    /// Measure dimension service interface
    /// </summary>
    public partial interface IMeasureService
    {
       
        /// <summary>
        /// Gets a measure dimension by identifier
        /// </summary>
        /// <param name="measureDimensionId">Measure dimension identifier</param>
        /// <returns>Measure dimension</returns>
        Task<MeasureDimension> GetMeasureDimensionById(string measureDimensionId);

        /// <summary>
        /// Gets a measure dimension by system keyword
        /// </summary>
        /// <param name="systemKeyword">The system keyword</param>
        /// <returns>Measure dimension</returns>
        Task<MeasureDimension> GetMeasureDimensionBySystemKeyword(string systemKeyword);

        /// <summary>
        /// Gets all measure dimensions
        /// </summary>
        /// <returns>Measure dimensions</returns>
        Task<IList<MeasureDimension>> GetAllMeasureDimensions();

        /// <summary>
        /// Inserts a measure dimension
        /// </summary>
        /// <param name="measure">Measure dimension</param>
        Task InsertMeasureDimension(MeasureDimension measure);

        /// <summary>
        /// Updates the measure dimension
        /// </summary>
        /// <param name="measure">Measure dimension</param>
        Task UpdateMeasureDimension(MeasureDimension measure);

        /// <summary>
        /// Deletes measure dimension
        /// </summary>
        /// <param name="measureDimension">Measure dimension</param>
        Task DeleteMeasureDimension(MeasureDimension measureDimension);

        /// <summary>
        /// Converts dimension
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <param name="sourceMeasureDimension">Source dimension</param>
        /// <param name="targetMeasureDimension">Target dimension</param>
        /// <param name="round">A value indicating whether a result should be rounded</param>
        /// <returns>Converted value</returns>
        Task<double> ConvertDimension(double value,
            MeasureDimension sourceMeasureDimension, MeasureDimension targetMeasureDimension, bool round = true);

        /// <summary>
        /// Converts to primary measure dimension
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <param name="sourceMeasureDimension">Source dimension</param>
        /// <returns>Converted value</returns>
        Task<double> ConvertToPrimaryMeasureDimension(double value,
            MeasureDimension sourceMeasureDimension);

        /// <summary>
        /// Converts from primary dimension
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <param name="targetMeasureDimension">Target dimension</param>
        /// <returns>Converted value</returns>
        Task<double> ConvertFromPrimaryMeasureDimension(double value,
            MeasureDimension targetMeasureDimension);

        
        /// <summary>
        /// Gets a measure weight by identifier
        /// </summary>
        /// <param name="measureWeightId">Measure weight identifier</param>
        /// <returns>Measure weight</returns>
        Task<MeasureWeight> GetMeasureWeightById(string measureWeightId);

        /// <summary>
        /// Gets a measure weight by system keyword
        /// </summary>
        /// <param name="systemKeyword">The system keyword</param>
        /// <returns>Measure weight</returns>
        Task<MeasureWeight> GetMeasureWeightBySystemKeyword(string systemKeyword);

        /// <summary>
        /// Gets all measure weights
        /// </summary>
        /// <returns>Measure weights</returns>
        Task<IList<MeasureWeight>> GetAllMeasureWeights();

        /// <summary>
        /// Inserts a measure weight
        /// </summary>
        /// <param name="measure">Measure weight</param>
        Task InsertMeasureWeight(MeasureWeight measure);

        /// <summary>
        /// Updates the measure weight
        /// </summary>
        /// <param name="measure">Measure weight</param>
        Task UpdateMeasureWeight(MeasureWeight measure);

        /// <summary>
        /// Deletes measure weight
        /// </summary>
        /// <param name="measureWeight">Measure weight</param>
        Task DeleteMeasureWeight(MeasureWeight measureWeight);

        /// <summary>
        /// Converts weight
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <param name="sourceMeasureWeight">Source weight</param>
        /// <param name="targetMeasureWeight">Target weight</param>
        /// <param name="round">A value indicating whether a result should be rounded</param>
        /// <returns>Converted value</returns>
        Task<double> ConvertWeight(double value,
            MeasureWeight sourceMeasureWeight, MeasureWeight targetMeasureWeight, bool round = true);

        /// <summary>
        /// Converts to primary measure weight
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <param name="sourceMeasureWeight">Source weight</param>
        /// <returns>Converted value</returns>
        Task<double> ConvertToPrimaryMeasureWeight(double value, MeasureWeight sourceMeasureWeight);

        /// <summary>
        /// Converts from primary weight
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <param name="targetMeasureWeight">Target weight</param>
        /// <returns>Converted value</returns>
        Task<double> ConvertFromPrimaryMeasureWeight(double value,
            MeasureWeight targetMeasureWeight);

        /// <summary>
        /// Gets a measure unit by identifier
        /// </summary>
        /// <param name="measureUnitId">Measure unit identifier</param>
        /// <returns>Measure dimension</returns>
        Task<MeasureUnit> GetMeasureUnitById(string measureUnitId);

        /// <summary>
        /// Gets all measure units
        /// </summary>
        /// <returns>Measure units</returns>
        Task<IList<MeasureUnit>> GetAllMeasureUnits();

        /// <summary>
        /// Inserts a measure unit
        /// </summary>
        /// <param name="measure">Measure unit</param>
        Task InsertMeasureUnit(MeasureUnit measure);

        /// <summary>
        /// Updates the measure unit
        /// </summary>
        /// <param name="measure">Measure unit</param>
        Task UpdateMeasureUnit(MeasureUnit measure);

        /// <summary>
        /// Deletes measure unit
        /// </summary>
        /// <param name="measureUnit">Measure unit</param>
        Task DeleteMeasureUnit(MeasureUnit measureUnit);

    }
}