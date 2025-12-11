FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["CharonDataProcessor.sln", "."]
COPY ["CharonDataProcessor/CharonDataProcessor.csproj", "CharonDataProcessor/"]

RUN dotnet restore "CharonDataProcessor/CharonDataProcessor.csproj"

COPY . .
WORKDIR "/src/CharonDataProcessor"
RUN dotnet build "CharonDataProcessor.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CharonDataProcessor.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN mkdir -p /app/logs

ENTRYPOINT ["dotnet", "CharonDataProcessor.dll"]

