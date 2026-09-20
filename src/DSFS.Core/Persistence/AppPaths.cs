namespace DSFS.Core.Persistence;

/// <summary>Resolves where DSFS stores its local JSON "database" files (the thesis's <c>Database</c> box in Figure 7).</summary>
public static class AppPaths
{
    public static string DataFolder
    {
        get
        {
            string root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folder = Path.Combine(root, "DSFS");
            Directory.CreateDirectory(folder);
            return folder;
        }
    }

    public static string UsersFile => Path.Combine(DataFolder, "users.json");
    public static string DocumentsFile => Path.Combine(DataFolder, "documents.json");
}
