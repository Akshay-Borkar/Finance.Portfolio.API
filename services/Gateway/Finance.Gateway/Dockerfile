FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy shared projects first (referenced by gateway)
COPY shared/Finance.SharedKernel.Auth/ shared/Finance.SharedKernel.Auth/
COPY services/Gateway/Finance.Gateway/ services/Gateway/Finance.Gateway/

WORKDIR /src/services/Gateway/Finance.Gateway
RUN dotnet publish Finance.Gateway.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Finance.Gateway.dll"]
