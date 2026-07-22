using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IdentityManagementSystem.API.Data;
using IdentityManagementSystem.API.Models;
using System.Text.Json;

namespace IdentityManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly IdentityManagementSystemContext _context;

        public ReportsController(IdentityManagementSystemContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats(string docNumber = "")
        {
            var result = new ReportsApiResponse();

            // 1. جستجوی سند خاص
            if (!string.IsNullOrEmpty(docNumber))
            {
                result.SearchedDocNumber = docNumber;
                result.DocRequestCount = await _context.Request
                    .CountAsync(r => r.DocumentNumber == docNumber);
            }

            // 2. 10 سند پر درخواست
            result.TopDocuments = await _context.Request
                .GroupBy(r => r.DocumentNumber)
                .Select(g => new DocStatsDto
                {
                    DocumentNumber = g.Key ?? "نامشخص",
                    RequestCount = g.Count()
                })
                .OrderByDescending(x => x.RequestCount)
                .Take(10)
                .ToListAsync();

            // 3. کاربر برتر (هفته، ماه، سال)
            var now = DateTime.UtcNow;
            result.TopUserWeek = await GetTopUser(now.AddDays(-7), now);
            result.TopUserMonth = await GetTopUser(now.AddMonths(-1), now);
            result.TopUserYear = await GetTopUser(now.AddYears(-1), now);

            // 4. آمار کلی
            var total = await _context.Request.CountAsync();
            var approved = await _context.Request.CountAsync(r => r.ValidateByExpert == true);
            var rejected = await _context.Request.CountAsync(r => r.ValidateByExpert == false);

            result.Stats = new RequestStatsDto
            {
                Total = total,
                Approved = approved,
                Rejected = rejected,
                Pending = total - approved - rejected
            };

            return Ok(result);
        }

        private async Task<UserStatsDto> GetTopUser(DateTime from, DateTime to)
        {
            var top = await _context.Request
                .Where(r => r.CreatedAt >= from && r.CreatedAt <= to)
                .GroupBy(r => r.CreatedBy)
                .Select(g => new { User = g.Key ?? "ناشناس", Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .FirstOrDefaultAsync();

            return top != null
                ? new UserStatsDto { Username = top.User, RequestCount = top.Count }
                : new UserStatsDto { Username = "هیچ", RequestCount = 0 };
        }
    }

    // DTOهای API (کپی-پیست کن)
    public class ReportsApiResponse
    {
        public string? SearchedDocNumber { get; set; }
        public int DocRequestCount { get; set; }
        public List<DocStatsDto> TopDocuments { get; set; } = new();
        public UserStatsDto TopUserWeek { get; set; } = new();
        public UserStatsDto TopUserMonth { get; set; } = new();
        public UserStatsDto TopUserYear { get; set; } = new();
        public RequestStatsDto Stats { get; set; } = new();
    }

    public class DocStatsDto
    {
        public string DocumentNumber { get; set; } = "";
        public int RequestCount { get; set; }
    }

    public class UserStatsDto
    {
        public string Username { get; set; } = "";
        public int RequestCount { get; set; }
    }

    public class RequestStatsDto
    {
        public int Total { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
        public int Pending { get; set; }
    }
}