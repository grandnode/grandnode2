# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
LABEL stage=build-env
WORKDIR /app

# Copy 
COPY ./src/ /app/

ARG GIT_COMMIT
ARG GIT_BRANCH

# Build plugins
RUN for plugin in /app/Plugins/*; do \
    dotnet build "$plugin" -c Release -p:SourceRevisionId=$GIT_COMMIT -p:GitBranch=$GIT_BRANCH; \
  done

# Publish Web project
RUN dotnet publish /app/Web/Grand.Web/Grand.Web.csproj -c Release -o ./build/release -p:SourceRevisionId=$GIT_COMMIT -p:GitBranch=$GIT_BRANCH

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
EXPOSE 8080
WORKDIR /app
COPY --from=build-env /app/build/release .
ENTRYPOINT ["dotnet", "Grand.Web.dll"]
