using Microsoft.Extensions.DependencyInjection;
using UltimateSolution.Application.DependencyInjection;

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
}
