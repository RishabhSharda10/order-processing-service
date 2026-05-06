FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/OrderProcessingService.Api/OrderProcessingService.Api.csproj src/OrderProcessingService.Api/
RUN dotnet restore src/OrderProcessingService.Api/OrderProcessingService.Api.csproj

COPY src/OrderProcessingService.Api/ src/OrderProcessingService.Api/
RUN dotnet publish src/OrderProcessingService.Api/OrderProcessingService.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "OrderProcessingService.Api.dll"]
