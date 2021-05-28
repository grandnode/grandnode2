using Grand.Business.System.Interfaces.Installation;
using Grand.Domain.Directory;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService : IInstallationService
    {
        protected virtual async Task InstallMeasures()
        {
            var measureDimensions = new List<MeasureDimension>
            {
                new MeasureDimension
                {
                    Name = "centimetre(s)",
                    SystemKeyword = "centimetres",
                    Ratio = 1,
                    DisplayOrder = 1,
                },
                new MeasureDimension
                {
                    Name = "inch(es)",
                    SystemKeyword = "inches",
                    Ratio = 0.393701,
                    DisplayOrder = 2,
                },
                new MeasureDimension
                {
                    Name = "feet",
                    SystemKeyword = "feet",
                    Ratio = 0.0328084,
                    DisplayOrder = 3,
                }
            };

            await _measureDimensionRepository.InsertAsync(measureDimensions);

            var measureWeights = new List<MeasureWeight>
            {
                new MeasureWeight
                {
                    Name = "ounce(s)",
                    SystemKeyword = "ounce",
                    Ratio = 16,
                    DisplayOrder = 1,
                },
                new MeasureWeight
                {
                    Name = "lb(s)",
                    SystemKeyword = "lb",
                    Ratio = 1,
                    DisplayOrder = 2,
                },
                new MeasureWeight
                {
                    Name = "kg(s)",
                    SystemKeyword = "kg",
                    Ratio = 0.45359237,
                    DisplayOrder = 3,
                },
                new MeasureWeight
                {
                    Name = "gram(s)",
                    SystemKeyword = "grams",
                    Ratio = 453.59237,
                    DisplayOrder = 4,
                }
            };

            await _measureWeightRepository.InsertAsync(measureWeights);

            var measureUnits = new List<MeasureUnit>
            {
                new MeasureUnit
                {
                    Name = "pcs.",
                    DisplayOrder = 1,
                },
                new MeasureUnit
                {
                    Name = "pair",
                    DisplayOrder = 2,
                },
                new MeasureUnit
                {
                    Name = "set",
                    DisplayOrder = 3,
                }
            };

            await _measureUnitRepository.InsertAsync(measureUnits);

        }
    }
}
