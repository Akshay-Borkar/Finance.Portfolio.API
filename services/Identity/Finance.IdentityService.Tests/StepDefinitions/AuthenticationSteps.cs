using Finance.IdentityService.Application.Exceptions;
using Finance.IdentityService.Application.Models;
using Finance.IdentityService.Domain;
using Finance.IdentityService.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;
using Reqnroll;
using System.IdentityModel.Tokens.Jwt;

namespace Finance.IdentityService.Tests.StepDefinitions;

[Binding]
public class AuthenticationSteps
{
    private readonly Mock<UserManager<ApplicationUser>> _userManager;
    private readonly Mock<SignInManager<ApplicationUser>> _signInManager;

    private readonly JwtSettings _jwtSettings = new()
    {
        Key = "supersecretkey1234567890abcdefghijk",
        Issuer = "TestIssuer",
        Audience = "TestAudience",
        DurationInMinutes = 60
    };

    private ApplicationUser? _user;
    private AuthResponse? _authResponse;
    private Exception? _thrownException;
    private readonly List<string> _roles = [];

    public AuthenticationSteps()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var contextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        _signInManager = new Mock<SignInManager<ApplicationUser>>(
            _userManager.Object, contextAccessor.Object, claimsFactory.Object, null!, null!, null!, null!);
    }

    private AuthService BuildService() =>
        new(_userManager.Object, _signInManager.Object, Options.Create(_jwtSettings));

    [Given(@"a registered user with username ""(.*)"" and email ""(.*)""")]
    public void GivenRegisteredUser(string username, string email)
    {
        _user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = username,
            Email = email,
            FirstName = "Test",
            LastName = "User"
        };
        _userManager.Setup(m => m.FindByNameAsync(username)).ReturnsAsync(_user);
    }

    [Given(@"the password ""(.*)"" is correct for that user")]
    public void GivenPasswordIsCorrect(string password)
    {
        _userManager.Setup(m => m.GetClaimsAsync(_user!)).ReturnsAsync([]);
        _userManager.Setup(m => m.GetRolesAsync(_user!)).ReturnsAsync(_roles);
        _signInManager
            .Setup(m => m.CheckPasswordSignInAsync(_user!, password, false))
            .ReturnsAsync(SignInResult.Success);
    }

    [Given(@"the password ""(.*)"" is incorrect for that user")]
    public void GivenPasswordIsIncorrect(string password) =>
        _signInManager
            .Setup(m => m.CheckPasswordSignInAsync(_user!, password, false))
            .ReturnsAsync(SignInResult.Failed);

    [Given(@"no user exists with username ""(.*)""")]
    public void GivenNoUserExists(string username) =>
        _userManager.Setup(m => m.FindByNameAsync(username)).ReturnsAsync((ApplicationUser?)null);

    [Given(@"the user has the role ""(.*)""")]
    public void GivenUserHasRole(string role)
    {
        _roles.Add(role);
        _userManager.Setup(m => m.GetRolesAsync(_user!)).ReturnsAsync(_roles);
    }

    [When(@"I log in with username ""(.*)"" and password ""(.*)""")]
    public async Task WhenILogIn(string username, string password)
    {
        _authResponse = await BuildService().Login(new AuthRequest { UserName = username, Password = password });
    }

    [When(@"I try to log in with username ""(.*)"" and password ""(.*)""")]
    public async Task WhenITryToLogIn(string username, string password)
    {
        try { await BuildService().Login(new AuthRequest { UserName = username, Password = password }); }
        catch (Exception ex) { _thrownException = ex; }
    }

    [Then(@"a JWT token should be returned")]
    public void ThenJwtTokenReturned() =>
        _authResponse!.Token.Should().NotBeNullOrEmpty();

    [Then(@"the response username should be ""(.*)""")]
    public void ThenResponseUsername(string username) =>
        _authResponse!.UserName.Should().Be(username);

    [Then(@"the response email should be ""(.*)""")]
    public void ThenResponseEmail(string email) =>
        _authResponse!.Email.Should().Be(email);

    [Then(@"the response user id should not be empty")]
    public void ThenResponseUserIdNotEmpty() =>
        _authResponse!.Id.Should().NotBeNullOrEmpty();

    [Then(@"a not found error should be raised during login")]
    public void ThenNotFoundErrorDuringLogin() =>
        _thrownException.Should().BeOfType<NotFoundException>();

    [Then(@"a bad request error should be raised during login with message ""(.*)""")]
    public void ThenBadRequestDuringLogin(string message) =>
        _thrownException.Should().BeOfType<BadRequestException>()
            .Which.Message.Should().Contain(message);

    [Then(@"the JWT token should contain a ""(.*)"" claim with the user's id")]
    public void ThenTokenContainsUidClaim(string claimType)
    {
        var token = new JwtSecurityTokenHandler().ReadJwtToken(_authResponse!.Token);
        token.Claims.Should().Contain(c => c.Type == claimType && c.Value == _user!.Id);
    }

    [Then(@"the JWT token should contain the role ""(.*)""")]
    public void ThenTokenContainsRole(string role)
    {
        var token = new JwtSecurityTokenHandler().ReadJwtToken(_authResponse!.Token);
        token.Claims.Should().Contain(c => c.Value == role);
    }
}
