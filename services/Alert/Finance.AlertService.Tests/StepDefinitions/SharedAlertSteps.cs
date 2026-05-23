using Finance.AlertService.Tests.Support;
using Reqnroll;

namespace Finance.AlertService.Tests.StepDefinitions;

[Binding]
public class SharedAlertSteps
{
    private readonly AlertTestContext _ctx;

    public SharedAlertSteps(AlertTestContext ctx) => _ctx = ctx;

    [Given(@"an alert user with id ""(.*)""")]
    public void GivenAlertUserId(string userId) =>
        _ctx.UserId = Guid.Parse(userId);

    [Given(@"the user's email is ""(.*)""")]
    public void GivenUserEmail(string email) =>
        _ctx.UserEmail = email;
}
