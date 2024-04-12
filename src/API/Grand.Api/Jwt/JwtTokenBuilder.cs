using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Grand.Api.Jwt;

public sealed class JwtTokenBuilder
{
    private readonly Dictionary<string, string> claims = new();
    private string audience = "";
    private int expiryInMinutes = 5;
    private string issuer = "";
    private SecurityKey securityKey;
    private bool useaudience;
    private bool useissuer;

    private void EnsureArguments()
    {
        if (securityKey == null)
            throw new ArgumentNullException(nameof(securityKey));

        if (useissuer && string.IsNullOrEmpty(issuer))
            throw new ArgumentNullException(nameof(issuer));

        if (useaudience && string.IsNullOrEmpty(audience))
            throw new ArgumentNullException(nameof(audience));
    }

    public JwtTokenBuilder AddSecurityKey(SecurityKey securityKey)
    {
        this.securityKey = securityKey;
        return this;
    }

    public JwtTokenBuilder AddIssuer(string issuer)
    {
        this.issuer = issuer;
        useissuer = true;
        return this;
    }

    public JwtTokenBuilder AddAudience(string audience)
    {
        this.audience = audience;
        useaudience = true;
        return this;
    }

    public JwtTokenBuilder AddClaim(string type, string value)
    {
        claims.Add(type, value);
        return this;
    }

    public JwtTokenBuilder AddClaims(Dictionary<string, string> claims)
    {
        foreach (var item in claims)
            if (!this.claims.ContainsKey(item.Key) && !string.IsNullOrEmpty(item.Value))
                this.claims.Add(item.Key, item.Value);
        return this;
    }

    public JwtTokenBuilder AddExpiry(int expiryInMinutes)
    {
        this.expiryInMinutes = expiryInMinutes;
        return this;
    }

    public JwtToken Build()
    {
        EnsureArguments();

        var claims = new List<Claim> {
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            }
            .Union(this.claims.Select(item => new Claim(item.Key, item.Value)));

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(expiryInMinutes),
            signingCredentials: new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256));

        return new JwtToken(token);
    }
}