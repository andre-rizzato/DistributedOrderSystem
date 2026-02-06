FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["InventoryService/InventoryService.csproj", "InventoryService/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "InventoryService/InventoryService.csproj"

COPY InventoryService/ InventoryService/
COPY Shared/ Shared/
RUN dotnet publish "InventoryService/InventoryService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "InventoryService.dll"]
