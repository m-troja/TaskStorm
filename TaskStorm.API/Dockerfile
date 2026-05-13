FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["TaskStorm.API/TaskStorm.API.csproj", "TaskStorm.API/"]
RUN dotnet restore "TaskStorm.API/TaskStorm.API.csproj"

COPY . .
WORKDIR "/src/TaskStorm.API"
RUN dotnet publish "TaskStorm.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

RUN mkdir -p /app/logs /app/uploads \
    && chown -R $APP_UID:$APP_UID /app/logs /app/uploads

USER $APP_UID
EXPOSE 6901

ENTRYPOINT ["dotnet", "TaskStorm.API.dll"]