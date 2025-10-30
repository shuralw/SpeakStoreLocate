using System.Security.Claims;
using System.Text.Json;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SpeakStoreLocate.ApiService;

public class GoogleAccessTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IHttpClientFactory _httpClientFactory;

    public GoogleAccessTokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IHttpClientFactory httpClientFactory)
        : base(options, logger, encoder)
    {
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        if (string.IsNullOrWhiteSpace(authHeader) ||
            !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            return AuthenticateResult.Fail("Leeres Token");
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            using var req = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v3/userinfo");
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            using var resp = await client.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
            {
                return AuthenticateResult.Fail($"Google UserInfo Fehler: {(int)resp.StatusCode}");
            }

            await using var stream = await resp.Content.ReadAsStreamAsync();
            var payload = await JsonSerializer.DeserializeAsync<JsonElement>(stream);

            if (!payload.TryGetProperty("sub", out var subProp))
            {
                return AuthenticateResult.Fail("Kein 'sub' im UserInfo");
            }

            var claims = new List<Claim>
            {
                new Claim("sub", subProp.GetString() ?? string.Empty),
                new Claim("iss", "https://accounts.google.com"),
            };

            if (payload.TryGetProperty("email", out var emailProp) && emailProp.ValueKind == JsonValueKind.String)
                claims.Add(new Claim("email", emailProp.GetString()!));
            if (payload.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String)
                claims.Add(new Claim("name", nameProp.GetString()!));
            if (payload.TryGetProperty("picture", out var picProp) && picProp.ValueKind == JsonValueKind.String)
                claims.Add(new Claim("picture", picProp.GetString()!));

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Fehler bei Google Access Token Validierung");
            return AuthenticateResult.Fail("Exception bei Introspection");
        }
    }
}