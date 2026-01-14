using System;
using System.Security.Cryptography;

namespace FoxPrint.Security
{
    /// <summary>
    /// Provides HMAC-SHA256 signing functionality for QR code data
    /// </summary>
    public static class HmacSigner
    {
        /// <summary>
        /// Generate HMAC-SHA256 signature for the given data
        /// </summary>
        /// <param name="data">Data to sign</param>
        /// <param name="secret">Shared secret key</param>
        /// <returns>Hexadecimal string representation of the signature</returns>
        public static string Sign(string data, string secret)
        {
            if (string.IsNullOrWhiteSpace(data))
                throw new ArgumentException("Data cannot be null or empty", nameof(data));

            if (string.IsNullOrWhiteSpace(secret))
                throw new ArgumentException("Secret cannot be null or empty", nameof(secret));

            using (var hmac = new HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret)))
            {
                byte[] hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
                return ByteArrayToHexString(hash);
            }
        }

        /// <summary>
        /// Verify HMAC-SHA256 signature using constant-time comparison
        /// </summary>
        /// <param name="data">Original data</param>
        /// <param name="signature">Signature to verify</param>
        /// <param name="secret">Shared secret key</param>
        /// <returns>True if signature is valid, false otherwise</returns>
        public static bool Verify(string data, string signature, string secret)
        {
            if (string.IsNullOrWhiteSpace(signature))
                return false;

            try
            {
                string expectedSignature = Sign(data, secret);
                return ConstantTimeEquals(signature, expectedSignature);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Convert byte array to hexadecimal string
        /// </summary>
        private static string ByteArrayToHexString(byte[] bytes)
        {
            System.Text.StringBuilder hex = new System.Text.StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
            {
                hex.AppendFormat("{0:x2}", b);
            }
            return hex.ToString();
        }

        /// <summary>
        /// Constant-time string comparison to prevent timing attacks
        /// </summary>
        private static bool ConstantTimeEquals(string a, string b)
        {
            if (a == null || b == null)
                return false;

            if (a.Length != b.Length)
                return false;

            int result = 0;
            for (int i = 0; i < a.Length; i++)
            {
                result |= a[i] ^ b[i];
            }

            return result == 0;
        }
    }
}
