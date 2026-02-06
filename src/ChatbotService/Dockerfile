FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["ChatbotService/ChatbotService.csproj", "ChatbotService/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "ChatbotService/ChatbotService.csproj"

COPY ChatbotService/ ChatbotService/
COPY Shared/ Shared/
RUN dotnet publish "ChatbotService/ChatbotService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "ChatbotService.dll"]
