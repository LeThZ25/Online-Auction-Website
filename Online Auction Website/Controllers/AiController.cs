using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OnlineAuctionWebsite.Models;
using OnlineAuctionWebsite.Models.Entities;

namespace OnlineAuctionWebsite.Controllers
{
	[Route("ai")]
	[IgnoreAntiforgeryToken] // API JSON; widget gọi fetch
	public class AiController : Controller
	{
		private readonly IHttpClientFactory _http;
		private readonly GeminiOptions _opt;
		private readonly ApplicationDbContext _db;

		public AiController(IHttpClientFactory http, IOptions<GeminiOptions> opt, ApplicationDbContext db)
		{
			_http = http;
			_opt = opt.Value;
			_db = db;
		}

		// ========================== CHAT ==========================
		// POST /ai/chat  body: { message: "..." }
		[HttpPost("chat")]
		public async Task<IActionResult> Chat([FromBody] AiChatRequest req)
		{
			if (string.IsNullOrWhiteSpace(_opt.ApiKey))
				return Json(new { ok = false, error = "Thiếu API key Gemini." });

			var userMsg = (req?.Message ?? "").Trim();
			if (string.IsNullOrEmpty(userMsg))
				return Json(new { ok = false, error = "Nội dung trống." });

			// 1) Lấy snapshot hàng hóa + danh sách cấu trúc
			var (snapshot, items) = await BuildInventorySnapshotAsync(userMsg);

			// 2) Prompt ngữ cảnh ngắn, ưu tiên dữ liệu snapshot
			var system = """
Bạn là trợ lý AI cho website đấu giá (tiếng Việt, lịch sự, ngắn gọn).
Ưu tiên dùng dữ liệu ở phần "DỮ LIỆU SẢN PHẨM".
Nếu người dùng hỏi "có sản phẩm/brand X không", hãy dò danh sách và trả tối đa 5 kết quả: tên – trạng thái – giá ước tính – link.
Nếu không chắc, nói "mình chưa rõ". Không bịa số liệu.
""";

			var prompt = $@"{system}

DỮ LIỆU SẢN PHẨM (rút gọn):
{snapshot}

Người dùng: {userMsg}
Hướng dẫn định dạng:
- Xuống dòng rõ ràng (đoạn ngắn 1–3 câu). Nếu liệt kê, dùng gạch đầu dòng ""- "".
- Nếu dẫn link, dùng URL đầy đủ đã cho trong dữ liệu.
";

			// gọi model: cấu hình → 2.5 flash → 2.0 flash
			var (ok, text, apiError) = await CallGeminiAsync(prompt, _opt.Model, _opt.ApiKey);
			if (!ok) (ok, text, apiError) = await CallGeminiAsync(prompt, "gemini-2.5-flash", _opt.ApiKey);
			if (!ok) (ok, text, apiError) = await CallGeminiAsync(prompt, "gemini-2.0-flash", _opt.ApiKey);

			var html = ok ? ToSimpleHtml(text ?? "") : null;

			return Json(new
			{
				ok,
				reply = ok ? text : null,
				replyHtml = ok ? html : null,
				items = items.Select(x => new
				{
					id = x.Id,
					title = x.Title,
					status = x.Status,
					currentPrice = x.CurrentPrice,
					url = Url.Action("Details", "Items", new { id = x.Id }, Request.Scheme, Request.Host.Value)
				}),
				error = ok ? null : (apiError ?? "Không nhận được phản hồi từ AI.")
			});
		}

		// =================== GỢI Ý MÔ TẢ (HTML) ===================
		// JSON: { title, category, notes, startingPrice? }
		// multipart form-data: cùng keys; file ảnh chỉ đếm số lượng để thêm ngữ cảnh
		[HttpPost("describe")]
		public async Task<IActionResult> Describe()
		{
			if (string.IsNullOrWhiteSpace(_opt.ApiKey))
				return Json(new { ok = false, error = "Thiếu API key Gemini." });

			string title, category, notes;
			string? startingPrice;

			if (Request.HasFormContentType)
			{
				var f = Request.Form;
				title = (f["Title"].ToString() ?? "").Trim();
				category = (f["Category"].ToString() ?? f["CategoryName"].ToString() ?? "").Trim();
				notes = (f["Notes"].ToString() ?? "").Trim();
				startingPrice = (f["StartingPrice"].ToString() ?? "").Trim();

				// đếm ảnh (không upload lên Gemini trong phiên bản nhẹ)
				var imageCount = Request.Form.Files?.Count ?? 0;
				if (imageCount > 0)
				{
					notes = string.IsNullOrEmpty(notes)
						? $"Có {imageCount} ảnh minh hoạ đính kèm."
						: $"{notes}\n(Có {imageCount} ảnh minh hoạ đính kèm.)";
				}
			}
			else
			{
				var body = await JsonSerializer.DeserializeAsync<AiDescribeRequest>(
					Request.Body,
					new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
				) ?? new AiDescribeRequest();

				title = (body.Title ?? "").Trim();
				category = (body.Category ?? "").Trim();
				notes = (body.Notes ?? "").Trim();
				startingPrice = body.StartingPrice?.ToString();
			}

			var priceLine = string.IsNullOrWhiteSpace(startingPrice) ? "" : $"- Giá khởi điểm (ước tính): {startingPrice}\n";

			var prompt = $"""
Bạn là biên tập viên đấu giá. Viết MÔ TẢ NGẮN (120–200 từ) bằng tiếng Việt, rõ ràng, trung thực:
- Nêu tình trạng, phụ kiện, công năng, điểm nổi bật, lưu ý.
- Không bịa thông số kỹ thuật nếu người dùng không cung cấp.
- Trả về HTML đơn giản: <p>...</p> và/hoặc <ul><li>...</li></ul>. Không style inline.

Thông tin:
- Tiêu đề: {title}
- Danh mục: {category}
{priceLine}- Ghi chú: {notes}
""";

			var (ok, text, apiError) = await CallGeminiAsync(prompt, _opt.Model, _opt.ApiKey);
			if (!ok) (ok, text, apiError) = await CallGeminiAsync(prompt, "gemini-2.5-flash", _opt.ApiKey);
			if (!ok) (ok, text, apiError) = await CallGeminiAsync(prompt, "gemini-2.0-flash", _opt.ApiKey);

			return Json(new
			{
				ok,
				html = ok ? text : null,
				error = ok ? null : (apiError ?? "Không tạo được mô tả.")
			});
		}

		// ============== GỢI Ý MÔ TẢ TEXT NGẮN (plain) ==============
		// body: { title, notes }
		[HttpPost("suggest-description")]
		public async Task<IActionResult> SuggestDescription([FromBody] AiSuggestRequest req)
		{
			if (string.IsNullOrWhiteSpace(_opt.ApiKey))
				return Json(new { ok = false, error = "Thiếu API key Gemini." });

			var title = (req?.Title ?? "").Trim();
			var notes = (req?.Notes ?? "").Trim();

			var prompt = $"""
Viết mô tả ngắn gọn (3–6 câu) bằng tiếng Việt cho bài đăng đấu giá, văn phong minh bạch.
Nếu phù hợp, gợi ý 3–6 bullet (mỗi bullet bắt đầu bằng "- ").
Không bịa thông số kỹ thuật. Văn bản thuần (không HTML).

Tiêu đề: {title}
Ghi chú: {notes}
""";

			var (ok, text, apiError) = await CallGeminiAsync(prompt, _opt.Model, _opt.ApiKey);
			if (!ok) (ok, text, apiError) = await CallGeminiAsync(prompt, "gemini-2.5-flash", _opt.ApiKey);
			if (!ok) (ok, text, apiError) = await CallGeminiAsync(prompt, "gemini-2.0-flash", _opt.ApiKey);

			return Json(new
			{
				ok,
				text = ok ? text : null,
				error = ok ? null : (apiError ?? "Không tạo được gợi ý.")
			});
		}

		// ================= Core Gemini REST v1beta =================
		private async Task<(bool ok, string? text, string? error)> CallGeminiAsync(string prompt, string model, string apiKey)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(model))
					model = "gemini-2.5-flash";

				var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={Uri.EscapeDataString(apiKey)}";
				var http = _http.CreateClient();

				var payload = new
				{
					contents = new[]
					{
						new {
							role = "user",
							parts = new[] { new { text = prompt } }
						}
					},
					generationConfig = new
					{
						temperature = 0.6,
						topP = 0.95,
						maxOutputTokens = 1024
					}
				};

				var req = new HttpRequestMessage(HttpMethod.Post, url)
				{
					Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
				};
				req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

				using var res = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
				var body = await res.Content.ReadAsStringAsync();

				if (!res.IsSuccessStatusCode)
				{
					string? msg = TryExtractGeminiError(body) ?? $"HTTP {(int)res.StatusCode}";
					return (false, null, msg);
				}

				using var doc = JsonDocument.Parse(body);
				var root = doc.RootElement;

				// candidates[0].content.parts[0].text
				var cand = root.GetProperty("candidates")[0];
				var parts = cand.GetProperty("content").GetProperty("parts");
				var text = parts[0].GetProperty("text").GetString();

				return (true, text, null);
			}
			catch (Exception ex)
			{
				return (false, null, ex.Message);
			}
		}

		private static string? TryExtractGeminiError(string json)
		{
			try
			{
				using var doc = JsonDocument.Parse(json);
				if (doc.RootElement.TryGetProperty("error", out var e))
				{
					var code = e.TryGetProperty("code", out var c) ? c.GetString() : null;
					var msg = e.TryGetProperty("message", out var m) ? m.GetString() : null;
					return $"{code}: {msg}";
				}
			}
			catch { /* ignore */ }
			return null;
		}

		// =================== Helpers: format HTML ===================
		private static string ToSimpleHtml(string text)
		{
			if (string.IsNullOrWhiteSpace(text)) return "<p></p>";
			text = text.Replace("\r\n", "\n");
			var enc = HtmlEncoder.Default.Encode(text);
			var blocks = Regex.Split(enc, @"\n{2,}");
			var sb = new StringBuilder();
			foreach (var b in blocks)
			{
				sb.Append("<p>").Append(b.Replace("\n", "<br/>")).Append("</p>");
			}
			return sb.ToString();
		}

		// ======== Build inventory snapshot (safe for EF Core) ========
		private sealed class ChatItemVM
		{
			public int Id { get; set; }
			public string Title { get; set; } = "";
			public string Status { get; set; } = "";
			public decimal CurrentPrice { get; set; }
		}

		private async Task<(string snapshot, List<ChatItemVM> items)> BuildInventorySnapshotAsync(string userMsg)
		{
			var now = DateTime.UtcNow;

			// Lấy token chữ/số (>=3), giữ cả bản raw và slug để match tiếng Việt
			var rawTokens = Regex.Matches(userMsg.ToLowerInvariant(), @"[\p{L}\p{Nd}]{3,}")
								 .Select(m => m.Value)
								 .Distinct()
								 .Take(8)
								 .ToList();

			var baseQuery = _db.Items.AsNoTracking()
				.Include(i => i.Sessions).ThenInclude(s => s.Bids)
				.Include(i => i.ItemTags).ThenInclude(it => it.Tag)
				.Include(i => i.Category);

			List<AuctionItem> merged = new();

			if (rawTokens.Count > 0)
			{
				foreach (var raw in rawTokens)
				{
					var slug = Slugify(raw);

					// Chỉ dùng các biểu thức có thể translate xuống DB
					var sub = await baseQuery
						.Where(i =>
							EF.Functions.Like(i.Title, $"%{raw}%") ||
							EF.Functions.Like(i.AssetCode, $"%{raw}%") ||
							(i.Category != null && EF.Functions.Like(i.Category.Name, $"%{raw}%")) ||
							i.ItemTags.Any(it =>
								EF.Functions.Like(it.Tag.Name, $"%{raw}%") ||
								EF.Functions.Like(it.Tag.Slug, $"%{slug}%")
							)
						)
						.OrderByDescending(i => i.CreatedAt)
						.Take(30)
						.ToListAsync();

					// Lọc tinh IN-MEMORY với Slugify (không để EF translate)
					sub = sub.Where(i =>
						(i.Title ?? "").Contains(raw, StringComparison.OrdinalIgnoreCase) ||
						(i.AssetCode ?? "").Contains(raw, StringComparison.OrdinalIgnoreCase) ||
						(i.Category?.Name != null && Slugify(i.Category.Name).Contains(slug)) ||
						i.ItemTags.Any(it =>
							(it.Tag?.Name ?? "").Contains(raw, StringComparison.OrdinalIgnoreCase) ||
							(it.Tag?.Slug ?? "").Contains(slug, StringComparison.OrdinalIgnoreCase)
						)
					).ToList();

					merged.AddRange(sub);
				}
			}
			else
			{
				// Không có từ khóa: ưu tiên Live rồi Newest
				var liveIds = await _db.Sessions.AsNoTracking()
					.Where(s => s.Status == AuctionSessionStatus.Live && s.StartUtc <= now && s.EndUtc > now)
					.OrderBy(s => s.EndUtc)
					.Select(s => s.ItemId)
					.Distinct()
					.Take(10)
					.ToListAsync();

				var liveItems = await baseQuery.Where(i => liveIds.Contains(i.Id)).ToListAsync();
				var newest = await baseQuery.OrderByDescending(i => i.CreatedAt).Take(12).ToListAsync();
				merged = liveItems.Concat(newest).GroupBy(i => i.Id).Select(g => g.First()).ToList();
			}

			var items = merged
				.GroupBy(i => i.Id)
				.Select(g => g.First())
				.OrderByDescending(i => i.Sessions.Any(s => s.Status == AuctionSessionStatus.Live && s.StartUtc <= now && s.EndUtc > now))
				.ThenByDescending(i => i.CreatedAt)
				.Take(12)
				.ToList();

			if (items.Count == 0)
				return ("(chưa có dữ liệu phù hợp cho truy vấn này)", new List<ChatItemVM>());

			string StatOf(AuctionItem i)
			{
				var live = i.Sessions.Any(s => s.Status == AuctionSessionStatus.Live && s.StartUtc <= now && s.EndUtc > now);
				if (live) return "Live";
				var upcoming = i.Sessions.Any(s => s.Status == AuctionSessionStatus.Scheduled && s.StartUtc > now);
				return upcoming ? "Upcoming" : "Ended";
			}

			decimal CurrentPrice(AuctionItem i)
			{
				var live = i.Sessions.FirstOrDefault(s => s.Status == AuctionSessionStatus.Live && s.StartUtc <= now && s.EndUtc > now);
				if (live?.Bids?.Count > 0) return live.Bids.Max(b => b.Amount);
				var any = i.Sessions.SelectMany(s => s.Bids ?? new List<Bid>()).ToList();
				return any.Count > 0 ? any.Max(b => b.Amount) : i.StartingPrice;
			}

			var list = new List<ChatItemVM>();
			var sb = new StringBuilder();
			foreach (var it in items.Take(12))
			{
				var url = Url.Action("Details", "Items", new { id = it.Id }, Request.Scheme, Request.Host.Value);
				var status = StatOf(it);
				var price = CurrentPrice(it);

				list.Add(new ChatItemVM
				{
					Id = it.Id,
					Title = it.Title,
					Status = status,
					CurrentPrice = price
				});

				sb.AppendLine($"- {it.Title} | {status} | ~{price:N0} đ | {url}");
			}

			return (sb.ToString(), list.Take(5).ToList());
		}

		// ================= Utilities: slug & bỏ dấu =================
		private static string Slugify(string? s)
		{
			s ??= "";
			s = s.Trim().ToLowerInvariant();
			s = RemoveDiacritics(s);
			foreach (var ch in new[] { ' ', '_', '.', ',', ';', '/', '\\', ':' })
				s = s.Replace(ch, '-');
			while (s.Contains("--")) s = s.Replace("--", "-");
			return s.Trim('-');
		}

		private static string RemoveDiacritics(string text)
		{
			if (string.IsNullOrEmpty(text)) return text;
			var normalized = text.Normalize(NormalizationForm.FormD);
			var sb = new StringBuilder();
			foreach (var ch in normalized)
			{
				var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
				if (cat != UnicodeCategory.NonSpacingMark) sb.Append(ch);
			}
			return sb.ToString().Normalize(NormalizationForm.FormC);
		}
	}

	// ============================ DTOs ============================
	public sealed class AiChatRequest { public string? Message { get; set; } }

	public sealed class AiDescribeRequest
	{
		public string? Title { get; set; }
		public string? Category { get; set; }
		public string? Notes { get; set; }
		public decimal? StartingPrice { get; set; } // optional
	}

	public sealed class AiSuggestRequest
	{
		public string? Title { get; set; }
		public string? Notes { get; set; }
	}
}