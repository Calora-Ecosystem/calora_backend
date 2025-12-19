FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY . .
WORKDIR /src/WebApi
RUN dotnet publish WebApi.csproj \
    -c $BUILD_CONFIGURATION \
    -o /publish \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /publish .
ENTRYPOINT ["dotnet", "WebApi.dll"]
