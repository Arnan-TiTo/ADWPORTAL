using QRCoder;

namespace miniApp.API.Services
{
    public class QrService
    {
        public byte[] MakePng(string content, int pixelsPerModule = 10)
        {
            var gen = new QRCodeGenerator();
            var data = gen.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            var png = new PngByteQRCode(data);
            return png.GetGraphic(pixelsPerModule);
        }
    }
}