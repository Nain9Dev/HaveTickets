using HaveTickets.Application.Abstractions;
using QRCoder;

namespace HaveTickets.Infrastructure.Services;

/// <summary>
/// Renders QR codes as PNG bytes.
/// <para>
/// Uses QRCoder's managed PNG writer rather than System.Drawing, so the image is
/// produced identically on Windows, on the Linux container and in CI, with no
/// native graphics dependency in the runtime image.
/// </para>
/// </summary>
public sealed class QrCodeGenerator : IQrCodeGenerator
{
    public byte[] GeneratePng(string payload, int pixelsPerModule = 10)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        ArgumentOutOfRangeException.ThrowIfLessThan(pixelsPerModule, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pixelsPerModule, 40);

        using var generator = new QRCodeGenerator();

        // Quartile correction: a ticket gets folded, creased and photographed off
        // a phone screen, so 25% redundancy is worth the extra modules.
        using QRCodeData data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);

        var png = new PngByteQRCode(data);
        return png.GetGraphic(pixelsPerModule);
    }
}
