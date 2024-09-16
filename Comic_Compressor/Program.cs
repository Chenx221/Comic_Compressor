using System.Text;
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
            if (threadCount < 1)
            {
                Console.WriteLine("无效线程数");
                return;
            }

            Console.WriteLine("目标格式：0 - webp, 1 - avif, 2 - JXL(JPEG-XL), 3 - JPG, 4 - PNG, 5 - BMP, 6 - 保留原格式");
            string? modeInput = Console.ReadLine();
            if (modeInput == null)
            {
                Console.WriteLine("无效格式");
                return;
            }

            Console.WriteLine("使用预设质量(默认使用)？(y/n)");
            string? input = Console.ReadLine()?.Trim().ToLower();
            bool usePresetQuality = input == null || input == "" || input == "y" || input == "yes";
            int targetQuality = -1;
            if (!usePresetQuality)
            {
                Console.WriteLine("Quality (0-100 INT):");
                string? targetQualityStr = Console.ReadLine();
                if (targetQualityStr == null)
                {
                    Console.WriteLine("无效输入");
                    return;
                }
                targetQuality = int.Parse(targetQualityStr);
                if (targetQuality < 0 || targetQuality > 100)
                {
                    Console.WriteLine("invalid image quality");
                    return;
                }
            }

            switch (modeInput)
            {
                case "0":
                    WebpCompressor.CompressImages(sourceImagePath, targetStoragePath, threadCount, usePresetQuality, targetQuality);
                    break;
                case "1":
                    AvifCompressor.CompressImages(sourceImagePath, targetStoragePath, threadCount, usePresetQuality, targetQuality);
                    break;
                case "2":
                    JxlCompressor.CompressImages(sourceImagePath, targetStoragePath, threadCount, usePresetQuality, targetQuality);
                    break;
                case "3":
                case "4":
                case "5":
                    LegacyFormatCompressor.CompressImages(sourceImagePath, targetStoragePath, threadCount, usePresetQuality, targetQuality, int.Parse(modeInput));
                    break;
                case "6":
                    MixProcessor.CompressImages(sourceImagePath, targetStoragePath, threadCount, usePresetQuality, targetQuality);
                    break;
                default:
                    Console.WriteLine("不支持的格式");
                    return;
            }
            Utils.GetCompressorResult(sourceImagePath, targetStoragePath);
        }
    }
}
