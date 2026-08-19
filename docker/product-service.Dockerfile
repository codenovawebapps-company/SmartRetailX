FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["source-code/ProductService/ProductService.csproj", "source-code/ProductService/"]
RUN dotnet restore "source-code/ProductService/ProductService.csproj"
COPY ["source-code/ProductService/", "source-code/ProductService/"]
WORKDIR "/src/source-code/ProductService"
RUN dotnet build "ProductService.csproj" -c Release -o /app/build
RUN dotnet publish "ProductService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ProductService.dll"]
