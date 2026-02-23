using System;
using System.IO;
using FoxPrint.Models;
using QRCoder;
using SkiaSharp;
using Svg.Skia;

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

            // When embedding a logo, force High ECC so the QR remains scannable
            // even with ~25% of the center obscured by the logo.
            QRCodeGenerator.ECCLevel eccLevel = options.EmbedLogo
                ? QRCodeGenerator.ECCLevel.H
                : MapErrorCorrectionLevel(options.ErrorCorrection);

            // Generate QR code using QRCoder library
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(url, eccLevel);

            if (options.EmbedLogo)
            {
                // Always start with PNG as intermediate for reliable SkiaSharp decoding,
                // then composite the logo and re-encode to the requested format.
                byte[] pngBytes = GeneratePng(qrCodeData, options);
                return EmbedLogoIntoImage(pngBytes, options);
            }

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

        private byte[] EmbedLogoIntoImage(byte[] pngBytes, QRCodeOptions options)
        {
            try
            {
                using (var qrBitmap = SKBitmap.Decode(pngBytes))
                {
                    if (qrBitmap == null)
                        return pngBytes;

                    var imageInfo = new SKImageInfo(qrBitmap.Width, qrBitmap.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
                    using (var workBitmap = new SKBitmap(imageInfo))
                    {
                        using (var canvas = new SKCanvas(workBitmap))
                        {
                            canvas.Clear(SKColors.White);
                            canvas.DrawBitmap(qrBitmap, 0, 0);
                            DrawLogoCenter(canvas, qrBitmap.Width, qrBitmap.Height);
                        }

                        if (options.Monochrome)
                            ApplyMonochromeFilter(workBitmap);

                        var skFormat = options.Format == ImageFormat.BMP
                            ? SKEncodedImageFormat.Bmp
                            : SKEncodedImageFormat.Png;

                        using (var image = SKImage.FromBitmap(workBitmap))
                        using (var data = image.Encode(skFormat, 100))
                        {
                            return data.ToArray();
                        }
                    }
                }
            }
            catch
            {
                // If logo embedding fails for any reason, return the plain QR code.
                return pngBytes;
            }
        }

        private void DrawLogoCenter(SKCanvas canvas, int width, int height)
        {
            var assembly = typeof(QRCodeImageGenerator).Assembly;
            const string resourceName = "FoxPrint.QrCodeGenerator.Resources.logo.svg";

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    return;

                using (var svg = new SKSvg())
                {
                    svg.Load(stream);

                    if (svg.Picture == null)
                        return;

                    var svgBounds = svg.Picture.CullRect;
                    if (svgBounds.Width <= 0 || svgBounds.Height <= 0)
                        return;

                    // Logo occupies 30% of the QR code width, height scaled to preserve aspect ratio.
                    float logoWidth = width * 0.30f;
                    float logoHeight = logoWidth * (svgBounds.Height / svgBounds.Width);

                    // White backing box with 10% padding on each side.
                    float padding = logoWidth * 0.10f;
                    float bgWidth = logoWidth + padding * 2;
                    float bgHeight = logoHeight + padding * 2;
                    float bgX = (width - bgWidth) / 2f;
                    float bgY = (height - bgHeight) / 2f;

                    using (var bgPaint = new SKPaint { Color = SKColors.White, IsAntialias = false })
                    {
                        canvas.DrawRect(bgX, bgY, bgWidth, bgHeight, bgPaint);
                    }

                    float scaleX = logoWidth / svgBounds.Width;
                    float scaleY = logoHeight / svgBounds.Height;

                    canvas.Save();
                    canvas.Translate(bgX + padding, bgY + padding);
                    canvas.Scale(scaleX, scaleY);
                    canvas.DrawPicture(svg.Picture);
                    canvas.Restore();
                }
            }
        }

        private static void ApplyMonochromeFilter(SKBitmap bitmap)
        {
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    byte luminance = (byte)(0.299f * pixel.Red + 0.587f * pixel.Green + 0.114f * pixel.Blue);
                    bitmap.SetPixel(x, y, luminance > 128 ? SKColors.White : SKColors.Black);
                }
            }
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
