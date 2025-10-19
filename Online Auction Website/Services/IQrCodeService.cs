public interface IQrCodeService
{
	byte[] GeneratePng(string payload, int pixelsPerModule = 10);
}