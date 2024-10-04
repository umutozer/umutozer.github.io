using Microsoft.AspNetCore.Mvc;
using PdfSharp.Pdf.IO;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace PdfCleaner.Controllers
{
    public class PdfController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Index2()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ViewBag.Message = "Lütfen bir PDF dosyası yükleyin.";
                return View("Index");
            }

            // PDF dosyasını geçici bir dizine kaydet
            var filePath = Path.Combine(Path.GetTempPath(), file.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // PDF dosyasını temizle
            var cleanedPdf = CleanPdf(filePath);

            // Temizlenmiş PDF dosyasını indirme işlemi
            var cleanedFileName = "Cleaned_" + file.FileName;
            return File(cleanedPdf, "application/pdf", cleanedFileName);
        }

        private byte[] CleanPdf(string filePath)
        {
            // PdfSharp kullanarak dosyayı okuma
            using (var document = PdfReader.Open(filePath, PdfDocumentOpenMode.Modify))
            {
                // Meta verileri temizleme
                document.Info.Title = "";
                document.Info.Author = "";
                document.Info.Subject = "";
                document.Info.Keywords = "";
                document.Info.Comment = "";
                

                // Yorumları ve anotasyonları temizleme
                foreach (var page in document.Pages)
                {
                    for (int i = page.Annotations.Count - 1; i >= 0; i--)
                    {
                        page.Annotations.Clear();
                    }
                }

                // Temizlenmiş PDF'yi bell ek üzerine kaydetme
                using (var ms = new MemoryStream())
                {
                    document.Save(ms, false);
                    return ms.ToArray();
                }
            }
        }
        [HttpPost]
        public async Task<IActionResult> CheckPdfContent(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ViewBag.Message = "Lütfen bir PDF dosyası yükleyin.";
                return View("Index");
            }

            // PDF dosyasını geçici bir dizine kaydet
            var filePath = Path.Combine(Path.GetTempPath(), file.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // PDF dosyasını oku ve telefon/email kontrolü yap
            var results = FindContactsInPdf(filePath);

            // Sonuçları ekranda göster
            ViewBag.Results = results;
            return View("Index2");
        }

        private List<string> FindContactsInPdf(string pdfPath)
        {
            var results = new List<string>();

            // Düzenli ifadeler
            var phoneRegex = new Regex(@"\b\d{10}\b|\b(\+?\d{1,3}[-.\s]?)?(\(?\d{3}\)?[-.\s]?)?\d{3}[-.\s]?\d{4}\b", RegexOptions.Compiled);
            var emailRegex = new Regex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.Compiled);
            var urlRegex = new Regex(@"\b(http|https)://[^\s/$.?#].[^\s]*\b", RegexOptions.Compiled); // URL bulma regex'i

            // PDF dosyasını oku
            using (var document = PdfDocument.Open(pdfPath))
            {
                int pageNumber = 1;
                foreach (var page in document.GetPages())
                {
                    var content = page.Text; // Sayfa metnini okuma

                    //// Telefon numarası kontrolü
                    //var phoneMatches = phoneRegex.Matches(content);
                    //foreach (Match match in phoneMatches)
                    //{
                    //    results.Add($"Telefon Numarası Bulundu: {match.Value}, Sayfa: {pageNumber}");
                    //}

                    // E-posta adresi kontrolü
                    var emailMatches = emailRegex.Matches(content);
                    foreach (Match match in emailMatches)
                    {
                        results.Add($"E-posta Adresi Bulundu: {match.Value}, Sayfa: {pageNumber}");
                    }

                    // Link kontrolü
                    var urlMatches = urlRegex.Matches(content);
                    foreach (Match match in urlMatches)
                    {
                        results.Add($"Link Bulundu: {match.Value}, Sayfa: {pageNumber}");
                    }

                    // Linklerin isimlerini kontrol et (URI ile)
                    foreach (var annotation in page.ExperimentalAccess.GetAnnotations())
                    {
                        if (annotation.Action != null)
                        {
                            results.Add($"Link Adı: {annotation.Action.Type}, Sayfa: {pageNumber}");
                        }
                    }

                    pageNumber++;
                }
            }

            return results;
        }


    }
}
