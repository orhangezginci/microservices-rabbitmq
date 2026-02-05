FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# Wir kopieren das Projekt und stellen die Pakete wieder her
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
# Wichtig für Docker: Auf Port 8080 lauschen
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Core.Api.dll"]