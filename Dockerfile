FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env
LABEL stage=build-env
WORKDIR /app

# Copy and build
COPY ./src /app

# build plugins
RUN dotnet build /app/Plugins/Authentication.Facebook -c Release
RUN dotnet build /app/Plugins/Authentication.Google -c Release
RUN dotnet build /app/Plugins/DiscountRules.Standard -c Release
RUN dotnet build /app/Plugins/ExchangeRate.McExchange -c Release
RUN dotnet build /app/Plugins/Payments.BrainTree -c Release
RUN dotnet build /app/Plugins/Payments.CashOnDelivery -c Release
RUN dotnet build /app/Plugins/Payments.PayPalStandard -c Release
RUN dotnet build /app/Plugins/Shipping.ByWeight -c Release
RUN dotnet build /app/Plugins/Shipping.FixedRateShipping -c Release
RUN dotnet build /app/Plugins/Shipping.ShippingPoint -c Release
RUN dotnet build /app/Plugins/Tax.CountryStateZip -c Release
RUN dotnet build /app/Plugins/Tax.FixedRate -c Release
RUN dotnet build /app/Plugins/Widgets.FacebookPixel -c Release
RUN dotnet build /app/Plugins/Widgets.GoogleAnalytics -c Release
RUN dotnet build /app/Plugins/Widgets.Slider -c Release

# build Web
RUN dotnet publish /app/Web/Grand.Web -c Release -o ./build/release

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:5.0
EXPOSE 80
ENV ASPNETCORE_URLS http://+:80
WORKDIR /app
COPY --from=build-env /app/build/release .
ENTRYPOINT ["dotnet", "Grand.Web.dll"]