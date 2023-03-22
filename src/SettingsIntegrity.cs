using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Werkr.Common.Settings {

    public class EncryptionResult {
        public EncryptionResult( string data, string key ) {
            Data = data;
            Key = key;
        }
        public readonly string Data;
        public readonly string Key;
    }

    /// <summary>
    /// A utility class that provides methods for encrypting and decrypting strings using AES,
    /// as well as verifying the integrity of the data using HMAC.
    /// </summary>
    public class SettingsIntegrity {

        #region Encrypt

        /// <summary>
        /// Encrypts the input string and returns a tuple containing the encrypted string and the key used for encryption.
        /// </summary>
        /// <param name="inputString">The string to be encrypted</param>
        /// <returns>An EncryptionResult object containing the encrypted string and the key used for encryption</returns>
        public static EncryptionResult Encrypt( string inputString, string stringKey = null ) {
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputString);
            using Aes aes = AesInit( stringKey != null ? Convert.FromBase64String( stringKey ) : null );
            return new EncryptionResult(
                Convert.ToBase64String(
                    GetEncryptedBytes(
                        aes,
                        Convert.ToBase64String( inputBytes ) + "\n" + GetBase64Hmac( aes.Key, inputBytes )
                    )
                ),
                Convert.ToBase64String( aes.Key )
            );
        }

        /// <summary>
        /// Encrypts the input data using the provided Aes object and returns the encrypted bytes.
        /// </summary>
        /// <param name="aes">The Aes object to be used for encryption</param>
        /// <param name="dataToEncrypt">The data to be encrypted</param>
        /// <returns>The encrypted bytes</returns>
        private static byte[] GetEncryptedBytes( Aes aes, string dataToEncrypt ) {
            using MemoryStream stream = new( );
            stream.Write( aes.IV, 0, aes.IV.Length );
            using (CryptoStream cryptoStream = new( stream, aes.CreateEncryptor( ), CryptoStreamMode.Write )) {
                using StreamWriter encryptWriter = new( cryptoStream );
                encryptWriter.Write( dataToEncrypt );
            }
            return stream.ToArray( );
        }

        #endregion Encrypt


        #region Decrypt

        /// <summary>
        /// Decrypts the input string using the provided key and returns the original input string.
        /// </summary>
        /// <param name="encryptedString">The encrypted string to be decrypted</param>
        /// <param name="stringKey">The key used to decrypt the string</param>
        /// <returns>The original input string</returns>
        public static string Decrypt( string encryptedString, string stringKey ) {
            using Aes aes = AesInit( Convert.FromBase64String( stringKey ) );
            string decryptionString = GetDecryptedString(aes, Convert.FromBase64String(encryptedString));

            string[] splitString = decryptionString.Split('\n');
            byte[] inputBytes = Convert.FromBase64String(splitString[0]);

            return GetBase64Hmac( aes.Key, inputBytes ) == splitString[1]
                ? Encoding.UTF8.GetString( inputBytes )
                : throw new CryptographicException( "HMAC check failed: data may have been tampered with." );
        }

        /// <summary>
        /// Decrypts the input data using the provided Aes object and returns the decrypted data as a string.
        /// </summary>
        /// <param name="aes">The Aes object to be used for decryption</param>
        /// <param name="dataToDecrypt">The data to be decrypted</param>
        /// <returns>The decrypted data as a string</returns>
        private static string GetDecryptedString( Aes aes, byte[] dataToDecrypt ) {
            using MemoryStream stream = new( dataToDecrypt );
            byte[] iv = new byte[aes.BlockSize / 8];
            _ = stream.Read( iv, 0, iv.Length );
            aes.IV = iv;

            using CryptoStream cryptoStream = new( stream, aes.CreateDecryptor( ), CryptoStreamMode.Read );
            using StreamReader reader = new( cryptoStream );
            return reader.ReadToEnd( );
        }

        #endregion Decrypt


        #region Shared

        /// <summary>
        /// Initializes an Aes object with a key (if provided) or generates a new key.
        /// Generates a new initialization vector (IV). The IV will be re-read from the encrypted data on decryption.
        /// </summary>
        /// <param name="key">The key to be used for encryption. If not provided, a new key is generated.</param>
        /// <returns>The initialized Aes object</returns>
        private static Aes AesInit( byte[] key = null ) {
            Aes aes = Aes.Create( );
            if (key != null) { aes.Key = key; } else { aes.GenerateKey( ); }
            aes.GenerateIV( );
            return aes;
        }

        /// <summary>
        /// Generates the base64-encoded HMAC of the input bytes using the key.
        /// </summary>
        /// <param name="key">The key to be used for generating the HMAC</param>
        /// <param name="inputBytes">The input bytes for which the HMAC is generated</param>
        /// <returns>The base64-encoded HMAC of the input bytes</returns>
        private static string GetBase64Hmac( byte[] key, byte[] inputBytes ) {
            using HMACSHA256 hmac = new( key );
            return Convert.ToBase64String( hmac.ComputeHash( inputBytes ) );
        }

        #endregion Shared

    }
}
