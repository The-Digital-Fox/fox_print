# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Nothing yet

## [1.0.1] - 2026-01-28

### Added
- **Check ID parameter**: `GenerateQRCodeUrl` now requires `checkId` (check/invoice ID from POS) as a parameter
- **Table Part parameter**: Added `tablePart` parameter for table section identifier (defaults to "n/a")
- **Environment support**: New `FoxNestEnvironment` enum for targeting different API environments (Local, Development, UAT, Production)
- **FoxNestEnvironments helper**: Static class providing URL mappings for all supported environments
- **Environment-based constructor**: New constructor accepting `FoxNestEnvironment` enum instead of hardcoded URLs
- **Enhanced slug verification**: Multiple `VerifySlug` overloads to extract different combinations of data (tableNumber, tablePart, checkId)
- **StoreId property**: Public getter to access the configured store ID
- **BaseUrl property**: Public getter to access the configured base URL

### Changed
- **BREAKING**: `GenerateQRCodeUrl` signature changed from `GenerateQRCodeUrl(string tableNumber)` to `GenerateQRCodeUrl(string tableNumber, string checkId, string tablePart = "n/a")`
- **Slug format updated**: Now includes `storeId:tableNumber:tablePart:checkId:timestamp` (previously `storeId:tableNumber:timestamp`)
- **Enhanced validation**: Added validation for checkId and tablePart to prevent colon characters

### Migration Guide
If upgrading from v1.0.0, update your code:
```csharp
// Old (v1.0.0)
string url = generator.GenerateQRCodeUrl("TABLE-15");

// New (v1.0.1+)
string url = generator.GenerateQRCodeUrl("TABLE-15", "CHK-12345");
// or with table part
string url = generator.GenerateQRCodeUrl("TABLE-15", "CHK-12345", "SECTION-A");
```

## [1.0.0] - 2026-01-14

### Added

- `ReceiptQRGenerator` class for generating cryptographically-signed QR code URLs
- `QRCodeImageGenerator` class for generating QR code images
- HMAC-SHA256 signature generation for secure, tamper-proof QR codes
- Base64URL encoding following RFC 4648 specification
- Multi-tenant support with store-specific QR codes
- Timestamp inclusion for expiration validation
- `VerifySlug` method for signature verification
- `ExtractSlugFromUrl` helper method
- Constant-time signature comparison to prevent timing attacks
- Support for PNG and BMP image formats
- `GenerateForThermalPrinter` convenience method (80mm paper optimization)
- `GenerateForReceiptPrinter` convenience method (58mm paper, ESC/POS)
- Configurable error correction levels (Low, Medium, Quartile, High)
- Monochrome mode for thermal printers
- `QRCodeOptions` configuration class with factory methods
- .NET Standard 2.0 target for maximum compatibility

### Security
- HMAC-SHA256 cryptographic signing
- Constant-time comparison for signature verification
- Input validation to prevent injection attacks

## Versioning Strategy

This project follows [Semantic Versioning 2.0.0](https://semver.org/):

- **MAJOR** version: Incompatible API changes
- **MINOR** version: New functionality in a backwards compatible manner
- **PATCH** version: Backwards compatible bug fixes

### Pre-release Versions

Pre-release versions use suffixes:
- `-alpha.N` - Early development, unstable API
- `-beta.N` - Feature complete, testing phase
- `-rc.N` - Release candidate, final testing

Example: `1.1.0-beta.1`

[Unreleased]: https://github.com/The-Digital-Fox/fox_print/compare/v1.0.1...HEAD
[1.0.1]: https://github.com/The-Digital-Fox/fox_print/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/The-Digital-Fox/fox_print/releases/tag/v1.0.0
