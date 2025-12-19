FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy AS base

RUN apt-get update && apt-get install -y \
    software-properties-common \
    && add-apt-repository universe \
    && apt-get update \
    && apt-get install -y \
        libtesseract4 \
        tesseract-ocr \
        libleptonica-dev \
        libopencv-dev \
    && rm -rf /var/lib/apt/lists/*

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
