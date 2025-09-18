# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

COPY DartsScoring.sln ./
COPY DartsScoring.Api/DartsScoring.Api.csproj DartsScoring.Api/
COPY DartsScoring.Tests/DartsScoring.Tests.csproj DartsScoring.Tests/
RUN dotnet restore

COPY . .
RUN dotnet publish DartsScoring.Api/DartsScoring.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:5200
EXPOSE 5200

ENTRYPOINT ["dotnet", "DartsScoring.Api.dll"]
