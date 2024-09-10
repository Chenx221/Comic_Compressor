using System.Text;
using System.Windows.Forms;
namespace Comic_Compressor
{
    internal class Program
    {
        [STAThread]
        static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine("请选择源图像所在位置：");
            string? sourceImagePath = GetFolderPath();
            if (string.IsNullOrEmpty(sourceImagePath))
            {
                Console.WriteLine("未选择文件夹，程序将退出。");
                return;
            }

            Console.WriteLine("请选择保存位置：");
            string? targetStoragePath = GetFolderPath();
            if (string.IsNullOrEmpty(targetStoragePath))
            {
                Console.WriteLine("未选择文件夹，程序将退出。");
                return;
            }

            Console.WriteLine("处理线程数：");
            int threadCount = int.Parse(Console.ReadLine() ?? "2");
            Console.WriteLine($"处理线程数设定：{threadCount}");

            Console.WriteLine("目标格式：0 - webp, 1 - avif, 2 - JXL(JPEG-XL), 3 - JPG, 4 - PNG, 5 - 保留原格式(best effort)");
            string? modeInput = Console.ReadLine();
            if (modeInput == null)
            {
                Console.WriteLine("无效输入");
                return;
            }

            switch (modeInput)
            {
                case "0":
                    WebpCompressor.CompressImages(sourceImagePath, targetStoragePath, threadCount);
                    GetCompressorResult(sourceImagePath, targetStoragePath);
                    break;
                case "1":
                    AvifCompressor.CompressImages(sourceImagePath, targetStoragePath, threadCount);
                    GetCompressorResult(sourceImagePath, targetStoragePath);
                    break;
                case "2":
                    JxlCompressor.CompressImages(sourceImagePath, targetStoragePath, threadCount);
                    GetCompressorResult(sourceImagePath, targetStoragePath);
                    break;
                case "3":
                case "4":
                    LegacyFormatCompressor.CompressImages(sourceImagePath, targetStoragePath, threadCount,int.Parse(modeInput));
                    GetCompressorResult(sourceImagePath, targetStoragePath);
                    break;
                case "5":
                    throw new NotImplementedException();
                default:
                    Console.WriteLine("不支持的格式");
                    break;
            }
        }


        private static string? GetFolderPath()
        {
            using var dialog = new FolderBrowserDialog();
            dialog.ShowNewFolderButton = false;

            DialogResult result = dialog.ShowDialog();

            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                return dialog.SelectedPath;
            }
            else
            {
                return null;
            }
        }

        private static long GetDirectorySize(string path)
        {
            long size = 0;

            foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                FileInfo fileInfo = new(file);
                size += fileInfo.Length;
            }

            return size;
        }

        private static string GetHumanReadableSize(long size)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = size;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }


        private static void GetCompressorResult(string source, string target)
        {
            long sourceSize = GetDirectorySize(source);
            long targetSize = GetDirectorySize(target);
            double reduced = (sourceSize - targetSize) * 1.0 / sourceSize;

            Console.WriteLine($"压缩前大小：{GetHumanReadableSize(sourceSize)}");
            Console.WriteLine($"压缩后大小：{GetHumanReadableSize(targetSize)}");
            Console.WriteLine($"体积已减少{reduced:P}");
        }

    }
}
