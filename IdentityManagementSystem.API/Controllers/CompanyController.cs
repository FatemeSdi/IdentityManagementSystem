using IdentityManagementSystem.API.Data;
using IdentityManagementSystem.API.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IdentityManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CompanyController : ControllerBase
    {
        private readonly IdentityManagementSystemContext _context;

        public CompanyController(IdentityManagementSystemContext context)
        {
            _context = context;
        }

        // شرکت‌های فعال، برای دراپ‌داون انتخاب شرکت موقع ثبت درخواست
        [HttpGet]
        [Authorize(Policy = "CanAccessServices")]
        public async Task<ActionResult<IEnumerable<CompanyViewModel>>> GetActiveCompanies()
        {
            var companies = await _context.Companies
                .Where(c => c.IsActive)
                .OrderBy(c => c.CompanyName)
                .Select(c => new CompanyViewModel
                {
                    CompanyId = c.CompanyId,
                    CompanyName = c.CompanyName
                })
                .ToListAsync();

            return Ok(companies);
        }
    }
}
