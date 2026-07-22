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

            var cartableItems = await _context.CartableItems
                .Include(ci => ci.Request)
                .Include(ci => ci.AssignedToUser)
                .Include(ci => ci.Cartable)
                .Where(ci => ci.Cartable != null
                            && ci.Cartable.UserId == userId
                            && (ci.AssignedTo == userId || ci.AssignedTo == null)
                            && ci.Request != null)
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
                    AssignedTo = ci.AssignedTo,
                    AssignedToName = ci.AssignedToUser != null ? $"{ci.AssignedToUser.Name} {ci.AssignedToUser.LastName}" : null,
                    AssignedAt = ci.AssignedAt,
                    ViewedAt = ci.ViewedAt,
                    Status = ci.Status,
                    Description = ci.Description,
                    ValidateByExpert = ci.ValidateByExpert

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
            _context.RequestHistory.Add(new RequestHistory
            {
                RequestId = cartableItem.RequestId,
                ExpertId = viewModel.AssignedTo, // کاربری که وظیفه به او اختصاص داده شده
                StatusId = 1,
                ActionDescription = $"آیتم کارتابل به کاربر {viewModel.AssignedTo} تخصیص یافت",
                CreatedAt = DateTime.UtcNow,
                UpdatedStatus = "Assigned",
                UpdatedStatusBy = User?.Identity?.Name, // یا Id کاربر جاری
                UpdatedStatusDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return NoContent();
        }

            // متد کمکی برای ایجاد آیتم کارتابل برای Request جدید
            [HttpPost("create-for-request")]
            public async Task<CartableItem> CreateCartableItemForRequest(long requestId, long cartableId, long? assignedToUserId = null)
            {
                var cartableItem = new CartableItem
                {
                    RequestId = requestId,
                    CartableId = cartableId,
                    AssignedTo = assignedToUserId,
                    AssignedAt = (DateTime)(assignedToUserId.HasValue ? DateTime.UtcNow : (DateTime?)null),
                    Status = "New"
                };

                _context.CartableItems.Add(cartableItem);
                await _context.SaveChangesAsync();

                return cartableItem;
            }
        }
    } 

