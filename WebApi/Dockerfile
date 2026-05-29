FROM ghcr.io/shimat/opencvsharp/ubuntu24-dotnet8-opencv4.12.0:20250815 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files first so NuGet restore is cached as a separate layer
COPY CaloraService.sln ./
COPY Core/Core.csproj Core/
COPY WebCore/WebCore.csproj WebCore/
COPY WebApi/WebApi.csproj WebApi/
RUN dotnet restore WebApi/WebApi.csproj

COPY . .
RUN dotnet publish WebApi/WebApi.csproj \
    -c $BUILD_CONFIGURATION \
    --no-restore \
    -o /publish \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /publish .
ENTRYPOINT ["dotnet", "WebApi.dll"]
