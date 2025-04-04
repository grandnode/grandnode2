﻿using Grand.Infrastructure.Migrations;

namespace Grand.Module.Migration.Migrations._2._0;

public class MigrationUpdateResourceString : IMigration
{
    public int Priority => 0;
    public DbVersion Version => new(2, 0);
    public Guid Identity => new("DC357BA8-B998-429A-8E22-9FE07BCB287D");
    public string Name => "Update resource string for english language 2.0";

    /// <summary>
    ///     Upgrade process
    /// </summary>
    /// <param name="database"></param>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public bool UpgradeProcess(IServiceProvider serviceProvider)
    {
        return serviceProvider.ImportLanguageResourcesFromXml("App_Data/Resources/Upgrade/en_200.xml");
    }
}