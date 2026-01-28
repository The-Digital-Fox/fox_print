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
        /// <param name="environment">FoxNest environment (default: Production)</param>
        public ReceiptQRGenerator(string storeId, string sharedSecret, FoxNestEnvironment environment = FoxNestEnvironment.Production)
            : this(storeId, sharedSecret, FoxNestEnvironments.GetUrl(environment))
        {
        }

        /// <summary>
        /// Initialize QR code generator for a store with custom base URL
        /// </summary>
        /// <param name="storeId">Store identifier (provided by FoxNest)</param>
        /// <param name="sharedSecret">Shared secret for the POS integration (provided by FoxNest)</param>
        /// <param name="baseUrl">Custom FoxNest API base URL</param>
        public ReceiptQRGenerator(string storeId, string sharedSecret, string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(storeId))
                throw new ArgumentException("Store ID cannot be empty", nameof(storeId));

            if (string.IsNullOrWhiteSpace(sharedSecret))
                throw new ArgumentException("Shared secret cannot be empty", nameof(sharedSecret));

            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentException("Base URL cannot be empty", nameof(baseUrl));

            _storeId = storeId;
            _sharedSecret = sharedSecret;
            _baseUrl = baseUrl.TrimEnd('/');
        }

        /// <summary>
        /// Generate a QR code URL for a receipt
        /// </summary>
        /// <param name="tableNumber">Table number from POS</param>
        /// <param name="checkId">Check/invoice ID from POS</param>
        /// <param name="tablePart">Table part/section identifier (default: "n/a")</param>
        /// <returns>Complete URL to encode as QR code</returns>
        public string GenerateQRCodeUrl(string tableNumber, string checkId, string tablePart = "n/a")
        {
            return GenerateQRCodeUrl(tableNumber, checkId, tablePart, DateTimeOffset.UtcNow);
        }

        /// <summary>
        /// Generate a QR code URL with specific timestamp (for testing)
        /// </summary>
        /// <param name="tableNumber">Table number from POS</param>
        /// <param name="checkId">Check/invoice ID from POS</param>
        /// <param name="tablePart">Table part/section identifier</param>
        /// <param name="timestamp">Timestamp to use</param>
        /// <returns>Complete URL to encode as QR code</returns>
        internal string GenerateQRCodeUrl(string tableNumber, string checkId, string tablePart, DateTimeOffset timestamp)
        {
            if (string.IsNullOrWhiteSpace(tableNumber))
                throw new ArgumentException("Table number cannot be empty", nameof(tableNumber));

            if (string.IsNullOrWhiteSpace(checkId))
                throw new ArgumentException("Check ID cannot be empty", nameof(checkId));

            if (string.IsNullOrWhiteSpace(tablePart))
                throw new ArgumentException("Table part cannot be empty", nameof(tablePart));

            // Validate no colons in data (reserved as delimiter)
            if (_storeId.Contains(":"))
                throw new InvalidOperationException("Store ID cannot contain colons");

            if (tableNumber.Contains(":"))
                throw new ArgumentException("Table number cannot contain colons", nameof(tableNumber));

            if (checkId.Contains(":"))
                throw new ArgumentException("Check ID cannot contain colons", nameof(checkId));

            if (tablePart.Contains(":"))
                throw new ArgumentException("Table part cannot contain colons", nameof(tablePart));

            long unixTimestamp = timestamp.ToUnixTimeSeconds();

            // Create data string: storeId:tableNumber:tablePart:checkId:timestamp
            string data = $"{_storeId}:{tableNumber}:{tablePart}:{checkId}:{unixTimestamp}";

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
            return VerifySlug(slug, out tableNumber, out _, out _);
        }

        /// <summary>
        /// Verify a QR code slug (for testing/debugging)
        /// </summary>
        /// <param name="slug">The slug portion of the URL</param>
        /// <param name="tableNumber">Output: extracted table number</param>
        /// <param name="tablePart">Output: extracted table part</param>
        /// <returns>True if signature is valid, false otherwise</returns>
        public bool VerifySlug(string slug, out string tableNumber, out string tablePart)
        {
            return VerifySlug(slug, out tableNumber, out tablePart, out _);
        }

        /// <summary>
        /// Verify a QR code slug (for testing/debugging)
        /// </summary>
        /// <param name="slug">The slug portion of the URL</param>
        /// <param name="tableNumber">Output: extracted table number</param>
        /// <param name="tablePart">Output: extracted table part</param>
        /// <param name="checkId">Output: extracted check ID</param>
        /// <returns>True if signature is valid, false otherwise</returns>
        public bool VerifySlug(string slug, out string tableNumber, out string tablePart, out string checkId)
        {
            tableNumber = null;
            tablePart = null;
            checkId = null;

            try
            {
                string decoded = Base64UrlEncoder.Decode(slug);
                string[] parts = decoded.Split(':');

                if (parts.Length != 6)
                    return false;

                string storeId = parts[0];
                tableNumber = parts[1];
                tablePart = parts[2];
                checkId = parts[3];
                string timestamp = parts[4];
                string providedSignature = parts[5];

                if (storeId != _storeId)
                    return false;

                string data = $"{storeId}:{tableNumber}:{tablePart}:{checkId}:{timestamp}";
                return HmacSigner.Verify(data, providedSignature, _sharedSecret);
            }
            catch
            {
                tableNumber = null;
                tablePart = null;
                checkId = null;
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
