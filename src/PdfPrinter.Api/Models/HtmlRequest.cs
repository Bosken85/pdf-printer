namespace PdfPrinter.Api.Models;

public class HtmlRequest
{
    /// <summary>Raw HTML string to render as PDF.</summary>
    public string Html { get; set; } = string.Empty;
}
