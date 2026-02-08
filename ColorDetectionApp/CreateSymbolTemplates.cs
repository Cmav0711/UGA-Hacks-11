using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Drawing;
using System;
using System.IO;

namespace ColorDetectionApp
{
    /// <summary>
    /// Utility class to create example symbol templates for testing symbol detection
    /// </summary>
    public class SymbolTemplateGenerator
    {
        public static void GenerateExampleTemplates()
        {
            string symbolsDir = "symbols";
            if (!Directory.Exists(symbolsDir))
            {
                Directory.CreateDirectory(symbolsDir);
            }

            // Create folders for each symbol with multiple training variations
            CreateSymbolFolder("circle", symbolsDir, CreateCircleVariations);
            CreateSymbolFolder("square", symbolsDir, CreateSquareVariations);
            CreateSymbolFolder("triangle", symbolsDir, CreateTriangleVariations);
            CreateSymbolFolder("star", symbolsDir, CreateStarVariations);
            CreateSymbolFolder("cross", symbolsDir, CreateCrossVariations);

            Console.WriteLine("Example symbol folders with training images created in symbols/ directory");
            Console.WriteLine("Each symbol folder contains multiple variations for better recognition");
        }

        private static void CreateSymbolFolder(string symbolName, string baseDir, Action<string> createVariations)
        {
            string symbolFolder = System.IO.Path.Combine(baseDir, symbolName);
            if (!Directory.Exists(symbolFolder))
            {
                Directory.CreateDirectory(symbolFolder);
            }
            createVariations(symbolFolder);
            Console.WriteLine($"Created {symbolName} folder with training images");
        }

        private static void CreateCircleTemplate(string path)
        {
            using (var image = new Image<Rgba32>(100, 100))
            {
                image.Mutate(ctx =>
                {
                    ctx.BackgroundColor(Color.Black);
                    var circle = new SixLabors.ImageSharp.Drawing.EllipsePolygon(new PointF(50, 50), 40);
                    ctx.Draw(Color.White, 2, circle);
                });
                image.Save(path);
            }
        }

        private static void CreateCircleVariations(string folderPath)
        {
            // Variation 1: Perfect circle
            using (var image = new Image<Rgba32>(100, 100))
            {
                image.Mutate(ctx =>
                {
                    ctx.BackgroundColor(Color.Black);
                    var circle = new SixLabors.ImageSharp.Drawing.EllipsePolygon(new PointF(50, 50), 40);
                    ctx.Draw(Color.White, 2, circle);
                });
                image.Save(System.IO.Path.Combine(folderPath, "circle_1.png"));
            }

            // Variation 2: Slightly smaller circle
            using (var image = new Image<Rgba32>(100, 100))
            {
                image.Mutate(ctx =>
                {
                    ctx.BackgroundColor(Color.Black);
                    var circle = new SixLabors.ImageSharp.Drawing.EllipsePolygon(new PointF(50, 50), 30);
                    ctx.Draw(Color.White, 2, circle);
                });
                image.Save(System.IO.Path.Combine(folderPath, "circle_2.png"));
            }

            // Variation 3: Slightly larger circle
            using (var image = new Image<Rgba32>(100, 100))
            {
                image.Mutate(ctx =>
                {
                    ctx.BackgroundColor(Color.Black);
                    var circle = new SixLabors.ImageSharp.Drawing.EllipsePolygon(new PointF(50, 50), 45);
                    ctx.Draw(Color.White, 2, circle);
                });
                image.Save(System.IO.Path.Combine(folderPath, "circle_3.png"));
            }

            // Variation 4: Off-center circle
            using (var image = new Image<Rgba32>(100, 100))
            {
                image.Mutate(ctx =>
                {
                    ctx.BackgroundColor(Color.Black);
                    var circle = new SixLabors.ImageSharp.Drawing.EllipsePolygon(new PointF(45, 55), 38);
                    ctx.Draw(Color.White, 2, circle);
                });
                image.Save(System.IO.Path.Combine(folderPath, "circle_4.png"));
            }
        }

        private static void CreateSquareTemplate(string path)
        {
            using (var image = new Image<Rgba32>(100, 100))
            {
                image.Mutate(ctx =>
                {
                    ctx.BackgroundColor(Color.Black);
                    var square = new RectangleF(15, 15, 70, 70);
                    ctx.Draw(Color.White, 2, square);
                });
                image.Save(path);
            }
        }

        private static void CreateSquareVariations(string folderPath)
        {
            // Variation 1: Perfect square
            using (var image = new Image<Rgba32>(100, 100))
            {
                image.Mutate(ctx =>
                {
                    ctx.BackgroundColor(Color.Black);
                    var square = new RectangleF(15, 15, 70, 70);
                    ctx.Draw(Color.White, 2, square);
                });
                image.Save(System.IO.Path.Combine(folderPath, "square_1.png"));
            }

            // Variation 2: Smaller square
            using (var image = new Image<Rgba32>(100, 100))
            {
                image.Mutate(ctx =>
                {
                    ctx.BackgroundColor(Color.Black);
                    var square = new RectangleF(25, 25, 50, 50);
                    ctx.Draw(Color.White, 2, square);
                });
                image.Save(System.IO.Path.Combine(folderPath, "square_2.png"));
            }

            // Variation 3: Larger square
            using (var image = new Image<Rgba32>(100, 100))
            {
                image.Mutate(ctx =>
                {
                    ctx.BackgroundColor(Color.Black);
                    var square = new RectangleF(10, 10, 80, 80);
                    ctx.Draw(Color.White, 2, square);
                });
                image.Save(System.IO.Path.Combine(folderPath, "square_3.png"));
            }

            // Variation 4: Rotated square (diamond)
            using (var image = new Image<Rgba32>(100, 100))
            {
                image.Mutate(ctx =>
                {
                    ctx.BackgroundColor(Color.Black);
                    var square = new Polygon(new LinearLineSegment(
                        new PointF(50, 10),
                        new PointF(90, 50),
                        new PointF(50, 90),
                        new PointF(10, 50)
                    ));
                    ctx.Draw(Color.White, 2, square);
                });
                image.Save(System.IO.Path.Combine(folderPath, "square_4.png"));
            }
        }

        private static void CreateTriangleTemplate(string path)
        {
            using (var image = new Image<Rgba32>(100, 100))
            {
                image.Mutate(ctx =>
                {
                    ctx.BackgroundColor(Color.Black);
                    var triangle = new Polygon(new LinearLineSegment(
                        new PointF(50, 10),
                        new PointF(90, 90),
                        new PointF(10, 90)
                    ));
                    ctx.Draw(Color.White, 2, triangle);
                });
                image.Save(path);
            }
        }

        private static void CreateTriangleVariations(string folderPath)
        {
            // Variation 1: Perfect triangle
            using (var image = new Image<Rgba32>(100, 100))
            {
                image.Mutate(ctx =>
                {
                    ctx.BackgroundColor(Color.Black);
                    var triangle = new Polygon(new LinearLineSegment(
                        new PointF(50, 10),
                        new PointF(90, 90),
                        new PointF(10, 90)
                    ));
                    ctx.Draw(Color.White, 2, triangle);
                });
                image.Save(System.IO.Path.Combine(folderPath, "triangle_1.png"));
            }

            // Variation 2: Smaller triangle
            using (var image = new Image<Rgba32>(100, 100))
            {
                image.Mutate(ctx =>
                {
                    ctx.BackgroundColor(Color.Black);
                    var triangle = new Polygon(new LinearLineSegment(
                        new PointF(50, 25),
                        new PointF(75, 75),
                        new PointF(25, 75)
                    ));
                    ctx.Draw(Color.White, 2, triangle);
                });
                image.Save(System.IO.Path.Combine(folderPath, "triangle_2.png"));
            }

            // Variation 3: Inverted triangle
            using (var image = new Image<Rgba32>(100, 100))
            {
                image.Mutate(ctx =>
                {
                    ctx.BackgroundColor(Color.Black);
                    var triangle = new Polygon(new LinearLineSegment(
                        new PointF(50, 90),
                        new PointF(90, 10),
                        new PointF(10, 10)
                    ));
                    ctx.Draw(Color.White, 2, triangle);
                });
                image.Save(System.IO.Path.Combine(folderPath, "triangle_3.png"));
            }

            // Variation 4: Slightly off-center triangle
            using (var image = new Image<Rgba32>(100, 100))
            {
                image.Mutate(ctx =>
                {
                    ctx.BackgroundColor(Color.Black);
                    var triangle = new Polygon(new LinearLineSegment(
                        new PointF(55, 15),
                        new PointF(85, 85),
                        new PointF(15, 85)
                    ));
                    ctx.Draw(Color.White, 2, triangle);
                });
                image.Save(System.IO.Path.Combine(folderPath, "triangle_4.png"));
            }
        }

        private static void CreateStarTemplate(string path)
        {
            using (var image = new Image<Rgba32>(100, 100))
            {
                image.Mutate(ctx =>
                {
                    ctx.BackgroundColor(Color.Black);
                    // Create a 5-pointed star
                    var points = new PointF[]
                    {
                        new PointF(50, 10),  // top
                        new PointF(61, 40),  // inner right top
                        new PointF(90, 40),  // outer right
                        new PointF(68, 60),  // inner right bottom
                        new PointF(79, 90),  // outer bottom right
                        new PointF(50, 73),  // inner bottom
                        new PointF(21, 90),  // outer bottom left
                        new PointF(32, 60),  // inner left bottom
                        new PointF(10, 40),  // outer left
                        new PointF(39, 40)   // inner left top
                    };
                    var star = new Polygon(new LinearLineSegment(points));
                    ctx.Draw(Color.White, 2, star);
                });
                image.Save(path);
            }
        }

        private static void CreateStarVariations(string folderPath)
        {
            // Variation 1: Standard 5-pointed star
            using (var image = new Image<Rgba32>(100, 100))
            {
                image.Mutate(ctx =>
                {
                    ctx.BackgroundColor(Color.Black);
                    var points = new PointF[]
                    {
                        new PointF(50, 10),  // top
                        new PointF(61, 40),  // inner right top
                        new PointF(90, 40),  // outer right
                        new PointF(68, 60),  // inner right bottom
                        new PointF(79, 90),  // outer bottom right
                        new PointF(50, 73),  // inner bottom
                        new PointF(21, 90),  // outer bottom left
                        new PointF(32, 60),  // inner left bottom
                        new PointF(10, 40),  // outer left
                        new PointF(39, 40)   // inner left top
                    };
                    var star = new Polygon(new LinearLineSegment(points));
                    ctx.Draw(Color.White, 2, star);
                });
                image.Save(System.IO.Path.Combine(folderPath, "star_1.png"));
            }

            // Variation 2: Smaller star
            using (var image = new Image<Rgba32>(100, 100))
            {
                image.Mutate(ctx =>
                {
                    ctx.BackgroundColor(Color.Black);
                    var points = new PointF[]
                    {
                        new PointF(50, 20),
                        new PointF(58, 43),
                        new PointF(80, 43),
                        new PointF(63, 58),
                        new PointF(70, 80),
                        new PointF(50, 68),
                        new PointF(30, 80),
                        new PointF(37, 58),
                        new PointF(20, 43),
                        new PointF(42, 43)
                    };
                    var star = new Polygon(new LinearLineSegment(points));
                    ctx.Draw(Color.White, 2, star);
                });
                image.Save(System.IO.Path.Combine(folderPath, "star_2.png"));
            }

            // Variation 3: Larger star
            using (var image = new Image<Rgba32>(100, 100))
            {
                image.Mutate(ctx =>
                {
                    ctx.BackgroundColor(Color.Black);
                    var points = new PointF[]
                    {
                        new PointF(50, 5),
                        new PointF(62, 38),
                        new PointF(95, 38),
                        new PointF(70, 62),
                        new PointF(82, 95),
                        new PointF(50, 77),
                        new PointF(18, 95),
                        new PointF(30, 62),
                        new PointF(5, 38),
                        new PointF(38, 38)
                    };
                    var star = new Polygon(new LinearLineSegment(points));
                    ctx.Draw(Color.White, 2, star);
                });
                image.Save(System.IO.Path.Combine(folderPath, "star_3.png"));
            }

            // Variation 4: Slightly rotated star
            using (var image = new Image<Rgba32>(100, 100))
            {
                image.Mutate(ctx =>
                {
                    ctx.BackgroundColor(Color.Black);
                    var points = new PointF[]
                    {
                        new PointF(55, 12),
                        new PointF(63, 42),
                        new PointF(88, 45),
                        new PointF(70, 63),
                        new PointF(75, 88),
                        new PointF(52, 75),
                        new PointF(25, 88),
                        new PointF(35, 63),
                        new PointF(12, 45),
                        new PointF(42, 42)
                    };
                    var star = new Polygon(new LinearLineSegment(points));
                    ctx.Draw(Color.White, 2, star);
                });
                image.Save(System.IO.Path.Combine(folderPath, "star_4.png"));
            }
        }

        private static void CreateCrossTemplate(string path)
        {
            using (var image = new Image<Rgba32>(100, 100))
            {
                image.Mutate(ctx =>
                {
                    ctx.BackgroundColor(Color.Black);
                    // Vertical bar
                    ctx.Draw(Color.White, 2, new RectangleF(40, 10, 20, 80));
                    // Horizontal bar
                    ctx.Draw(Color.White, 2, new RectangleF(10, 40, 80, 20));
                });
                image.Save(path);
            }
        }

        private static void CreateCrossVariations(string folderPath)
        {
            // Variation 1: Standard cross
            using (var image = new Image<Rgba32>(100, 100))
            {
                image.Mutate(ctx =>
                {
                    ctx.BackgroundColor(Color.Black);
                    // Vertical bar
                    ctx.Draw(Color.White, 2, new RectangleF(40, 10, 20, 80));
                    // Horizontal bar
                    ctx.Draw(Color.White, 2, new RectangleF(10, 40, 80, 20));
                });
                image.Save(System.IO.Path.Combine(folderPath, "cross_1.png"));
            }

            // Variation 2: Thicker cross
            using (var image = new Image<Rgba32>(100, 100))
            {
                image.Mutate(ctx =>
                {
                    ctx.BackgroundColor(Color.Black);
                    // Vertical bar
                    ctx.Draw(Color.White, 2, new RectangleF(35, 10, 30, 80));
                    // Horizontal bar
                    ctx.Draw(Color.White, 2, new RectangleF(10, 35, 80, 30));
                });
                image.Save(System.IO.Path.Combine(folderPath, "cross_2.png"));
            }

            // Variation 3: Thinner cross
            using (var image = new Image<Rgba32>(100, 100))
            {
                image.Mutate(ctx =>
                {
                    ctx.BackgroundColor(Color.Black);
                    // Vertical bar
                    ctx.Draw(Color.White, 2, new RectangleF(45, 15, 10, 70));
                    // Horizontal bar
                    ctx.Draw(Color.White, 2, new RectangleF(15, 45, 70, 10));
                });
                image.Save(System.IO.Path.Combine(folderPath, "cross_3.png"));
            }

            // Variation 4: Plus sign style
            using (var image = new Image<Rgba32>(100, 100))
            {
                image.Mutate(ctx =>
                {
                    ctx.BackgroundColor(Color.Black);
                    // Vertical bar
                    ctx.Draw(Color.White, 2, new RectangleF(42, 20, 16, 60));
                    // Horizontal bar
                    ctx.Draw(Color.White, 2, new RectangleF(20, 42, 60, 16));
                });
                image.Save(System.IO.Path.Combine(folderPath, "cross_4.png"));
            }
        }
    }
}
