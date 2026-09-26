FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["BookingWeb.Api/BookingWeb.Api.csproj", "BookingWeb.Api/"]
COPY ["BookingWeb.Application/BookingWeb.Application.csproj", "BookingWeb.Application/"]
COPY ["BookingWeb.Domain/BookingWeb.Domain.csproj", "BookingWeb.Domain/"]
COPY ["BookingWeb.Infrastructure/BookingWeb.Infrastructure.csproj", "BookingWeb.Infrastructure/"]
RUN dotnet restore "BookingWeb.Api/BookingWeb.Api.csproj"
COPY . .
WORKDIR "/src/BookingWeb.Api"
RUN dotnet build "./BookingWeb.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BookingWeb.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BookingWeb.Api.dll"]
