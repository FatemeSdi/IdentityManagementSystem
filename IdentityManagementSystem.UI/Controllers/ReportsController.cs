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

        // فعلاً فقط UI: داده‌ی صفحه نمونه‌ست و سرویس‌های گزارش بعداً به همین اکشن وصل می‌شن
        public IActionResult Index()
        {
            return View();
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