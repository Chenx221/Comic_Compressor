using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using ShellProgressBar;

namespace Comic_Compressor
{
    internal class WebpCompressor
    {
        internal static void CompressImages(string sourceImagePath, string targetStoragePath, int threadCount)
        {
            // Step 1: Get all subdirectories and store them in a list
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

            // Step 2: Iterate through each subdirectory in order
            foreach (string subdirectory in subdirectories)
            {
                // Step 3: Process each directory
                ProcessDirectory(subdirectory, sourceImagePath, targetStoragePath, progressBar, threadCount);
            }

            Console.WriteLine("All directories processed successfully.");
        }

        private static void ProcessDirectory(string subdirectory, string sourceImagePath, string targetStoragePath, ShellProgressBar.ProgressBar progressBar, int threadCount)
        {
            // Get the relative path of the subdirectory
            string relativePath = Path.GetRelativePath(sourceImagePath, subdirectory);

            // Create the corresponding subdirectory in the target storage path
            string targetSubdirectory = Path.Combine(targetStoragePath, relativePath);
            Directory.CreateDirectory(targetSubdirectory);

            // Get all image files in the subdirectory (jpg and png)
            string[] imageFiles = GetImageFiles(subdirectory);

            // Set up ParallelOptions to limit the number of concurrent threads
            ParallelOptions options = new()
            {
                MaxDegreeOfParallelism = threadCount // Adjust this value to set the number of concurrent threads
            };

            // Process each image file in parallel
            Parallel.ForEach(imageFiles, options, imageFile =>
            {
                // Set the target file path with the .webp extension
                string targetFilePath = Path.Combine(targetSubdirectory, Path.GetFileNameWithoutExtension(imageFile) + ".webp");

                // Check if the target file already exists
                if (!File.Exists(targetFilePath))
                {
                    CompressImage(imageFile, targetFilePath);

                    // Update progress bar safely
                    lock (progressBar)
                    {
                        progressBar.Tick($"Processed {Path.GetFileName(imageFile)}");
                    }
                }
                else
                {
                    lock (progressBar) { progressBar.Tick($"Skipped {Path.GetFileName(imageFile)}"); }
                }
            });
        }

        private static string[] GetImageFiles(string directoryPath)
        {
            // Get all image files supported by ImageSharp
            string[] supportedExtensions = ["*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif", "*.tiff", "*.gif"];
            List<string> allFiles = [];

            foreach (string extension in supportedExtensions)
            {
                allFiles.AddRange(Directory.GetFiles(directoryPath, extension, SearchOption.TopDirectoryOnly));
            }

            return [.. allFiles];
        }

        private static void CompressImage(string sourceFilePath, string targetFilePath)
        {
            using SixLabors.ImageSharp.Image image = SixLabors.ImageSharp.Image.Load(sourceFilePath);
            // Check the longest side of the image and resize if necessary
            int maxDimension = Math.Max(image.Width, image.Height);
            if (maxDimension > 1200)
            {
                double scaleFactor = 1200.0 / maxDimension;
                int newWidth = (int)(image.Width * scaleFactor);
                int newHeight = (int)(image.Height * scaleFactor);
                image.Mutate(x => x.Resize(newWidth, newHeight));
            }

            // Save the image as WebP with a quality level of 85 (for lossy compression)
            var encoder = new WebpEncoder
            {
                Quality = 90,
                FileFormat = WebpFileFormatType.Lossy
            };

            image.Save(targetFilePath, encoder);
        }
    }
}
