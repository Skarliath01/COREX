using System.Management;
using System.Runtime.Versioning;
using ManagementEnumerationOptions = System.Management.EnumerationOptions;

namespace Corex.Engine.Wmi;

[SupportedOSPlatform("windows")]
public sealed class WmiQueryExecutor : IWmiQueryExecutor
{
    public IEnumerable<IReadOnlyDictionary<string, object?>> Execute(
        string wmiClass,
        string[] properties,
        string? condition = null,
        string namespacePath = @"root\cimv2")
    {
        var props = string.Join(", ", properties);
        var query = condition is null
            ? $"SELECT {props} FROM {wmiClass}"
            : $"SELECT {props} FROM {wmiClass} WHERE {condition}";

        var opts = new ManagementEnumerationOptions
        {
            Timeout = TimeSpan.FromSeconds(3),
            Rewindable = false
        };

        using var searcher = new ManagementObjectSearcher(
            new ManagementScope(namespacePath),
            new ObjectQuery(query),
            opts);

        using var results = searcher.Get();

        var list = new List<IReadOnlyDictionary<string, object?>>();
        foreach (ManagementObject obj in results)
        {
            using (obj)
            {
                var dict = new Dictionary<string, object?>(properties.Length);
                foreach (var prop in properties)
                    dict[prop] = obj[prop];
                list.Add(dict);
            }
        }
        return list;
    }
}
