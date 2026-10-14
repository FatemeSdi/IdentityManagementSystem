using System.Net.Http.Headers;
using System.Text;
using IdentityManagementSystem.UI.Filters;
using Microsoft.AspNetCore.Mvc;

namespace IdentityManagementSystem.UI.Controllers
{
    /// <summary>
    /// پراکسی UI برای نوبت‌دهی ارزیابی (API: api/EvalAppointment):
    ///   * Slots       — صفحه‌ی مدیریت نوبت‌های خالی هر محوطه (ادمین)
    ///   * FreeSlots   — نوبت‌های خالی برای مودال «ثبت نوبت» کارشناس
    ///   * SetAppointment — ثبت نوبت (تایید درخواست + پیامک)
    /// پاسخ API عیناً به فرانت برگردونده می‌شه (success/message از خود API).
    /// </summary>
    [NoCacheFilter]
    public class EvalAppointmentController : Controller
    {
        private readonly HttpClient _client;
        private readonly ILogger<EvalAppointmentController> _logger;

        public EvalAppointmentController(IHttpClientFactory httpClientFactory, ILogger<EvalAppointmentController> logger)
        {
            _client = httpClientFactory.CreateClient("PomixApi");
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Slots()
        {
            ViewBag.Companies = await GetCompaniesAsync();
            return View();
        }

        [HttpGet]
        public Task<IActionResult> SlotList(int companyId, string? fromDate, string? toDate)
        {
            var url = $"EvalAppointment/Slots?companyId={companyId}";
            var from = PersianDateHelper.ToGregorianParam(fromDate);
            var to = PersianDateHelper.ToGregorianParam(toDate);
            if (from != null) url += $"&fromDate={Uri.EscapeDataString(from)}";
            if (to != null) url += $"&toDate={Uri.EscapeDataString(to)}";
            return ForwardAsync(HttpMethod.Get, url);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Generate([FromBody] GenerateSlotsInput input)
        {
            var body = new
            {
                input.CompanyId,
                FromDate = PersianDateHelper.ToGregorianParam(input.FromDate) ?? string.Empty,
                ToDate = PersianDateHelper.ToGregorianParam(input.ToDate) ?? string.Empty,
                input.DaysOfWeek,
                input.StartTime,
                input.EndTime,
                input.DurationMinutes,
                input.Capacity
            };
            return ForwardAsync(HttpMethod.Post, "EvalAppointment/Slots/Generate", body);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> DeleteSlot(int slotId) =>
            ForwardAsync(HttpMethod.Delete, $"EvalAppointment/Slots/{slotId}");

        [HttpGet]
        public Task<IActionResult> FreeSlots(long requestId) =>
            ForwardAsync(HttpMethod.Get, $"EvalAppointment/FreeSlots?requestId={requestId}");

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> SetAppointment([FromBody] SetAppointmentInput input) =>
            ForwardAsync(HttpMethod.Post, "EvalAppointment/SetAppointment", new
            {
                input.RequestId,
                input.SlotId,
                Date = PersianDateHelper.ToGregorianParam(input.Date),
                input.Time
            });

        private async Task<IActionResult> ForwardAsync(HttpMethod method, string url, object? body = null)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "توکن یافت نشد. لطفاً دوباره وارد سیستم شوید." });

            try
            {
                using var message = new HttpRequestMessage(method, url);
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                if (body != null)
                {
                    message.Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
                }

                var response = await _client.SendAsync(message);
                var content = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden && string.IsNullOrWhiteSpace(content))
                    return Json(new { success = false, message = "شما به این بخش دسترسی ندارید." });

                if (string.IsNullOrWhiteSpace(content) || !content.TrimStart().StartsWith("{"))
                {
                    _logger.LogWarning("EvalAppointment API {Url} returned {StatusCode}: {Content}", url, response.StatusCode, content);
                    return Json(new { success = false, message = $"خطا از سمت سرور: {(int)response.StatusCode}" });
                }

                return Content(content, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در فراخوانی EvalAppointment API {Url}", url);
                return Json(new { success = false, message = "خطا در ارتباط با سرور." });
            }
        }

        private async Task<List<CompanyOption>> GetCompaniesAsync()
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
                return new List<CompanyOption>();

            try
            {
                using var message = new HttpRequestMessage(HttpMethod.Get, "Company");
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await _client.SendAsync(message);
                if (!response.IsSuccessStatusCode)
                    return new List<CompanyOption>();

                var content = await response.Content.ReadAsStringAsync();
                return Newtonsoft.Json.JsonConvert.DeserializeObject<List<CompanyOption>>(content) ?? new List<CompanyOption>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در دریافت لیست شرکت‌ها");
                return new List<CompanyOption>();
            }
        }
    }

    public class GenerateSlotsInput
    {
        public int CompanyId { get; set; }
        public string? FromDate { get; set; }    // شمسی از datepicker
        public string? ToDate { get; set; }
        public List<int>? DaysOfWeek { get; set; }
        public string StartTime { get; set; } = "08:00";
        public string EndTime { get; set; } = "14:00";
        public int DurationMinutes { get; set; } = 30;
        public int Capacity { get; set; } = 1;
    }

    public class SetAppointmentInput
    {
        public long RequestId { get; set; }
        public int SlotId { get; set; }
        public string? Date { get; set; }   // شمسی از datepicker (وقتی نوبت دلخواهه)
        public string? Time { get; set; }   // HH:mm
    }
}
