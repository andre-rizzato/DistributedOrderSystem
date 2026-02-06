FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["CustomerService/CustomerService.csproj", "CustomerService/"]
RUN dotnet restore "CustomerService/CustomerService.csproj"

COPY CustomerService/ CustomerService/
RUN dotnet publish "CustomerService/CustomerService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "CustomerService.dll"]
