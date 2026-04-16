FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY OhbPortal.sln ./
COPY src/OhbPortal.Domain/OhbPortal.Domain.csproj src/OhbPortal.Domain/
COPY src/OhbPortal.Application/OhbPortal.Application.csproj src/OhbPortal.Application/
COPY src/OhbPortal.Infrastructure/OhbPortal.Infrastructure.csproj src/OhbPortal.Infrastructure/
COPY src/OhbPortal.Web/OhbPortal.Web.csproj src/OhbPortal.Web/
RUN dotnet restore

COPY . .
RUN dotnet publish src/OhbPortal.Web/OhbPortal.Web.csproj -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

# Uploads-Verzeichnis persistent (Railway Volumes mount hier)
RUN mkdir -p /app/wwwroot/uploads && chmod -R 775 /app/wwwroot/uploads

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "OhbPortal.Web.dll"]
