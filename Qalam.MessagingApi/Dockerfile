# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only this project's csproj and restore (no shared project refs)
COPY ["Qalam.MessagingApi/Qalam.MessagingApi.csproj", "Qalam.MessagingApi/"]
RUN dotnet restore "Qalam.MessagingApi/Qalam.MessagingApi.csproj"

# Copy project files and publish
COPY Qalam.MessagingApi/ Qalam.MessagingApi/
WORKDIR "/src/Qalam.MessagingApi"
RUN dotnet publish "Qalam.MessagingApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Final
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for healthcheck
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

EXPOSE 80

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Qalam.MessagingApi.dll"]
