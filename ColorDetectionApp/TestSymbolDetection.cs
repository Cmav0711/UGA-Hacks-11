using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using System;
using System.IO;

namespace ColorDetectionApp
{
    /// <summary>
    /// Test utility for symbol detection
    /// </summary>
    public class SymbolDetectionTest
    {
        public static void RunTest()
        {
            Console.WriteLine("Symbol Detection Test");
            Console.WriteLine("====================\n");

            // Ensure symbol templates exist
            if (!Directory.Exists("symbols") || Directory.GetDirectories("symbols").Length == 0)
            {
                Console.WriteLine("Generating symbol templates first...");
                SymbolTemplateGenerator.GenerateExampleTemplates();
                Console.WriteLine();
            }

            // Create a test image containing a circle
            string testImagePath = "test_symbol_image.png";
            CreateTestImage(testImagePath);
            Console.WriteLine($"Created test image: {testImagePath}");

            // Run symbol detection on the test image
            Console.WriteLine("\nRunning symbol detection...");
            Program.DetectAndRecordSymbolsEnhanced(testImagePath);
            
            Console.WriteLine("\nTest complete! Check detected_symbols.csv for results.");
        }

        private static void CreateTestImage(string path)
        {
            // Create a test image with a circle that matches our template
            using (var image = new Image<Rgba32>(300, 300))
            {
                image.Mutate(ctx =>
                {
                    ctx.BackgroundColor(Color.Black);
                    // Draw a circle similar to the template
                    var circle = new SixLabors.ImageSharp.Drawing.EllipsePolygon(
                        new PointF(150, 150), 50);
                    ctx.Fill(Color.White, circle);
                });
                image.Save(path);
            }
        }
    }
}
