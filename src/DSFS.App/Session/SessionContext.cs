using DSFS.Core.Models;
using DSFS.Core.Persistence;

namespace DSFS.App.Session;

/// <summary>
/// Process-wide singletons for the currently logged-in user and the shared repositories,
/// so every form talks to the same in-memory + on-disk state.
/// </summary>
public static class SessionContext
{
    public static UserAccount? CurrentUser { get; set; }

    public static UserRepository Users { get; } = new();

    public static DocumentRepository Documents { get; } = new();

    public static void SignOut() => CurrentUser = null;
}
