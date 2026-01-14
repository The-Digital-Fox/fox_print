# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Nothing yet

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

[Unreleased]: https://github.com/The-Digital-Fox/fox_print/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/The-Digital-Fox/fox_print/releases/tag/v1.0.0
