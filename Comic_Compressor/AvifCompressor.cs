using LibHeifSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using ShellProgressBar;
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.PixelFormats;

namespace Comic_Compressor
{
    internal class AvifCompressor
    {
        internal static void CompressImages(string sourceImagePath, string targetStoragePath)
        {
            LibHeifSharpDllImportResolver.Register();
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
                ProcessDirectory(subdirectory, sourceImagePath, targetStoragePath, progressBar);
            }

            Console.WriteLine("All directories processed successfully.");
        }

        private static void ProcessDirectory(string subdirectory, string sourceImagePath, string targetStoragePath, ShellProgressBar.ProgressBar progressBar)
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
                MaxDegreeOfParallelism = 2 // Adjust this value to set the number of concurrent threads
            };

            // Process each image file in parallel
            Parallel.ForEach(imageFiles, options, imageFile =>
            {
                // Set the target file path with the .avif extension
                string targetFilePath = Path.Combine(targetSubdirectory, Path.GetFileNameWithoutExtension(imageFile) + ".avif");

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
            int quality = 80;
            //int quality = 70;
            var format = HeifCompressionFormat.Av1;
            bool saveAlphaChannel = false;
            bool writeTwoProfiles = false;

            try
            {
                // Load the image and ensure it's in Rgb24 format
                using var image = SixLabors.ImageSharp.Image.Load(sourceFilePath);
                var rgbImage = image.CloneAs<Rgb24>();

                //// Check the longest side of the image and resize if necessary
                //int maxDimension = Math.Max(rgbImage.Width, rgbImage.Height);
                //if (maxDimension > 1200)
                //{
                //    double scaleFactor = 1200.0 / maxDimension;
                //    int newWidth = (int)(rgbImage.Width * scaleFactor);
                //    int newHeight = (int)(rgbImage.Height * scaleFactor);
                //    rgbImage.Mutate(x => x.Resize(newWidth, newHeight));
                //}

                // Save as AVIF format
                using var context = new HeifContext();
                HeifEncoderDescriptor? encoderDescriptor = null;

                if (LibHeifInfo.HaveEncoder(format))
                {
                    var encoderDescriptors = context.GetEncoderDescriptors(format);
                    encoderDescriptor = encoderDescriptors[0];
                }
                else
                {
                    Console.WriteLine("No AV1 encoder available.");
                    return;
                }

                using HeifEncoder encoder = context.GetEncoder(encoderDescriptor);
                if (writeTwoProfiles && !LibHeifInfo.CanWriteTwoColorProfiles)
                {
                    writeTwoProfiles = false;
                    Console.WriteLine($"Warning: LibHeif version {LibHeifInfo.Version} cannot write two color profiles.");
                }

                using var heifImage = CreateHeifImage(rgbImage, writeTwoProfiles, out var metadata);
                encoder.SetLossyQuality(quality);

                var encodingOptions = new HeifEncodingOptions
                {
                    SaveAlphaChannel = saveAlphaChannel,
                    WriteTwoColorProfiles = writeTwoProfiles
                };
                context.EncodeImage(heifImage, encoder, encodingOptions);
                context.WriteToFile(targetFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }


        private static HeifImage CreateHeifImage(Image<Rgb24> image,
                                                 bool writeTwoColorProfiles,
                                                 out ImageMetadata metadata)
        {
            HeifImage? heifImage = null;
            HeifImage? temp = null;

            try
            {
                metadata = image.Metadata;
                temp = ConvertToHeifImage(image);

                if (writeTwoColorProfiles && metadata.IccProfile != null)
                {
                    temp.IccColorProfile = new HeifIccColorProfile(metadata.IccProfile.ToByteArray());

                    temp.NclxColorProfile = new HeifNclxColorProfile(ColorPrimaries.BT709,
                                                                     TransferCharacteristics.Srgb,
                                                                     MatrixCoefficients.BT601,
                                                                     fullRange: true);
                }
                else
                {
                    if (metadata.IccProfile != null)
                    {
                        temp.IccColorProfile = new HeifIccColorProfile(metadata.IccProfile.ToByteArray());
                    }
                    else
                    {
                        temp.NclxColorProfile = new HeifNclxColorProfile(ColorPrimaries.BT709,
                                                                         TransferCharacteristics.Srgb,
                                                                         MatrixCoefficients.BT601,
                                                                         fullRange: true);
                    }
                }

                heifImage = temp;
                temp = null;
            }
            finally
            {
                temp?.Dispose();
            }

            return heifImage;
        }

        private static HeifImage ConvertToHeifImage(Image<Rgb24> image)
        {
            bool isGrayscale = IsGrayscale(image);

            var colorspace = isGrayscale ? HeifColorspace.Monochrome : HeifColorspace.Rgb;
            var chroma = colorspace == HeifColorspace.Monochrome ? HeifChroma.Monochrome : HeifChroma.InterleavedRgb24;

            HeifImage? heifImage = null;
            HeifImage? temp = null;

            try
            {
                temp = new HeifImage(image.Width, image.Height, colorspace, chroma);

                if (colorspace == HeifColorspace.Monochrome)
                {
                    temp.AddPlane(HeifChannel.Y, image.Width, image.Height, 8);
                    CopyGrayscale(image, temp);
                }
                else
                {
                    temp.AddPlane(HeifChannel.Interleaved, image.Width, image.Height, 8);
                    CopyRgb(image, temp);
                }

                heifImage = temp;
                temp = null;
            }
            finally
            {
                temp?.Dispose();
            }

            return heifImage;
        }

        private static unsafe void CopyGrayscale(Image<Rgb24> image, HeifImage heifImage)
        {
            var grayPlane = heifImage.GetPlane(HeifChannel.Y);

            byte* grayPlaneScan0 = (byte*)grayPlane.Scan0;
            int grayPlaneStride = grayPlane.Stride;

            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    var src = accessor.GetRowSpan(y);
                    byte* dst = grayPlaneScan0 + (y * grayPlaneStride);

                    for (int x = 0; x < accessor.Width; x++)
                    {
                        ref var pixel = ref src[x];

                        dst[0] = pixel.R;

                        dst++;
                    }
                }
            });
        }

        private static unsafe void CopyRgb(Image<Rgb24> image, HeifImage heifImage)
        {
            var interleavedData = heifImage.GetPlane(HeifChannel.Interleaved);

            byte* srcScan0 = (byte*)interleavedData.Scan0;
            int srcStride = interleavedData.Stride;

            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    var src = accessor.GetRowSpan(y);
                    byte* dst = srcScan0 + (y * srcStride);

                    for (int x = 0; x < accessor.Width; x++)
                    {
                        ref var pixel = ref src[x];

                        dst[0] = pixel.R;
                        dst[1] = pixel.G;
                        dst[2] = pixel.B;

                        dst += 3;
                    }
                }
            });
        }

        private static bool IsGrayscale(Image<Rgb24> image)
        {
            bool isGrayscale = true;

            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    var src = accessor.GetRowSpan(y);

                    for (int x = 0; x < accessor.Width; x++)
                    {
                        ref var pixel = ref src[x];

                        if (!(pixel.R == pixel.G && pixel.G == pixel.B))
                        {
                            isGrayscale = false;
                            break;
                        }
                    }

                    if (!isGrayscale)
                    {
                        break;
                    }
                }
            });

            return isGrayscale;
        }

    }
}
