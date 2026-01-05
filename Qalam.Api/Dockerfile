# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file
COPY ["Qalam.sln", "."]

# Copy csproj files and restore dependencies
COPY ["Qalam.Api/Qalam.Api.csproj", "Qalam.Api/"]
COPY ["Qalam.Core/Qalam.Core.csproj", "Qalam.Core/"]
COPY ["Qalam.Data/Qalam.Data.csproj", "Qalam.Data/"]
COPY ["Qalam.Infrastructure/Qalam.Infrastructure.csproj", "Qalam.Infrastructure/"]
COPY ["Qalam.Service/Qalam.Service.csproj", "Qalam.Service/"]

RUN dotnet restore "Qalam.Api/Qalam.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/Qalam.Api"
RUN dotnet build "Qalam.Api.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "Qalam.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Final
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Qalam.Api.dll"]

