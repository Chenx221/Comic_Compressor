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

            Console.WriteLine("请输入目标存储位置：");
            string? targetStoragePath = GetFolderPath();
            if (string.IsNullOrEmpty(targetStoragePath))
            {
                Console.WriteLine("未选择文件夹，程序将退出。");
                return;
            }

            Console.WriteLine("处理线程数：");
            int threadCount = int.Parse(Console.ReadLine() ?? "2");
            Console.WriteLine($"处理线程数设定：{threadCount}");

            Console.WriteLine("请选择压缩模式：0 - 压缩成webp，1 - 压缩成avif");
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
                default:
                    Console.WriteLine("不支持的模式");
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

            // 遍历目录中的所有文件
            foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                // 获取文件的大小并累加到总大小中
                FileInfo fileInfo = new(file);
                size += fileInfo.Length;
            }

            return size;
        }

        private static void GetCompressorResult(string source, string target)
        {
            long sourceSize = GetDirectorySize(source);
            long targetSize = GetDirectorySize(target);
            double reduced = (sourceSize - targetSize) * 1.0 / sourceSize;

            Console.WriteLine($"源目录大小：{sourceSize} 字节");
            Console.WriteLine($"目标目录大小：{targetSize} 字节");
            Console.WriteLine($"已减少：{reduced:P}的体积");
        }

    }
}
