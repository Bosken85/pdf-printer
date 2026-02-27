# ── Build stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet publish src/PdfPrinter.Api/PdfPrinter.Api.csproj \
      -c Release \
      -o /out \
      --no-self-contained

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /out .

# Install Chromium browser and all required system libraries in one step.
# playwright.sh is copied as part of the published output by Microsoft.Playwright.
RUN bash playwright.sh install --with-deps chromium

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PdfPrinter.Api.dll"]
