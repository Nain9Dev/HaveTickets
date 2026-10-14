namespace HaveTickets.Application.Abstractions;

/// <summary>Renders a ticket code as a scannable image.</summary>
public interface IQrCodeGenerator
{
    /// <summary>Returns a PNG encoding <paramref name="payload"/>.</summary>
    byte[] GeneratePng(string payload, int pixelsPerModule = 10);
}
