# PDF Printer

An ASP.NET Core Web API that converts HTML pages with CSS styling into A4-formatted PDFs using a headless Chromium browser via [Microsoft Playwright](https://playwright.dev/dotnet/).

## Features

- Converts any HTML + CSS to a pixel-perfect A4 PDF
- Full CSS support: flexbox, grid, gradients, custom fonts, print backgrounds
- Two input modes: file upload or raw HTML string
- Configurable page margins via `appsettings.json`
- Swagger UI for interactive testing
- Chromium installs itself automatically on first startup

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) — for running locally
- [Docker](https://www.docker.com/) — for running in a container (Linux or Windows)

## Getting Started

```bash
# Clone / enter the repo
cd c:\projects\pdf-printer

# Run the API
dotnet run --project src/PdfPrinter.Api
```

On first startup, Playwright will automatically download the Chromium browser if it is not already present. Once ready, the API is available at:

- HTTP — `http://localhost:5000`
- HTTPS — `https://localhost:5001`
- Swagger UI — `https://localhost:5001/swagger`

## API Endpoints

### `POST /api/pdf/from-file`

Upload an `.html` file and receive a rendered A4 PDF.

**Content-Type:** `multipart/form-data`

| Field | Type | Description |
|-------|------|-------------|
| `file` | File | The `.html` file to convert |

**Response:** `application/pdf` — the rendered PDF as a binary download.

**Example (curl):**
```bash
curl -X POST https://localhost:5001/api/pdf/from-file \
  -F "file=@samples/sample-invoice.html" \
  --output invoice.pdf
```

---

### `POST /api/pdf/from-html`

Send raw HTML in a JSON body and receive a rendered A4 PDF.

**Content-Type:** `application/json`

```json
{
  "html": "<html><body><h1>Hello, PDF!</h1></body></html>"
}
```

**Response:** `application/pdf` — the rendered PDF as a binary download.

**Example (curl):**
```bash
curl -X POST https://localhost:5001/api/pdf/from-html \
  -H "Content-Type: application/json" \
  -d '{"html": "<h1 style=\"font-family:Arial\">Hello</h1>"}' \
  --output output.pdf
```

## Configuration

Page margins and background printing are configurable in [`src/PdfPrinter.Api/appsettings.json`](src/PdfPrinter.Api/appsettings.json):

```json
{
  "PdfOptions": {
    "MarginTop": "10mm",
    "MarginBottom": "10mm",
    "MarginLeft": "10mm",
    "MarginRight": "10mm",
    "PrintBackground": true
  }
}
```

All margin values accept CSS length units: `mm`, `cm`, `in`, `px`.

## Project Structure

```
pdf-printer/
├── Dockerfile
├── .dockerignore
├── PdfPrinter.sln
├── samples/
│   ├── sample-invoice.html         # Example: styled invoice
│   └── sample-report.html          # Example: quarterly report
└── src/
    └── PdfPrinter.Api/
        ├── Controllers/
        │   └── PdfController.cs    # API endpoints
        ├── Models/
        │   └── HtmlRequest.cs      # Request model for /from-html
        ├── Services/
        │   ├── IPdfGeneratorService.cs
        │   └── PdfGeneratorService.cs  # Playwright PDF engine
        ├── Program.cs
        └── appsettings.json
```

## How It Works

1. The API receives HTML (as a file upload or JSON body).
2. A singleton `PdfGeneratorService` owns a long-lived headless Chromium browser instance.
3. For each request, a new browser page is opened, the HTML is loaded, and Playwright's PDF export is called with `Format = "A4"` and `PrintBackground = true`.
4. The resulting PDF bytes are streamed back to the caller.

The browser instance is reused across all requests, so startup cost is paid only once.

## Sample Files

Two ready-to-use sample HTML pages are included in the [`samples/`](samples/) folder:

| File | Description |
|------|-------------|
| `sample-invoice.html` | A styled invoice with line items, totals, and a company header |
| `sample-report.html` | A quarterly business report with KPI cards, tables, and progress bars |

Test them immediately:
```bash
curl -X POST https://localhost:5001/api/pdf/from-file \
  -F "file=@samples/sample-report.html" \
  --output report.pdf
```

## Docker

The included [`Dockerfile`](Dockerfile) produces a self-contained Linux image. Chromium and all required system libraries are installed at image build time, so the container starts instantly with no downloads at runtime.

### Build and run

```bash
# Build the image
docker build -t pdf-printer .

# Run on port 8080
docker run -p 8080:8080 pdf-printer
```

The API is then available at `http://localhost:8080`. Test it:

```bash
curl -X POST http://localhost:8080/api/pdf/from-file \
  -F "file=@samples/sample-invoice.html" \
  --output invoice.pdf
```

### How the image is structured

| Stage | Base image | What happens |
|-------|-----------|--------------|
| `build` | `mcr.microsoft.com/dotnet/sdk:10.0` | Compiles and publishes the app |
| `runtime` | `mcr.microsoft.com/dotnet/aspnet:10.0` | Copies published output, runs `playwright.sh install --with-deps chromium` to install Chromium and all Linux system libraries |

The `--with-deps` flag in the Playwright install script handles the `apt-get` installation of every library Chromium needs (NSS, ATK, X11, GBM, etc.) automatically.

### Docker Compose example

```yaml
services:
  pdf-printer:
    build: .
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
```

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.Playwright` | 1.58.0 | Headless Chromium browser for HTML rendering |
| `Swashbuckle.AspNetCore` | 10.1.4 | Swagger UI |
| `Microsoft.AspNetCore.OpenApi` | 10.0.3 | OpenAPI document generation |
