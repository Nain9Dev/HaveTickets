using NetArchTest.Rules;
using Xunit;

namespace HaveTickets.Api.Tests;

public sealed class ArchitectureTests
{
    private static readonly System.Reflection.Assembly DomainAssembly = typeof(HaveTickets.Domain.Ticketing.Ticket).Assembly;
    private static readonly System.Reflection.Assembly ApplicationAssembly = typeof(HaveTickets.Application.DependencyInjection).Assembly;
    private static readonly System.Reflection.Assembly InfrastructureAssembly = typeof(HaveTickets.Infrastructure.DependencyInjection).Assembly;

    [Fact]
    public void Domain_Should_Not_Have_Dependency_On_Other_Projects()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "HaveTickets.Application",
                "HaveTickets.Infrastructure",
                "HaveTickets.Api",
                "HaveTickets.Client",
                "HaveTickets.WebUI")
            .GetResult();

        Assert.True(result.IsSuccessful, $"Domain layer has illegal dependencies on outer layers: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Application_Should_Not_Have_Dependency_On_Infrastructure_Or_Api()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "HaveTickets.Infrastructure",
                "HaveTickets.Api",
                "HaveTickets.Client",
                "HaveTickets.WebUI")
            .GetResult();

        Assert.True(result.IsSuccessful, $"Application layer has illegal dependencies: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Infrastructure_Should_Not_Have_Dependency_On_Api_Or_Clients()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "HaveTickets.Api",
                "HaveTickets.Client",
                "HaveTickets.WebUI")
            .GetResult();

        Assert.True(result.IsSuccessful, $"Infrastructure layer has illegal dependencies: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Domain_Entities_Should_Not_Depend_On_EntityFrameworkCore()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespace("HaveTickets.Domain")
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, $"Domain entities must not depend on EF Core: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
