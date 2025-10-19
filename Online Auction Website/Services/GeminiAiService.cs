using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OnlineAuctionWebsite.Models.ViewModels;

namespace OnlineAuctionWebsite.Services
{
	public class GeminiAiService : IAiDescriptionService
	{
		private readonly IHttpClientFactory _httpFactory;
		private readonly ILogger<GeminiAiService> _log;
		private readonly string _apiKey;

		public GeminiAiService(IConfiguration cfg, IHttpClientFactory httpFactory, ILogger<GeminiAiService> log)
		{
			_httpFactory = httpFactory;
			_log = log;
			_apiKey = cfg["Gemini:ApiKey"] ?? "";
		}

		public async Task<AiDescriptionResult> GenerateAsync(AiDescribeVM vm, CancellationToken ct = default)
		{
			if (string.IsNullOrWhiteSpace(_apiKey))
				throw new InvalidOperationException("Thiếu Gemini:ApiKey trong appsettings.");

			var endpoint =
				$"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";

			// parts: prompt + các ảnh inline base64
			var parts = new List<object>
			{
				new { text = BuildPrompt(vm) }
			};

			foreach (var f in vm.Images ?? new())
			{
				if (f?.Length > 0)
				{
					using var ms = new MemoryStream();
					await f.CopyToAsync(ms, ct);
					var b64 = Convert.ToBase64String(ms.ToArray());
					parts.Add(new
					{
						inlineData = new
						{
							mimeType = string.IsNullOrWhiteSpace(f.ContentType) ? "image/jpeg" : f.ContentType,
							data = b64
						}
					});
				}
			}

			// Yêu cầu structured output (JSON) với 2 field: descriptionHtml + tags
			var payload = new
			{
				contents = new[]
				{
					new { role = "user", parts }
				},
				generationConfig = new
				{
					temperature = 0.4,
					responseMimeType = "application/json",
					responseSchema = new
					{
						type = "object",
						properties = new
						{
							descriptionHtml = new { type = "string" },
							tags = new
							{
								type = "array",
								items = new { type = "string" }
							}
						},
						required = new[] { "descriptionHtml" }
					}
				}
			};

			var http = _httpFactory.CreateClient();
			var req = new HttpRequestMessage(HttpMethod.Post, endpoint)
			{
				Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
			};

			var resp = await http.SendAsync(req, ct);
			var body = await resp.Content.ReadAsStringAsync(ct);
			resp.EnsureSuccessStatusCode();

			// Kết quả của Gemini: candidates[0].content.parts[0].text -> là JSON string
			using var doc = JsonDocument.Parse(body);
			var textJson = doc.RootElement
				.GetProperty("candidates")[0]
				.GetProperty("content")
				.GetProperty("parts")[0]
				.GetProperty("text")
				.GetString();

			if (string.IsNullOrWhiteSpace(textJson))
				return new AiDescriptionResult { DescriptionHtml = "Không nhận được nội dung từ AI." };

			using var resultDoc = JsonDocument.Parse(textJson);
			var desc = resultDoc.RootElement.GetProperty("descriptionHtml").GetString() ?? "";
			var tags = new List<string>();
			if (resultDoc.RootElement.TryGetProperty("tags", out var tArr) && tArr.ValueKind == JsonValueKind.Array)
			{
				foreach (var t in tArr.EnumerateArray())
				{
					var s = t.GetString();
					if (!string.IsNullOrWhiteSpace(s)) tags.Add(s);
				}
			}

			return new AiDescriptionResult { DescriptionHtml = desc, Tags = tags };
		}

		private static string BuildPrompt(AiDescribeVM vm)
		{
			// Nhắc nhở AI trả về JSON (structured output) và viết tiếng Việt
			var sb = new StringBuilder();
			sb.AppendLine("Bạn là trợ lý viết mô tả sản phẩm đấu giá. Viết bằng tiếng Việt, rõ ràng, trung lập.");
			sb.AppendLine("Phân tích ảnh để nêu màu sắc, chất liệu, tình trạng (xước/móp/nứt nếu thấy).");
			sb.AppendLine("Trả về JSON với 2 field: descriptionHtml (cho phép <p>,<ul>,<li>,<h4>,<table>) và tags (tối đa 6 tag, dạng slug không dấu).");
			sb.AppendLine();
			sb.AppendLine("Thông tin văn bản:");
			sb.AppendLine($"- Tên: {vm.Title}");
			sb.AppendLine($"- Danh mục: {vm.CategoryName}");
			sb.AppendLine($"- Giá khởi điểm: {(vm.StartingPrice.HasValue ? vm.StartingPrice.Value.ToString("N0") + "đ" : "không có")}");
			if (!string.IsNullOrWhiteSpace(vm.Condition)) sb.AppendLine($"- Tình trạng: {vm.Condition}");
			if (!string.IsNullOrWhiteSpace(vm.Brand)) sb.AppendLine($"- Thương hiệu: {vm.Brand}");
			if (vm.Year.HasValue) sb.AppendLine($"- Năm: {vm.Year}");
			if (!string.IsNullOrWhiteSpace(vm.Material)) sb.AppendLine($"- Chất liệu: {vm.Material}");
			if (!string.IsNullOrWhiteSpace(vm.Dimensions)) sb.AppendLine($"- Kích thước: {vm.Dimensions}");
			if (!string.IsNullOrWhiteSpace(vm.Location)) sb.AppendLine($"- Vị trí: {vm.Location}");
			if (!string.IsNullOrWhiteSpace(vm.Notes)) sb.AppendLine($"- Ghi chú: {vm.Notes}");
			sb.AppendLine();
			sb.AppendLine("Yêu cầu bố cục:");
			sb.AppendLine("- Tiêu đề phụ: 'Tổng quan', 'Tình trạng', 'Thông số/Thông tin', 'Phụ kiện/Đi kèm' (nếu có).");
			sb.AppendLine("- Cuối cùng thêm bullet 'Lưu ý' (nêu hạn chế nếu phát hiện qua ảnh).");
			return sb.ToString();
		}
	}
}