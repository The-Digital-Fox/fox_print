using FoxPrint;
using Xunit;

namespace FoxPrint.QrCodeGenerator.Tests;

public class ReceiptQRGeneratorTests
{
    private const string TestStoreId = "store_abc_123";
    private const string TestSecret = "test-secret-key-12345";
    private const string TestTableNumber = "TABLE-15";

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        var generator = new ReceiptQRGenerator(TestStoreId, TestSecret);

        Assert.NotNull(generator);
        Assert.Equal(TestStoreId, generator.StoreId);
        Assert.Equal("https://api.foxnest.com", generator.BaseUrl);
    }

    [Fact]
    public void Constructor_WithCustomBaseUrl_UsesProvidedUrl()
    {
        var generator = new ReceiptQRGenerator(TestStoreId, TestSecret, "https://custom.api.com");

        Assert.Equal("https://custom.api.com", generator.BaseUrl);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyStoreId_ThrowsArgumentException(string? storeId)
    {
        Assert.Throws<ArgumentException>(() => new ReceiptQRGenerator(storeId!, TestSecret));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptySecret_ThrowsArgumentException(string? secret)
    {
        Assert.Throws<ArgumentException>(() => new ReceiptQRGenerator(TestStoreId, secret!));
    }

    [Fact]
    public void GenerateQRCodeUrl_WithValidTableNumber_ReturnsValidUrl()
    {
        var generator = new ReceiptQRGenerator(TestStoreId, TestSecret);

        var url = generator.GenerateQRCodeUrl(TestTableNumber);

        Assert.NotNull(url);
        Assert.StartsWith("https://api.foxnest.com/v1/scan/", url);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GenerateQRCodeUrl_WithEmptyTableNumber_ThrowsArgumentException(string? tableNumber)
    {
        var generator = new ReceiptQRGenerator(TestStoreId, TestSecret);

        Assert.Throws<ArgumentException>(() => generator.GenerateQRCodeUrl(tableNumber!));
    }

    [Fact]
    public void GenerateQRCodeUrl_WithColonInTableNumber_ThrowsArgumentException()
    {
        var generator = new ReceiptQRGenerator(TestStoreId, TestSecret);

        Assert.Throws<ArgumentException>(() => generator.GenerateQRCodeUrl("TABLE:15"));
    }

    [Fact]
    public void VerifySlug_WithValidSlug_ReturnsTrue()
    {
        var generator = new ReceiptQRGenerator(TestStoreId, TestSecret);
        var url = generator.GenerateQRCodeUrl(TestTableNumber);
        var slug = ReceiptQRGenerator.ExtractSlugFromUrl(url);

        var result = generator.VerifySlug(slug, out var extractedTableNumber);

        Assert.True(result);
        Assert.Equal(TestTableNumber, extractedTableNumber);
    }

    [Fact]
    public void VerifySlug_WithTamperedSlug_ReturnsFalse()
    {
        var generator = new ReceiptQRGenerator(TestStoreId, TestSecret);
        var url = generator.GenerateQRCodeUrl(TestTableNumber);
        var slug = ReceiptQRGenerator.ExtractSlugFromUrl(url);
        var tamperedSlug = slug.Substring(0, slug.Length - 5) + "XXXXX";

        var result = generator.VerifySlug(tamperedSlug, out _);

        Assert.False(result);
    }

    [Fact]
    public void VerifySlug_WithDifferentSecret_ReturnsFalse()
    {
        var generator1 = new ReceiptQRGenerator(TestStoreId, TestSecret);
        var generator2 = new ReceiptQRGenerator(TestStoreId, "different-secret");
        var url = generator1.GenerateQRCodeUrl(TestTableNumber);
        var slug = ReceiptQRGenerator.ExtractSlugFromUrl(url);

        var result = generator2.VerifySlug(slug, out _);

        Assert.False(result);
    }

    [Fact]
    public void ExtractSlugFromUrl_WithValidUrl_ReturnsSlug()
    {
        var url = "https://api.foxnest.com/v1/scan/abc123xyz";

        var slug = ReceiptQRGenerator.ExtractSlugFromUrl(url);

        Assert.Equal("abc123xyz", slug);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ExtractSlugFromUrl_WithEmptyUrl_ThrowsArgumentException(string? url)
    {
        Assert.Throws<ArgumentException>(() => ReceiptQRGenerator.ExtractSlugFromUrl(url!));
    }
}
