using OpenCvSharp;
using System;
using System.Diagnostics;
using System.Runtime.GC;

public static class FftShiftBenchmark
{
    public static void RunComprehensiveBenchmark()
    {
        Console.WriteLine("FFT Shift Performance Comparison");
        Console.WriteLine("================================");
        
        // Test different image sizes
        var testSizes = new[]
        {
            new Size(256, 256),    // Small
            new Size(512, 512),    // Medium  
            new Size(1024, 1024),  // Large
            new Size(2048, 2048),  // Very Large
            new Size(4096, 4096)   // Huge
        };

        var testTypes = new[]
        {
            MatType.CV_32F,  // Single precision float
            MatType.CV_64F,  // Double precision float
            MatType.CV_32FC2 // Complex float (common for FFT)
        };

        foreach (var matType in testTypes)
        {
            Console.WriteLine($"\nTesting with {matType}:");
            Console.WriteLine(new string('-', 50));
            
            foreach (var size in testSizes)
            {
                Console.WriteLine($"\nImage Size: {size.Width}x{size.Height}");
                BenchmarkAllMethods(size, matType);
            }
        }
    }

    private static void BenchmarkAllMethods(Size size, MatType matType)
    {
        const int iterations = 10;
        
        // Create test data
        using var originalMat = CreateTestMatrix(size, matType);
        
        var methods = new[]
        {
            ("Original", (Action<Mat>)FftShiftMinimalAllocation),
            ("Optimal", OptimizedFftShift.FftShiftOptimal),
            ("RowWise", OptimizedFftShift.FftShiftRowWise),
            ("BlockWise", OptimizedFftShift.FftShiftBlockWise),
            ("PowerOfTwo", OptimizedFftShift.FftShiftPowerOfTwo),
            ("MemoryMapped", OptimizedFftShift.FftShiftMemoryMapped)
        };

        Console.WriteLine($"{"Method",-15} {"Time (ms)",-12} {"Memory (MB)",-12} {"Speedup",-10}");
        Console.WriteLine(new string('-', 55));

        double originalTime = 0;

        foreach (var (name, method) in methods)
        {
            try
            {
                var (avgTime, peakMemory) = BenchmarkMethod(originalMat, method, iterations);
                
                if (name == "Original")
                    originalTime = avgTime;
                
                double speedup = originalTime > 0 ? originalTime / avgTime : 1.0;
                
                Console.WriteLine($"{name,-15} {avgTime,-12:F2} {peakMemory,-12:F2} {speedup,-10:F2}x");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{name,-15} ERROR: {ex.Message}");
            }
        }
    }

    private static (double avgTime, double peakMemoryMB) BenchmarkMethod(Mat originalMat, Action<Mat> method, int iterations)
    {
        var times = new double[iterations];
        long initialMemory = System.GC.GetTotalMemory(true);
        long peakMemory = initialMemory;

        // Warmup
        using (var warmupMat = originalMat.Clone())
        {
            method(warmupMat);
        }

        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
        System.GC.Collect();

        var sw = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            using var testMat = originalMat.Clone();
            
            long memBefore = System.GC.GetTotalMemory(false);
            var iterSw = Stopwatch.StartNew();
            
            method(testMat);
            
            iterSw.Stop();
            long memAfter = System.GC.GetTotalMemory(false);
            
            times[i] = iterSw.Elapsed.TotalMilliseconds;
            peakMemory = Math.Max(peakMemory, memAfter);
        }

        sw.Stop();

        // Calculate average time
        double sum = 0;
        foreach (var time in times)
            sum += time;
        
        double avgTime = sum / iterations;
        double peakMemoryMB = (peakMemory - initialMemory) / (1024.0 * 1024.0);

        return (avgTime, Math.Max(0, peakMemoryMB));
    }

    private static Mat CreateTestMatrix(Size size, MatType matType)
    {
        var mat = new Mat(size, matType);
        
        // Fill with realistic FFT-like data (frequency domain pattern)
        Random rand = new Random(42); // Fixed seed for reproducibility
        
        unsafe
        {
            if (matType == MatType.CV_32F)
            {
                var data = new float[size.Width * size.Height];
                for (int i = 0; i < data.Length; i++)
                    data[i] = (float)(rand.NextDouble() * 255.0);
                
                fixed (float* ptr = data)
                {
                    Mat.FromPixelData(size.Height, size.Width, MatType.CV_32F, (IntPtr)ptr).CopyTo(mat);
                }
            }
            else if (matType == MatType.CV_64F)
            {
                var data = new double[size.Width * size.Height];
                for (int i = 0; i < data.Length; i++)
                    data[i] = rand.NextDouble() * 255.0;
                
                fixed (double* ptr = data)
                {
                    Mat.FromPixelData(size.Height, size.Width, MatType.CV_64F, (IntPtr)ptr).CopyTo(mat);
                }
            }
            else if (matType == MatType.CV_32FC2)
            {
                var data = new float[size.Width * size.Height * 2];
                for (int i = 0; i < data.Length; i++)
                    data[i] = (float)(rand.NextDouble() * 255.0);
                
                fixed (float* ptr = data)
                {
                    Mat.FromPixelData(size.Height, size.Width, MatType.CV_32FC2, (IntPtr)ptr).CopyTo(mat);
                }
            }
        }
        
        return mat;
    }

    // Original method for comparison
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

    public static void ValidateCorrectness()
    {
        Console.WriteLine("\nValidating correctness of all methods...");
        Console.WriteLine("========================================");

        var testSize = new Size(128, 128);
        using var referenceMat = CreateTestMatrix(testSize, MatType.CV_32F);
        using var originalResult = referenceMat.Clone();
        
        // Get reference result
        FftShiftMinimalAllocation(originalResult);

        var methods = new[]
        {
            ("Optimal", OptimizedFftShift.FftShiftOptimal),
            ("RowWise", OptimizedFftShift.FftShiftRowWise),
            ("BlockWise", OptimizedFftShift.FftShiftBlockWise),
            ("PowerOfTwo", OptimizedFftShift.FftShiftPowerOfTwo),
            ("MemoryMapped", OptimizedFftShift.FftShiftMemoryMapped)
        };

        bool allPassed = true;

        foreach (var (name, method) in methods)
        {
            try
            {
                using var testMat = referenceMat.Clone();
                method(testMat);
                
                // Compare with reference
                using var diff = new Mat();
                Cv2.Absdiff(originalResult, testMat, diff);
                var maxDiff = Cv2.MinMaxLoc(diff).maxVal;
                
                bool passed = maxDiff < 1e-6; // Tolerance for floating point
                Console.WriteLine($"{name,-15}: {(passed ? "PASS" : "FAIL")} (max diff: {maxDiff:E2})");
                
                if (!passed)
                    allPassed = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{name,-15}: ERROR - {ex.Message}");
                allPassed = false;
            }
        }

        Console.WriteLine($"\nOverall validation: {(allPassed ? "PASS" : "FAIL")}");
    }
}