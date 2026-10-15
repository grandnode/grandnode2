using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Data;
using Grand.Domain.Customers;
using System.Security.Cryptography;
using System.Text;

namespace Grand.Business.Authentication.Services;

/// <summary>
///     Keeps the revocation time of a subject in its own record, not on the subject's links: a link removed in the panels
///     (or all of them, after the provider changed) must not bring the revoked tokens back. Not cached: the check must
///     hold at once on every instance, and the record is read by its key
/// </summary>
public class OidcTokenRevocationService(IRepository<OidcTokenRevocation> revocationRepository)
    : IOidcTokenRevocationService
{
    public async Task RevokeTokens(string subject)
    {
        ArgumentException.ThrowIfNullOrEmpty(subject);
        var id = RevocationId(subject);
        var now = DateTime.UtcNow;

        if (await revocationRepository.GetByIdAsync(id) == null)
            try
            {
                await revocationRepository.InsertAsync(new OidcTokenRevocation {
                    Id = id, Subject = subject, RevokedBeforeUtc = now
                });
                return;
            }
            catch (Exception)
            {
                //a concurrent revocation stored the record first: move its time forward below; any other failure is
                //not swallowed, the caller (back-channel logout) answers with an error and the provider retries
                if (await revocationRepository.GetByIdAsync(id) == null)
                    throw;
            }

        //only forward: a later revocation already stored is never undone
        await revocationRepository.UpdateOneAsync(x => x.Id == id && x.RevokedBeforeUtc < now,
            UpdateBuilder<OidcTokenRevocation>.Create().Set(x => x.RevokedBeforeUtc, now));
    }

    public async Task<bool> IsRevoked(string subject, DateTime issuedUtc)
    {
        if (string.IsNullOrEmpty(subject))
            return false;
        var revocation = await revocationRepository.GetByIdAsync(RevocationId(subject));
        return revocation != null && revocation.RevokedBeforeUtc > issuedUtc;
    }

    public Task DeleteExpired()
    {
        var expired = DateTime.UtcNow - OidcSessionDefaults.RevocationRetention;
        return revocationRepository.DeleteManyAsync(x => x.RevokedBeforeUtc < expired);
    }

    /// <summary>
    ///     The first 12 bytes of SHA-256 over "OidcTokenRevocation:" + subject, as 24 lower-case hex characters (shaped
    ///     like an ObjectId); subjects are case-sensitive
    /// </summary>
    public static string RevocationId(string subject)
    {
        ArgumentException.ThrowIfNullOrEmpty(subject);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("OidcTokenRevocation:" + subject));
        return Convert.ToHexStringLower(hash, 0, 12);
    }
}
