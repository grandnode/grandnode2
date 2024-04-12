using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Business.System.Services.Migrations._2._1;

public class MigrationProductRating : IMigration
{
    public int Priority => 0;
    public DbVersion Version => new(2, 1);
    public Guid Identity => new("4AD1CA78-5FE4-4A6C-A70C-D2036FA32E3E");
    public string Name => "Update average product rating";

    /// <summary>
    ///     Upgrade process
    /// </summary>
    /// <param name="database"></param>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public bool UpgradeProcess(IDatabaseContext database, IServiceProvider serviceProvider)
    {
        var productRepository = serviceProvider.GetRequiredService<IRepository<Product>>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationProductRating>>();
        try
        {
            foreach (var product in productRepository.Table)
            {
                var update = UpdateBuilder<Product>.Create()
                    .Set(x => x.AvgRating,
                        product.ApprovedTotalReviews == 0
                            ? 0
                            : Math.Round((double)product.ApprovedRatingSum / product.ApprovedTotalReviews, 2));

                productRepository.UpdateOneAsync(x => x.Id == product.Id, update).GetAwaiter().GetResult();
            }
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - RemoveOldActivityLogType");
        }

        return true;
    }
}