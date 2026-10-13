using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using ExcelDataReader;
using Soluvion.API.Interfaces;
using UglyToad.PdfPig;

namespace Soluvion.API.Services
{
    public class DocumentTextExtractorService : IDocumentTextExtractorService
    {
        private readonly ILogger<DocumentTextExtractorService> _logger;

        static DocumentTextExtractorService()
        {
            // Biztosítjuk a CodePagesEncodingProvider regisztrációját a régebbi Excel/Word kódlapokhoz (Windows-1250 / CP1252)
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public DocumentTextExtractorService(ILogger<DocumentTextExtractorService> logger)
        {
            _logger = logger;
        }

        public bool IsExtractableDocument(string fileName, string? contentType)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".xlsx" or ".xls" or ".docx" or ".doc" or ".pdf" or ".csv" or ".txt" => true,
                _ => contentType switch
                {
                    "application/pdf" => true,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => true,
                    "application/vnd.ms-excel" => true,
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => true,
                    "application/msword" => true,
                    _ => false
                }
            };
        }

        public async Task<string?> ExtractTextAsync(IFormFile file)
        {
            if (file == null || file.Length == 0) return null;

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            try
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                return ext switch
                {
                    ".xlsx" or ".xls" => ExtractFromExcel(memoryStream, file.FileName),
                    ".docx" => ExtractFromDocx(memoryStream, file.FileName),
                    ".doc" => ExtractFromDocBinary(memoryStream, file.FileName),
                    ".pdf" => ExtractFromPdf(memoryStream, file.FileName),
                    ".csv" or ".txt" => ExtractFromPlainText(memoryStream, file.FileName),
                    _ => null
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Nem sikerült a szöveg kinyerése a fájlból: {FileName}", file.FileName);
                return null;
            }
        }

        private static string ExtractFromExcel(Stream stream, string fileName)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[DOKUMENTUM: {fileName} (Excel táblázat)]");

            using var reader = ExcelReaderFactory.CreateReader(stream);
            int sheetIndex = 0;
            do
            {
                sheetIndex++;
                sb.AppendLine($"\n--- Munkalap #{sheetIndex}: {reader.Name} ---");

                int rowIndex = 0;
                while (reader.Read())
                {
                    rowIndex++;
                    var cells = new List<string>();
                    bool hasData = false;

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var cellVal = reader.GetValue(i)?.ToString()?.Trim() ?? string.Empty;
                        if (!string.IsNullOrEmpty(cellVal)) hasData = true;
                        cells.Add(cellVal.Replace("|", "/").Replace("\r", " ").Replace("\n", " "));
                    }

                    if (hasData)
                    {
                        sb.AppendLine("| " + string.Join(" | ", cells) + " |");
                    }
                }
            } while (reader.NextResult());

            return sb.ToString();
        }

        private static string ExtractFromDocx(Stream stream, string fileName)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[DOKUMENTUM: {fileName} (Word dokumentum)]");

            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, true);
            var entry = archive.GetEntry("word/document.xml");
            if (entry == null) return string.Empty;

            using var entryStream = entry.Open();
            var doc = XDocument.Load(entryStream);
            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

            var body = doc.Root?.Element(w + "body");
            if (body == null) return string.Empty;

            foreach (var element in body.Elements())
            {
                if (element.Name == w + "tbl")
                {
                    sb.AppendLine("\n[Táblázat]");
                    foreach (var row in element.Elements(w + "tr"))
                    {
                        var cells = new List<string>();
                        foreach (var cell in row.Elements(w + "tc"))
                        {
                            var cellText = string.Join(" ", cell.Descendants(w + "t").Select(t => t.Value)).Trim();
                            cells.Add(cellText.Replace("|", "/").Replace("\r", " ").Replace("\n", " "));
                        }
                        if (cells.Any(c => !string.IsNullOrEmpty(c)))
                        {
                            sb.AppendLine("| " + string.Join(" | ", cells) + " |");
                        }
                    }
                    sb.AppendLine();
                }
                else if (element.Name == w + "p")
                {
                    var text = string.Join("", element.Descendants(w + "t").Select(t => t.Value)).Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        sb.AppendLine(text);
                    }
                }
            }

            return sb.ToString();
        }

        private static string ExtractFromDocBinary(Stream stream, string fileName)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[DOKUMENTUM: {fileName} (Word 97-2003 bináris)]");

            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            var bytes = ms.ToArray();

            var currentAscii = new StringBuilder();
            foreach (var b in bytes)
            {
                if ((b >= 32 && b <= 126) || b == 10 || b == 13 || (b >= 160 && b <= 255))
                {
                    currentAscii.Append((char)b);
                }
                else
                {
                    if (currentAscii.Length >= 4)
                    {
                        sb.AppendLine(currentAscii.ToString());
                    }
                    currentAscii.Clear();
                }
            }
            if (currentAscii.Length >= 4)
            {
                sb.AppendLine(currentAscii.ToString());
            }

            return sb.ToString();
        }

        private static string ExtractFromPdf(Stream stream, string fileName)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[DOKUMENTUM: {fileName} (PDF)]");

            using var pdf = PdfDocument.Open(stream);
            foreach (var page in pdf.GetPages())
            {
                sb.AppendLine($"\n--- PDF Oldal #{page.Number} ---");
                if (!string.IsNullOrWhiteSpace(page.Text))
                {
                    sb.AppendLine(page.Text);
                }
            }

            return sb.ToString();
        }

        private static string ExtractFromPlainText(Stream stream, string fileName)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, true);
            return $"[DOKUMENTUM: {fileName}]\n" + reader.ReadToEnd();
        }
    }
}

