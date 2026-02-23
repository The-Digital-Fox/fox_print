using System;
using System.IO;
using FoxPrint;
using FoxPrint.Models;

namespace ConsoleSample
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("=== FoxPrint - Receipt QR Code Generator ===");
            Console.WriteLine();

            try
            {
                // Get configuration from user
                string storeId = PromptForInput("Enter Store ID");
                string sharedSecret = PromptForInput("Enter Shared Secret");
                string tablePart = PromptForInput("Enter Table Part");
                int fromTable = PromptForInt("Enter From Table Number");
                int toTable = PromptForInt("Enter To Table Number");
                FoxNestEnvironment environment = PromptForEnvironment();

                Console.WriteLine();
                Console.WriteLine("=== Configuration ===");
                Console.WriteLine($"Store ID: {storeId}");
                Console.WriteLine($"Environment: {environment} ({FoxNestEnvironments.GetUrl(environment)})");
                Console.WriteLine($"Table Range: {fromTable} - {toTable}");
                Console.WriteLine();

                // Validate table range
                if (fromTable > toTable)
                {
                    Console.WriteLine("ERROR: 'From Table' must be less than or equal to 'To Table'");
                    return;
                }

                // Initialize generators
                var qrGenerator = new ReceiptQRGenerator(storeId, sharedSecret, environment);
                var imageGenerator = new QRCodeImageGenerator();

                // Create output directory inside the ConsoleSample project folder
                string projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
                string outputDir = Path.Combine(projectDir, "output", storeId, environment.ToString().ToLowerInvariant());
                Directory.CreateDirectory(outputDir);

                Console.WriteLine($"Generating QR codes for {toTable - fromTable + 1} tables...");
                Console.WriteLine();

                Console.WriteLine("Logo embedding: ENABLED (High error correction applied automatically)");
                Console.WriteLine();

                int successCount = 0;
                for (int tableNum = fromTable; tableNum <= toTable; tableNum++)
                {
                    string tableNumber = tableNum.ToString();
                    string checkId = $"CHK-{tableNum:D5}"; // Example check ID

                    try
                    {
                        // Generate QR code URL
                        string qrUrl = qrGenerator.GenerateQRCodeUrl(tableNumber, checkId, string.IsNullOrEmpty(tablePart) ? "n/a" : tablePart);

                        // Generate PNG image with embedded FoxPrint logo
                        byte[] qrImage = imageGenerator.GenerateImage(qrUrl, new QRCodeOptions
                        {
                            Size = 400,
                            Format = ImageFormat.PNG,
                            EmbedLogo = true
                        });

                        // Save to file
                        string fileName = $"table_{tableNum:D3}.png";
                        string filePath = Path.Combine(outputDir, fileName);
                        File.WriteAllBytes(filePath, qrImage);

                        Console.WriteLine($"  Table {tableNum}: {filePath}");
                        Console.WriteLine($"    URL: {qrUrl}");
                        Console.WriteLine($"    Check ID: {checkId}");

                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  Table {tableNum}: ERROR - {ex.Message}");
                    }
                }

                Console.WriteLine();
                Console.WriteLine("=== Summary ===");
                Console.WriteLine($"Generated: {successCount}/{toTable - fromTable + 1} QR codes");
                Console.WriteLine($"Output Directory: {Path.GetFullPath(outputDir)}");
                Console.WriteLine();

                // Generate a verification test
                if (successCount > 0)
                {
                    Console.WriteLine("=== Verification ===");
                    string testTable = fromTable.ToString();
                    string testCheckId = $"CHK-{fromTable:D5}";
                    string testUrl = qrGenerator.GenerateQRCodeUrl(testTable, testCheckId);
                    string slug = ReceiptQRGenerator.ExtractSlugFromUrl(testUrl);
                    bool isValid = qrGenerator.VerifySlug(slug, out string extractedTable, out string extractedTablePart, out string extractedCheckId);

                    Console.WriteLine($"Test Table: {testTable}");
                    Console.WriteLine($"Test Check ID: {testCheckId}");
                    Console.WriteLine($"Slug Valid: {(isValid ? "YES" : "NO")}");
                    Console.WriteLine($"Extracted Table: {extractedTable}");
                    Console.WriteLine($"Extracted Check ID: {extractedCheckId}");
                    Console.WriteLine($"Extracted Table Part: {extractedTablePart}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            Console.WriteLine();
        }

        static string PromptForInput(string prompt)
        {
            Console.Write($"{prompt}: ");
            string input = Console.ReadLine()?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentException($"{prompt} cannot be empty");
            }

            return input;
        }

        static int PromptForInt(string prompt)
        {
            Console.Write($"{prompt}: ");
            string input = Console.ReadLine()?.Trim() ?? string.Empty;

            if (!int.TryParse(input, out int value) || value < 0)
            {
                throw new ArgumentException($"{prompt} must be a valid positive number");
            }

            return value;
        }

        static FoxNestEnvironment PromptForEnvironment()
        {
            Console.WriteLine();
            Console.WriteLine("Select Environment:");
            Console.WriteLine("  1. Local       (http://localhost:3007)");
            Console.WriteLine("  2. Development (https://dev-go-v2.thedigitalfox.com)");
            Console.WriteLine("  3. UAT         (https://uat-go-v2.thedigitalfox.com)");
            Console.WriteLine("  4. Production  (https://go.thedigitalfox.com)");
            Console.Write("Enter choice (1-4): ");

            string input = Console.ReadLine()?.Trim() ?? string.Empty;

            return input switch
            {
                "1" => FoxNestEnvironment.Local,
                "2" => FoxNestEnvironment.Development,
                "3" => FoxNestEnvironment.Uat,
                "4" => FoxNestEnvironment.Production,
                _ => throw new ArgumentException("Invalid environment selection. Please enter 1, 2, 3, or 4.")
            };
        }
    }
}
