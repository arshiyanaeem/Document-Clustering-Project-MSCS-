using System.Text.Json;

namespace DSFS.Core.Persistence;

/// <summary>
/// The DSFS "Database" (Figure 7) is implemented as a simple, dependency-free JSON file store
/// using System.Text.Json from the base class library. This keeps the project buildable with
/// nothing but the .NET SDK - swap this out for SQL Server / SQLite / EF Core in a production
/// deployment without touching any of the Core algorithm classes.
/// </summary>
public static class JsonFileStore<T>
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static List<T> Load(string path)
    {
        if (!File.Exists(path)) return new List<T>();
        string json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json)) return new List<T>();
        return JsonSerializer.Deserialize<List<T>>(json, Options) ?? new List<T>();
    }

    public static void Save(string path, List<T> items)
    {
        string json = JsonSerializer.Serialize(items, Options);
        File.WriteAllText(path, json);
    }
}
