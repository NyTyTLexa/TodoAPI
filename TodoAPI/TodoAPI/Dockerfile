# См. статью по ссылке https://aka.ms/customizecontainer,
# чтобы узнать как настроить контейнер отладки и как Visual Studio использует этот Dockerfile для ускорения отладки.

# --- Stage 1: базовый runtime (используется в VS при Fast Mode Debug) ---
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# --- Stage 2: build ---
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG BUILD_CONFIGURATION=Debug
WORKDIR /src

# Копируем csproj и восстанавливаем зависимости
COPY ["TodoAPI.csproj", "./"]
RUN dotnet restore "./TodoAPI.csproj"

# Копируем всё остальное и собираем
COPY . .
WORKDIR "/src"
RUN dotnet build "TodoAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

# --- Stage 3: publish ---
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "TodoAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# --- Stage 4: final runtime ---
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TodoAPI.dll"]
