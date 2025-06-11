FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["HabitHub/HabitHub.csproj", "HabitHub/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["Persistence/Persistence.csproj", "Persistence/"]
RUN dotnet restore "HabitHub/HabitHub.csproj"
COPY . .
WORKDIR "/src/HabitHub"
RUN dotnet build "HabitHub.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "HabitHub.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HabitHub.dll"]
