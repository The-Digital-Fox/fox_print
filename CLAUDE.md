# FoxPrint - Receipt QR Code Generation Library

## Project Overview

**FoxPrint** is a multi-platform library that enables POS systems to generate ephemeral, cryptographically-signed QR codes for printing on receipts. These QR codes allow customers to scan and pay directly through FoxNest without requiring real-time API calls during receipt printing.

### Key Design Principles

1. **Stateless & Secure**: No database lookups, HMAC-SHA256 signed slugs
2. **Multi-tenant by Design**: One POS integration handles multiple stores
3. **Platform Agnostic**: Starting with .NET Standard 2.0, expandable to other platforms
4. **Zero Backend Dependency at Print Time**: POS generates QR codes client-side
5. **Legacy Compatible**: .NET Standard 2.0 ensures compatibility with older systems

---

## Architecture

### Multi-Tenant Model

```
POS Integration (e.g., "Winmax")
├── Shared Secret: "winmax_prod_secret_xyz"
├── Store A (store_a_123)
├── Store B (store_b_456)
└── Store C (store_c_789)
```

**Key Points:**
- **One secret per POS integration** (not per store)
- **Slug contains Store ID** (POS ID determined by backend from secret or store lookup)
- Backend extracts Store ID → looks up POS integration → retrieves secret → verifies signature
- Scalable: Adding new stores requires no code changes
- POS systems only need to know `storeId` and `sharedSecret`

### Data Flow

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. POS System (Client-Side)                                      │
│    - Calls: GenerateQRCode(storeId, tableNumber, secret)        │
│    - Prints QR code on receipt                                   │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│ 2. Customer Scans QR Code                                        │
│    - URL: https://api.foxnest.com/v1/scan/{slug}                │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│ 3. FoxNest Backend                                               │
│    - Decodes slug                                                │
│    - Extracts: storeId, tableNumber, timestamp                  │
│    - Looks up storeId → finds posId and secret                  │
│    - Verifies HMAC-SHA256 signature with secret                 │
│    - Fetches order from POS using tableNumber                   │
│    - Redirects to payment flow                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Slug Structure

**Data Format:**
```
storeId:tableNumber:timestamp
```

**Example:**
```
store_abc_123:TABLE-15:1704067200
```

**Signed Slug (before Base64URL encoding):**
```
store_abc_123:TABLE-15:1704067200:a3f5d8e9c2b1...
                                  ^^^^^^^^^^^^^^
                                  HMAC-SHA256 signature
```

**Final URL:**
```
https://api.foxnest.com/v1/scan/c3RvcmVfYWJjXzEyMzpUQUJMRS0xNToxNzA0MDY3MjAwOmEzZjVkOGU5YzJiMQ
```

**Note:**
- The backend determines `posId` by looking up `storeId` in the database, which contains the POS provider information and associated secret
- **Table-based ordering**: POS systems expose APIs to query orders by `tableNumber`, not by check/invoice number
- The receipt is printed for a specific table, and that table number is encoded in the QR code

---

## Project Structure

```
fox_print/
├── CLAUDE.md                          # This file - project guide
├── WINMAX_RECEIPT_QR_INTEGRATION.md  # Original integration spec
├── README.md                          # Public-facing documentation
├── LICENSE
│
├── src/
│   ├── FoxPrint.Core/                # .NET Standard 2.0 library
│   │   ├── FoxPrint.Core.csproj
│   │   ├── ReceiptQRGenerator.cs     # Main public API (URL generation)
│   │   ├── Models/
│   │   │   ├── QRCodeRequest.cs
│   │   │   └── QRCodeResult.cs
│   │   ├── Security/
│   │   │   └── HmacSigner.cs
│   │   └── Encoding/
│   │       └── Base64UrlEncoder.cs
│   │
│   ├── FoxPrint.QRCode/              # QR Code image generation
│   │   ├── FoxPrint.QRCode.csproj
│   │   ├── QRCodeImageGenerator.cs   # Image generation API
│   │   ├── Models/
│   │   │   ├── QRCodeOptions.cs      # Size, format, error correction
│   │   │   └── ImageFormat.cs        # PNG, BMP, SVG
│   │   └── Printers/
│   │       ├── ThermalPrinterHelper.cs   # Thermal printer optimization
│   │       └── ReceiptPrinterHelper.cs   # Receipt printer formats
│   │
│   ├── FoxPrint.DotNet/              # .NET 10 modern implementation
│   │   └── FoxPrint.DotNet.csproj
│   │
│   └── FoxPrint.JavaScript/          # Future: JS/TS library
│       └── package.json
│
├── tests/
│   ├── FoxPrint.Core.Tests/
│   │   ├── FoxPrint.Core.Tests.csproj
│   │   ├── ReceiptQRGeneratorTests.cs
│   │   ├── HmacSignerTests.cs
│   │   └── IntegrationTests.cs
│   │
│   ├── FoxPrint.QRCode.Tests/
│   │   ├── FoxPrint.QRCode.Tests.csproj
│   │   ├── QRCodeImageGeneratorTests.cs
│   │   └── PrinterHelperTests.cs
│   │
│   └── TestVectors/                  # Known good test cases
│       └── test-vectors.json
│
├── samples/
│   ├── WinmaxSample/                 # Example integration
│   │   ├── Program.cs
│   │   ├── WinmaxSample.csproj
│   │   └── output/                   # Generated QR code images
│   │
│   ├── ConsoleSample/                # Interactive testing
│   │   ├── Program.cs
│   │   └── output/                   # Test QR codes
│   │
│   └── PrinterSample/                # Printer-specific examples
│       ├── ThermalPrinterExample.cs
│       ├── ReceiptPrinterExample.cs
│       └── output/
│
├── docs/
│   ├── integration-guide.md          # For POS integrators
│   ├── api-reference.md              # Library API docs
│   └── backend-implementation.md     # For backend team
│
├── .github/
│   └── workflows/
│       ├── build-test.yml            # CI: Build + test
│       ├── publish-nuget.yml         # CD: Publish to NuGet
│       └── publish-npm.yml           # Future: Publish to npm
│
├── .vscode/
│   ├── launch.json                   # Debug configurations
│   ├── tasks.json                    # Build tasks
│   └── settings.json                 # Workspace settings
│
└── scripts/
    ├── setup-dev-env.sh              # Linux dev setup
    ├── generate-test-vectors.sh      # Create test data
    └── publish-local.sh              # Local NuGet testing
```

---

## Implementation Phases

### Phase 1: Core Library (.NET Standard 2.0)
**Goal:** Production-ready library for POS integrations

**Deliverables:**
- [x] Project structure
- [ ] `FoxPrint.Core` library implementation (URL generation)
- [ ] `FoxPrint.QRCode` library implementation (image generation)
- [ ] Unit tests (>90% coverage)
- [ ] Integration tests with test vectors
- [ ] Sample console application with image output
- [ ] Printer-specific samples
- [ ] API documentation

### Phase 2: Packaging & Distribution
**Goal:** Easy consumption via NuGet

**Deliverables:**
- [ ] NuGet package configuration (.nuspec)
- [ ] Icon, README for NuGet listing
- [ ] GitHub Actions CI/CD pipeline
- [ ] Private/public NuGet feed setup
- [ ] Versioning strategy (SemVer)

### Phase 3: Backend Integration
**Goal:** FoxNest backend can decode and process QR codes

**Deliverables:**
- [ ] `/v1/scan/:slug` endpoint (NestJS)
- [ ] Multi-tenant secret management
- [ ] POS provider abstraction (`getOrderIdByCheckNumber`)
- [ ] Error handling & logging
- [ ] Analytics events

### Phase 4: Additional Platforms (Optional)
**Goal:** Support non-.NET POS systems

**Deliverables:**
- [ ] JavaScript/TypeScript library (npm)
- [ ] Python library (PyPI)
- [ ] Java library (Maven)
- [ ] Platform-specific CI/CD pipelines

---

## Development Environment Setup

### Prerequisites (Linux + VS Code)

1. **Install .NET 10 SDK**
   ```bash
   wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
   chmod +x dotnet-install.sh
   ./dotnet-install.sh --channel 10.0
   ```

2. **Verify Installation**
   ```bash
   dotnet --version  # Should show 10.x.x
   ```

3. **Install VS Code Extensions**
   ```bash
   code --install-extension ms-dotnettools.csdevkit
   code --install-extension ms-dotnettools.csharp
   code --install-extension ms-dotnettools.vscode-dotnet-runtime
   ```

4. **Create Solution**
   ```bash
   cd /home/nikola/digital_fox/fox_print
   dotnet new sln -n FoxPrint
   ```

5. **Create Core Library (.NET Standard 2.0)**
   ```bash
   mkdir -p src/FoxPrint.Core
   cd src/FoxPrint.Core
   dotnet new classlib -f netstandard2.0
   cd ../..
   dotnet sln add src/FoxPrint.Core/FoxPrint.Core.csproj
   ```

6. **Create Test Project**
   ```bash
   mkdir -p tests/FoxPrint.Core.Tests
   cd tests/FoxPrint.Core.Tests
   dotnet new xunit
   dotnet add reference ../../src/FoxPrint.Core/FoxPrint.Core.csproj
   cd ../..
   dotnet sln add tests/FoxPrint.Core.Tests/FoxPrint.Core.Tests.csproj
   ```

7. **Restore Dependencies**
   ```bash
   dotnet restore
   ```

8. **Build & Test**
   ```bash
   dotnet build
   dotnet test
   ```

### VS Code Configuration

**.vscode/tasks.json**
```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "build",
      "command": "dotnet",
      "type": "process",
      "args": ["build", "${workspaceFolder}/FoxPrint.sln"],
      "problemMatcher": "$msCompile"
    },
    {
      "label": "test",
      "command": "dotnet",
      "type": "process",
      "args": ["test", "${workspaceFolder}/FoxPrint.sln"],
      "problemMatcher": "$msCompile"
    }
  ]
}
```

**.vscode/launch.json**
```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core Launch (console)",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/samples/ConsoleSample/bin/Debug/net10.0/ConsoleSample.dll",
      "args": [],
      "cwd": "${workspaceFolder}/samples/ConsoleSample",
      "stopAtEntry": false,
      "console": "internalConsole"
    }
  ]
}
```

---

## Core Library API Design

### Public API (FoxPrint.Core) - URL Generation

```csharp
using System;

namespace FoxPrint.Core
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
            _baseUrl = baseUrl.TrimEnd('/');
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
        internal string GenerateQRCodeUrl(string tableNumber, DateTimeOffset timestamp)
        {
            if (string.IsNullOrWhiteSpace(tableNumber))
                throw new ArgumentException("Table number cannot be empty", nameof(tableNumber));

            // Validate no colons in data (reserved as delimiter)
            if (_storeId.Contains(":") || tableNumber.Contains(":"))
                throw new ArgumentException("Store ID and table number cannot contain colons");

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
        public bool VerifySlug(string slug, out string tableNumber)
        {
            try
            {
                string decoded = Base64UrlEncoder.Decode(slug);
                string[] parts = decoded.Split(':');

                if (parts.Length != 4)
                {
                    tableNumber = null;
                    return false;
                }

                string storeId = parts[0];
                tableNumber = parts[1];
                string timestamp = parts[2];
                string providedSignature = parts[3];

                if (storeId != _storeId)
                    return false;

                string data = $"{storeId}:{tableNumber}:{timestamp}";
                string expectedSignature = HmacSigner.Sign(data, _sharedSecret);

                return providedSignature == expectedSignature;
            }
            catch
            {
                tableNumber = null;
                return false;
            }
        }
    }
}
```

### Usage Example (Core Library - URL Generation Only)

```csharp
using FoxPrint.Core;

// Initialize once per store (e.g., at application startup)
var generator = new ReceiptQRGenerator(
    storeId: "store_abc_123",
    sharedSecret: "your-shared-secret-from-foxnest"
);

// Generate QR code URL for each receipt
string qrUrl = generator.GenerateQRCodeUrl(
    tableNumber: "TABLE-15"
);

// Print qrUrl as QR code on receipt
Console.WriteLine($"QR Code URL: {qrUrl}");
// Output: https://api.foxnest.com/v1/scan/c3RvcmVfYWJjXzEyMzpUQUJMRS0xNToxNzA0MDY3MjAwOmEzZjVkOGU5YzJiMQ...
```

### Usage Example (With QR Code Image Generation)

```csharp
using FoxPrint.Core;
using FoxPrint.QRCode;

// Initialize QR generator
var generator = new ReceiptQRGenerator(
    storeId: "store_abc_123",
    sharedSecret: "your-shared-secret-from-foxnest"
);

// Initialize image generator
var imageGenerator = new QRCodeImageGenerator();

// Generate QR code URL
string qrUrl = generator.GenerateQRCodeUrl(tableNumber: "TABLE-15");

// Generate QR code image for thermal printer
byte[] qrImageBytes = imageGenerator.GenerateImage(
    qrUrl,
    new QRCodeOptions
    {
        Size = 200,                          // 200x200 pixels
        Format = ImageFormat.PNG,
        ErrorCorrection = ErrorCorrectionLevel.Medium,
        PrinterType = PrinterType.Thermal    // Optimized for thermal printers
    }
);

// Save to file or send to printer
File.WriteAllBytes("receipt_qr.png", qrImageBytes);

// Or for receipt printers (BMP format, monochrome)
byte[] receiptQr = imageGenerator.GenerateImage(
    qrUrl,
    new QRCodeOptions
    {
        Size = 150,
        Format = ImageFormat.BMP,
        Monochrome = true,                   // Black and white only
        PrinterType = PrinterType.Receipt
    }
);
```

---

## QR Code Image Generation API (FoxPrint.QRCode)

### Public API

```csharp
using System;
using System.IO;

namespace FoxPrint.QRCode
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
                throw new ArgumentException("Size must be between 50 and 1000 pixels", nameof(options));

            // Generate QR code using QRCoder library (compatible with .NET Standard 2.0)
            var qrGenerator = new QRCoder.QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(url, options.ErrorCorrection);

            // Generate image based on format
            return options.Format switch
            {
                ImageFormat.PNG => GeneratePng(qrCodeData, options),
                ImageFormat.BMP => GenerateBmp(qrCodeData, options),
                ImageFormat.SVG => GenerateSvg(qrCodeData, options),
                _ => throw new NotSupportedException($"Format {options.Format} not supported")
            };
        }

        /// <summary>
        /// Generate QR code and save directly to file
        /// </summary>
        public void GenerateImageToFile(string url, string filePath, QRCodeOptions options = null)
        {
            byte[] imageBytes = GenerateImage(url, options);
            File.WriteAllBytes(filePath, imageBytes);
        }

        /// <summary>
        /// Generate QR code optimized for thermal printer (monochrome, BMP)
        /// </summary>
        public byte[] GenerateForThermalPrinter(string url, int size = 200)
        {
            return GenerateImage(url, new QRCodeOptions
            {
                Size = size,
                Format = ImageFormat.BMP,
                Monochrome = true,
                ErrorCorrection = ErrorCorrectionLevel.Medium,
                PrinterType = PrinterType.Thermal
            });
        }

        /// <summary>
        /// Generate QR code optimized for receipt printer (ESC/POS compatible)
        /// </summary>
        public byte[] GenerateForReceiptPrinter(string url, int size = 150)
        {
            return GenerateImage(url, new QRCodeOptions
            {
                Size = size,
                Format = ImageFormat.BMP,
                Monochrome = true,
                ErrorCorrection = ErrorCorrectionLevel.Low,  // Smaller QR code
                PrinterType = PrinterType.Receipt
            });
        }

        private byte[] GeneratePng(QRCodeData qrCodeData, QRCodeOptions options)
        {
            var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
            return qrCode.GetGraphic(
                pixelsPerModule: options.Size / 33,  // 33 modules is typical QR code size
                darkColor: options.Monochrome ? new byte[] { 0, 0, 0 } : new byte[] { 0, 0, 0 },
                lightColor: options.Monochrome ? new byte[] { 255, 255, 255 } : new byte[] { 255, 255, 255 }
            );
        }

        private byte[] GenerateBmp(QRCodeData qrCodeData, QRCodeOptions options)
        {
            var qrCode = new QRCoder.BitmapByteQRCode(qrCodeData);
            return qrCode.GetGraphic(
                pixelsPerModule: options.Size / 33,
                darkColor: options.Monochrome ? new byte[] { 0, 0, 0 } : new byte[] { 0, 0, 0 },
                lightColor: options.Monochrome ? new byte[] { 255, 255, 255 } : new byte[] { 255, 255, 255 }
            );
        }

        private byte[] GenerateSvg(QRCodeData qrCodeData, QRCodeOptions options)
        {
            var qrCode = new QRCoder.SvgQRCode(qrCodeData);
            string svgString = qrCode.GetGraphic(
                pixelsPerModule: options.Size / 33,
                darkColor: "#000000",
                lightColor: "#FFFFFF"
            );
            return System.Text.Encoding.UTF8.GetBytes(svgString);
        }
    }

    /// <summary>
    /// Options for QR code image generation
    /// </summary>
    public class QRCodeOptions
    {
        /// <summary>
        /// Size in pixels (default: 200)
        /// </summary>
        public int Size { get; set; } = 200;

        /// <summary>
        /// Image format (default: PNG)
        /// </summary>
        public ImageFormat Format { get; set; } = ImageFormat.PNG;

        /// <summary>
        /// Error correction level (default: Medium)
        /// </summary>
        public ErrorCorrectionLevel ErrorCorrection { get; set; } = ErrorCorrectionLevel.Medium;

        /// <summary>
        /// Generate monochrome image (black and white only, default: false)
        /// </summary>
        public bool Monochrome { get; set; } = false;

        /// <summary>
        /// Optimize for specific printer type (default: None)
        /// </summary>
        public PrinterType PrinterType { get; set; } = PrinterType.None;

        /// <summary>
        /// Default options
        /// </summary>
        public static QRCodeOptions Default => new QRCodeOptions();
    }

    /// <summary>
    /// Image format for QR code
    /// </summary>
    public enum ImageFormat
    {
        PNG,
        BMP,
        SVG
    }

    /// <summary>
    /// Error correction level for QR code
    /// Low = 7% recovery, Medium = 15%, Quartile = 25%, High = 30%
    /// </summary>
    public enum ErrorCorrectionLevel
    {
        Low,        // Good for clean printing (receipt printers)
        Medium,     // Recommended for most use cases
        Quartile,   // Good for damaged/dirty environments
        High        // Maximum error correction (larger QR code)
    }

    /// <summary>
    /// Printer type for optimization
    /// </summary>
    public enum PrinterType
    {
        None,       // No specific optimization
        Thermal,    // Thermal printers (80mm paper, high contrast)
        Receipt,    // Receipt printers (58mm paper, ESC/POS compatible)
        Laser       // Laser/Inkjet printers (high resolution)
    }
}
```

### Dependencies

**FoxPrint.QRCode.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="QRCoder" Version="1.4.3" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../FoxPrint.Core/FoxPrint.Core.csproj" />
  </ItemGroup>
</Project>
```

**Note:** QRCoder is a popular, well-maintained .NET library compatible with .NET Standard 2.0, making it perfect for legacy POS systems.

---

## Backend Implementation (NestJS)

### Multi-Tenant Secret Management

**Database Schema (Store Configuration):**
```typescript
// stores table
{
  id: string;
  pos_provider: string; // "winmax", "toast", etc.
  pos_config: {
    pos_id: string;           // "winmax_prod"
    qr_shared_secret: string; // Stored per POS integration
    // ... other config
  }
}
```

**Configuration Service:**
```typescript
// pos-config.service.ts
async getSharedSecretForPosId(posId: string): Promise<string> {
  // Lookup secret by posId
  // Could be cached in memory for performance
  const config = await this.configRepository.findByPosId(posId);
  return config.qr_shared_secret;
}
```

### Scan Endpoint Implementation

**Controller:**
```typescript
// pos-public.controller.ts
@Get('/scan/:slug')
@Public()
@ApiOperation({ summary: 'Scan receipt QR code and redirect to payment' })
@ApiResponse({ status: 302, description: 'Redirect to payment URL' })
@ApiResponse({ status: 400, description: 'Invalid or tampered QR code' })
@Redirect(undefined, 302)
async scanReceiptQRCode(@Param('slug') slug: string) {
  return {
    url: await this.posService.processReceiptQRCode(slug),
  };
}
```

**Service:**
```typescript
// pos.service.ts
async processReceiptQRCode(slug: string): Promise<string> {
  try {
    // 1. Decode base64url
    const base64 = slug.replace(/-/g, '+').replace(/_/g, '/');
    const decoded = Buffer.from(base64, 'base64').toString('utf-8');

    // 2. Parse components: storeId:tableNumber:timestamp:signature
    const parts = decoded.split(':');
    if (parts.length !== 4) {
      throw new Error('Invalid QR code format');
    }

    const [storeId, tableNumber, timestamp, providedSignature] = parts;

    // 3. Look up store configuration to get POS provider and secret
    const { config, posProvider } = await this.authService.initializePosConfiguration(storeId);
    const sharedSecret = config.qr_shared_secret || process.env.QR_SHARED_SECRET;

    if (!sharedSecret) {
      this.logger.error(`No shared secret found for store: ${storeId}`);
      throw new Error('Invalid store configuration');
    }

    // 4. Verify signature
    const data = `${storeId}:${tableNumber}:${timestamp}`;
    const expectedSignature = this.generateSignature(data, sharedSecret);

    if (providedSignature !== expectedSignature) {
      this.logger.error('QR code signature verification failed', { storeId, tableNumber });
      throw new Error('Invalid or tampered QR code');
    }

    // 5. Optional: Verify timestamp freshness
    const maxAge = 24 * 60 * 60; // 24 hours
    const now = Math.floor(Date.now() / 1000);
    const qrAge = now - parseInt(timestamp);
    if (qrAge > maxAge) {
      this.logger.warn('QR code expired', { storeId, tableNumber, qrAge });
      throw new Error('QR code expired');
    }

    // 6. Get provider for this POS
    const provider = this.providerFactory.getProvider(posProvider);

    // 7. Fetch order using tableNumber (POS systems expose table-based APIs)
    const orderId = await provider.getOrderIdByTableNumber(storeId, tableNumber);

    if (!orderId) {
      this.logger.info('No order found for table', { storeId, tableNumber });
      return this.urlBuilderService.getErrorPageUrl('no-bill');
    }

    // 8. Get redirect URL
    const baseUrl = config.redirect_url || process.env.REDIRECT_URL;

    // 9. Build redirect URL
    const urlWithParams = this.urlBuilderService.buildRedirectUrl(
      {
        storeId,
        tableNumber,
        tablePart: undefined,
        url: baseUrl,
        orderType: OrderType.dine_in,
        firstTimeScan: true,
        isReceiptQR: true,  // Flag to indicate this is from a receipt QR
      },
      orderId,
    );

    // 10. Emit analytics event
    this.eventBus.emit(POS_EVENTS.QR_SCANNED, {
      storeId: config.establishment_name ?? storeId,
      qrCodeId: slug,
      qrType: 'receipt',
      posProvider,
      status: 'success',
      tableNumber,
    });

    return urlWithParams;
  } catch (error) {
    this.logger.error(`Failed to process receipt QR code: ${error.message}`);
    return this.urlBuilderService.getErrorPageUrl('invalid-qr');
  }
}

private generateSignature(data: string, secret: string): string {
  const hmac = crypto.createHmac('sha256', secret);
  hmac.update(data);
  return hmac.digest('hex');
}
```

---

## Packaging & Distribution

### NuGet Package Configuration

**FoxPrint.Core.csproj (relevant sections):**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>

    <!-- Package Metadata -->
    <PackageId>FoxPrint.Core</PackageId>
    <Version>1.0.0</Version>
    <Authors>Digital Fox</Authors>
    <Company>Digital Fox</Company>
    <Product>FoxPrint</Product>
    <Description>Generate cryptographically-signed QR codes for FoxNest receipt printing. Enables POS systems to create ephemeral QR codes without real-time API calls.</Description>
    <PackageTags>qr-code;pos;payment;receipt;foxnest</PackageTags>
    <PackageProjectUrl>https://github.com/The-Digital-Fox/fox_print</PackageProjectUrl>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageIcon>icon.png</PackageIcon>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <RepositoryUrl>https://github.com/The-Digital-Fox/fox_print</RepositoryUrl>
    <RepositoryType>git</RepositoryType>

    <!-- Build Configuration -->
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
  </PropertyGroup>

  <ItemGroup>
    <None Include="../../icon.png" Pack="true" PackagePath="/" />
    <None Include="../../README.md" Pack="true" PackagePath="/" />
  </ItemGroup>
</Project>
```

### CI/CD Pipeline (GitHub Actions)

**.github/workflows/publish-nuget.yml:**
```yaml
name: Publish to NuGet

on:
  push:
    tags:
      - 'v*.*.*'  # Trigger on version tags (e.g., v1.0.0)

jobs:
  publish:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '10.0.x'

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --configuration Release --no-restore

    - name: Test
      run: dotnet test --configuration Release --no-build --verbosity normal

    - name: Pack
      run: dotnet pack src/FoxPrint.Core/FoxPrint.Core.csproj --configuration Release --no-build --output ./nupkgs

    - name: Push to NuGet
      run: dotnet nuget push ./nupkgs/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json --skip-duplicate
```

---

## Testing Strategy

### Unit Tests

**Test Categories:**
1. **HMAC Signature Generation** - Verify correct signature creation
2. **Base64URL Encoding** - Test URL-safe encoding
3. **Data Validation** - Ensure proper input validation
4. **Slug Parsing** - Test decoding and verification
5. **Error Handling** - Invalid inputs, tampered signatures

**Example Test:**
```csharp
[Fact]
public void GenerateQRCodeUrl_ValidInputs_ReturnsCorrectFormat()
{
    // Arrange
    var generator = new QRCodeGenerator("winmax", "test-secret");

    // Act
    string url = generator.GenerateQRCodeUrl("store_123", "CHK-001");

    // Assert
    Assert.StartsWith("https://api.foxnest.com/v1/scan/", url);
    Assert.True(generator.VerifySlug(url.Split('/').Last(), out var storeId, out var checkNumber));
    Assert.Equal("store_123", storeId);
    Assert.Equal("CHK-001", checkNumber);
}

[Fact]
public void GenerateQRCodeUrl_TamperedSlug_FailsVerification()
{
    // Arrange
    var generator = new QRCodeGenerator("winmax", "test-secret");
    string url = generator.GenerateQRCodeUrl("store_123", "CHK-001");
    string slug = url.Split('/').Last();

    // Tamper with slug
    string tamperedSlug = slug.Substring(0, slug.Length - 5) + "xxxxx";

    // Act & Assert
    Assert.False(generator.VerifySlug(tamperedSlug, out _, out _));
}
```

### Integration Tests

**Test Vectors (test-vectors.json):**
```json
{
  "vectors": [
    {
      "posId": "winmax",
      "storeId": "store_abc_123",
      "checkNumber": "CHK-45678",
      "timestamp": 1704067200,
      "secret": "test-secret-key-123",
      "expectedSignature": "a3f5d8e9c2b1...",
      "expectedSlug": "d2lubWF4OnN0b3JlX2FiY18xMjM...",
      "expectedUrl": "https://api.foxnest.com/v1/scan/d2lubWF4OnN0b3JlX2FiY18xMjM..."
    }
  ]
}
```

---

## Security Considerations

### Secret Management

1. **POS Side (Client):**
   - Store secret in secure configuration (encrypted config file, environment variables)
   - NEVER hardcode secrets in source code
   - Rotate secrets periodically (coordinate with FoxNest team)

2. **Backend Side:**
   - Store secrets encrypted at rest in database
   - Use environment variables or secret management service (AWS Secrets Manager, Azure Key Vault)
   - Implement secret rotation mechanism

### Signature Verification

- Always use constant-time comparison for signature verification to prevent timing attacks
- Log failed verification attempts for security monitoring
- Implement rate limiting on `/v1/scan/:slug` endpoint

### Timestamp Validation

- Reject QR codes older than 24 hours (configurable)
- Prevents replay attacks with old receipts
- Consider timezone handling (always use UTC)

---

## Deployment Checklist

### Phase 1: Development
- [ ] Set up development environment (.NET 10 + VS Code)
- [ ] Create solution structure
- [ ] Implement core library (FoxPrint.Core)
- [ ] Write unit tests (>90% coverage)
- [ ] Create sample applications
- [ ] Generate test vectors

### Phase 2: Testing
- [ ] Integration testing with test vectors
- [ ] Security audit (signature verification, input validation)
- [ ] Performance testing (1M+ QR code generations)
- [ ] Cross-platform testing (Windows, Linux, macOS)
- [ ] Legacy .NET Framework compatibility testing (.NET Framework 4.6.1+)

### Phase 3: Backend Integration
- [ ] Implement `/v1/scan/:slug` endpoint
- [ ] Multi-tenant secret management
- [ ] POS provider abstraction
- [ ] Error page URLs configured
- [ ] Analytics events integrated
- [ ] Monitoring & alerting set up

### Phase 4: Packaging
- [ ] NuGet package configuration
- [ ] Package icon & README
- [ ] CI/CD pipeline (GitHub Actions)
- [ ] Private NuGet feed (for testing)
- [ ] Public NuGet feed (for production)

### Phase 5: Documentation
- [ ] API reference documentation
- [ ] Integration guide for POS developers
- [ ] Backend implementation guide
- [ ] Security best practices guide
- [ ] Troubleshooting guide

### Phase 6: Deployment
- [ ] Generate unique secrets for each POS integration
- [ ] Configure secrets in backend
- [ ] Deploy backend endpoint (dev → uat → prod)
- [ ] Publish NuGet package
- [ ] Notify integration partners
- [ ] Monitor for issues

---

## Next Steps

### Immediate Actions (Phase 1)

1. **Environment Setup:**
   ```bash
   # Install .NET 10 SDK
   ./dotnet-install.sh --channel 10.0

   # Install VS Code extensions
   code --install-extension ms-dotnettools.csdevkit

   # Create solution
   dotnet new sln -n FoxPrint
   ```

2. **Project Scaffolding:**
   ```bash
   # Create library project
   dotnet new classlib -n FoxPrint.Core -f netstandard2.0 -o src/FoxPrint.Core

   # Create test project
   dotnet new xunit -n FoxPrint.Core.Tests -o tests/FoxPrint.Core.Tests

   # Add references
   dotnet add tests/FoxPrint.Core.Tests reference src/FoxPrint.Core
   ```

3. **Implementation Priority:**
   - [ ] `HmacSigner.cs` - Core security component
   - [ ] `Base64UrlEncoder.cs` - URL-safe encoding
   - [ ] `QRCodeGenerator.cs` - Public API
   - [ ] Unit tests for each component
   - [ ] Integration tests with test vectors

---

## FAQ

### Q: Why .NET Standard 2.0 instead of .NET 10?
**A:** Legacy POS systems often run on older .NET Framework versions. .NET Standard 2.0 provides compatibility with .NET Framework 4.6.1+, .NET Core 2.0+, and .NET 5+. This maximizes adoption.

### Q: Can we use .NET 10 features?
**A:** Create a separate `FoxPrint.DotNet` library targeting .NET 10 for modern systems. The core library must remain .NET Standard 2.0 for maximum compatibility.

### Q: Why one secret per POS instead of per store?
**A:** Scalability. A POS integration with 1000 stores would require managing 1000 secrets. With one secret per POS, we only manage one, and the slug contains store identification.

### Q: How do we handle secret rotation?
**A:** Implement versioned secrets. The slug could include a key version indicator, allowing graceful migration. Coordinate rotation with POS partners.

### Q: What if a QR code is scanned twice?
**A:** The backend should handle idempotency. If an order is already paid, redirect to a "receipt" or "thank you" page instead of payment flow.

### Q: Can we add more data to the slug?
**A:** Yes, but keep it minimal. Every extra field increases QR code size. Current design is optimized for small, scannable QR codes.

---

## Contact & Support

**Project Lead:** Nikola
**Repository:** https://github.com/The-Digital-Fox/fox_print
**Documentation:** https://docs.thedigitalfox.com/integrations/receipt-qr
**Support:** dev@thedigitalfox.com

---

**Version:** 1.0.0
**Last Updated:** 2026-01-13
**Status:** Project Initialization Phase
