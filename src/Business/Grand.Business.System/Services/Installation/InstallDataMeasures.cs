using Grand.Domain.Directory;

namespace Grand.Business.System.Services.Installation;

public partial class InstallationService
{
    protected virtual Task InstallMeasures()
    {
        var measureDimensions = new List<MeasureDimension> {
            new() {
                Name = "centimetre(s)",
                SystemKeyword = "centimetres",
                Ratio = 1,
                DisplayOrder = 1
            },
            new() {
                Name = "inch(es)",
                SystemKeyword = "inches",
                Ratio = 0.393701,
                DisplayOrder = 2
            },
            new() {
                Name = "feet",
                SystemKeyword = "feet",
                Ratio = 0.0328084,
                DisplayOrder = 3
            }
        };

        measureDimensions.ForEach(x => _measureDimensionRepository.Insert(x));

        var measureWeights = new List<MeasureWeight> {
            new() {
                Name = "ounce(s)",
                SystemKeyword = "ounce",
                Ratio = 16,
                DisplayOrder = 1
            },
            new() {
                Name = "lb(s)",
                SystemKeyword = "lb",
                Ratio = 1,
                DisplayOrder = 2
            },
            new() {
                Name = "kg(s)",
                SystemKeyword = "kg",
                Ratio = 0.45359237,
                DisplayOrder = 3
            },
            new() {
                Name = "gram(s)",
                SystemKeyword = "grams",
                Ratio = 453.59237,
                DisplayOrder = 4
            }
        };

        measureWeights.ForEach(x => _measureWeightRepository.Insert(x));

        var measureUnits = new List<MeasureUnit> {
            new() {
                Name = "pcs.",
                DisplayOrder = 1
            },
            new() {
                Name = "pair",
                DisplayOrder = 2
            },
            new() {
                Name = "set",
                DisplayOrder = 3
            }
        };
        measureUnits.ForEach(x => _measureUnitRepository.Insert(x));
        return Task.CompletedTask;
    }
}