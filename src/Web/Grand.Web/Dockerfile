#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["src/Web/Grand.Web/Grand.Web.csproj", "src/Web/Grand.Web/"]
COPY ["src/Business/Grand.Business.Catalog/Grand.Business.Catalog.csproj", "src/Business/Grand.Business.Catalog/"]
COPY ["src/Business/Grand.Business.Storage/Grand.Business.Storage.csproj", "src/Business/Grand.Business.Storage/"]
COPY ["src/Business/Grand.Business.Common/Grand.Business.Common.csproj", "src/Business/Grand.Business.Common/"]
COPY ["src/Core/Grand.Domain/Grand.Domain.csproj", "src/Core/Grand.Domain/"]
COPY ["src/Core/Grand.SharedKernel/Grand.SharedKernel.csproj", "src/Core/Grand.SharedKernel/"]
COPY ["src/Core/Grand.Infrastructure/Grand.Infrastructure.csproj", "src/Core/Grand.Infrastructure/"]
COPY ["src/Business/Grand.Business.Messages/Grand.Business.Messages.csproj", "src/Business/Grand.Business.Messages/"]
COPY ["src/Business/Grand.Business.System/Grand.Business.System.csproj", "src/Business/Grand.Business.System/"]
COPY ["src/Business/Grand.Business.Checkout/Grand.Business.Checkout.csproj", "src/Business/Grand.Business.Checkout/"]
COPY ["src/Business/Grand.Business.Customers/Grand.Business.Customers.csproj", "src/Business/Grand.Business.Customers/"]
COPY ["src/Business/Grand.Business.Cms/Grand.Business.Cms.csproj", "src/Business/Grand.Business.Cms/"]
COPY ["src/Business/Grand.Business.Marketing/Grand.Business.Marketing.csproj", "src/Business/Grand.Business.Marketing/"]
COPY ["src/Web/Grand.Web.Admin/Grand.Web.Admin.csproj", "src/Web/Grand.Web.Admin/"]
COPY ["src/Business/Grand.Business.Authentication/Grand.Business.Authentication.csproj", "src/Business/Grand.Business.Authentication/"]
COPY ["src/Web/Grand.Web.Common/Grand.Web.Common.csproj", "src/Web/Grand.Web.Common/"]
COPY ["src/API/Grand.Api/Grand.Api.csproj", "src/API/Grand.Api/"]
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
