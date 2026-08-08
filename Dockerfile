# Define base image for runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# Define base image for build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["HaveTickets.Api/HaveTickets.Api.csproj", "HaveTickets.Api/"]
COPY ["HaveTickets.Application/HaveTickets.Application.csproj", "HaveTickets.Application/"]
COPY ["HaveTickets.Domain/HaveTickets.Domain.csproj", "HaveTickets.Domain/"]
COPY ["HaveTickets.Infrastructure/HaveTickets.Infrastructure.csproj", "HaveTickets.Infrastructure/"]
RUN dotnet restore "HaveTickets.Api/HaveTickets.Api.csproj"

# Copy all source code and build
COPY . .
WORKDIR "/src/HaveTickets.Api"
RUN dotnet build "HaveTickets.Api.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "HaveTickets.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build the final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HaveTickets.Api.dll"]
