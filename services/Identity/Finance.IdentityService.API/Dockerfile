FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY shared/Finance.SharedKernel.Auth/ shared/Finance.SharedKernel.Auth/
COPY services/Identity/ services/Identity/

WORKDIR /src/services/Identity/Finance.IdentityService.API
RUN dotnet publish Finance.IdentityService.API.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Finance.IdentityService.API.dll"]
