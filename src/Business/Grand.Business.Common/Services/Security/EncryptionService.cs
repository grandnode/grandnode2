using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using System.Security.Cryptography;

namespace Grand.Business.Common.Services.Security;

public class EncryptionService : IEncryptionService
{
    private const string Pbkdf2Prefix = "PBKDF2";
    private const int Pbkdf2SaltSize = 16;
    private const int Pbkdf2HashSize = 32;
    private const int DefaultIterations = 210_000;

    private readonly SecurityConfig _securityConfig;

    public EncryptionService(SecurityConfig securityConfig = null)
    {
        _securityConfig = securityConfig ?? new SecurityConfig();
    }

    private int Iterations =>
        _securityConfig.PasswordHashIterations > 0 ? _securityConfig.PasswordHashIterations : DefaultIterations;

    /// <summary>
    ///     Create salt key
    /// </summary>
    /// <param name="size">Key size</param>
    /// <returns>Salt key</returns>
    public virtual string CreateSaltKey(int size)
    {
        // Generate a cryptographic random number
        var rng = RandomNumberGenerator.Create();

        var buff = new byte[size];
        rng.GetBytes(buff);

        // Return a Base64 string representation of the random number
        return Convert.ToBase64String(buff);
    }

    /// <summary>
    ///     Create a password hash
    /// </summary>
    /// <param name="password">password</param>
    /// <param name="saltKey">Salk key</param>
    /// <param name="passwordFormat"></param>
    /// <returns>Password hash</returns>
    public virtual string CreatePasswordHash(string password, string saltKey,
        HashedPasswordFormat passwordFormat = HashedPasswordFormat.SHA1)
    {
        var saltAndPassword = string.Concat(password, saltKey);
        HashAlgorithm algorithm = passwordFormat switch {
            HashedPasswordFormat.SHA1 => SHA1.Create(),
            HashedPasswordFormat.SHA256 => SHA256.Create(),
            HashedPasswordFormat.SHA384 => SHA384.Create(),
            HashedPasswordFormat.SHA512 => SHA512.Create(),
            _ => throw new NotSupportedException("Not supported format")
        };
        if (algorithm == null)
            throw new ArgumentException("Unrecognized hash name");

        var hashByteArray = algorithm.ComputeHash(Encoding.UTF8.GetBytes(saltAndPassword));
        return Convert.ToHexString(hashByteArray);
    }

    /// <summary>
    ///     Encrypt text
    /// </summary>
    /// <param name="plainText">Text to encrypt</param>
    /// <param name="privateKey">Encryption private key</param>
    /// <returns>Encrypted text</returns>
    public virtual string EncryptText(string plainText, string privateKey)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        if (string.IsNullOrEmpty(privateKey) || privateKey.Length != 24)
            throw new Exception("Wrong private key");

        var tDes = TripleDES.Create();

        tDes.Key = new ASCIIEncoding().GetBytes(privateKey);
        tDes.IV = new ASCIIEncoding().GetBytes(privateKey[^8..]);

        var encryptedBinary = EncryptTextToMemory(plainText, tDes.Key, tDes.IV);
        return Convert.ToBase64String(encryptedBinary);
    }

    /// <summary>
    ///     Decrypt text
    /// </summary>
    /// <param name="cipherText">Text to decrypt</param>
    /// <param name="encryptionPrivateKey">Encryption private key</param>
    /// <returns>Decrypted text</returns>
    public virtual string DecryptText(string cipherText, string encryptionPrivateKey)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        if (string.IsNullOrEmpty(encryptionPrivateKey) || encryptionPrivateKey.Length != 24)
            throw new Exception("Wrong encrypt private key");

        var tDes = TripleDES.Create();
        tDes.Key = new ASCIIEncoding().GetBytes(encryptionPrivateKey);
        tDes.IV = new ASCIIEncoding().GetBytes(encryptionPrivateKey[^8..]);

        var buffer = Convert.FromBase64String(cipherText);
        return DecryptTextFromMemory(buffer, tDes.Key, tDes.IV);
    }

    /// <summary>
    ///     Creates a strong, self-describing PBKDF2 (HMAC-SHA256) password hash.
    ///     Format: PBKDF2$1$&lt;iterations&gt;$&lt;saltBase64&gt;$&lt;hashBase64&gt;
    /// </summary>
    public virtual string HashPassword(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        var salt = RandomNumberGenerator.GetBytes(Pbkdf2SaltSize);
        var iterations = Iterations;
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            PepperedPassword(password), salt, iterations, HashAlgorithmName.SHA256, Pbkdf2HashSize);

        return string.Join('$',
            Pbkdf2Prefix, "1", iterations.ToString(), Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }

    public virtual bool VerifyPassword(string enteredPassword, PasswordFormat passwordFormat, string storedPassword,
        string storedSalt, HashedPasswordFormat legacyHashedFormat)
    {
        if (enteredPassword == null || storedPassword == null)
            return false;

        switch (passwordFormat)
        {
            case PasswordFormat.Clear:
                return FixedTimeEquals(enteredPassword, storedPassword);
            case PasswordFormat.Encrypted:
                return FixedTimeEquals(EncryptText(enteredPassword, storedSalt), storedPassword);
            case PasswordFormat.Hashed:
                return IsPbkdf2Hash(storedPassword)
                    ? VerifyPbkdf2(enteredPassword, storedPassword)
                    : FixedTimeEquals(CreatePasswordHash(enteredPassword, storedSalt, legacyHashedFormat),
                        storedPassword);
            default:
                return false;
        }
    }

    public virtual bool PasswordHashNeedsUpgrade(PasswordFormat passwordFormat, string storedPassword)
    {
        //up to date only if it is a PBKDF2 hash whose embedded cost meets the currently configured iteration count;
        //everything else (Clear/Encrypted, legacy SHA, or a weaker/unparseable PBKDF2 hash) is upgraded on next login
        if (passwordFormat != PasswordFormat.Hashed || !IsPbkdf2Hash(storedPassword))
            return true;

        var parts = storedPassword.Split('$');
        return parts.Length != 5 || !int.TryParse(parts[2], out var iterations) || iterations < Iterations;
    }

    private static bool IsPbkdf2Hash(string storedPassword)
    {
        return storedPassword != null && storedPassword.StartsWith(Pbkdf2Prefix + "$", StringComparison.Ordinal);
    }

    private bool VerifyPbkdf2(string enteredPassword, string storedPassword)
    {
        //PBKDF2$1$<iterations>$<saltBase64>$<hashBase64>
        var parts = storedPassword.Split('$');
        if (parts.Length != 5 || !int.TryParse(parts[2], out var iterations) || iterations <= 0)
            return false;

        try
        {
            var salt = Convert.FromBase64String(parts[3]);
            var expected = Convert.FromBase64String(parts[4]);
            if (expected.Length == 0)
                return false;

            var actual = Rfc2898DeriveBytes.Pbkdf2(
                PepperedPassword(enteredPassword), salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            //corrupted/malformed stored hash - fail closed instead of throwing on the auth path
            return false;
        }
    }

    private byte[] PepperedPassword(string password)
    {
        var pepper = _securityConfig.PasswordHashKey;
        //a NUL separator prevents ambiguity between password and pepper boundaries
        return Encoding.UTF8.GetBytes(string.IsNullOrEmpty(pepper) ? password : password + "\0" + pepper);
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
    }

    #region Utilities

    private static byte[] EncryptTextToMemory(string data, byte[] key, byte[] iv)
    {
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, TripleDES.Create().CreateEncryptor(key, iv), CryptoStreamMode.Write))
        {
            var toEncrypt = new UnicodeEncoding().GetBytes(data);
            cs.Write(toEncrypt, 0, toEncrypt.Length);
            cs.FlushFinalBlock();
        }

        return ms.ToArray();
    }

    private static string DecryptTextFromMemory(byte[] data, byte[] key, byte[] iv)
    {
        using var ms = new MemoryStream(data);
        using var cs = new CryptoStream(ms, TripleDES.Create().CreateDecryptor(key, iv), CryptoStreamMode.Read);
        var sr = new StreamReader(cs, new UnicodeEncoding());
        return sr.ReadLine();
    }

    #endregion
}