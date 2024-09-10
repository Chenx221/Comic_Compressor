using ImageMagick;
using System.Text;
using System.Windows.Forms;
namespace Comic_Compressor
{
    internal class Program
    {
        [STAThread]
        static void Main()
        {
            //OpenCL.IsEnabled = true;
            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine("请选择源图像所在位置：");
            string? sourceImagePath = Utils.GetFolderPath();
            if (string.IsNullOrEmpty(sourceImagePath))
            {
                Console.WriteLine("未选择文件夹，程序将退出。");
                return;
            }

            Console.WriteLine("请选择保存位置：");
            string? targetStoragePath = Utils.GetFolderPath();
            if (string.IsNullOrEmpty(targetStoragePath))
            {
                Console.WriteLine("未选择文件夹，程序将退出。");
                return;
            }

            Console.WriteLine("处理线程数：");
            int threadCount = int.Parse(Console.ReadLine() ?? "2");
            Console.WriteLine($"处理线程数设定：{threadCount}");

            Console.WriteLine("目标格式：0 - webp, 1 - avif, 2 - JXL(JPEG-XL), 3 - JPG, 4 - PNG, 5 - BMP, 6 - 保留原格式(best effort)");
            string? modeInput = Console.ReadLine();
            if (modeInput == null)
            {
                Console.WriteLine("无效格式");
                return;
            }

            switch (modeInput)
            {
                case "0":
                    WebpCompressor.CompressImages(sourceImagePath, targetStoragePath, threadCount);
                    Utils.GetCompressorResult(sourceImagePath, targetStoragePath);
                    break;
                case "1":
                    AvifCompressor.CompressImages(sourceImagePath, targetStoragePath, threadCount);
                    Utils.GetCompressorResult(sourceImagePath, targetStoragePath);
                    break;
                case "2":
                    JxlCompressor.CompressImages(sourceImagePath, targetStoragePath, threadCount);
                    Utils.GetCompressorResult(sourceImagePath, targetStoragePath);
                    break;
                case "3":
                case "4":
                case "5":
                    LegacyFormatCompressor.CompressImages(sourceImagePath, targetStoragePath, threadCount,int.Parse(modeInput));
                    Utils.GetCompressorResult(sourceImagePath, targetStoragePath);
                    break;
                case "6":
                    throw new NotImplementedException();
                default:
                    Console.WriteLine("不支持的格式");
                    break;
            }
        }
    }
}
