using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace IdentityManagementSystem.UI.Controllers
{
    /// <summary>مدیریت نقش‌ها (فقط ادمین؛ حذف نداریم، فقط فعال/غیرفعال). منطق و ولیدیشن سمت API است.</summary>
    public class RolesController : Controller
    {
        private readonly HttpClient _client;
        private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

        public RolesController(IHttpClientFactory httpClientFactory)
        {
            _client = httpClientFactory.CreateClient("PomixApi");
        }

        private bool TryAuth()
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token)) return false;
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return true;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!TryAuth()) return RedirectToAction("LoginPage", "Home");

            try
            {
                var response = await _client.GetAsync("Auth/GetAllRoles");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var roles = JsonSerializer.Deserialize<List<RoleRow>>(json, JsonOpts) ?? new List<RoleRow>();
                    return View(roles);
                }

                ViewBag.ErrorMessage = response.StatusCode == System.Net.HttpStatusCode.Forbidden
                    ? "دسترسی به مدیریت نقش‌ها فقط برای ادمین مجاز است."
                    : "خطا در دریافت نقش‌ها: " + await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "خطا در ارتباط با سرور: " + ex.Message;
            }
            return View(new List<RoleRow>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string roleName)
        {
            if (!TryAuth()) return Json(new { success = false, message = "لطفاً ابتدا وارد سیستم شوید." });
            var response = await _client.PostAsJsonAsync("Auth/CreateRole", new { RoleName = roleName });
            return await ToJson(response, "خطا در ایجاد نقش");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, string roleName)
        {
            if (!TryAuth()) return Json(new { success = false, message = "لطفاً ابتدا وارد سیستم شوید." });
            var response = await _client.PutAsJsonAsync($"Auth/UpdateRole/{id}", new { RoleName = roleName });
            return await ToJson(response, "خطا در ویرایش نقش");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetActive(int id, bool active)
        {
            if (!TryAuth()) return Json(new { success = false, message = "لطفاً ابتدا وارد سیستم شوید." });
            var response = await _client.PostAsync($"Auth/SetRoleActive/{id}?active={active.ToString().ToLower()}", null);
            return await ToJson(response, "خطا در تغییر وضعیت نقش");
        }

        private static async Task<IActionResult> ToJson(HttpResponseMessage response, string fallback)
        {
            var body = await response.Content.ReadAsStringAsync();
            string? message = null;
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("message", out var m)) message = m.GetString();
            }
            catch { /* پاسخ غیر JSON */ }

            if (response.IsSuccessStatusCode)
                return new JsonResult(new { success = true, message = message ?? "انجام شد." });

            return new JsonResult(new { success = false, message = message ?? $"{fallback} ({(int)response.StatusCode})" });
        }
    }

    public class RoleRow
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int UserCount { get; set; }
    }
}
