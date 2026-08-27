using Mediator;
using Microsoft.Extensions.DependencyInjection;
using UltimateSolution.Application.DependencyInjection;
using UltimateSolution.Application.Features.Identity;

namespace UltimateSolution.Application.Tests;

public sealed class ApplicationServiceCollectionExtensionsTests
{
    [Fact]
    public void AddApplicationReturnsTheSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddApplication();

        Assert.Same(services, result);
    }

    [Fact]
    public void AddApplicationRegistersMediatorHandlersAsScoped()
    {
        var services = new ServiceCollection();
        _ = services.AddApplication();

        var descriptor = Assert.Single(
            services,
            candidate => candidate.ServiceType == typeof(IRequestHandler<RegisterUserCommand, AuthTokenResponse>));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }
}
