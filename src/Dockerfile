FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["CharonDataProcessor/src/CharonDataProcessor/CharonDataProcessor.csproj", "CharonDataProcessor/src/CharonDataProcessor/"]

COPY ["nuget-packages/", "/nuget-packages/"]

RUN if [ -d "/nuget-packages" ] && [ "$(ls -A /nuget-packages/*.nupkg 2>/dev/null)" ]; then \
        dotnet nuget add source /nuget-packages --name local-packages; \
    fi

RUN dotnet restore "CharonDataProcessor/src/CharonDataProcessor/CharonDataProcessor.csproj"

COPY ["CharonDataProcessor/src/CharonDataProcessor/", "CharonDataProcessor/src/CharonDataProcessor/"]

WORKDIR "/src/CharonDataProcessor/src/CharonDataProcessor"
RUN dotnet build "CharonDataProcessor.csproj" -c Release -o /app/build

FROM build AS publish
WORKDIR "/src/CharonDataProcessor/src/CharonDataProcessor"
RUN dotnet publish "CharonDataProcessor.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN mkdir -p /app/logs

ENTRYPOINT ["dotnet", "CharonDataProcessor.dll"]

