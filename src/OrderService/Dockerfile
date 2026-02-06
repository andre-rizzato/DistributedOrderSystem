FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["OrderService/OrderService.csproj", "OrderService/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "OrderService/OrderService.csproj"

COPY OrderService/ OrderService/
COPY Shared/ Shared/
RUN dotnet publish "OrderService/OrderService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "OrderService.dll"]
