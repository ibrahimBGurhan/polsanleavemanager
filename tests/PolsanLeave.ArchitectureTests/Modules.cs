using System.Reflection;

namespace PolsanLeave.ArchitectureTests;

/// <summary>Module map from docs/architecture.md — the single place the tests learn it from.</summary>
internal static class Modules
{
    public const string Prefix = "PolsanLeave";

    public static readonly string[] Names = ["Calendar", "Identity", "Balance", "Leave"];

    /// <summary>FD-4: allowed module dependency directions (Contracts only, FD-1).</summary>
    public static readonly IReadOnlyDictionary<string, string[]> AllowedDependencies = new Dictionary<string, string[]>
    {
        ["Leave"] = ["Balance", "Calendar", "Identity"],
        ["Balance"] = ["Calendar", "Identity"],
        ["Identity"] = [],
        ["Calendar"] = [],
    };

    public static Assembly Implementation(string module) => Assembly.Load($"{Prefix}.{module}");

    public static Assembly Contracts(string module) => Assembly.Load($"{Prefix}.{module}.Contracts");

    public static Assembly SharedKernel => Assembly.Load($"{Prefix}.SharedKernel");

    public static Assembly Web => Assembly.Load($"{Prefix}.Web");

    public static IEnumerable<Assembly> Production =>
        Names.SelectMany(m => new[] { Implementation(m), Contracts(m) }).Append(SharedKernel).Append(Web);

    public static IEnumerable<string> ReferencedProjectAssemblies(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(n => n.StartsWith(Prefix + ".", StringComparison.Ordinal));

    public static IEnumerable<object[]> ModuleNames => Names.Select(n => new object[] { n });
}
