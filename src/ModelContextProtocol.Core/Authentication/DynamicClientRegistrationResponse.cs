using System.Text.Json.Serialization;

namespace ModelContextProtocol.Authentication;

/// <summary>
/// Represents a client registration response for OAuth 2.0 Dynamic Client Registration (RFC 7591).
/// </summary>
public sealed class DynamicClientRegistrationResponse
{
    /// <summary>
    /// Gets or initializes the client identifier.
    /// </summary>
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; }

    /// <summary>
    /// Gets or initializes the client secret.
    /// </summary>
    [JsonPropertyName("client_secret")]
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Gets or initializes the redirect URIs for the client.
    /// </summary>
    [JsonPropertyName("redirect_uris")]
    public IList<string>? RedirectUris { get; set; }

    /// <summary>
    /// Gets or initializes the token endpoint authentication method.
    /// </summary>
    [JsonPropertyName("token_endpoint_auth_method")]
    public string? TokenEndpointAuthMethod { get; set; }

    /// <summary>
    /// Gets or initializes the grant types that the client will use.
    /// </summary>
    [JsonPropertyName("grant_types")]
    public IList<string>? GrantTypes { get; set; }

    /// <summary>
    /// Gets or initializes the response types that the client will use.
    /// </summary>
    [JsonPropertyName("response_types")]
    public IList<string>? ResponseTypes { get; set; }

    /// <summary>
    /// Gets or initializes the timestamp at which the client ID was issued.
    /// </summary>
    [JsonPropertyName("client_id_issued_at")]
    public long? ClientIdIssuedAt { get; set; }

    /// <summary>
    /// Gets or initializes the client secret expiration time.
    /// </summary>
    [JsonPropertyName("client_secret_expires_at")]
    public long? ClientSecretExpiresAt { get; set; }
}
