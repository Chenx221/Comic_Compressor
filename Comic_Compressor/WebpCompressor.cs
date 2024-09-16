using ImageMagick;
using ShellProgressBar;

namespace Comic_Compressor
{
    internal class WebpCompressor : Utils
    {
        internal static void CompressImages(string sourceImagePath, string targetStoragePath, int threadCount, bool usePresetQuality, int Quality)
        {
            MagickFormat targetFormat = MagickFormat.WebP;
            string targetExtension = ".webp";
            int targetQuality = usePresetQuality ? 90 : Quality;

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
