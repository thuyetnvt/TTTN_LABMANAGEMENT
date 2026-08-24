FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build

WORKDIR /src
COPY LabManagementAPI.csproj ./
RUN dotnet restore

COPY . .
RUN dotnet publish LabManagementAPI.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final

RUN apk add --no-cache icu-libs tzdata ttf-dejavu

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    TZ=Asia/Ho_Chi_Minh

WORKDIR /app
COPY --from=build --chown=1654:1654 /app/publish .
RUN mkdir -p /app/uploads/equipment-decisions \
    && chown -R 1654:1654 /app/uploads

USER 1654
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD wget -q -O /dev/null http://127.0.0.1:8080/health || exit 1

ENTRYPOINT ["dotnet", "LabManagementAPI.dll"]
