using System;
using System.IO;
using FoxPrint.Models;
using QRCoder;

namespace FoxPrint
{
    /// <summary>
    /// Generates QR code images optimized for receipt and thermal printers
    /// </summary>
    public class QRCodeImageGenerator
    {
        /// <summary>
        /// Generate a QR code image from a URL
        /// </summary>
        /// <param name="url">The URL to encode (from ReceiptQRGenerator)</param>
        /// <param name="options">Image generation options</param>
        /// <returns>Image bytes in the specified format</returns>
        public byte[] GenerateImage(string url, QRCodeOptions options = null)
        {
            options = options ?? QRCodeOptions.Default;

            // Validate input
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL cannot be empty", nameof(url));

            if (options.Size < 50 || options.Size > 1000)
                throw new ArgumentException("Size must be between 50 and 1000 pixels", nameof(options.Size));

            // Map error correction level
            QRCodeGenerator.ECCLevel eccLevel = MapErrorCorrectionLevel(options.ErrorCorrection);

            // Generate QR code using QRCoder library
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(url, eccLevel);

            // Generate image based on format
            switch (options.Format)
            {
                case ImageFormat.PNG:
                    return GeneratePng(qrCodeData, options);

                case ImageFormat.BMP:
                    return GenerateBmp(qrCodeData, options);

                default:
                    throw new NotSupportedException($"Format {options.Format} not supported");
            }
        }

        /// <summary>
        /// Generate QR code and save directly to file
        /// </summary>
        /// <param name="url">The URL to encode</param>
        /// <param name="filePath">Path to save the image</param>
        /// <param name="options">Image generation options</param>
        public void GenerateImageToFile(string url, string filePath, QRCodeOptions options = null)
        {
            byte[] imageBytes = GenerateImage(url, options);
            File.WriteAllBytes(filePath, imageBytes);
        }

        /// <summary>
        /// Generate QR code optimized for thermal printer (monochrome, BMP)
        /// </summary>
        /// <param name="url">The URL to encode</param>
        /// <param name="size">Size in pixels (default: 200)</param>
        /// <returns>BMP image bytes</returns>
        public byte[] GenerateForThermalPrinter(string url, int size = 200)
        {
            return GenerateImage(url, QRCodeOptions.ThermalPrinter(size));
        }

        /// <summary>
        /// Generate QR code optimized for receipt printer (ESC/POS compatible)
        /// </summary>
        /// <param name="url">The URL to encode</param>
        /// <param name="size">Size in pixels (default: 150)</param>
        /// <returns>BMP image bytes</returns>
        public byte[] GenerateForReceiptPrinter(string url, int size = 150)
        {
            return GenerateImage(url, QRCodeOptions.ReceiptPrinter(size));
        }

        private byte[] GeneratePng(QRCodeData qrCodeData, QRCodeOptions options)
        {
            var qrCode = new PngByteQRCode(qrCodeData);
            int pixelsPerModule = CalculatePixelsPerModule(options.Size);

            byte[] darkColor = new byte[] { 0, 0, 0 };
            byte[] lightColor = new byte[] { 255, 255, 255 };

            return qrCode.GetGraphic(pixelsPerModule, darkColor, lightColor);
        }

        private byte[] GenerateBmp(QRCodeData qrCodeData, QRCodeOptions options)
        {
            var qrCode = new BitmapByteQRCode(qrCodeData);
            int pixelsPerModule = CalculatePixelsPerModule(options.Size);

            return qrCode.GetGraphic(pixelsPerModule);
        }

        /// <summary>
        /// Calculate pixels per module based on desired size.
        /// QR codes typically have 21-177 modules depending on version and data.
        /// We use 33 as a baseline for medium-sized QR codes.
        /// </summary>
        private int CalculatePixelsPerModule(int desiredSize)
        {
            const int typicalModuleCount = 33;
            int pixelsPerModule = desiredSize / typicalModuleCount;

            // Ensure at least 1 pixel per module, max 30 for quality
            return Math.Max(1, Math.Min(30, pixelsPerModule));
        }

        /// <summary>
        /// Map our error correction enum to QRCoder's enum
        /// </summary>
        private QRCodeGenerator.ECCLevel MapErrorCorrectionLevel(ErrorCorrectionLevel level)
        {
            switch (level)
            {
                case ErrorCorrectionLevel.Low:
                    return QRCodeGenerator.ECCLevel.L;

                case ErrorCorrectionLevel.Medium:
                    return QRCodeGenerator.ECCLevel.M;

                case ErrorCorrectionLevel.Quartile:
                    return QRCodeGenerator.ECCLevel.Q;

                case ErrorCorrectionLevel.High:
                    return QRCodeGenerator.ECCLevel.H;

                default:
                    return QRCodeGenerator.ECCLevel.M;
            }
        }
    }
}
