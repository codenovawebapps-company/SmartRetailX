FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["source-code/NotificationService/NotificationService.csproj", "source-code/NotificationService/"]
RUN dotnet restore "source-code/NotificationService/NotificationService.csproj"
COPY ["source-code/NotificationService/", "source-code/NotificationService/"]
WORKDIR "/src/source-code/NotificationService"
RUN dotnet build "NotificationService.csproj" -c Release -o /app/build
RUN dotnet publish "NotificationService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NotificationService.dll"]
