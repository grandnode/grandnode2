# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
LABEL stage=build-env
WORKDIR /app

# Copy 
COPY Directory.Packages.props /app/
COPY ./src/ /app/

ARG GIT_COMMIT
ARG GIT_BRANCH

# Build modules
RUN for module in /app/Modules/*; do \
    dotnet build "$module" -c Release -p:SourceRevisionId=$GIT_COMMIT -p:GitBranch=$GIT_BRANCH; \
  done

# Build plugins
RUN for plugin in /app/Plugins/*; do \
    dotnet build "$plugin" -c Release -p:SourceRevisionId=$GIT_COMMIT -p:GitBranch=$GIT_BRANCH; \
  done

# Publish Web project
RUN dotnet publish /app/Web/Grand.Web/Grand.Web.csproj -c Release -o ./build/release -p:SourceRevisionId=$GIT_COMMIT -p:GitBranch=$GIT_BRANCH

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

EXPOSE 8080
WORKDIR /app
COPY --from=build-env /app/build/release .

RUN chown -R app:app /app/App_Data /app/wwwroot /app/Plugins

USER app

ENTRYPOINT ["dotnet", "Grand.Web.dll"]
