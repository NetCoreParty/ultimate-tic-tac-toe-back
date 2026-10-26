FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/UltimateTicTacToe.Api/UltimateTicTacToe.API.csproj", "src/UltimateTicTacToe.Api/"]
COPY ["src/UltimateTicTacToe.Core/UltimateTicTacToe.Core.csproj", "src/UltimateTicTacToe.Core/"]
COPY ["src/UltimateTicTacToe.Storage/UltimateTicTacToe.Storage.csproj", "src/UltimateTicTacToe.Storage/"]

RUN dotnet restore "src/UltimateTicTacToe.Api/UltimateTicTacToe.API.csproj"

COPY . .
WORKDIR "/src/src/UltimateTicTacToe.Api"
RUN dotnet publish "UltimateTicTacToe.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "UltimateTicTacToe.API.dll"]
