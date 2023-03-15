FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
LABEL stage=build-env
WORKDIR /app

# Copy and build
COPY ./src /app

ARG GIT_COMMIT
ARG GIT_BRANCH

# build plugins
RUN dotnet build /app/Plugins/Authentication.Facebook -c Release -p:SourceRevisionId=$GIT_COMMIT -p:GitBranch=$GIT_BRANCH
RUN dotnet build /app/Plugins/Authentication.Google -c Release -p:SourceRevisionId=$GIT_COMMIT -p:GitBranch=$GIT_BRANCH
RUN dotnet build /app/Plugins/DiscountRules.Standard -c Release -p:SourceRevisionId=$GIT_COMMIT -p:GitBranch=$GIT_BRANCH
RUN dotnet build /app/Plugins/ExchangeRate.McExchange -c Release -p:SourceRevisionId=$GIT_COMMIT -p:GitBranch=$GIT_BRANCH
RUN dotnet build /app/Plugins/Payments.BrainTree -c Release -p:SourceRevisionId=$GIT_COMMIT -p:GitBranch=$GIT_BRANCH
RUN dotnet build /app/Plugins/Payments.CashOnDelivery -c Release -p:SourceRevisionId=$GIT_COMMIT -p:GitBranch=$GIT_BRANCH
RUN dotnet build /app/Plugins/Payments.PayPalStandard -c Release -p:SourceRevisionId=$GIT_COMMIT -p:GitBranch=$GIT_BRANCH
RUN dotnet build /app/Plugins/Shipping.ByWeight -c Release -p:SourceRevisionId=$GIT_COMMIT -p:GitBranch=$GIT_BRANCH
RUN dotnet build /app/Plugins/Shipping.FixedRateShipping -c Release -p:SourceRevisionId=$GIT_COMMIT -p:GitBranch=$GIT_BRANCH
RUN dotnet build /app/Plugins/Shipping.ShippingPoint -c Release -p:SourceRevisionId=$GIT_COMMIT -p:GitBranch=$GIT_BRANCH
RUN dotnet build /app/Plugins/Tax.CountryStateZip -c Release -p:SourceRevisionId=$GIT_COMMIT -p:GitBranch=$GIT_BRANCH
RUN dotnet build /app/Plugins/Tax.FixedRate -c Release -p:SourceRevisionId=$GIT_COMMIT -p:GitBranch=$GIT_BRANCH
RUN dotnet build /app/Plugins/Widgets.FacebookPixel -c Release -p:SourceRevisionId=$GIT_COMMIT -p:GitBranch=$GIT_BRANCH
RUN dotnet build /app/Plugins/Widgets.GoogleAnalytics -c Release -p:SourceRevisionId=$GIT_COMMIT -p:GitBranch=$GIT_BRANCH
RUN dotnet build /app/Plugins/Widgets.Slider -c Release -p:SourceRevisionId=$GIT_COMMIT -p:GitBranch=$GIT_BRANCH

# restore
RUN dotnet restore /app/Web/Grand.Web/Grand.Web.csproj

#build
RUN dotnet build /app/Web/Grand.Web/Grand.Web.csproj --no-restore -c Release -p:SourceRevisionId=$GIT_COMMIT -p:GitBranch=$GIT_BRANCH

# publish Web
RUN dotnet publish /app/Web/Grand.Web --no-restore -c Release -o ./build/release -p:SourceRevisionId=$GIT_COMMIT -p:GitBranch=$GIT_BRANCH

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
EXPOSE 80
ENV ASPNETCORE_URLS http://+:80
WORKDIR /app
COPY --from=build-env /app/build/release .
ENTRYPOINT ["dotnet", "Grand.Web.dll"]