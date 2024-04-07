FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build-env
LABEL stage=build-env
WORKDIR /app

# Copy and build
COPY ./src /app

ARG GIT_COMMIT
ARG GIT_BRANCH

# build plugins
RUN set -e; \
    for dir in /app/Plugins/*; do \
        if [ -d "$dir" ]; then \
            dotnet build $dir -c Release -p:SourceRevisionId=$GIT_COMMIT -p:GitBranch=$GIT_BRANCH; \
        fi; \
    done

#build
RUN dotnet build /app/Web/Grand.Web/Grand.Web.csproj --no-restore -c Release -p:SourceRevisionId=$GIT_COMMIT -p:GitBranch=$GIT_BRANCH

# publish Web
RUN dotnet publish /app/Web/Grand.Web --no-restore -c Release -o ./build/release -p:SourceRevisionId=$GIT_COMMIT -p:GitBranch=$GIT_BRANCH

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine3.19

EXPOSE 8080

WORKDIR /app
COPY --from=build-env /app/build/release .
ENTRYPOINT ["dotnet", "Grand.Web.dll"]