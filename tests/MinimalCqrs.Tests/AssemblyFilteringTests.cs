using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MinimalCqrs.Tests;

/// <summary>
/// Verifies that the assembly exclusion prefix list prevents GetTypes() from being called
/// on framework/tooling assemblies that may not be fully loadable at runtime
/// (e.g. Microsoft.EntityFrameworkCore.Design during ef migrations bundle).
/// </summary>
public class AssemblyFilteringTests
{
    [Fact]
    public void System_CoreLib_matches_exclusion_prefix()
    {
        var coreLib = typeof(object).Assembly; // System.Private.CoreLib
        Assert.Contains(MainExtensions._assemblyExclusions, p => coreLib.FullName!.StartsWith(p));
    }

    [Fact]
    public void Microsoft_assembly_matches_exclusion_prefix()
    {
        var msAssembly = typeof(IServiceCollection).Assembly; // Microsoft.Extensions.DependencyInjection.Abstractions
        Assert.Contains(MainExtensions._assemblyExclusions, p => msAssembly.FullName!.StartsWith(p));
    }

    [Fact]
    public void User_assembly_is_not_excluded()
    {
        var testAssembly = typeof(AssemblyFilteringTests).Assembly;
        Assert.DoesNotContain(MainExtensions._assemblyExclusions, p => testAssembly.FullName!.StartsWith(p));
    }

    [Fact]
    public void AddMinimalCqrs_does_not_throw_when_framework_assemblies_are_loaded()
    {
        // Regression test: before prefix filtering, GetTypes() on assemblies like
        // Microsoft.EntityFrameworkCore.Design threw ReflectionTypeLoadException.
        var services = new ServiceCollection();
        var ex = Record.Exception(() => services.AddMinimalCqrs());
        Assert.Null(ex);
    }
}
