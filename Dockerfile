# 1. Derleme (Build) Aşaması - .NET 10.0 SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["NotificationEngine.Api/NotificationEngine.Api.csproj", "NotificationEngine.Api/"]
COPY ["NotificationEngine.Service/NotificationEngine.Service.csproj", "NotificationEngine.Service/"]
COPY ["NotificationEngine.Core/NotificationEngine.Core.csproj", "NotificationEngine.Core/"]
COPY ["NotificationEngine.Infrastructure/NotificationEngine.Infrastructure.csproj", "NotificationEngine.Infrastructure/"]

RUN dotnet restore "NotificationEngine.Api/NotificationEngine.Api.csproj"

COPY . .
WORKDIR "/src/NotificationEngine.Api"
RUN dotnet build "NotificationEngine.Api.csproj" -c Release -o /app/build --no-restore

FROM build AS publish
RUN dotnet publish "NotificationEngine.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

# 2. Çalışma Zamanı (Runtime) Aşaması - .NET 10.0 ASP.NET
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Coolify healthcheck'i konteynerin içinden curl ile çalışır; aspnet imajında yok.
# Kod katmanından önce kuruluyor, böylece her kod değişikliğinde yeniden kurulmaz.
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "NotificationEngine.Api.dll"]