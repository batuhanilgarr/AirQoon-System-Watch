FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY AirQoon.Web/AirQoon.Web.csproj AirQoon.Web/
RUN dotnet restore AirQoon.Web/AirQoon.Web.csproj

COPY AirQoon.Web/ AirQoon.Web/
RUN dotnet publish AirQoon.Web/AirQoon.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./

EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

ENTRYPOINT ["dotnet", "AirQoon.Web.dll"]
