# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
       krb5-user libkrb5-3 libgssapi-krb5-2 ca-certificates \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Kopiuj pliki projektu dla lepszego cache'owania
COPY ["AITS.Api.csproj", "./"]
RUN dotnet restore "AITS.Api.csproj"

# Kopiuj resztę plików źródłowych (cache się zepsuje jeśli zmienią się pliki)
COPY . .

# Build i publish
RUN dotnet publish "AITS.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "AITS.Api.dll"]

