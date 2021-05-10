using Grand.Domain.Customers;

namespace Grand.Business.Common.Interfaces.Security
{
    public interface IEncryptionService 
    {
        /// <summary>
        /// Create salt key
        /// </summary>
        /// <param name="size">Key size</param>
        /// <returns>Salt key</returns>
        string CreateSaltKey(int size);

        /// <summary>
        /// Create a password hash
        /// </summary>
        /// <param name="password">{assword</param>
        /// <param name="saltkey">Salk key</param>
        /// <param name="passwordFormat">Hashed Password format (hash algorithm)</param>
        /// <returns>Password hash</returns>
        string CreatePasswordHash(string password, string saltkey, HashedPasswordFormat passwordFormat = HashedPasswordFormat.SHA1);

        /// <summary>
        /// Encrypt text
        /// </summary>
        /// <param name="plainText">Text to encrypt</param>
        /// <param name="privateKey">Encryption private key</param>
        /// <returns>Encrypted text</returns>
        string EncryptText(string plainText, string privateKey);

        /// <summary>
        /// Decrypt text
        /// </summary>
        /// <param name="cipherText">Text to decrypt</param>
        /// <param name="encryptionPrivateKey">Encryption private key</param>
        /// <returns>Decrypted text</returns>
        string DecryptText(string cipherText, string encryptionPrivateKey);
    }
}
