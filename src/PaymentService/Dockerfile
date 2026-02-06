FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["PaymentService/PaymentService.csproj", "PaymentService/"]
RUN dotnet restore "PaymentService/PaymentService.csproj"

COPY PaymentService/ PaymentService/
RUN dotnet publish "PaymentService/PaymentService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "PaymentService.dll"]
