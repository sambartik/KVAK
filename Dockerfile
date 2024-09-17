FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/KVAK.Core/KVAK.Core.csproj", "src/KVAK.Core/"]
COPY ["src/KVAK.Networking/KVAK.Networking.csproj", "src/KVAK.Networking/"]
RUN dotnet restore "src/KVAK.Core/KVAK.Core.csproj"
COPY . .
WORKDIR "/src/src/KVAK.Core"
RUN dotnet publish "KVAK.Core.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KVAK.Core.dll"]
