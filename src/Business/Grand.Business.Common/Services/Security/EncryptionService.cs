﻿using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Customers;
using System.Security.Cryptography;

namespace Grand.Business.Common.Services.Security
{
    public class EncryptionService : IEncryptionService
    {
        /// <summary>
        /// Create salt key
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
        /// Create a password hash
        /// </summary>
        /// <param name="password">password</param>
        /// <param name="saltKey">Salk key</param>
        /// <param name="passwordFormat"></param>
        /// <returns>Password hash</returns>
        public virtual string CreatePasswordHash(string password, string saltKey, HashedPasswordFormat passwordFormat = HashedPasswordFormat.SHA1)
        {
            var saltAndPassword = string.Concat(password, saltKey);
            HashAlgorithm algorithm = passwordFormat switch
            {
                HashedPasswordFormat.SHA1 => SHA1.Create(),
                HashedPasswordFormat.SHA256 => SHA256.Create(),
                HashedPasswordFormat.SHA384 => SHA384.Create(),
                HashedPasswordFormat.SHA512 => SHA512.Create(),
                _ => throw new NotSupportedException("Not supported format")
            };
            if (algorithm == null)
                throw new ArgumentException("Unrecognized hash name");

            var hashByteArray = algorithm.ComputeHash(Encoding.UTF8.GetBytes(saltAndPassword));
            return BitConverter.ToString(hashByteArray).Replace("-", "");
        }

        /// <summary>
        /// Encrypt text
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
        /// Decrypt text
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

        #region Utilities

        private byte[] EncryptTextToMemory(string data, byte[] key, byte[] iv)
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

        private string DecryptTextFromMemory(byte[] data, byte[] key, byte[] iv)
        {
            using var ms = new MemoryStream(data);
            using var cs = new CryptoStream(ms, TripleDES.Create().CreateDecryptor(key, iv), CryptoStreamMode.Read);
            var sr = new StreamReader(cs, new UnicodeEncoding());
            return sr.ReadLine();
        }

        #endregion
    }
}