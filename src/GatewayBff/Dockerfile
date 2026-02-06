FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["GatewayBff/GatewayBff.csproj", "GatewayBff/"]
RUN dotnet restore "GatewayBff/GatewayBff.csproj"

COPY GatewayBff/ GatewayBff/
RUN dotnet publish "GatewayBff/GatewayBff.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "GatewayBff.dll"]
