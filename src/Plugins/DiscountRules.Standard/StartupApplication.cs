﻿using DiscountRules.Standard.Providers;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DiscountRules.Standard;

public class StartupApplication : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDiscountProvider, DiscountProvider>();
        services.AddScoped<CustomerGroupDiscountRule>();
        services.AddScoped<HadSpentAmountDiscountRule>();
        services.AddScoped<HasAllProductsDiscountRule>();
        services.AddScoped<HasOneProductDiscountRule>();
        services.AddScoped<ShoppingCartDiscountRule>();
    }

    public int Priority => 10;

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
    }

    public bool BeforeConfigure => false;
}