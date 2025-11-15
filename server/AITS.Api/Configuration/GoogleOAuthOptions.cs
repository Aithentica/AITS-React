namespace AITS.Api.Configuration;

public sealed class GoogleOAuthOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    /// <summary>
    /// Domyślny redirect używany w środowisku backendowym (opcjonalnie nadpisywany przez żądanie).
    /// </summary>
    public string? DefaultRedirectUri { get; set; }
}



