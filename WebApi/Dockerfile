FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS base

RUN apt-get update && apt-get install -y \
    libopencv-core-dev \
    libopencv-imgproc-dev \
    libopencv-imgcodecs-dev \
    libopencv-video-dev \
    libopencv-highgui-dev \
    libglib2.0-0 \
    libsm6 \
    libxext6 \
    libxrender1 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS publish
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
