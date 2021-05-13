#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["src/API/Grand.Api/Grand.Api.csproj", "src/API/Grand.Api/"]
COPY ["src/Core/Grand.Domain/Grand.Domain.csproj", "src/Core/Grand.Domain/"]
COPY ["src/Core/Grand.SharedKernel/Grand.SharedKernel.csproj", "src/Core/Grand.SharedKernel/"]
COPY ["src/Core/Grand.Infrastructure/Grand.Infrastructure.csproj", "src/Core/Grand.Infrastructure/"]
COPY ["src/Business/Grand.Business.Catalog/Grand.Business.Catalog.csproj", "src/Business/Grand.Business.Catalog/"]
COPY ["src/Business/Grand.Business.Storage/Grand.Business.Storage.csproj", "src/Business/Grand.Business.Storage/"]
COPY ["src/Business/Grand.Business.Common/Grand.Business.Common.csproj", "src/Business/Grand.Business.Common/"]
COPY ["src/Business/Grand.Business.Messages/Grand.Business.Messages.csproj", "src/Business/Grand.Business.Messages/"]
COPY ["src/Business/Grand.Business.System/Grand.Business.System.csproj", "src/Business/Grand.Business.System/"]
COPY ["src/Business/Grand.Business.Checkout/Grand.Business.Checkout.csproj", "src/Business/Grand.Business.Checkout/"]
COPY ["src/Business/Grand.Business.Customers/Grand.Business.Customers.csproj", "src/Business/Grand.Business.Customers/"]
COPY ["src/Business/Grand.Business.Cms/Grand.Business.Cms.csproj", "src/Business/Grand.Business.Cms/"]
COPY ["src/Business/Grand.Business.Marketing/Grand.Business.Marketing.csproj", "src/Business/Grand.Business.Marketing/"]
COPY ["src/Business/Grand.Business.Authentication/Grand.Business.Authentication.csproj", "src/Business/Grand.Business.Authentication/"]
COPY ["src/Web/Grand.Web.Admin/Grand.Web.Admin.csproj", "src/Web/Grand.Web.Admin/"]
COPY ["src/Web/Grand.Web.Common/Grand.Web.Common.csproj", "src/Web/Grand.Web.Common/"]
COPY ["src/Web/Grand.Web/Grand.Web.csproj", "src/Web/Grand.Web/"]

#Plugins
COPY ["src/Plugins/Authentication.Facebook/Authentication.Facebook.csproj", "src/Plugins/Authentication.Facebook/"]
COPY ["src/Plugins/Authentication.Google/Authentication.Google.csproj", "src/Plugins/Authentication.Google/"]
COPY ["src/Plugins/DiscountRules.Standard/DiscountRules.Standard.csproj", "src/Plugins/DiscountRules.Standard/"]
COPY ["src/Plugins/ExchangeRate.McExchange/ExchangeRate.McExchange.csproj", "src/Plugins/ExchangeRate.McExchange/"]
COPY ["src/Plugins/Payments.BrainTree/Payments.BrainTree.csproj", "src/Plugins/Payments.BrainTree/"]
COPY ["src/Plugins/Payments.CashOnDelivery/Payments.CashOnDelivery.csproj", "src/Plugins/Payments.CashOnDelivery/"]
COPY ["src/Plugins/Payments.PayPalStandard/Payments.PayPalStandard.csproj", "src/Plugins/Payments.PayPalStandard/"]
COPY ["src/Plugins/Shipping.ByWeight/Shipping.ByWeight.csproj", "src/Plugins/Shipping.ByWeight/"]
COPY ["src/Plugins/Shipping.FixedRateShipping/Shipping.FixedRateShipping.csproj", "src/Plugins/Shipping.FixedRateShipping/"]
COPY ["src/Plugins/Shipping.ShippingPoint/Shipping.ShippingPoint.csproj", "src/Plugins/Shipping.ShippingPoint/"]
COPY ["src/Plugins/Tax.CountryStateZip/Tax.CountryStateZip.csproj", "src/Plugins/Tax.CountryStateZip/"]
COPY ["src/Plugins/Tax.FixedRate/Tax.FixedRate.csproj", "src/Plugins/Tax.FixedRate/"]
COPY ["src/Plugins/Widgets.FacebookPixel/Widgets.FacebookPixel.csproj", "src/Plugins/Widgets.FacebookPixel/"]
COPY ["src/Plugins/Widgets.GoogleAnalytics/Widgets.GoogleAnalytics.csproj", "src/Plugins/Widgets.GoogleAnalytics/"]
COPY ["src/Plugins/Widgets.Slider/Widgets.Slider.csproj", "src/Plugins/Widgets.Slider/"]

RUN dotnet build "src/Plugins/Authentication.Facebook/Authentication.Facebook.csproj"
RUN dotnet build "src/Plugins/Authentication.Google/Authentication.Google.csproj"
RUN dotnet build "src/Plugins/DiscountRules.Standard/DiscountRules.Standard.csproj"
RUN dotnet build "src/Plugins/ExchangeRate.McExchange/ExchangeRate.McExchange.csproj"
RUN dotnet build "src/Plugins/Payments.BrainTree/Payments.BrainTree.csproj"
RUN dotnet build "src/Plugins/Payments.CashOnDelivery/Payments.CashOnDelivery.csproj"
RUN dotnet build "src/Plugins/Payments.PayPalStandard/Payments.PayPalStandard.csproj"
RUN dotnet build "src/Plugins/Shipping.ByWeight/Shipping.ByWeight.csproj"
RUN dotnet build "src/Plugins/Shipping.FixedRateShipping/Shipping.FixedRateShipping.csproj"
RUN dotnet build "src/Plugins/Shipping.ShippingPoint/Shipping.ShippingPoint.csproj"
RUN dotnet build "src/Plugins/Tax.CountryStateZip/Tax.CountryStateZip.csproj"
RUN dotnet build "src/Plugins/Tax.FixedRate/Tax.FixedRate.csproj"
RUN dotnet build "src/Plugins/Widgets.FacebookPixel/Widgets.FacebookPixel.csproj"
RUN dotnet build "src/Plugins/Widgets.GoogleAnalytics/Widgets.GoogleAnalytics.csproj"
RUN dotnet build "src/Plugins/Widgets.Slider/Widgets.Slider.csproj"

RUN dotnet restore "src/Web/Grand.Web/Grand.Web.csproj"
COPY . .
WORKDIR "/src/src/Web/Grand.Web"
RUN dotnet build "Grand.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Grand.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Grand.Web.dll"]