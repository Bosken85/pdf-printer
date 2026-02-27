using Microsoft.AspNetCore.Mvc;
using PdfPrinter.Api.Models;
using PdfPrinter.Api.Services;

namespace PdfPrinter.Api.Controllers;

[ApiController]
[Route("api/pdf")]
public class PdfController(IPdfGeneratorService pdfGenerator, ILogger<PdfController> logger) : ControllerBase
{
    /// <summary>
    /// Upload an HTML file and receive an A4 PDF in response.
    /// </summary>
    [HttpPost("from-file")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "application/pdf")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> FromFile(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file uploaded.");

        if (!file.FileName.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only .html files are accepted.");

        string html;
        using (var reader = new StreamReader(file.OpenReadStream()))
            html = await reader.ReadToEndAsync();

        logger.LogInformation("Generating PDF from uploaded file '{FileName}' ({Size} bytes)", file.FileName, file.Length);

        var pdfBytes = await pdfGenerator.GenerateFromHtmlAsync(html);

        var downloadName = Path.GetFileNameWithoutExtension(file.FileName) + ".pdf";
        return File(pdfBytes, "application/pdf", downloadName);
    }

    /// <summary>
    /// Send raw HTML in the request body and receive an A4 PDF in response.
    /// </summary>
    [HttpPost("from-html")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "application/pdf")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> FromHtml([FromBody] HtmlRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Html))
            return BadRequest("HTML content must not be empty.");

        logger.LogInformation("Generating PDF from raw HTML ({Length} chars)", request.Html.Length);

        var pdfBytes = await pdfGenerator.GenerateFromHtmlAsync(request.Html);

        return File(pdfBytes, "application/pdf", "output.pdf");
    }
}
