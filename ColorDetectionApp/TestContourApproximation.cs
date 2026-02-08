using System;
using System.IO;
using OpenCvSharp;

namespace ColorDetectionApp
{
    /// <summary>
    /// Test class for validating contour approximation functionality
    /// </summary>
    public class TestContourApproximation
    {
        public static void RunTests()
        {
            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine("Contour Approximation Test Suite");
            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine();

            int passed = 0;
            int failed = 0;

            // Test 1: Basic approximation with different epsilon values
            Console.WriteLine("Test 1: Basic Contour Approximation");
            Console.WriteLine("-".PadRight(60, '-'));
            try
            {
                // Create a simple square contour
                Point[] squareContour = new Point[]
                {
                    new Point(10, 10),
                    new Point(11, 10),
                    new Point(12, 10),
                    new Point(90, 10),
                    new Point(91, 10),
                    new Point(90, 11),
                    new Point(90, 90),
                    new Point(90, 91),
                    new Point(89, 90),
                    new Point(10, 90),
                    new Point(9, 90),
                    new Point(10, 89),
                };

                var approx1 = ContourApproximation.ApproximateContour(squareContour, 0.01);
                var approx2 = ContourApproximation.ApproximateContour(squareContour, 0.05);

                Console.WriteLine($"Original contour points: {squareContour.Length}");
                Console.WriteLine($"Approximation with epsilon 0.01: {approx1.Length} points");
                Console.WriteLine($"Approximation with epsilon 0.05: {approx2.Length} points");

                if (approx1.Length >= approx2.Length && approx2.Length <= squareContour.Length)
                {
                    Console.WriteLine("✓ PASS: Higher epsilon produces fewer points");
                    passed++;
                }
                else
                {
                    Console.WriteLine("✗ FAIL: Epsilon behavior incorrect");
                    failed++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ FAIL: Exception - {ex.Message}");
                failed++;
            }
            Console.WriteLine();

            // Test 2: Multi-level analysis
            Console.WriteLine("Test 2: Multi-Level Analysis");
            Console.WriteLine("-".PadRight(60, '-'));
            try
            {
                // Create a circle-like contour
                Point[] circleContour = new Point[100];
                for (int i = 0; i < 100; i++)
                {
                    double angle = 2 * Math.PI * i / 100;
                    circleContour[i] = new Point(
                        (int)(50 + 40 * Math.Cos(angle)),
                        (int)(50 + 40 * Math.Sin(angle))
                    );
                }

                var results = ContourApproximation.AnalyzeAtMultipleLevels(circleContour);

                Console.WriteLine($"Analyzed contour with {circleContour.Length} points at {results.Count} levels");
                
                bool correctOrdering = true;
                for (int i = 1; i < results.Count; i++)
                {
                    if (results[i].ApproximatedPoints > results[i-1].ApproximatedPoints)
                    {
                        correctOrdering = false;
                        break;
                    }
                }

                foreach (var result in results)
                {
                    Console.WriteLine($"  Epsilon {result.EpsilonFactor:F3}: {result.ApproximatedPoints} points " +
                                    $"(compression: {result.CompressionRatio:F3})");
                }

                if (results.Count > 0 && correctOrdering)
                {
                    Console.WriteLine("✓ PASS: Multi-level analysis generates correct sequence");
                    passed++;
                }
                else
                {
                    Console.WriteLine("✗ FAIL: Multi-level analysis ordering incorrect");
                    failed++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ FAIL: Exception - {ex.Message}");
                failed++;
            }
            Console.WriteLine();

            // Test 3: Epsilon range validation
            Console.WriteLine("Test 3: Epsilon Range Validation");
            Console.WriteLine("-".PadRight(60, '-'));
            try
            {
                Point[] testContour = new Point[]
                {
                    new Point(0, 0), new Point(10, 0), new Point(20, 0),
                    new Point(20, 10), new Point(20, 20),
                    new Point(10, 20), new Point(0, 20), new Point(0, 10)
                };

                var veryLowEpsilon = ContourApproximation.ApproximateContour(testContour, 0.001);
                var veryHighEpsilon = ContourApproximation.ApproximateContour(testContour, 0.20);

                Console.WriteLine($"Very low epsilon (0.001): {veryLowEpsilon.Length} points");
                Console.WriteLine($"Very high epsilon (0.20): {veryHighEpsilon.Length} points");

                if (veryLowEpsilon.Length >= veryHighEpsilon.Length && veryHighEpsilon.Length >= 3)
                {
                    Console.WriteLine("✓ PASS: Extreme epsilon values handled correctly");
                    passed++;
                }
                else
                {
                    Console.WriteLine("✗ FAIL: Extreme epsilon values not handled correctly");
                    failed++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ FAIL: Exception - {ex.Message}");
                failed++;
            }
            Console.WriteLine();

            // Test 4: ContourApproximationResult structure
            Console.WriteLine("Test 4: Result Structure Validation");
            Console.WriteLine("-".PadRight(60, '-'));
            try
            {
                var result = new ContourApproximationResult
                {
                    EpsilonFactor = 0.04,
                    Epsilon = 10.5,
                    OriginalPoints = 100,
                    ApproximatedPoints = 8,
                    CompressionRatio = 0.08,
                    Contour = new Point[] { new Point(0, 0), new Point(10, 10) }
                };

                bool validStructure = 
                    result.EpsilonFactor == 0.04 &&
                    result.Epsilon == 10.5 &&
                    result.OriginalPoints == 100 &&
                    result.ApproximatedPoints == 8 &&
                    result.CompressionRatio == 0.08 &&
                    result.Contour.Length == 2;

                Console.WriteLine($"Result structure fields: {(validStructure ? "All valid" : "Invalid")}");

                if (validStructure)
                {
                    Console.WriteLine("✓ PASS: Result structure works correctly");
                    passed++;
                }
                else
                {
                    Console.WriteLine("✗ FAIL: Result structure has issues");
                    failed++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ FAIL: Exception - {ex.Message}");
                failed++;
            }
            Console.WriteLine();

            // Test 5: Integration with EnhancedShapeDetector
            Console.WriteLine("Test 5: EnhancedShapeDetector Integration");
            Console.WriteLine("-".PadRight(60, '-'));
            try
            {
                // This test validates that the enhanced shape detector accepts epsilon parameter
                // We can't fully test without images, but we can verify the API exists
                
                Console.WriteLine("Verifying API signature for DetectShape with epsilon...");
                var methodInfo = typeof(EnhancedShapeDetector).GetMethod("DetectShape");
                var parameters = methodInfo?.GetParameters();
                
                bool hasEpsilonParam = false;
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        if (param.Name == "epsilonFactor")
                        {
                            hasEpsilonParam = true;
                            Console.WriteLine($"  Found parameter: {param.Name} (type: {param.ParameterType})");
                        }
                    }
                }

                if (hasEpsilonParam)
                {
                    Console.WriteLine("✓ PASS: EnhancedShapeDetector supports epsilon parameter");
                    passed++;
                }
                else
                {
                    Console.WriteLine("✗ FAIL: EnhancedShapeDetector missing epsilon parameter");
                    failed++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ FAIL: Exception - {ex.Message}");
                failed++;
            }
            Console.WriteLine();

            // Summary
            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine("Test Summary");
            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine($"Passed: {passed}");
            Console.WriteLine($"Failed: {failed}");
            Console.WriteLine($"Total:  {passed + failed}");
            Console.WriteLine();

            if (failed == 0)
            {
                Console.WriteLine("✓ All tests passed!");
            }
            else
            {
                Console.WriteLine($"✗ {failed} test(s) failed");
            }
            Console.WriteLine();
        }
    }
}
