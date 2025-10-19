using Microsoft.AspNetCore.Mvc;
using QRCoder;
public class QrCodeService : IQrCodeService
{
	public byte[] GeneratePng(string payload, int ppm = 10)
	{
		using var gen = new QRCodeGenerator();
		using var data = gen.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
		using var qr = new PngByteQRCode(data);
		return qr.GetGraphic(ppm);
	}
}
