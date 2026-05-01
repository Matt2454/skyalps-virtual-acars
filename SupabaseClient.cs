using Supabase;

namespace SkyAlpsACARS;

public static class SupabaseClient
{
    private static Client? _instance;
    private static readonly object _lock = new();

    public static Client Instance
    {
        get
        {
            if (_instance == null)
            {
                throw new InvalidOperationException("Supabase client not initialized. Call InitializeAsync first.");
            }
            return _instance;
        }
    }

    public static bool IsInitialized => _instance != null;

    public static async Task InitializeAsync(string url, string key)
    {
        var options = new SupabaseOptions
        {
            AutoConnectRealtime = true
        };

        _instance = new Client(url, key, options);
        await _instance.InitializeAsync();
    }

    public static async Task<(bool success, string? error)> LoginAsync(string email, string password)
    {
        try
        {
            var session = await Instance.Auth.SignIn(email, password);
            return session != null ? (true, null) : (false, "Login failed");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public static async Task LogoutAsync()
    {
        await Instance.Auth.SignOut();
    }

    public static string? GetCurrentUserId()
    {
        return Instance.Auth.CurrentSession?.User?.Id;
    }
}