using ImageMagick;
using System.Collections.Concurrent;

namespace Comic_Compressor
{
    internal class Utils
    {
        // Compress an image file
        // Resize the image if its max dimension is larger than 1200
        // Set the quality of the compressed image
        public static void CompressImage(string sourceFilePath, string targetFilePath, MagickFormat mFormat, int quality)
        {
            using MagickImage image = new(sourceFilePath);

            uint maxDimension = image.Width > image.Height ? image.Width : image.Height;
            if (maxDimension > 1200)
            {
                double scaleFactor = 1200.0 / maxDimension;
                uint newWidth = (uint)(image.Width * scaleFactor);
                uint newHeight = (uint)(image.Height * scaleFactor);
                image.Resize(newWidth, newHeight);
            }
            image.Quality = (uint)quality;

            image.Write(targetFilePath, mFormat);
        }

        // Get all image files in a directory with supported extensions
        public static string[] GetImageFiles(string directoryPath)
        {
            string[] supportedExtensions = ["*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif", "*.tiff", "*.tif", "*.jxl", "*.avif", "*.webp"];
            ConcurrentBag<string> allFiles = [];

            foreach (string extension in supportedExtensions)
            {
                foreach (var file in Directory.EnumerateFiles(directoryPath, extension, SearchOption.TopDirectoryOnly))
                {
                    allFiles.Add(file);
                }
            }
            return [.. allFiles];
        }

        // Process all image files in a directory
        public static void ProcessDirectory(string subdirectory, string sourceImagePath, string targetStoragePath, ShellProgressBar.ProgressBar progressBar, int threadCount, string format, MagickFormat mFormat, int quality)
        {
            string relativePath = Path.GetRelativePath(sourceImagePath, subdirectory);

            string targetSubdirectory = Path.Combine(targetStoragePath, relativePath);
            Directory.CreateDirectory(targetSubdirectory);

            string[] imageFiles = GetImageFiles(subdirectory);

            ParallelOptions options = new()
            {
                MaxDegreeOfParallelism = threadCount // Adjust this value to set the number of concurrent threads
            };

            Parallel.ForEach(imageFiles, options, imageFile =>
            {
                string targetFilePath = Path.Combine(targetSubdirectory, Path.GetFileNameWithoutExtension(imageFile) + format);

                if (!File.Exists(targetFilePath))
                {
                    CompressImage(imageFile, targetFilePath, mFormat, quality);

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

        //Process all image files in a directory, save as origin format
        public static void ProcessDirectory(string subdirectory, string sourceImagePath, string targetStoragePath, ShellProgressBar.ProgressBar progressBar, int threadCount, int quality)
        {
            string relativePath = Path.GetRelativePath(sourceImagePath, subdirectory);

            string targetSubdirectory = Path.Combine(targetStoragePath, relativePath);
            Directory.CreateDirectory(targetSubdirectory);

            string[] imageFiles = GetImageFiles(subdirectory);

            ParallelOptions options = new()
            {
                MaxDegreeOfParallelism = threadCount // Adjust this value to set the number of concurrent threads
            };

            Parallel.ForEach(imageFiles, options, imageFile =>
            {
                string targetFilePath = Path.Combine(targetSubdirectory, Path.GetFileName(imageFile));
                //detect file format
                //supportedExtensions = ["*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif", "*.tiff", "*.jxl", "*.avif", "*.webp"];
                string extension = Path.GetExtension(targetFilePath).ToLower();

                MagickFormat mFormat = extension switch
                {
                    ".jpg" or ".jpeg" => MagickFormat.Jpeg,
                    ".png" => MagickFormat.Png,
                    ".bmp" => MagickFormat.Bmp,
                    ".gif" => MagickFormat.Gif,
                    ".tiff" or ".tif" => MagickFormat.Tiff,
                    ".jxl" => MagickFormat.Jxl,
                    ".avif" => MagickFormat.Avif,
                    ".webp" => MagickFormat.WebP,
                    _ => throw new Exception()//这个位置怎么还会有意外情况
                };
                if (!File.Exists(targetFilePath))
                {
                    CompressImage(imageFile, targetFilePath, mFormat, quality);

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

        // Display the compression result
        public static void GetCompressorResult(string source, string target)
        {
            long sourceSize = GetDirectorySize(source);
            long targetSize = GetDirectorySize(target);
            double reduced = (sourceSize - targetSize) * 1.0 / sourceSize;

            Console.WriteLine($"压缩前大小：{GetHumanReadableSize(sourceSize)}");
            Console.WriteLine($"压缩后大小：{GetHumanReadableSize(targetSize)}");
            Console.WriteLine($"体积已减少{reduced:P}");
        }

        // Get the size of a directory
        public static long GetDirectorySize(string path)
        {
            long size = 0;

            foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                FileInfo fileInfo = new(file);
                size += fileInfo.Length;
            }

            return size;
        }

        // Get the path of a folder via a dialog
        public static string? GetFolderPath()
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

        // Get the human-readable size of a file
        public static string GetHumanReadableSize(long size)
        {
            string[] sizes = ["B", "KB", "MB", "GB", "TB"];
            double len = size;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}