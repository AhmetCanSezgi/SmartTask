FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["SmartTask.API/SmartTask.API.csproj", "SmartTask.API/"]
COPY ["SmartTask.Application/SmartTask.Application.csproj", "SmartTask.Application/"]
COPY ["SmartTask.Domain/SmartTask.Domain.csproj", "SmartTask.Domain/"]
COPY ["SmartTask.Infrastructure/SmartTask.Infrastructure.csproj", "SmartTask.Infrastructure/"]
RUN dotnet restore "SmartTask.API/SmartTask.API.csproj"
COPY . .
RUN dotnet build "SmartTask.API/SmartTask.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SmartTask.API/SmartTask.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SmartTask.API.dll"]
