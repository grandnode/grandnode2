# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0.301 AS build-env
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
LABEL org.opencontainers.image.title="GrandNode" \
      org.opencontainers.image.description="GrandNode e-commerce platform" \
      org.opencontainers.image.url="https://grandnode.com" \
      org.opencontainers.image.documentation="https://grandnode.com/knowledgebase" \
      org.opencontainers.image.source="https://github.com/grandnode/grandnode2" \
      org.opencontainers.image.vendor="GrandNode" \
      org.opencontainers.image.licenses="GPL-3.0-only"

EXPOSE 8080
WORKDIR /app
COPY --from=build-env /app/build/release .

RUN mkdir -p /app/media && chown -R app:app /app/App_Data /app/wwwroot /app/Plugins /app/media

USER app

ENTRYPOINT ["dotnet", "Grand.Web.dll"]
