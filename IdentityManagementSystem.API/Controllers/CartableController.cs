using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IdentityManagementSystem.API.Models.ViewModels;
using IdentityManagementSystem.API.Data;
using IdentityManagementSystem.API.Models;

namespace IdentityManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CartablesController : ControllerBase
    {
        private readonly IdentityManagementSystemContext _context;

        public CartablesController(IdentityManagementSystemContext context)
        {
            _context = context;
        }

        // GET: api/Cartables/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<CartableItemViewModel>>> GetCartableItems(long userId)
        {
            var callerUserId = long.Parse(User.FindFirst("UserId")?.Value ?? "0");
            if (callerUserId == 0 || callerUserId != userId)
                return Unauthorized("شما مجاز به مشاهده کارتابل این کاربر نیستید.");

            // AssignedTo روی CartableItem همیشه GroupId‌ه (نگاه کن به کامنت روی CartableItem.AssignedTo تو
            // Models.cs) — عضویت از طریق UserGroups چک می‌شه، و بین آیتم‌های همون گروه فقط اونایی که هنوز
            // کسی take نکرده یا خودم take کردم رو می‌بینم.
            var myGroupIds = await _context.UserGroups
                .Where(ug => ug.UserId == userId)
                .Select(ug => (long)ug.GroupId)
                .ToListAsync();

            var cartableItems = await _context.CartableItems
                .Include(ci => ci.Request)
                .Include(ci => ci.IsTakenByUser)
                .Where(ci => ci.Request != null
                            && ci.AssignedTo != null && myGroupIds.Contains(ci.AssignedTo.Value)
                            && (ci.IsTakenBy == null || ci.IsTakenBy == userId))
                .Select(ci => new CartableItemViewModel
                {
                    ItemId = ci.ItemId,
                    RequestId = ci.RequestId,
                    NationalId = ci.Request!.NationalId,
                    DocumentNumber = ci.Request!.DocumentNumber,
                    VerificationCode = ci.Request!.VerificationCode,
                    IsMatch = ci.Request!.IsMatch,
                    IsExist = ci.Request!.IsExist,
                    IsNationalIdInResponse = ci.Request!.IsNationalIdInResponse,
                    IsNationalIdInLawyers = ci.Request!.IsNationalIdInLawyers,
                    CreatedAt = ci.Request!.CreatedAt,
                    AssignedTo = ci.IsTakenBy,
                    AssignedToName = ci.IsTakenByUser != null ? $"{ci.IsTakenByUser.Name} {ci.IsTakenByUser.LastName}" : null,
                    AssignedAt = ci.AssignedAt,
                    ViewedAt = ci.ViewedAt,
                    Status = ci.Status,
                    Description = ci.Description, // NotMapped - will be null unless populated
                    ValidateByExpert = ci.ValidateByExpert // NotMapped
                })
                .ToListAsync();

            return Ok(cartableItems);
        }

        // POST: api/Cartables/assign
        [HttpPost("assign")]
        public async Task<IActionResult> AssignCartableItem(AssignCartableItemViewModel viewModel)
        {
            var callerUserId = long.Parse(User.FindFirst("UserId")?.Value ?? "0");
            if (callerUserId == 0)
                return Unauthorized("کاربر شناسایی نشد.");

            var cartableItem = await _context.CartableItems.FindAsync(viewModel.ItemId);
            if (cartableItem == null)
                return NotFound();

            cartableItem.AssignedTo = viewModel.AssignedTo;
            cartableItem.AssignedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // 🟢 اضافه کردن رکورد تاریخچه برای عملیات Assign
            // ExpertId در DB از نوع nvarchar است
            _context.RequestHistory.Add(new RequestHistory
            {
                RequestId = cartableItem.RequestId,
                ExpertId = viewModel.AssignedTo.ToString(), // string مطابق اسکیما
                StatusId = 1,
                ActionDescription = $"آیتم کارتابل به کاربر {viewModel.AssignedTo} تخصیص یافت",
                CreatedAt = DateTime.UtcNow,
                UpdatedStatus = "Assigned",
                UpdatedStatusBy = User?.Identity?.Name,
                UpdatedStatusDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // متد کمکی برای ایجاد آیتم کارتابل برای Request جدید
        [HttpPost("create-for-request")]
        public async Task<ActionResult<CartableItem>> CreateCartableItemForRequest(long requestId, long cartableId, long? assignedToUserId = null)
        {
            var cartableItem = new CartableItem
            {
                RequestId = requestId,
                CartableId = cartableId,
                AssignedTo = assignedToUserId,
                AssignedAt = assignedToUserId.HasValue ? DateTime.UtcNow : null,
                Status = "New"
            };

            _context.CartableItems.Add(cartableItem);
            await _context.SaveChangesAsync();

            return Ok(cartableItem);
        }
    }
}