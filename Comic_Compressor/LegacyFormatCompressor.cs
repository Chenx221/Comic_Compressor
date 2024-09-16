using ImageMagick;
using ShellProgressBar;
namespace Comic_Compressor
{
    //Process images in legacy format(JPG,PNG)
    internal class LegacyFormatCompressor : Utils
    {
        internal static void CompressImages(string sourceImagePath, string targetStoragePath, int threadCount, bool usePresetQuality, int Quality, int format)
        {
            MagickFormat targetFormat = format switch
            {
                3 => MagickFormat.Jpeg,
                4 => MagickFormat.Png,
                5 => MagickFormat.Bmp,
                _ => throw new Exception(),
            };

            string targetExtension = format switch
            {
                3 => ".jpg",
                4 => ".png",
                5 => ".bmp",
                _ => throw new Exception(),
            };

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
