using System;
using System.IO;
using FoxPrint;
using FoxPrint.Models;

namespace ConsoleSample
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== FoxPrint - Receipt QR Code Generator ===");
            Console.WriteLine();

            // Configuration
            string storeId = "store_demo_123";
            string sharedSecret = "demo-secret-key-change-in-production";
            string tableNumber = "TABLE-15";

            try
            {
                // Initialize the QR code URL generator
                var qrGenerator = new ReceiptQRGenerator(storeId, sharedSecret);

                Console.WriteLine($"Store ID: {storeId}");
                Console.WriteLine($"Table Number: {tableNumber}");
                Console.WriteLine();

                // Generate QR code URL
                string qrUrl = qrGenerator.GenerateQRCodeUrl(tableNumber);
                Console.WriteLine($"QR Code URL:");
                Console.WriteLine($"{qrUrl}");
                Console.WriteLine();

                // Verify the slug
                string slug = ReceiptQRGenerator.ExtractSlugFromUrl(qrUrl);
                bool isValid = qrGenerator.VerifySlug(slug, out string extractedTableNumber);

                Console.WriteLine($"Slug Verification: {(isValid ? "VALID" : "INVALID")}");
                Console.WriteLine($"Extracted Table Number: {extractedTableNumber}");
                Console.WriteLine();

                // Initialize image generator
                var imageGenerator = new QRCodeImageGenerator();

                // Generate different formats
                Console.WriteLine("Generating QR code images...");
                Console.WriteLine();

                // Ensure output directory exists
                Directory.CreateDirectory("output");

                // 1. PNG format (general use)
                GenerateAndSave(imageGenerator, qrUrl, "output/qr_code.png",
                    new QRCodeOptions
                    {
                        Size = 200,
                        Format = ImageFormat.PNG,
                        ErrorCorrection = ErrorCorrectionLevel.Medium
                    },
                    "PNG format (200x200)");

                // 2. BMP format for thermal printer
                byte[] thermalQR = imageGenerator.GenerateForThermalPrinter(qrUrl, 200);
                File.WriteAllBytes("output/qr_thermal.bmp", thermalQR);
                Console.WriteLine($"  Thermal printer format: output/qr_thermal.bmp ({thermalQR.Length} bytes)");

                // 3. BMP format for receipt printer
                byte[] receiptQR = imageGenerator.GenerateForReceiptPrinter(qrUrl, 150);
                File.WriteAllBytes("output/qr_receipt.bmp", receiptQR);
                Console.WriteLine($"  Receipt printer format: output/qr_receipt.bmp ({receiptQR.Length} bytes)");

                // 4. High error correction (for damaged environments)
                GenerateAndSave(imageGenerator, qrUrl, "output/qr_high_ecc.png",
                    new QRCodeOptions
                    {
                        Size = 250,
                        Format = ImageFormat.PNG,
                        ErrorCorrection = ErrorCorrectionLevel.High
                    },
                    "High error correction (250x250)");

                Console.WriteLine();
                Console.WriteLine("=== Success! ===");
                Console.WriteLine("All QR codes have been generated in the 'output' directory.");
                Console.WriteLine();
                Console.WriteLine("Next steps:");
                Console.WriteLine("1. Open the images to verify they work");
                Console.WriteLine("2. Scan them with your phone to test the URL");
                Console.WriteLine("3. Integrate the library into your POS system");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            Console.WriteLine();
        }

        static void GenerateAndSave(QRCodeImageGenerator generator, string url, string filePath, QRCodeOptions options, string description)
        {
            byte[] imageBytes = generator.GenerateImage(url, options);
            File.WriteAllBytes(filePath, imageBytes);
            Console.WriteLine($"  {description}: {filePath} ({imageBytes.Length} bytes)");
        }
    }
}
