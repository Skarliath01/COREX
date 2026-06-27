namespace Corex.Engine.Wmi;

public interface IWmiQueryExecutor
{
    IEnumerable<IReadOnlyDictionary<string, object?>> Execute(
        string wmiClass,
        string[] properties,
        string? condition = null,
        string namespacePath = @"root\cimv2");
}
