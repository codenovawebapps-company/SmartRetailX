FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["source-code/UserService/UserService.csproj", "source-code/UserService/"]
RUN dotnet restore "source-code/UserService/UserService.csproj"
COPY ["source-code/UserService/", "source-code/UserService/"]
WORKDIR "/src/source-code/UserService"
RUN dotnet build "UserService.csproj" -c Release -o /app/build
RUN dotnet publish "UserService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "UserService.dll"]
