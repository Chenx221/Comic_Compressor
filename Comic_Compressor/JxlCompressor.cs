using ImageMagick;
using ShellProgressBar;

namespace Comic_Compressor
{
    internal class JxlCompressor : Utils
    {
        internal static void CompressImages(string sourceImagePath, string targetStoragePath, int threadCount)
        {
            //config
            MagickFormat targetFormat = MagickFormat.Jxl;
            string targetExtension = ".jxl";
            int targetQuality = 90;

            List<string> subdirectories = new(Directory.GetDirectories(sourceImagePath, "*", SearchOption.AllDirectories));

            int totalFiles = 0;
            foreach (string subdirectory in subdirectories)
            {
                totalFiles += GetImageFiles(subdirectory).Length;
            }

            using var progressBar = new ShellProgressBar.ProgressBar(totalFiles, "Compressing images", new ProgressBarOptions
            {
                ProgressCharacter = '─',
                ProgressBarOnBottom = true
            });

            foreach (string subdirectory in subdirectories)
            {
                ProcessDirectory(subdirectory, sourceImagePath, targetStoragePath, progressBar, threadCount, targetExtension, targetFormat, targetQuality);
            }

            Console.WriteLine("All directories processed successfully.");
        }
    }
}
