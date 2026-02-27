using Microsoft.Playwright;

namespace PdfPrinter.Api.Services;

public sealed class PdfGeneratorService : IPdfGeneratorService, IAsyncDisposable
{
    private readonly IConfiguration _config;
    private readonly ILogger<PdfGeneratorService> _logger;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public PdfGeneratorService(IConfiguration config, ILogger<PdfGeneratorService> logger)
    {
        _config = config;
        _logger = logger;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized) return;

        await _initLock.WaitAsync();
        try
        {
            if (_initialized) return;

            _logger.LogInformation("Launching Playwright Chromium browser...");
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });
            _initialized = true;
            _logger.LogInformation("Chromium browser ready.");
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<byte[]> GenerateFromHtmlAsync(string html)
    {
        await EnsureInitializedAsync();

        var page = await _browser!.NewPageAsync();
        try
        {
            await page.SetContentAsync(html, new PageSetContentOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            var pdf = await page.PdfAsync(BuildPdfOptions());
            return pdf;
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    public async Task<byte[]> GenerateFromFileAsync(string filePath)
    {
        var html = await File.ReadAllTextAsync(filePath);
        return await GenerateFromHtmlAsync(html);
    }

    private PagePdfOptions BuildPdfOptions()
    {
        var section = _config.GetSection("PdfOptions");

        return new PagePdfOptions
        {
            Format = "A4",
            PrintBackground = section.GetValue("PrintBackground", true),
            Margin = new Margin
            {
                Top = section.GetValue("MarginTop", "10mm"),
                Bottom = section.GetValue("MarginBottom", "10mm"),
                Left = section.GetValue("MarginLeft", "10mm"),
                Right = section.GetValue("MarginRight", "10mm")
            }
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null) await _browser.DisposeAsync();
        _playwright?.Dispose();
        _initLock.Dispose();
    }
}
