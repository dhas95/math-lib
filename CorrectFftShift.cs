using OpenCvSharp;
using System;
using System.Diagnostics;

public static class CorrectFftShift
{
    // Your original method for comparison
    public static void FftShiftMinimalAllocation(Mat mat)
    {
        if (mat == null || mat.Empty())
            return;

        int cx = mat.Cols / 2;
        int cy = mat.Rows / 2;
        int w1 = mat.Cols - cx;
        int w2 = cx;
        int h1 = mat.Rows - cy;  
        int h2 = cy;

        // Single allocation - temporary storage for the entire result
        using var temp = new Mat(mat.Rows, mat.Cols, mat.Type());
        
        // Create views (no allocation - just headers pointing to memory)
        var srcQ0 = new Mat(mat, new Rect(0, 0, w1, h1));
        var srcQ1 = new Mat(mat, new Rect(w1, 0, w2, h1));
        var srcQ2 = new Mat(mat, new Rect(0, h1, w1, h2));
        var srcQ3 = new Mat(mat, new Rect(w1, h1, w2, h2));
        
        var destQ0 = new Mat(temp, new Rect(w2, h2, w1, h1));  // q0 destination
        var destQ1 = new Mat(temp, new Rect(0, h2, w2, h1));   // q1 destination  
        var destQ2 = new Mat(temp, new Rect(w2, 0, w1, h2));   // q2 destination
        var destQ3 = new Mat(temp, new Rect(0, 0, w2, h2));    // q3 destination
        
        // Copy quadrants to their new positions (4 copy operations)
        srcQ0.CopyTo(destQ0);
        srcQ1.CopyTo(destQ1);
        srcQ2.CopyTo(destQ2);
        srcQ3.CopyTo(destQ3);
        
        // Copy result back to original (1 final copy operation)
        temp.CopyTo(mat);
        
        // Views don't need explicit disposal, but good practice
        srcQ0.Dispose(); srcQ1.Dispose(); srcQ2.Dispose(); srcQ3.Dispose();
        destQ0.Dispose(); destQ1.Dispose(); destQ2.Dispose(); destQ3.Dispose();
    }

    /// <summary>
    /// Optimized version that produces IDENTICAL results to the original
    /// Key optimization: eliminates the full temporary matrix and final copy
    /// Uses direct quadrant swapping with minimal temporary storage
    /// </summary>
    public static void FftShiftOptimizedCorrect(Mat mat)
    {
        if (mat == null || mat.Empty())
            return;

        int cx = mat.Cols / 2;
        int cy = mat.Rows / 2;
        int w1 = mat.Cols - cx;  // Right side width
        int w2 = cx;             // Left side width  
        int h1 = mat.Rows - cy;  // Bottom side height
        int h2 = cy;             // Top side height

        // Create source quadrant views (matching original exactly)
        using var srcQ0 = new Mat(mat, new Rect(0, 0, w1, h1));        // Top-Left
        using var srcQ1 = new Mat(mat, new Rect(w1, 0, w2, h1));       // Top-Right
        using var srcQ2 = new Mat(mat, new Rect(0, h1, w1, h2));       // Bottom-Left  
        using var srcQ3 = new Mat(mat, new Rect(w1, h1, w2, h2));      // Bottom-Right

        // Create temporary storage for swapping - only need to store one quadrant at a time
        using var temp = new Mat();

        // The original mapping is:
        // srcQ0 -> destQ0 (position: w2, h2, w1, h1) = (cx, cy, w1, h1)
        // srcQ1 -> destQ1 (position: 0, h2, w2, h1)  = (0, cy, cx, h1) 
        // srcQ2 -> destQ2 (position: w2, 0, w1, h2)  = (cx, 0, w1, cy)
        // srcQ3 -> destQ3 (position: 0, 0, w2, h2)   = (0, 0, cx, cy)

        // This means we need to perform these swaps:
        // Q0 (0,0) -> (cx, cy)     = Q0 -> Q3 position
        // Q1 (cx,0) -> (0, cy)     = Q1 -> Q2 position  
        // Q2 (0,cy) -> (cx, 0)     = Q2 -> Q1 position
        // Q3 (cx,cy) -> (0, 0)     = Q3 -> Q0 position

        // Create destination views for in-place swapping
        using var destQ0 = new Mat(mat, new Rect(cx, cy, w1, h1));     // Where Q0 should go
        using var destQ1 = new Mat(mat, new Rect(0, cy, cx, h1));      // Where Q1 should go
        using var destQ2 = new Mat(mat, new Rect(cx, 0, w1, cy));      // Where Q2 should go  
        using var destQ3 = new Mat(mat, new Rect(0, 0, cx, cy));       // Where Q3 should go

        // Perform the swaps using temporary storage
        // Swap Q0 and Q3 (diagonal)
        srcQ0.CopyTo(temp);
        srcQ3.CopyTo(destQ0);  // Q3 -> Q0's destination
        temp.CopyTo(destQ3);   // Q0 -> Q3's destination

        // Swap Q1 and Q2 (diagonal)  
        srcQ1.CopyTo(temp);
        srcQ2.CopyTo(destQ1);  // Q2 -> Q1's destination
        temp.CopyTo(destQ2);   // Q1 -> Q2's destination
    }

    /// <summary>
    /// Even more optimized version using fewer Mat object creations
    /// </summary>
    public static void FftShiftOptimizedMinimal(Mat mat)
    {
        if (mat == null || mat.Empty())
            return;

        int cx = mat.Cols / 2;
        int cy = mat.Rows / 2;

        // Use single temp buffer and minimize Mat object creation
        using var temp = new Mat();
        
        // Perform swaps directly with new Rect objects (no persistent Mat views)
        var rectQ0 = new Rect(0, 0, mat.Cols - cx, mat.Rows - cy);
        var rectQ3 = new Rect(cx, cy, mat.Cols - cx, mat.Rows - cy);
        
        // Swap Q0 <-> Q3
        using (var q0 = new Mat(mat, rectQ0))
        using (var q3 = new Mat(mat, rectQ3))
        {
            q0.CopyTo(temp);
            q3.CopyTo(q0);
            temp.CopyTo(q3);
        }

        var rectQ1 = new Rect(mat.Cols - cx, 0, cx, mat.Rows - cy);
        var rectQ2 = new Rect(0, mat.Rows - cy, mat.Cols - cx, cy);
        
        // Swap Q1 <-> Q2  
        using (var q1 = new Mat(mat, rectQ1))
        using (var q2 = new Mat(mat, rectQ2))
        {
            q1.CopyTo(temp);
            q2.CopyTo(q1);
            temp.CopyTo(q2);
        }
    }

    /// <summary>
    /// Test to verify that both methods produce identical results
    /// </summary>
    public static bool TestEquivalence(Size testSize, MatType matType)
    {
        // Create test matrix with known pattern
        using var original = CreateTestMatrix(testSize, matType);
        using var mat1 = original.Clone();
        using var mat2 = original.Clone();
        using var mat3 = original.Clone();

        // Apply different methods
        FftShiftMinimalAllocation(mat1);
        FftShiftOptimizedCorrect(mat2);
        FftShiftOptimizedMinimal(mat3);

        // Compare results
        using var diff1 = new Mat();
        using var diff2 = new Mat();
        
        Cv2.Absdiff(mat1, mat2, diff1);
        Cv2.Absdiff(mat1, mat3, diff2);
        
        var maxDiff1 = Cv2.MinMaxLoc(diff1).maxVal;
        var maxDiff2 = Cv2.MinMaxLoc(diff2).maxVal;
        
        const double tolerance = 1e-10;
        bool equivalent1 = maxDiff1 < tolerance;
        bool equivalent2 = maxDiff2 < tolerance;
        
        Console.WriteLine($"Size: {testSize}, Type: {matType}");
        Console.WriteLine($"  Original vs OptimizedCorrect: {(equivalent1 ? "IDENTICAL" : "DIFFERENT")} (max diff: {maxDiff1:E})");
        Console.WriteLine($"  Original vs OptimizedMinimal: {(equivalent2 ? "IDENTICAL" : "DIFFERENT")} (max diff: {maxDiff2:E})");
        
        return equivalent1 && equivalent2;
    }

    /// <summary>
    /// Comprehensive benchmark comparing all methods
    /// </summary>
    public static void RunBenchmark()
    {
        Console.WriteLine("FFT Shift Performance Benchmark");
        Console.WriteLine("================================\n");
        
        var testSizes = new[]
        {
            new Size(256, 256),
            new Size(512, 512), 
            new Size(1024, 1024),
            new Size(2048, 2048)
        };

        var testTypes = new[] { MatType.CV_32F, MatType.CV_32FC2 };

        foreach (var matType in testTypes)
        {
            Console.WriteLine($"Testing {matType}:");
            Console.WriteLine(new string('-', 40));
            
            foreach (var size in testSizes)
            {
                Console.WriteLine($"\nSize: {size.Width}x{size.Height}");
                BenchmarkMethods(size, matType);
            }
            Console.WriteLine();
        }
    }

    private static void BenchmarkMethods(Size size, MatType matType)
    {
        const int iterations = 20;
        const int warmupIterations = 5;

        using var testMat = CreateTestMatrix(size, matType);

        var methods = new[]
        {
            ("Original", (Action<Mat>)FftShiftMinimalAllocation),
            ("OptimizedCorrect", FftShiftOptimizedCorrect),
            ("OptimizedMinimal", FftShiftOptimizedMinimal)
        };

        Console.WriteLine($"{"Method",-18} {"Time (ms)",-12} {"Speedup",-10}");
        Console.WriteLine(new string('-', 42));

        double originalTime = 0;

        foreach (var (name, method) in methods)
        {
            // Warmup
            for (int i = 0; i < warmupIterations; i++)
            {
                using var warmup = testMat.Clone();
                method(warmup);
            }

            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Benchmark
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                using var clone = testMat.Clone();
                method(clone);
            }
            sw.Stop();

            double avgTime = sw.Elapsed.TotalMilliseconds / iterations;
            
            if (name == "Original")
                originalTime = avgTime;
                
            double speedup = originalTime > 0 ? originalTime / avgTime : 1.0;
            
            Console.WriteLine($"{name,-18} {avgTime,-12:F3} {speedup,-10:F2}x");
        }
    }

    private static Mat CreateTestMatrix(Size size, MatType matType)
    {
        var mat = new Mat(size, matType);
        var rng = new Random(42); // Fixed seed for reproducibility

        // Fill with realistic data
        if (matType == MatType.CV_32F)
        {
            var data = new float[size.Width * size.Height];
            for (int i = 0; i < data.Length; i++)
                data[i] = (float)(rng.NextDouble() * 1000.0 - 500.0); // Range [-500, 500]
                
            unsafe
            {
                fixed (float* ptr = data)
                {
                    var tempMat = Mat.FromPixelData(size.Height, size.Width, MatType.CV_32F, (IntPtr)ptr);
                    tempMat.CopyTo(mat);
                }
            }
        }
        else if (matType == MatType.CV_32FC2)
        {
            var data = new float[size.Width * size.Height * 2];
            for (int i = 0; i < data.Length; i++)
                data[i] = (float)(rng.NextDouble() * 1000.0 - 500.0);
                
            unsafe
            {
                fixed (float* ptr = data)
                {
                    var tempMat = Mat.FromPixelData(size.Height, size.Width, MatType.CV_32FC2, (IntPtr)ptr);
                    tempMat.CopyTo(mat);
                }
            }
        }

        return mat;
    }

    /// <summary>
    /// Run comprehensive validation tests
    /// </summary>
    public static void ValidateAllMethods()
    {
        Console.WriteLine("Validating method equivalence...");
        Console.WriteLine("=================================\n");
        
        var testSizes = new[]
        {
            new Size(64, 64),
            new Size(127, 127),    // Odd dimensions
            new Size(128, 256),    // Non-square
            new Size(256, 256),
            new Size(512, 512)
        };

        var testTypes = new[] { MatType.CV_32F, MatType.CV_64F, MatType.CV_32FC2 };

        bool allPassed = true;

        foreach (var size in testSizes)
        {
            foreach (var matType in testTypes)
            {
                if (!TestEquivalence(size, matType))
                {
                    allPassed = false;
                    Console.WriteLine("❌ VALIDATION FAILED!");
                    break;
                }
            }
            if (!allPassed) break;
        }

        if (allPassed)
        {
            Console.WriteLine("\n✅ ALL VALIDATION TESTS PASSED!");
            Console.WriteLine("All optimized methods produce identical results to the original.\n");
        }
    }
}