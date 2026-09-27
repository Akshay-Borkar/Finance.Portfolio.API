FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Central Package Management: every csproj omits Version, so the restore fails without this.
COPY Directory.Packages.props ./
COPY shared/Finance.SharedKernel.Auth/ shared/Finance.SharedKernel.Auth/
COPY shared/Finance.SharedKernel.Logging/ shared/Finance.SharedKernel.Logging/
COPY shared/Finance.SharedKernel.Telemetry/ shared/Finance.SharedKernel.Telemetry/
COPY services/Sentiment/ services/Sentiment/

WORKDIR /src/services/Sentiment/Finance.SentimentService.API
RUN dotnet publish Finance.SentimentService.API.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/MLModel
COPY --from=build /src/services/Sentiment/Finance.SentimentService.Infrastructure/MLModel/sentiment_data.csv /app/MLModel/
ENTRYPOINT ["dotnet", "Finance.SentimentService.API.dll"]
