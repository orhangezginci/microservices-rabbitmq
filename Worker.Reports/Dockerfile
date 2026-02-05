# 1. Build-Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Kopiere die Projektdatei und stelle Abhängigkeiten wieder her
# Der Stern sorgt dafür, dass er die .csproj findet, egal wie sie heißt
COPY *.csproj ./
RUN dotnet restore

# Kopiere den restlichen Quellcode
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# 2. Runtime-Stage
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Erstelle den Ordner, in dem die CSV-Dateien landen sollen
RUN mkdir -p /app/reports

ENTRYPOINT ["dotnet", "Worker.Reports.dll"]