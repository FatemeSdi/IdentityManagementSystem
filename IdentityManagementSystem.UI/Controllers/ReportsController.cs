using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;

namespace IdentityManagementSystem.UI.Controllers
{
    public class ReportsController : Controller
    {
        private readonly HttpClient _client;
        private readonly IHttpContextAccessor _accessor;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(
            IHttpClientFactory clientFactory,
            IHttpContextAccessor accessor,
            ILogger<ReportsController> logger)
        {
            _client = clientFactory.CreateClient("PomixApi");
            _accessor = accessor;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string docNumber = "")
        {
            var token = _accessor.HttpContext?.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "لطفاً ابتدا وارد شوید.";
                return RedirectToAction("Login", "Home");
            }

            try
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var url = $"api/Reports/stats?docNumber={HttpUtility.UrlEncode(docNumber)}";
                var response = await _client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("API Error: {Status} - {Error}", response.StatusCode, error);
                    ViewBag.Error = "خطا در دریافت گزارش‌ها. لطفاً دوباره تلاش کنید.";
                    return View(new ReportsViewModel());
                }

                var json = await response.Content.ReadAsStringAsync();
                var apiResult = JsonSerializer.Deserialize<ReportsApiResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (apiResult == null)
                {
                    ViewBag.Error = "داده‌ای دریافت نشد.";
                    return View(new ReportsViewModel());
                }

                // تبدیل به ViewModel
                var model = new ReportsViewModel
                {
                    SearchedDocNumber = apiResult.SearchedDocNumber,
                    DocRequestCount = apiResult.DocRequestCount,
                    TopDocuments = apiResult.TopDocuments?.Select(d => new DocStats
                    {
                        DocumentNumber = d.DocumentNumber,
                        RequestCount = d.RequestCount
                    }).ToList() ?? new List<DocStats>(),

                    TopUserWeek = new UserStats
                    {
                        Username = apiResult.TopUserWeek?.Username ?? "هیچ",
                        RequestCount = apiResult.TopUserWeek?.RequestCount ?? 0
                    },
                    TopUserMonth = new UserStats
                    {
                        Username = apiResult.TopUserMonth?.Username ?? "هیچ",
                        RequestCount = apiResult.TopUserMonth?.RequestCount ?? 0
                    },
                    TopUserYear = new UserStats
                    {
                        Username = apiResult.TopUserYear?.Username ?? "هیچ",
                        RequestCount = apiResult.TopUserYear?.RequestCount ?? 0
                    },
                    Stats = new RequestStats
                    {
                        Total = apiResult.Stats?.Total ?? 0,
                        Approved = apiResult.Stats?.Approved ?? 0,
                        Rejected = apiResult.Stats?.Rejected ?? 0,
                        Pending = apiResult.Stats?.Pending ?? 0
                    }
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در دریافت گزارش‌ها");
                ViewBag.Error = "خطای سرور. لطفاً با پشتیبانی تماس بگیرید.";
                return View(new ReportsViewModel());
            }
        }
    }

    // ==================== ViewModelهای UI ====================
    public class ReportsViewModel
    {
        public string? SearchedDocNumber { get; set; }
        public int DocRequestCount { get; set; }
        public List<DocStats> TopDocuments { get; set; } = new();
        public UserStats TopUserWeek { get; set; } = new();
        public UserStats TopUserMonth { get; set; } = new();
        public UserStats TopUserYear { get; set; } = new();
        public RequestStats Stats { get; set; } = new();
    }

    public class DocStats
    {
        public string DocumentNumber { get; set; } = "";
        public int RequestCount { get; set; }
    }

    public class UserStats
    {
        public string Username { get; set; } = "";
        public int RequestCount { get; set; }
    }

    public class RequestStats
    {
        public int Total { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
        public int Pending { get; set; }
    }

    // ==================== DTOهای API (برای Deserialize) ====================
    public class ReportsApiResponse
    {
        public string? SearchedDocNumber { get; set; }
        public int DocRequestCount { get; set; }
        public List<DocStats>? TopDocuments { get; set; }
        public UserStats? TopUserWeek { get; set; }
        public UserStats? TopUserMonth { get; set; }
        public UserStats? TopUserYear { get; set; }
        public RequestStats? Stats { get; set; }
    }
}