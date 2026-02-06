FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["NotificationService/NotificationService.csproj", "NotificationService/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "NotificationService/NotificationService.csproj"

COPY NotificationService/ NotificationService/
COPY Shared/ Shared/
RUN dotnet publish "NotificationService/NotificationService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "NotificationService.dll"]
