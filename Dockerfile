# syntax=docker/dockerfile:1

# ---- build ----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore first so this layer is cached until a project file changes.
COPY AgenticApiDemo.sln AgenticApiDemo.csproj ./
COPY tests/AgenticApiDemo.Tests/AgenticApiDemo.Tests.csproj tests/AgenticApiDemo.Tests/
RUN dotnet restore AgenticApiDemo.sln

COPY . .
RUN dotnet build AgenticApiDemo.sln -c Release --no-restore

# ---- test (docker build --target test) ------------------------------------
FROM build AS test
RUN dotnet test AgenticApiDemo.sln -c Release --no-build --verbosity normal

# ---- publish --------------------------------------------------------------
FROM build AS publish
RUN dotnet publish AgenticApiDemo.csproj -c Release --no-build -o /app

# ---- runtime --------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_NOLOGO=true \
    DOTNET_CLI_TELEMETRY_OPTOUT=true

COPY --from=publish /app .

# Run as the image's non-root user.
USER $APP_UID
EXPOSE 8080

ENTRYPOINT ["dotnet", "AgenticApiDemo.dll"]
