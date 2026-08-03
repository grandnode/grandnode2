# ==========================
# Build stage
# ==========================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
LABEL stage=build-env
WORKDIR /app
ARG GIT_COMMIT=local
ARG GIT_BRANCH=local
# Copy source
COPY Directory.Packages.props .
COPY ./src/ ./

# Restore
RUN dotnet restore /app/Web/Grand.Web/Grand.Web.csproj
#################################
# Build application
#################################

# Publish application
RUN dotnet publish \
    /app/Web/Grand.Web/Grand.Web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    -p:SourceRevisionId=$GIT_COMMIT \
    -p:GitBranch=$GIT_BRANCH


# ==========================
# Runtime stage
# ==========================

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

EXPOSE 8080

COPY --from=build-env /app/publish .

# Ensure runtime folders exist
RUN mkdir -p /app/App_Data \
    && mkdir -p /app/App_Data/DataProtectionKeys \
    && mkdir -p /app/wwwroot

# Give ownership to the non-root user
RUN chown -R app:app /app

USER app

ENTRYPOINT ["dotnet", "Grand.Web.dll"]
