FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["source-code/PaymentService/PaymentService.csproj", "source-code/PaymentService/"]
RUN dotnet restore "source-code/PaymentService/PaymentService.csproj"
COPY ["source-code/PaymentService/", "source-code/PaymentService/"]
WORKDIR "/src/source-code/PaymentService"
RUN dotnet build "PaymentService.csproj" -c Release -o /app/build
RUN dotnet publish "PaymentService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PaymentService.dll"]
