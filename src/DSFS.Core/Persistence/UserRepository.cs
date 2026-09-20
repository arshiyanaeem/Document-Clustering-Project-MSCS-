using DSFS.Core.Models;
using DSFS.Core.Security;

namespace DSFS.Core.Persistence;

/// <summary>Backs the Admin Interface / User Interface accounts (section 5.2, 5.4.2).</summary>
public class UserRepository
{
    private readonly string _path;
    private List<UserAccount> _users;

    public UserRepository(string? path = null)
    {
        _path = path ?? AppPaths.UsersFile;
        _users = JsonFileStore<UserAccount>.Load(_path);
        EnsureSeedAdmin();
    }

    private void EnsureSeedAdmin()
    {
        if (_users.Any(u => u.Role == UserRole.Admin))
            return;

        var (hash, salt) = PasswordHasher.Hash("Admin@123");
        _users.Add(new UserAccount
        {
            Id = NextId(),
            Username = "admin",
            FullName = "System Administrator",
            Email = "admin@dsfs.local",
            ResearchDomain = "N/A",
            Role = UserRole.Admin,
            PasswordHash = hash,
            PasswordSalt = salt
        });
        Save();
    }

    private int NextId() => _users.Count == 0 ? 1 : _users.Max(u => u.Id) + 1;

    public IReadOnlyList<UserAccount> GetAll() => _users;

    public UserAccount? FindByUsername(string username) =>
        _users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

    public bool UsernameExists(string username) => FindByUsername(username) is not null;

    public UserAccount Register(string username, string fullName, string email, string researchDomain, string password, UserRole role = UserRole.Scholar)
    {
        if (UsernameExists(username))
            throw new InvalidOperationException($"Username '{username}' is already taken.");

        var (hash, salt) = PasswordHasher.Hash(password);
        var user = new UserAccount
        {
            Id = NextId(),
            Username = username,
            FullName = fullName,
            Email = email,
            ResearchDomain = researchDomain,
            Role = role,
            PasswordHash = hash,
            PasswordSalt = salt
        };
        _users.Add(user);
        Save();
        return user;
    }

    public UserAccount? Authenticate(string username, string password)
    {
        var user = FindByUsername(username);
        if (user is null) return null;
        return PasswordHasher.Verify(password, user.PasswordHash, user.PasswordSalt) ? user : null;
    }

    public void Update(UserAccount user)
    {
        int idx = _users.FindIndex(u => u.Id == user.Id);
        if (idx >= 0) _users[idx] = user;
        Save();
    }

    private void Save() => JsonFileStore<UserAccount>.Save(_path, _users);
}
