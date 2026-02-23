namespace FoxPrint.Models
{
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
        /// Embed the FoxPrint logo in the center of the QR code (default: true).
        /// Automatically uses High error correction to ensure scannability.
        /// </summary>
        public bool EmbedLogo { get; set; } = true;

        /// <summary>
        /// Default options
        /// </summary>
        public static QRCodeOptions Default => new QRCodeOptions();

        /// <summary>
        /// Options optimized for thermal printers (80mm paper)
        /// </summary>
        public static QRCodeOptions ThermalPrinter(int size = 200) => new QRCodeOptions
        {
            Size = size,
            Format = ImageFormat.BMP,
            Monochrome = true,
            EmbedLogo = false,
            ErrorCorrection = ErrorCorrectionLevel.Medium,
            PrinterType = PrinterType.Thermal
        };

        /// <summary>
        /// Options optimized for receipt printers (58mm paper, ESC/POS)
        /// </summary>
        public static QRCodeOptions ReceiptPrinter(int size = 150) => new QRCodeOptions
        {
            Size = size,
            Format = ImageFormat.BMP,
            Monochrome = true,
            EmbedLogo = false,
            ErrorCorrection = ErrorCorrectionLevel.Low,
            PrinterType = PrinterType.Receipt
        };
    }

    /// <summary>
    /// Image format for QR code
    /// </summary>
    public enum ImageFormat
    {
        /// <summary>PNG format - good for general use</summary>
        PNG,

        /// <summary>BMP format - best for thermal/receipt printers</summary>
        BMP
    }

    /// <summary>
    /// Error correction level for QR code.
    /// Low = 7% recovery, Medium = 15%, Quartile = 25%, High = 30%
    /// </summary>
    public enum ErrorCorrectionLevel
    {
        /// <summary>Low error correction (7% recovery) - smallest QR code</summary>
        Low,

        /// <summary>Medium error correction (15% recovery) - recommended for most cases</summary>
        Medium,

        /// <summary>Quartile error correction (25% recovery) - good for damaged environments</summary>
        Quartile,

        /// <summary>High error correction (30% recovery) - largest QR code, maximum protection</summary>
        High
    }

    /// <summary>
    /// Printer type for optimization
    /// </summary>
    public enum PrinterType
    {
        /// <summary>No specific optimization</summary>
        None,

        /// <summary>Thermal printers (80mm paper, high contrast)</summary>
        Thermal,

        /// <summary>Receipt printers (58mm paper, ESC/POS compatible)</summary>
        Receipt,

        /// <summary>Laser/Inkjet printers (high resolution)</summary>
        Laser
    }
}
