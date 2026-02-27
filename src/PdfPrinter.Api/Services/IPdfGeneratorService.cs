namespace PdfPrinter.Api.Services;

public interface IPdfGeneratorService
{
    /// <summary>Renders <paramref name="html"/> to an A4 PDF and returns the raw bytes.</summary>
    Task<byte[]> GenerateFromHtmlAsync(string html);

    /// <summary>Reads an HTML file from <paramref name="filePath"/> and renders it to an A4 PDF.</summary>
    Task<byte[]> GenerateFromFileAsync(string filePath);
}
