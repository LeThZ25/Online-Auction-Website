using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineAuctionWebsite.Models;
using OnlineAuctionWebsite.Models.Entities;
using System.Security.Claims;

namespace OnlineAuctionWebsite.Controllers
{
	[Authorize]
	public class PaymentsController : Controller
	{
		private readonly ApplicationDbContext _db;
		private readonly IWebHostEnvironment _env;

		private const string PROVIDER_DEPOSIT = "DEPOSIT";
		private const string PROVIDER_WINNER = "WINNER";

		public PaymentsController(ApplicationDbContext db, IWebHostEnvironment env)
		{
			_db = db; _env = env;
		}

		// -------------------------------------------------------------
		// STEP 2: Router chung cho thanh toán (đặt trước / người thắng)
		// /Payments/Checkout?regId=123[&method=qr|bank]
		// -------------------------------------------------------------
		[HttpGet]
		public async Task<IActionResult> Checkout(int regId, string? method = null)
		{
			var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

			var reg = await _db.Registrations
				.Include(r => r.Session).ThenInclude(s => s.Item)
				.FirstOrDefaultAsync(r => r.Id == regId);

			if (reg == null) return NotFound();
			if (reg.UserId != uid) return Forbid();

			// 1) Nếu đang chờ nộp tiền đặt trước -> đưa sang Deposit
			if (reg.Status == AuctionRegistration.StatusPendingDeposit)
				return RedirectToAction(nameof(Deposit), new { sessionId = reg.SessionId });

			// 2) Nếu phiên đã kết thúc và bạn là người thắng -> tạo payment "WINNER"
			var session = reg.Session;
			var now = DateTime.UtcNow;
			var isEnded = session.EndUtc <= now || session.Status == AuctionSessionStatus.Ended;

			if (isEnded)
			{
				// Lấy giá chốt & người dẫn đầu (tránh lỗi OrderBy(decimal) của SQLite -> lôi ra memory rồi sort)
				var bids = await _db.Bids
					.Where(b => b.SessionId == reg.SessionId)
					.Select(b => new { b.BidderId, b.Amount, b.CreatedAt })
					.ToListAsync();

				var top = bids
					.OrderByDescending(b => b.Amount)
					.ThenBy(b => b.CreatedAt) // ưu tiên bid sớm nếu bằng giá
					.FirstOrDefault();

				if (top == null)
				{
					TempData["Info"] = "Phiên không có lượt trả giá nào.";
					return RedirectToAction("Details", "Items", new { id = session.ItemId });
				}

				if (top.BidderId != uid)
				{
					TempData["Info"] = "Bạn không phải người thắng phiên này.";
					return RedirectToAction("Details", "Items", new { id = session.ItemId });
				}

				var finalPrice = top.Amount;

				// Tổng tiền đặt trước đã hoàn tất của user cho phiên này
				var depositPaid = await _db.Payments
					.Where(p => p.SessionId == reg.SessionId
								&& p.UserId == uid
								&& p.Status == PaymentStatus.Completed
								&& p.Provider == PROVIDER_DEPOSIT)
					.Select(p => (decimal?)p.Amount).SumAsync() ?? 0m;

				var amountDue = Math.Max(0m, finalPrice - depositPaid);

				// Nếu đã đủ (VD: đặt trước >= giá chốt)
				if (amountDue <= 0m)
				{
					TempData["Success"] = "Bạn đã thanh toán đầy đủ. Không cần nộp thêm.";
					return RedirectToAction("Details", "Items", new { id = session.ItemId });
				}

				// Tạo hoặc tái sử dụng 1 payment WINNER đang Pending cho người dùng
				var payment = await EnsurePendingPaymentAsync(
					sessionId: reg.SessionId,
					userId: uid,
					amount: amountDue,
					provider: PROVIDER_WINNER);

				// Chuyển hướng theo phương thức (mặc định bank)
				var goQr = string.Equals(method, "qr", StringComparison.OrdinalIgnoreCase);
				return goQr
					? RedirectToAction(nameof(Qr), new { id = payment.Id })
					: RedirectToAction(nameof(Bank), new { id = payment.Id });
			}

			// 3) Phiên chưa kết thúc -> quay lại chi tiết
			TempData["Info"] = "Đăng ký đã được ghi nhận. Bạn có thể đấu giá khi phiên mở.";
			return RedirectToAction("Details", "Items", new { id = session.ItemId });
		}

		private async Task<Payment> EnsurePendingPaymentAsync(int sessionId, string userId, decimal amount, string provider)
		{
			// Nếu đã có payment Pending cùng provider -> tái sử dụng để tránh nhân đôi
			var existed = await _db.Payments
				.Where(p => p.SessionId == sessionId
						 && p.UserId == userId
						 && p.Provider == provider
						 && p.Status == PaymentStatus.Pending)
				.OrderByDescending(p => p.CreatedAt)
				.FirstOrDefaultAsync();

			if (existed != null)
			{
				// cân bằng lại số tiền nếu cần
				if (existed.Amount != amount)
				{
					existed.Amount = amount;
					await _db.SaveChangesAsync();
				}
				return existed;
			}

			var payment = new Payment
			{
				SessionId = sessionId,
				UserId = userId,
				Amount = amount,
				Status = PaymentStatus.Pending,
				Method = PaymentMethod.BankTransfer, // sẽ đổi khi user chọn QR
				Provider = provider,
				CreatedAt = DateTime.UtcNow
			};

			_db.Payments.Add(payment);
			await _db.SaveChangesAsync();
			return payment;
		}

		// ======================= B1: Nộp đặt trước =======================
		[HttpGet]
		public async Task<IActionResult> Deposit(int sessionId)
		{
			var s = await _db.Sessions.Include(x => x.Item).FirstOrDefaultAsync(x => x.Id == sessionId);
			if (s == null) return NotFound();

			var vm = new DepositVM
			{
				SessionId = sessionId,
				SuggestedAmount = s.DepositAmount > 0 ? s.DepositAmount : s.Item.StartingPrice
			};
			return View(vm);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Deposit(DepositVM vm)
		{
			if (vm.Amount <= 0) ModelState.AddModelError(nameof(vm.Amount), "Số tiền không hợp lệ.");
			if (!ModelState.IsValid) return View(vm);

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

			var p = new Payment
			{
				SessionId = vm.SessionId,
				UserId = userId,
				Amount = vm.Amount,
				Status = PaymentStatus.Pending,
				Method = vm.Method == PayMethod.Bank ? PaymentMethod.BankTransfer : PaymentMethod.Qr,
				Provider = "deposit",                  // <— thêm dòng này để phân biệt
				CreatedAt = DateTime.UtcNow
			};
			_db.Payments.Add(p);
			await _db.SaveChangesAsync();

			return vm.Method == PayMethod.Bank
				? RedirectToAction(nameof(Bank), new { id = p.Id })
				: RedirectToAction(nameof(Qr), new { id = p.Id });
		}

		// ======================= B2A: Bank Transfer =======================
		[HttpGet]
		public async Task<IActionResult> Bank(int id)
		{
			var p = await _db.Payments.Include(x => x.Session).ThenInclude(s => s.Item).FirstOrDefaultAsync(x => x.Id == id);
			if (p == null) return NotFound();
			if (p.Status == PaymentStatus.Completed) return RedirectToAction(nameof(Done), new { id });

			var vm = new BankVM
			{
				PaymentId = id,
				Amount = p.Amount,
				AccountName = "CONG TY DAU GIA OUROBOROS",
				AccountNumber = "123 456 789",
				BankName = "Vietcombank - CN Thái Nguyên",
				TransferNote = $"DG-{p.SessionId}-{p.UserId?.Substring(0, 6)}"
			};
			return View(vm);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> BankConfirm(int paymentId)
		{
			var p = await _db.Payments.FindAsync(paymentId);
			if (p == null) return NotFound();

			p.Status = PaymentStatus.Completed;
			p.PaidAt = DateTime.UtcNow;
			await _db.SaveChangesAsync();

			// Nếu là deposit => duyệt đăng ký
			if (p.Provider == "deposit")
			{
				var reg = await _db.Registrations
					.FirstOrDefaultAsync(r => r.SessionId == p.SessionId && r.UserId == p.UserId);
				if (reg != null && reg.Status == AuctionRegistration.StatusPendingDeposit)
				{
					reg.Status = AuctionRegistration.StatusApproved;
					reg.PaymentId = p.Id;
					await _db.SaveChangesAsync();
				}
			}

			return RedirectToAction(nameof(Done), new { id = paymentId });
		}

		// ======================= B2B: QR =======================
		[HttpGet]
		public async Task<IActionResult> Qr(int id)
		{
			var p = await _db.Payments.Include(x => x.Session).FirstOrDefaultAsync(x => x.Id == id);
			if (p == null) return NotFound();
			if (p.Status == PaymentStatus.Completed) return RedirectToAction(nameof(Done), new { id });

			// chuyển sang QR -> cập nhật method để hiển thị cho đúng
			if (p.Method != PaymentMethod.Qr)
			{
				p.Method = PaymentMethod.Qr;
				await _db.SaveChangesAsync();
			}

			ViewBag.QrPath = "/images/qr.png";
			ViewBag.Amount = p.Amount;
			ViewBag.PaymentId = id;
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> QrConfirm(int paymentId)
		{
			var p = await _db.Payments.FindAsync(paymentId);
			if (p == null) return NotFound();

			p.Status = PaymentStatus.Completed;
			p.PaidAt = DateTime.UtcNow;
			await _db.SaveChangesAsync();

			if (p.Provider == "deposit")
			{
				var reg = await _db.Registrations
					.FirstOrDefaultAsync(r => r.SessionId == p.SessionId && r.UserId == p.UserId);
				if (reg != null && reg.Status == AuctionRegistration.StatusPendingDeposit)
				{
					reg.Status = AuctionRegistration.StatusApproved;
					reg.PaymentId = p.Id;
					await _db.SaveChangesAsync();
				}
			}

			return RedirectToAction(nameof(Done), new { id = paymentId });
		}

		[HttpGet]
		public async Task<IActionResult> Settle(int sessionId)
		{
			var session = await _db.Sessions
				.Include(s => s.Item)
				.Include(s => s.Bids)
				.FirstOrDefaultAsync(s => s.Id == sessionId);
			if (session == null) return NotFound();

			// Chỉ cho thanh toán sau khi phiên kết thúc
			if (session.Status != AuctionSessionStatus.Ended || DateTime.UtcNow < session.EndUtc)
			{
				TempData["Error"] = "Phiên chưa kết thúc.";
				return RedirectToAction("Details", "Items", new { id = session.ItemId });
			}

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

			// Xác định top bid (tie-break theo CreatedAt)
			var top = session.Bids
				.OrderByDescending(b => b.Amount)
				.ThenBy(b => b.CreatedAt)
				.FirstOrDefault();

			if (top == null || top.BidderId != userId)
			{
				TempData["Error"] = "Bạn không phải người thắng phiên này.";
				return RedirectToAction("Details", "Items", new { id = session.ItemId });
			}

			// Đã thanh toán rồi?
			var alreadyPaid = await _db.Payments.AnyAsync(p =>
				p.SessionId == sessionId &&
				p.UserId == userId &&
				p.Status == PaymentStatus.Completed &&
				p.Provider == "settlement");

			if (alreadyPaid)
			{
				TempData["Info"] = "Bạn đã thanh toán tiền trúng cho phiên này.";
				return RedirectToAction("Details", "Items", new { id = session.ItemId });
			}

			// Reuse Deposit.cshtml với mục đích "settlement"
			var vm = new DepositVM
			{
				SessionId = sessionId,
				SuggestedAmount = top.Amount,
				Amount = top.Amount,
				Method = PayMethod.Bank
			};
			ViewBag.Purpose = "settlement";  // giúp view đổi tiêu đề/nút
			ViewBag.Title = "Thanh toán tiền trúng";
			return View("Deposit", vm);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Settle(DepositVM vm)
		{
			if (vm.Amount <= 0)
				ModelState.AddModelError(nameof(vm.Amount), "Số tiền không hợp lệ.");

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
			var session = await _db.Sessions
				.Include(s => s.Bids)
				.FirstOrDefaultAsync(s => s.Id == vm.SessionId);
			if (session == null) return NotFound();

			var top = session.Bids
				.OrderByDescending(b => b.Amount)
				.ThenBy(b => b.CreatedAt)
				.FirstOrDefault();

			if (top == null || top.BidderId != userId)
				return Forbid();

			if (vm.Amount < top.Amount)
				ModelState.AddModelError(nameof(vm.Amount), $"Số tiền phải ≥ giá trúng {top.Amount:N0}.");

			if (!ModelState.IsValid)
			{
				ViewBag.Purpose = "settlement";
				ViewBag.Title = "Thanh toán tiền trúng";
				return View("Deposit", vm);
			}

			var p = new Payment
			{
				SessionId = vm.SessionId,
				UserId = userId,
				Amount = vm.Amount,
				Status = PaymentStatus.Pending,
				Method = vm.Method == PayMethod.Bank ? PaymentMethod.BankTransfer : PaymentMethod.Qr,
				Provider = "settlement", // <— đánh dấu đây là thanh toán tiền trúng
				CreatedAt = DateTime.UtcNow
			};
			_db.Payments.Add(p);
			await _db.SaveChangesAsync();

			return vm.Method == PayMethod.Bank
				? RedirectToAction(nameof(Bank), new { id = p.Id })
				: RedirectToAction(nameof(Qr), new { id = p.Id });
		}
		// ======================= B3: Hoàn tất =======================
		[HttpGet]
		public async Task<IActionResult> Done(int id)
		{
			var p = await _db.Payments
				.Include(x => x.Session).ThenInclude(s => s.Item)
				.FirstOrDefaultAsync(x => x.Id == id);
			if (p == null) return NotFound();

			if (p.Provider == "settlement")
				TempData["Success"] = $"Thanh toán tiền trúng {p.Amount:N0}đ thành công.";
			else
				TempData["Success"] = $"Nộp tiền đặt trước {p.Amount:N0}đ thành công.";

			return View(p);
		}

		private async Task ApproveRegistrationAsync(int sessionId, string userId, int? paymentId = null)
		{
			var reg = await _db.Registrations
				.FirstOrDefaultAsync(r => r.SessionId == sessionId && r.UserId == userId);

			if (reg != null && reg.Status == AuctionRegistration.StatusPendingDeposit)
			{
				reg.Status = AuctionRegistration.StatusApproved;
				if (paymentId.HasValue)
					reg.PaymentId = paymentId.Value; // nếu entity có cột PaymentId
			}
		}
	}

	// ====== ViewModels nhỏ cho payment ======
	public enum PayMethod { Bank, Qr }

	public class DepositVM
	{
		public int SessionId { get; set; }
		public decimal SuggestedAmount { get; set; }
		public decimal Amount { get; set; }
		public PayMethod Method { get; set; } = PayMethod.Bank;
	}
	public class BankVM
	{
		public int PaymentId { get; set; }
		public decimal Amount { get; set; }
		public string AccountName { get; set; } = "";
		public string AccountNumber { get; set; } = "";
		public string BankName { get; set; } = "";
		public string TransferNote { get; set; } = "";
	}
}