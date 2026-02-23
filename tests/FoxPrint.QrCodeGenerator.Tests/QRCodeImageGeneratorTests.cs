using FoxPrint.Models;
using Xunit;

namespace FoxPrint.QrCodeGenerator.Tests;

public class QRCodeImageGeneratorTests
{
    private const string TestUrl = "https://api.foxnest.com/v1/scan/test123";

    [Fact]
    public void GenerateImage_WithDefaultOptions_ReturnsPngBytes()
    {
        var generator = new QRCodeImageGenerator();

        var result = generator.GenerateImage(TestUrl);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
        // PNG magic bytes
        Assert.Equal(0x89, result[0]);
        Assert.Equal(0x50, result[1]); // P
        Assert.Equal(0x4E, result[2]); // N
        Assert.Equal(0x47, result[3]); // G
    }

    [Fact]
    public void GenerateImage_WithBmpFormat_ReturnsBmpBytes()
    {
        var generator = new QRCodeImageGenerator();
        var options = new QRCodeOptions { Format = ImageFormat.BMP, EmbedLogo = false };

        var result = generator.GenerateImage(TestUrl, options);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
        // BMP magic bytes
        Assert.Equal((byte)'B', result[0]);
        Assert.Equal((byte)'M', result[1]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GenerateImage_WithEmptyUrl_ThrowsArgumentException(string? url)
    {
        var generator = new QRCodeImageGenerator();

        Assert.Throws<ArgumentException>(() => generator.GenerateImage(url!));
    }

    [Theory]
    [InlineData(49)]
    [InlineData(1001)]
    public void GenerateImage_WithInvalidSize_ThrowsArgumentException(int size)
    {
        var generator = new QRCodeImageGenerator();
        var options = new QRCodeOptions { Size = size };

        Assert.Throws<ArgumentException>(() => generator.GenerateImage(TestUrl, options));
    }

    [Fact]
    public void GenerateForThermalPrinter_ReturnsMonochromeBmp()
    {
        var generator = new QRCodeImageGenerator();

        var result = generator.GenerateForThermalPrinter(TestUrl);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
        // BMP magic bytes
        Assert.Equal((byte)'B', result[0]);
        Assert.Equal((byte)'M', result[1]);
    }

    [Fact]
    public void GenerateForReceiptPrinter_ReturnsMonochromeBmp()
    {
        var generator = new QRCodeImageGenerator();

        var result = generator.GenerateForReceiptPrinter(TestUrl);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
        // BMP magic bytes
        Assert.Equal((byte)'B', result[0]);
        Assert.Equal((byte)'M', result[1]);
    }

    [Theory]
    [InlineData(ErrorCorrectionLevel.Low)]
    [InlineData(ErrorCorrectionLevel.Medium)]
    [InlineData(ErrorCorrectionLevel.Quartile)]
    [InlineData(ErrorCorrectionLevel.High)]
    public void GenerateImage_WithDifferentErrorCorrection_Succeeds(ErrorCorrectionLevel level)
    {
        var generator = new QRCodeImageGenerator();
        var options = new QRCodeOptions { ErrorCorrection = level };

        var result = generator.GenerateImage(TestUrl, options);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public void GenerateImageToFile_CreatesFile()
    {
        var generator = new QRCodeImageGenerator();
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_qr_{Guid.NewGuid()}.png");

        try
        {
            generator.GenerateImageToFile(TestUrl, tempFile);

            Assert.True(File.Exists(tempFile));
            var bytes = File.ReadAllBytes(tempFile);
            Assert.True(bytes.Length > 0);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    // --- Logo embedding tests ---

    [Fact]
    public void LogoResource_IsEmbeddedInAssembly()
    {
        var assembly = typeof(QRCodeImageGenerator).Assembly;
        var names = assembly.GetManifestResourceNames();

        // Surface all embedded names in the failure message to diagnose mismatches.
        var found = Array.Exists(names, n => n == "FoxPrint.Resources.logo.svg");
        Assert.True(found,
            $"Expected embedded resource 'FoxPrint.Resources.logo.svg' not found. " +
            $"Available resources: [{string.Join(", ", names)}]");
    }

    [Fact]
    public void LogoResource_CanBeLoadedAsStream()
    {
        var assembly = typeof(QRCodeImageGenerator).Assembly;
        using var stream = assembly.GetManifestResourceStream("FoxPrint.Resources.logo.svg");

        Assert.NotNull(stream);
        Assert.True(stream!.Length > 0);
    }

    [Fact]
    public void GenerateImage_WithEmbedLogo_ReturnsPngBytes()
    {
        var generator = new QRCodeImageGenerator();
        var options = new QRCodeOptions { EmbedLogo = true, Format = ImageFormat.PNG };

        var result = generator.GenerateImage(TestUrl, options);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
        Assert.Equal(0x89, result[0]);
        Assert.Equal(0x50, result[1]); // P
        Assert.Equal(0x4E, result[2]); // N
        Assert.Equal(0x47, result[3]); // G
    }

    [Fact]
    public void GenerateImage_WithEmbedLogo_BytesDifferFromSameEccPlainQr()
    {
        // Both images use High ECC (EmbedLogo=true forces it). The ONLY expected
        // difference is the logo composited in the center. Identical bytes means
        // EmbedLogoIntoImage silently fell back without drawing anything.
        var generator = new QRCodeImageGenerator();
        const int size = 300;

        // Plain QR at High ECC — same matrix as the logo version, no logo drawn.
        var plainBytes = generator.GenerateImage(TestUrl, new QRCodeOptions
        {
            Size = size,
            Format = ImageFormat.PNG,
            EmbedLogo = false,
            ErrorCorrection = ErrorCorrectionLevel.High
        });

        // Logo QR — also uses High ECC (forced internally), logo should be composited.
        var logoBytes = generator.GenerateImage(TestUrl, new QRCodeOptions
        {
            Size = size,
            Format = ImageFormat.PNG,
            EmbedLogo = true
        });

        Assert.False(plainBytes.SequenceEqual(logoBytes),
            "Logo-embedded PNG is byte-for-byte identical to the same-ECC plain QR code — " +
            "DrawLogoCenter is not running or EmbedLogoIntoImage is silently falling back.");
    }

    [Fact]
    public void GenerateImage_WithEmbedLogoAndBmpFormat_ReturnsPngBytes()
    {
        // SkiaSharp does not support BMP encoding; when EmbedLogo=true the output
        // is always PNG regardless of the requested format.
        var generator = new QRCodeImageGenerator();
        var options = new QRCodeOptions { EmbedLogo = true, Format = ImageFormat.BMP };

        var result = generator.GenerateImage(TestUrl, options);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
        Assert.Equal(0x89, result[0]);
        Assert.Equal(0x50, result[1]); // P
        Assert.Equal(0x4E, result[2]); // N
        Assert.Equal(0x47, result[3]); // G
    }
}
