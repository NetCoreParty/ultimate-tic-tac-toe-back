#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj files first for better layer caching
COPY ["src/UltimateTicTacToe.Api/UltimateTicTacToe.API.csproj", "src/UltimateTicTacToe.Api/"]
COPY ["src/UltimateTicTacToe.Core/UltimateTicTacToe.Core.csproj", "src/UltimateTicTacToe.Core/"]
COPY ["src/UltimateTicTacToe.Storage/UltimateTicTacToe.Storage.csproj", "src/UltimateTicTacToe.Storage/"]

RUN dotnet restore "src/UltimateTicTacToe.Api/UltimateTicTacToe.API.csproj"

COPY . .
WORKDIR "/src/src/UltimateTicTacToe.Api"
RUN dotnet build "UltimateTicTacToe.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "UltimateTicTacToe.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UltimateTicTacToe.API.dll"]