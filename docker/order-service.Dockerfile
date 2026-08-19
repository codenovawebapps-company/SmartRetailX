FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["source-code/OrderService/OrderService.csproj", "source-code/OrderService/"]
RUN dotnet restore "source-code/OrderService/OrderService.csproj"
COPY ["source-code/OrderService/", "source-code/OrderService/"]
WORKDIR "/src/source-code/OrderService"
RUN dotnet build "OrderService.csproj" -c Release -o /app/build
RUN dotnet publish "OrderService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OrderService.dll"]
