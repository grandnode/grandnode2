﻿using Grand.Data;
using Grand.Domain.Stores;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._1._1;

public class MigrationUpdateStore : IMigration
{
    public int Priority => 0;
    public DbVersion Version => new(1, 1);
    public Guid Identity => new("31F803A6-4B54-42F5-9868-6115F51BA594");
    public string Name => "Update store - add domains";

    /// <summary>
    ///     Upgrade process
    /// </summary>
    /// <param name="database"></param>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public bool UpgradeProcess(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<Store>>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationUpdateStore>>();
        try
        {
            foreach (var store in repository.Table)
                if (!store.Domains.Any())
                {
                    var storeUri = new Uri(store.Url);
                    var httpscheme = store.SslEnabled ? "https" : "http";
                    var domain = new DomainHost {
                        HostName = storeUri.Authority,
                        Url = $"{httpscheme}://{storeUri.Authority}",
                        Primary = true
                    };
                    store.Domains.Add(domain);
                    repository.Update(store);
                }
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - MigrationUpdateStore");
        }

        return true;
    }
}