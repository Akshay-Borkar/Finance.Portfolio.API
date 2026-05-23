using Finance.IdentityService.Application.Exceptions;
using Finance.IdentityService.Application.Models;
using Finance.IdentityService.Domain;
using Finance.IdentityService.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;
using Reqnroll;

namespace Finance.IdentityService.Tests.StepDefinitions;

[Binding]
public class RegistrationSteps
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

    private RegistrationResponse? _response;
    private Exception? _thrownException;
    private ApplicationUser? _capturedUser;
    private string _capturedRole = string.Empty;

    public RegistrationSteps()
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

    private void SetupSuccessfulCreate(string? password = null)
    {
        var setup = password is null
            ? _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            : _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), password));

        setup.Callback<ApplicationUser, string>((u, _) => _capturedUser = u)
             .ReturnsAsync(IdentityResult.Success);

        _userManager
            .Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .Callback<ApplicationUser, string>((_, r) => _capturedRole = r)
            .ReturnsAsync(IdentityResult.Success);
    }

    [Given(@"the identity system rejects the password with error ""(.*)""")]
    public void GivenIdentityRejectsPassword(string errorMessage)
    {
        _userManager
            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = errorMessage }));
    }

    [When(@"I register with first name ""(.*)"" last name ""(.*)"" username ""(.*)"" email ""(.*)"" password ""(.*)"" and role ""(.*)""")]
    public async Task WhenIRegister(string firstName, string lastName, string username, string email, string password, string role)
    {
        SetupSuccessfulCreate();
        _response = await BuildService().Register(new RegistrationRequest
        {
            FirstName = firstName,
            LastName = lastName,
            UserName = username,
            Email = email,
            Password = password,
            Role = role
        });
    }

    [When(@"I try to register with username ""(.*)"" email ""(.*)"" password ""(.*)"" and role ""(.*)""")]
    public async Task WhenITryToRegister(string username, string email, string password, string role)
    {
        try
        {
            await BuildService().Register(new RegistrationRequest
            {
                FirstName = "Test",
                LastName = "User",
                UserName = username,
                Email = email,
                Password = password,
                Role = role
            });
        }
        catch (Exception ex)
        {
            _thrownException = ex;
        }
    }

    [Then(@"the registration should succeed")]
    public void ThenRegistrationSucceeds() =>
        _response.Should().NotBeNull();

    [Then(@"the returned user id should not be empty")]
    public void ThenReturnedUserIdNotEmpty() =>
        _response!.UserId.Should().NotBeNullOrEmpty();

    [Then(@"the created user should have email confirmed set to true")]
    public void ThenEmailConfirmedIsTrue() =>
        _capturedUser!.EmailConfirmed.Should().BeTrue();

    [Then(@"a bad request error should be raised during registration")]
    public void ThenBadRequestDuringRegistration() =>
        _thrownException.Should().BeOfType<BadRequestException>();

    [Then(@"the error should mention ""(.*)""")]
    public void ThenErrorMentions(string text) =>
        _thrownException!.Message.Should().Contain(text);

    [Then(@"the user should be added to the role ""(.*)""")]
    public void ThenUserAddedToRole(string role) =>
        _capturedRole.Should().Be(role);
}
