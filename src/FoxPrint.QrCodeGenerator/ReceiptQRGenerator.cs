using System;
using FoxPrint.Encoding;
using FoxPrint.Security;

namespace FoxPrint
{
    /// <summary>
    /// Generates cryptographically-signed QR code URLs for receipt printing
    /// </summary>
    public class ReceiptQRGenerator
    {
        private readonly string _baseUrl;
        private readonly string _storeId;
        private readonly string _sharedSecret;

        /// <summary>
        /// Initialize QR code generator for a store
        /// </summary>
        /// <param name="storeId">Store identifier (provided by FoxNest)</param>
        /// <param name="sharedSecret">Shared secret for the POS integration (provided by FoxNest)</param>
        /// <param name="baseUrl">FoxNest API base URL (default: https://api.foxnest.com)</param>
        public ReceiptQRGenerator(string storeId, string sharedSecret, string baseUrl = "https://api.foxnest.com")
        {
            if (string.IsNullOrWhiteSpace(storeId))
                throw new ArgumentException("Store ID cannot be empty", nameof(storeId));

            if (string.IsNullOrWhiteSpace(sharedSecret))
                throw new ArgumentException("Shared secret cannot be empty", nameof(sharedSecret));

            _storeId = storeId;
            _sharedSecret = sharedSecret;
            _baseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseUrl));
        }

        /// <summary>
        /// Generate a QR code URL for a receipt
        /// </summary>
        /// <param name="tableNumber">Table number from POS</param>
        /// <returns>Complete URL to encode as QR code</returns>
        public string GenerateQRCodeUrl(string tableNumber)
        {
            return GenerateQRCodeUrl(tableNumber, DateTimeOffset.UtcNow);
        }

        /// <summary>
        /// Generate a QR code URL with specific timestamp (for testing)
        /// </summary>
        /// <param name="tableNumber">Table number from POS</param>
        /// <param name="timestamp">Timestamp to use</param>
        /// <returns>Complete URL to encode as QR code</returns>
        internal string GenerateQRCodeUrl(string tableNumber, DateTimeOffset timestamp)
        {
            if (string.IsNullOrWhiteSpace(tableNumber))
                throw new ArgumentException("Table number cannot be empty", nameof(tableNumber));

            // Validate no colons in data (reserved as delimiter)
            if (_storeId.Contains(":"))
                throw new InvalidOperationException("Store ID cannot contain colons");

            if (tableNumber.Contains(":"))
                throw new ArgumentException("Table number cannot contain colons", nameof(tableNumber));

            long unixTimestamp = timestamp.ToUnixTimeSeconds();

            // Create data string: storeId:tableNumber:timestamp
            string data = $"{_storeId}:{tableNumber}:{unixTimestamp}";

            // Generate HMAC-SHA256 signature
            string signature = HmacSigner.Sign(data, _sharedSecret);

            // Combine data + signature
            string signedData = $"{data}:{signature}";

            // Base64URL encode
            string slug = Base64UrlEncoder.Encode(signedData);

            // Return complete URL
            return $"{_baseUrl}/v1/scan/{slug}";
        }

        /// <summary>
        /// Verify a QR code slug (for testing/debugging)
        /// </summary>
        /// <param name="slug">The slug portion of the URL</param>
        /// <param name="tableNumber">Output: extracted table number</param>
        /// <returns>True if signature is valid, false otherwise</returns>
        public bool VerifySlug(string slug, out string tableNumber)
        {
            tableNumber = null;

            try
            {
                string decoded = Base64UrlEncoder.Decode(slug);
                string[] parts = decoded.Split(':');

                if (parts.Length != 4)
                    return false;

                string storeId = parts[0];
                tableNumber = parts[1];
                string timestamp = parts[2];
                string providedSignature = parts[3];

                if (storeId != _storeId)
                    return false;

                string data = $"{storeId}:{tableNumber}:{timestamp}";
                return HmacSigner.Verify(data, providedSignature, _sharedSecret);
            }
            catch
            {
                tableNumber = null;
                return false;
            }
        }

        /// <summary>
        /// Extract slug from a complete QR code URL
        /// </summary>
        /// <param name="url">Complete QR code URL</param>
        /// <returns>Extracted slug</returns>
        public static string ExtractSlugFromUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL cannot be empty", nameof(url));

            // Extract slug from URL like: https://api.foxnest.com/v1/scan/{slug}
            int lastSlashIndex = url.LastIndexOf('/');
            if (lastSlashIndex == -1 || lastSlashIndex == url.Length - 1)
                throw new ArgumentException("Invalid URL format", nameof(url));

            return url.Substring(lastSlashIndex + 1);
        }

        /// <summary>
        /// Get the store ID this generator is configured for
        /// </summary>
        public string StoreId => _storeId;

        /// <summary>
        /// Get the base URL this generator is configured for
        /// </summary>
        public string BaseUrl => _baseUrl;
    }
}
