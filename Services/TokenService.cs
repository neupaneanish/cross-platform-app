using System;
using System.Threading;
using GitCredentialManager;
using TuinFounder.External.Authentication.V1;

namespace TuinFounder.Services;

public interface ITokenService
{
    event Action<bool>? AuthChanged;
    event Action<bool>? SessionExpired;
    void Save(Token token);
    string? GetAccess();
    string? GetRefresh();
    void Delete(bool expired);
    bool IsAuthenticated();
    (string? Refresh, bool ExpiringSoon) RequiredRefresh();
}

public record TokenState(
    string Access,
    string Refresh,
    DateTimeOffset ExpireAt
);

public class TokenService : ITokenService
{
    private const string AppName = "TuinFounder";
    private const string TargetUrl = "https://founder.tuin.dev";
    private const string AccessKey = "Access";
    private const string RefreshKey = "Refresh";
    private const string ExpireAtKey = "ExpireAt";

    private static readonly ICredentialStore CredentialStore = CredentialManager.Create(AppName);

    private readonly Lock _lock = new();
    private TokenState? _tokenState = Load();

    public event Action<bool>? AuthChanged;
    public event Action<bool>? SessionExpired;

    public void Save(Token token)
    {
        lock (_lock)
        {
            CredentialStore.AddOrUpdate(TargetUrl, AccessKey, token.Access);
            CredentialStore.AddOrUpdate(TargetUrl, RefreshKey, token.Refresh);
            CredentialStore.AddOrUpdate(TargetUrl, ExpireAtKey, token.ExpireAt.Seconds.ToString());

            _tokenState = new TokenState(token.Access, token.Refresh, token.ExpireAt.ToDateTimeOffset());
        }

        AuthChanged?.Invoke(true);
        SessionExpired?.Invoke(false);
    }

    public string? GetAccess()
    {
        lock (_lock)
        {
            return _tokenState?.Access;
        }
    }

    public string? GetRefresh()
    {
        lock (_lock)
        {
            return _tokenState?.Refresh;
        }
    }

    public void Delete(bool expired)
    {
        lock (_lock)
        {
            CredentialStore.Remove(TargetUrl, AccessKey);
            CredentialStore.Remove(TargetUrl, RefreshKey);
            CredentialStore.Remove(TargetUrl, ExpireAtKey);
            _tokenState = null;
        }

        AuthChanged?.Invoke(false);
        SessionExpired?.Invoke(expired);
    }

    public bool IsAuthenticated()
    {
        lock (_lock)
        {
            return _tokenState is not null;
        }
    }

    public (string? Refresh, bool ExpiringSoon) RequiredRefresh()
    {
        lock (_lock)
        {
            if (_tokenState is null) return (null, true);
            var expiringSoon = _tokenState.ExpireAt <= DateTimeOffset.UtcNow.AddSeconds(30);
            return (_tokenState.Refresh, expiringSoon);
        }
    }

    private static TokenState? Load()
    {
        try
        {
            var access = CredentialStore.Get(TargetUrl, AccessKey)?.Password;
            var refresh = CredentialStore.Get(TargetUrl, RefreshKey)?.Password;
            var expireAt = CredentialStore.Get(TargetUrl, ExpireAtKey)?.Password;

            if (string.IsNullOrWhiteSpace(access) ||
                string.IsNullOrWhiteSpace(refresh) ||
                string.IsNullOrWhiteSpace(expireAt) ||
                !long.TryParse(expireAt, out var seconds))
                return null;
            return new TokenState(
                access,
                refresh,
                DateTimeOffset.FromUnixTimeSeconds(seconds)
            );
        }
        catch (Exception)
        {
            return null;
        }
    }
}