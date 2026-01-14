using System;

namespace FoxPrint.Encoding
{
    /// <summary>
    /// Provides Base64URL encoding/decoding functionality (RFC 4648)
    /// </summary>
    public static class Base64UrlEncoder
    {
        /// <summary>
        /// Encode a string to Base64URL format
        /// </summary>
        /// <param name="input">String to encode</param>
        /// <returns>Base64URL encoded string</returns>
        public static string Encode(string input)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("Input cannot be null or empty", nameof(input));

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(input);
            return Encode(bytes);
        }

        /// <summary>
        /// Encode a byte array to Base64URL format
        /// </summary>
        /// <param name="bytes">Bytes to encode</param>
        /// <returns>Base64URL encoded string</returns>
        public static string Encode(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                throw new ArgumentException("Bytes cannot be null or empty", nameof(bytes));

            string base64 = Convert.ToBase64String(bytes);

            // Convert Base64 to Base64URL:
            // Replace '+' with '-'
            // Replace '/' with '_'
            // Remove padding '='
            return base64
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        /// <summary>
        /// Decode a Base64URL string
        /// </summary>
        /// <param name="input">Base64URL encoded string</param>
        /// <returns>Decoded string</returns>
        public static string Decode(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("Input cannot be null or empty", nameof(input));

            byte[] bytes = DecodeToBytes(input);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// Decode a Base64URL string to byte array
        /// </summary>
        /// <param name="input">Base64URL encoded string</param>
        /// <returns>Decoded byte array</returns>
        public static byte[] DecodeToBytes(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("Input cannot be null or empty", nameof(input));

            // Convert Base64URL to Base64:
            // Replace '-' with '+'
            // Replace '_' with '/'
            // Add padding '=' if needed
            string base64 = input
                .Replace('-', '+')
                .Replace('_', '/');

            // Add padding
            int padding = (4 - (base64.Length % 4)) % 4;
            if (padding > 0)
            {
                base64 += new string('=', padding);
            }

            return Convert.FromBase64String(base64);
        }

        /// <summary>
        /// Check if a string is valid Base64URL format
        /// </summary>
        /// <param name="input">String to validate</param>
        /// <returns>True if valid Base64URL, false otherwise</returns>
        public static bool IsValidBase64Url(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            // Base64URL should only contain A-Z, a-z, 0-9, -, _
            foreach (char c in input)
            {
                if (!((c >= 'A' && c <= 'Z') ||
                      (c >= 'a' && c <= 'z') ||
                      (c >= '0' && c <= '9') ||
                      c == '-' ||
                      c == '_'))
                {
                    return false;
                }
            }

            try
            {
                DecodeToBytes(input);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
