# FoxPrint - Receipt QR Code Generator

[![NuGet](https://img.shields.io/nuget/v/FoxPrint.QrCodeGenerator.svg)](https://www.nuget.org/packages/FoxPrint.QrCodeGenerator)
[![NuGet Downloads](https://img.shields.io/nuget/dt/FoxPrint.QrCodeGenerator.svg)](https://www.nuget.org/packages/FoxPrint.QrCodeGenerator)
[![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-blue)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![CI](https://github.com/The-Digital-Fox/fox_print/actions/workflows/ci.yml/badge.svg)](https://github.com/The-Digital-Fox/fox_print/actions/workflows/ci.yml)

**FoxPrint** is a library that enables POS systems to generate cryptographically-signed QR codes for printing on receipts. These QR codes allow customers to scan and pay directly through FoxNest without requiring real-time API calls during receipt printing.

## Key Features

- **Stateless & Secure** - No database lookups, HMAC-SHA256 signed slugs
- **Multi-tenant by Design** - One POS integration handles multiple stores
- **Platform Agnostic** - .NET Standard 2.0 ensures compatibility with older systems
- **Zero Backend Dependency at Print Time** - POS generates QR codes client-side
- **Printer-Optimized** - Specialized formats for thermal and receipt printers
- **Multiple Image Formats** - PNG, BMP output

## Installation

```bash
dotnet add package FoxPrint.QrCodeGenerator
```

Or via Package Manager Console:

```powershell
Install-Package FoxPrint.QrCodeGenerator
```

Or add to your `.csproj`:

```xml
<PackageReference Include="FoxPrint.QrCodeGenerator" Version="1.0.0" />
```

## Quick Start

### Basic Usage (URL Generation Only)

```csharp
using FoxPrint;

// Initialize once per store (e.g., at application startup)
var generator = new ReceiptQRGenerator(
    storeId: "store_abc_123",
    sharedSecret: "your-shared-secret-from-foxnest"
);

// Generate QR code URL for each receipt
string qrUrl = generator.GenerateQRCodeUrl(tableNumber: "TABLE-15");

Console.WriteLine(qrUrl);
// Output: https://api.foxnest.com/v1/scan/c3RvcmVfYWJjXzEyMzpUQUJMRS0xNToxNzA0MDY3MjAwOmEzZjU...
```

### Complete Usage (With Image Generation)

```csharp
using FoxPrint;
using FoxPrint.Models;

// Initialize generators
var qrGenerator = new ReceiptQRGenerator("store_abc_123", "your-secret");
var imageGenerator = new QRCodeImageGenerator();

// Generate QR code URL
string qrUrl = qrGenerator.GenerateQRCodeUrl("TABLE-15");

// Generate QR code image for thermal printer (80mm paper)
byte[] thermalQR = imageGenerator.GenerateForThermalPrinter(qrUrl, size: 200);
File.WriteAllBytes("receipt_qr.bmp", thermalQR);

// Generate QR code image for receipt printer (58mm paper, ESC/POS)
byte[] receiptQR = imageGenerator.GenerateForReceiptPrinter(qrUrl, size: 150);
File.WriteAllBytes("receipt_qr_small.bmp", receiptQR);

// Custom options
byte[] customQR = imageGenerator.GenerateImage(qrUrl, new QRCodeOptions
{
    Size = 300,
    Format = ImageFormat.PNG,
    ErrorCorrection = ErrorCorrectionLevel.High,
    Monochrome = true
});
```

## API Reference

### ReceiptQRGenerator

```csharp
public class ReceiptQRGenerator
{
    // Constructor
    public ReceiptQRGenerator(string storeId, string sharedSecret, string baseUrl = "https://api.foxnest.com");

    // Generate QR code URL
    public string GenerateQRCodeUrl(string tableNumber);

    // Verify a QR code slug (for testing)
    public bool VerifySlug(string slug, out string tableNumber);

    // Extract slug from URL
    public static string ExtractSlugFromUrl(string url);

    // Properties
    public string StoreId { get; }
    public string BaseUrl { get; }
}
```

### QRCodeImageGenerator

```csharp
public class QRCodeImageGenerator
{
    // Generate image with options
    public byte[] GenerateImage(string url, QRCodeOptions options = null);

    // Generate and save to file
    public void GenerateImageToFile(string url, string filePath, QRCodeOptions options = null);

    // Convenience methods
    public byte[] GenerateForThermalPrinter(string url, int size = 200);
    public byte[] GenerateForReceiptPrinter(string url, int size = 150);
}
```

### QRCodeOptions

```csharp
public class QRCodeOptions
{
    public int Size { get; set; } = 200;                          // 50-1000 pixels
    public ImageFormat Format { get; set; } = ImageFormat.PNG;    // PNG, BMP
    public ErrorCorrectionLevel ErrorCorrection { get; set; }     // Low, Medium, Quartile, High
    public bool Monochrome { get; set; } = false;                 // Black & white only
    public PrinterType PrinterType { get; set; }                  // None, Thermal, Receipt, Laser

    // Factory methods
    public static QRCodeOptions ThermalPrinter(int size = 200);
    public static QRCodeOptions ReceiptPrinter(int size = 150);
}
```

## Error Correction Levels

| Level | Recovery | QR Size | Use Case |
|-------|----------|---------|----------|
| **Low** | 7% | Smallest | Clean environments (receipt printers) |
| **Medium** | 15% | Medium | Recommended for most cases |
| **Quartile** | 25% | Large | Dirty/damaged environments |
| **High** | 30% | Largest | Maximum protection |

## Printer Optimization

### Thermal Printers (80mm paper)

```csharp
byte[] qr = imageGenerator.GenerateForThermalPrinter(url, 200);
// - BMP format, Monochrome (high contrast), 200x200 pixels, Medium error correction
```

### Receipt Printers (58mm paper, ESC/POS)

```csharp
byte[] qr = imageGenerator.GenerateForReceiptPrinter(url, 150);
// - BMP format, Monochrome, 150x150 pixels, Low error correction (smaller QR code)
```

## Platform Compatibility

- .NET Framework 4.6.1+
- .NET Core 2.0+
- .NET 5, 6, 7, 8, 10+
- Mono, Xamarin, Unity (2018.1+)

## Security Features

| Feature | Description |
|---------|-------------|
| **HMAC-SHA256 Signature** | Cryptographically signed to prevent tampering |
| **Stateless Design** | No database lookups, fully self-contained |
| **Timestamp Included** | Supports expiration validation |
| **Constant-Time Comparison** | Prevents timing attacks |

## Building from Source

```bash
# Clone repository
git clone https://github.com/The-Digital-Fox/fox_print.git
cd fox_print

# Build
dotnet build

# Run tests
dotnet test

# Run sample
dotnet run --project samples/ConsoleSample
```

## Versioning

This project follows [Semantic Versioning 2.0.0](https://semver.org/):

- **MAJOR** version: Incompatible API changes
- **MINOR** version: New functionality (backwards compatible)
- **PATCH** version: Bug fixes (backwards compatible)

See [CHANGELOG.md](CHANGELOG.md) for version history.

## License

MIT License - see [LICENSE](LICENSE) file for details.

## Support

- **Technical Support:** dev@thedigitalfox.com
- **GitHub Issues:** https://github.com/The-Digital-Fox/fox_print/issues

---

**Version:** 1.0.0 | **Status:** Production Ready
