using Grand.Domain.Customers;

namespace Grand.Business.Core.Interfaces.Common.Security;

public interface IEncryptionService
{
    /// <summary>
    ///     Create salt key
    /// </summary>
    /// <param name="size">Key size</param>
    /// <returns>Salt key</returns>
    string CreateSaltKey(int size);

    /// <summary>
    ///     Create a password hash
    /// </summary>
    /// <param name="password">password</param>
    /// <param name="saltKey">Salk key</param>
    /// <param name="passwordFormat">Hashed Password format (hash algorithm)</param>
    /// <returns>Password hash</returns>
    string CreatePasswordHash(string password, string saltKey,
        HashedPasswordFormat passwordFormat = HashedPasswordFormat.SHA1);

    /// <summary>
    ///     Creates a strong, self-describing password hash for the current default algorithm (PBKDF2/HMAC-SHA256).
    ///     The salt and parameters are embedded in the returned value, so a separate salt field is not needed and
    ///     verification never depends on a global setting. Store the result in <c>Customer.Password</c> with
    ///     <see cref="PasswordFormat.Hashed" />.
    /// </summary>
    /// <param name="password">Plain-text password</param>
    /// <returns>Self-describing hash string</returns>
    string HashPassword(string password);

    /// <summary>
    ///     Verifies an entered password against a stored credential in a format-aware, constant-time way.
    ///     Handles Clear, Encrypted, the modern PBKDF2 hash and legacy SHA-x hashes transparently.
    /// </summary>
    /// <param name="enteredPassword">Plain-text password supplied by the user</param>
    /// <param name="passwordFormat">Stored password format (<c>Customer.PasswordFormatId</c>)</param>
    /// <param name="storedPassword">Stored password/hash (<c>Customer.Password</c>)</param>
    /// <param name="storedSalt">Stored salt (<c>Customer.PasswordSalt</c>); ignored for PBKDF2</param>
    /// <param name="legacyHashedFormat">Hash algorithm used by legacy SHA hashes (global setting)</param>
    /// <returns>True when the password matches</returns>
    bool VerifyPassword(string enteredPassword, PasswordFormat passwordFormat, string storedPassword,
        string storedSalt, HashedPasswordFormat legacyHashedFormat);

    /// <summary>
    ///     Indicates whether a stored credential uses a weak/legacy format (Clear, Encrypted or a non-PBKDF2 hash)
    ///     and should be transparently re-hashed to the modern format after the next successful authentication.
    /// </summary>
    bool PasswordHashNeedsUpgrade(PasswordFormat passwordFormat, string storedPassword);

    /// <summary>
    ///     Returns true when the stored value is the modern self-describing (PBKDF2) hash produced by
    ///     <see cref="HashPassword" />. Use it to pick the verification path for records whose password format is
    ///     not tracked in a separate field (e.g. API users), instead of inferring it from other columns.
    /// </summary>
    bool IsHashedPassword(string storedPassword);

    /// <summary>
    ///     Encrypt text (legacy TripleDES). Retained only to verify pre-existing reversibly-encrypted values
    ///     (legacy customer "Encrypted" password format and legacy API users). Do NOT use for new data or for
    ///     protecting secrets at rest - hash passwords with <see cref="HashPassword" /> and use ASP.NET Core Data
    ///     Protection for reversible secrets.
    /// </summary>
    /// <param name="plainText">Text to encrypt</param>
    /// <param name="privateKey">Encryption private key</param>
    /// <returns>Encrypted text</returns>
    string EncryptText(string plainText, string privateKey);

    /// <summary>
    ///     Decrypt text (legacy TripleDES). Legacy counterpart of <see cref="EncryptText" />; see its remarks.
    /// </summary>
    /// <param name="cipherText">Text to decrypt</param>
    /// <param name="encryptionPrivateKey">Encryption private key</param>
    /// <returns>Decrypted text</returns>
    string DecryptText(string cipherText, string encryptionPrivateKey);
}