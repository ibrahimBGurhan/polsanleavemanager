using Mono.Cecil;
using Mono.Cecil.Cil;
using NetArchTest.Rules;

namespace PolsanLeave.ArchitectureTests;

/// <summary>Forbidden dependencies FD-1 … FD-6 (docs/architecture.md).</summary>
public class ModuleBoundaryTests
{
    [Theory]
    [MemberData(nameof(Modules.ModuleNames), MemberType = typeof(Modules))]
    public void FD1_Module_ReferencesOtherModulesOnlyThroughContracts(string module)
    {
        var referenced = Modules.ReferencedProjectAssemblies(Modules.Implementation(module));
        var otherImplementations = Modules.Names.Where(m => m != module).Select(m => $"{Modules.Prefix}.{m}");

        Assert.Empty(referenced.Intersect(otherImplementations));
    }

    [Theory]
    [MemberData(nameof(Modules.ModuleNames), MemberType = typeof(Modules))]
    public void FD1_Module_DoesNotUseOtherModulesInternals(string module)
    {
        var otherInternals = Modules.Names.Where(m => m != module)
            .Select(m => $"{Modules.Prefix}.{m}.Internal").ToArray();

        var result = Types.InAssembly(Modules.Implementation(module))
            .ShouldNot().HaveDependencyOnAny(otherInternals)
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Theory]
    [MemberData(nameof(Modules.ModuleNames), MemberType = typeof(Modules))]
    public void FD1_Contracts_DoNotReferenceAnyImplementation(string module)
    {
        var referenced = Modules.ReferencedProjectAssemblies(Modules.Contracts(module));
        var implementations = Modules.Names.Select(m => $"{Modules.Prefix}.{m}").Append($"{Modules.Prefix}.Web");

        Assert.Empty(referenced.Intersect(implementations));
    }

    [Theory]
    [MemberData(nameof(Modules.ModuleNames), MemberType = typeof(Modules))]
    public void FD2_Domain_IsFrameworkFree(string module)
    {
        var result = Types.InAssembly(Modules.Implementation(module))
            .That().ResideInNamespaceContaining(".Internal.Domain")
            .ShouldNot().HaveDependencyOnAny("Microsoft.AspNetCore", "Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void FD3_Web_DoesNotUseDataAccessOrModuleInternals()
    {
        var forbidden = Modules.Names.Select(m => $"{Modules.Prefix}.{m}.Internal")
            .Append("Microsoft.EntityFrameworkCore").ToArray();

        var result = Types.InAssembly(Modules.Web)
            .ShouldNot().HaveDependencyOnAny(forbidden)
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Theory]
    [MemberData(nameof(Modules.ModuleNames), MemberType = typeof(Modules))]
    public void FD4_Module_DependsOnlyInAllowedDirections(string module)
    {
        var allowed = Modules.AllowedDependencies[module].Append(module)
            .Select(m => $"{Modules.Prefix}.{m}.Contracts");
        var referencedContracts = Modules.ReferencedProjectAssemblies(Modules.Implementation(module))
            .Where(n => n.EndsWith(".Contracts", StringComparison.Ordinal));

        Assert.Empty(referencedContracts.Except(allowed));
    }

    [Fact]
    public void FD5_Production_NeverReadsSystemClockDirectly()
    {
        string[] banned =
        [
            "System.DateTime System.DateTime::get_Now()",
            "System.DateTime System.DateTime::get_UtcNow()",
            "System.DateTime System.DateTime::get_Today()",
            "System.DateTimeOffset System.DateTimeOffset::get_Now()",
            "System.DateTimeOffset System.DateTimeOffset::get_UtcNow()",
        ];

        var violations = new List<string>();
        foreach (var assembly in Modules.Production)
        {
            using var definition = AssemblyDefinition.ReadAssembly(assembly.Location);
            var methods = definition.MainModule.GetTypes()
                .SelectMany(t => t.Methods)
                .Where(m => m.HasBody);
            foreach (var method in methods)
            {
                violations.AddRange(method.Body.Instructions
                    .Where(i => i.OpCode.FlowControl == FlowControl.Call && i.Operand is MethodReference r && banned.Contains(r.FullName))
                    .Select(i => $"{method.FullName} -> {((MethodReference)i.Operand).FullName}"));
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void FD6_Production_ContainsNoAuthenticationHandlerOrTestInfrastructure()
    {
        foreach (var assembly in Modules.Production)
        {
            Assert.DoesNotContain($"{Modules.Prefix}.TestInfrastructure", Modules.ReferencedProjectAssemblies(assembly));

            var result = Types.InAssembly(assembly)
                .ShouldNot().Inherit(typeof(Microsoft.AspNetCore.Authentication.AuthenticationHandler<>))
                .GetResult();
            Assert.True(result.IsSuccessful, Describe(result));
        }
    }

    private static string Describe(NetArchTest.Rules.TestResult result) =>
        "Violating types: " + string.Join(", ", result.FailingTypeNames ?? []);
}
