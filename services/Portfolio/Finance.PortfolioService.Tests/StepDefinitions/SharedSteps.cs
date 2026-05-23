using Finance.PortfolioService.Tests.Support;
using Reqnroll;

namespace Finance.PortfolioService.Tests.StepDefinitions;

[Binding]
public class SharedSteps
{
    private readonly PortfolioTestContext _ctx;

    public SharedSteps(PortfolioTestContext ctx) => _ctx = ctx;

    [Given(@"a user with id ""(.*)""")]
    public void GivenAUserWithId(string userId) =>
        _ctx.UserId = Guid.Parse(userId);

    [Given(@"a sector with id ""(.*)""")]
    public void GivenASectorWithId(string sectorId) =>
        _ctx.SectorId = Guid.Parse(sectorId);
}
