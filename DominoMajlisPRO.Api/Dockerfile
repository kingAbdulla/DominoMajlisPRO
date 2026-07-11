FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY DominoMajlisPRO.Api/DominoMajlisPRO.Api.csproj DominoMajlisPRO.Api/
RUN dotnet restore DominoMajlisPRO.Api/DominoMajlisPRO.Api.csproj
COPY DominoMajlisPRO.Api/ DominoMajlisPRO.Api/
RUN dotnet publish DominoMajlisPRO.Api/DominoMajlisPRO.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "DominoMajlisPRO.Api.dll"]
