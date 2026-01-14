using FoxPrint;
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
        var options = new QRCodeOptions { Format = ImageFormat.BMP };

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
}
