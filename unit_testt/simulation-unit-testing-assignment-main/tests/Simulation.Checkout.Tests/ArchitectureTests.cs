
using FluentAssertions;
using NetArchTest.Rules;
using Simulation.Checkout.Domain;
using Xunit;

namespace Simulation.Checkout.Tests;

public class ArchitectureTests
{
    [Fact]
    public void Domain_ShouldNotDependOnApplication()
    {
        var result = Types
            .InAssembly(typeof(Order).Assembly)
            .That()
            .ResideInNamespace("Simulation.Checkout.Domain")
            .ShouldNot()
            .HaveDependencyOn("Simulation.Checkout.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Domain_ShouldNotDependOnInfrastructure()
    {
        var result = Types
            .InAssembly(typeof(Order).Assembly)
            .That()
            .ResideInNamespace("Simulation.Checkout.Domain")
            .ShouldNot()
            .HaveDependencyOn("Simulation.Checkout.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}