FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["source-code/InventoryService/InventoryService.csproj", "source-code/InventoryService/"]
RUN dotnet restore "source-code/InventoryService/InventoryService.csproj"
COPY ["source-code/InventoryService/", "source-code/InventoryService/"]
WORKDIR "/src/source-code/InventoryService"
RUN dotnet build "InventoryService.csproj" -c Release -o /app/build
RUN dotnet publish "InventoryService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "InventoryService.dll"]
