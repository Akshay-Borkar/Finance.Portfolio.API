namespace Finance.PortfolioService.Infrastructure.AI;

public class AzureSearchSettings
{
    public string Endpoint { get; set; } = string.Empty;
    public string AdminKey { get; set; } = string.Empty;
    public string IndexName { get; set; } = "financial-documents";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Endpoint) &&
        !string.IsNullOrWhiteSpace(AdminKey);
}
