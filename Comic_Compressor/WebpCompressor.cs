using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Comic_Compressor
{
    internal class WebpCompressor
    {
        internal static void CompressImages(string sourceImagePath, string targetStoragePath)
        {
            // Step 1: Get all subdirectories and store them in a list
            List<string> subdirectories = new(Directory.GetDirectories(sourceImagePath, "*", SearchOption.AllDirectories));

            // Step 2: Iterate through each subdirectory in order
            foreach (string subdirectory in subdirectories)
            {
                // Step 3: Process each directory
                ProcessDirectory(subdirectory, sourceImagePath, targetStoragePath);
            }
        }

        private static void ProcessDirectory(string subdirectory, string sourceImagePath, string targetStoragePath)
        {
            // Get the relative path of the subdirectory
            string relativePath = Path.GetRelativePath(sourceImagePath, subdirectory);

            // Create the corresponding subdirectory in the target storage path
            string targetSubdirectory = Path.Combine(targetStoragePath, relativePath);
            Directory.CreateDirectory(targetSubdirectory);

            // Get all image files in the subdirectory (jpg and png)
            string[] imageFiles = GetImageFiles(subdirectory);

            // Iterate through each image file
            foreach (string imageFile in imageFiles)
            {
                // Set the target file path with the .webp extension
                string targetFilePath = Path.Combine(targetSubdirectory, Path.GetFileNameWithoutExtension(imageFile) + ".webp");
                CompressImage(imageFile, targetFilePath);
            }

            Console.WriteLine($"{Path.GetFileName(subdirectory)} processed successfully.");
        }

        private static string[] GetImageFiles(string directoryPath)
        {
            // Get all image files supported by ImageSharp
            string[] supportedExtensions = ["*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif", "*.tiff"];
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

            // Save the image as WebP with a quality level of 70 (for lossy compression)
            var encoder = new WebpEncoder
            {
                Quality = 70,
                FileFormat = WebpFileFormatType.Lossy
            };

            image.Save(targetFilePath, encoder);
        }
    }
}
