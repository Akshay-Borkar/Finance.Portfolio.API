FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Central Package Management plus the shared TargetFramework/analyzer settings live at the repo
# root, and the build context does not include root files unless they are copied explicitly.
COPY Directory.Build.props Directory.Packages.props ./
COPY shared/Finance.Contracts/ shared/Finance.Contracts/
COPY shared/Finance.Integrations.MarketAux/ shared/Finance.Integrations.MarketAux/
COPY shared/Finance.SharedKernel.Auth/ shared/Finance.SharedKernel.Auth/
COPY shared/Finance.SharedKernel.Logging/ shared/Finance.SharedKernel.Logging/
COPY shared/Finance.SharedKernel.Messaging/ shared/Finance.SharedKernel.Messaging/
COPY shared/Finance.SharedKernel.Telemetry/ shared/Finance.SharedKernel.Telemetry/
COPY services/Agent/ services/Agent/

WORKDIR /src/services/Agent/Finance.AgentService.API
RUN dotnet publish Finance.AgentService.API.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Finance.AgentService.API.dll"]
