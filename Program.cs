using OpenCvSharp;
using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("FFT Shift Optimization Demo");
        Console.WriteLine("===========================\n");

        try
        {
            // First validate that all methods produce correct results
            FftShiftBenchmark.ValidateCorrectness();
            
            Console.WriteLine("\nPress any key to run performance benchmarks...");
            Console.ReadKey();
            Console.WriteLine();
            
            // Run comprehensive benchmarks
            FftShiftBenchmark.RunComprehensiveBenchmark();
            
            Console.WriteLine("\n\nRecommendations:");
            Console.WriteLine("================");
            Console.WriteLine("1. For most cases: Use FftShiftOptimal() - best balance of performance and memory usage");
            Console.WriteLine("2. For very large images (>2048x2048): Use FftShiftMemoryMapped() to avoid memory pressure");
            Console.WriteLine("3. For power-of-2 dimensions: Use FftShiftPowerOfTwo() for slight additional optimization");
            Console.WriteLine("4. For wide images with good cache: Use FftShiftRowWise()");
            Console.WriteLine("5. For systems with limited memory: Use FftShiftBlockWise()");
            
            Console.WriteLine("\nKey optimizations implemented:");
            Console.WriteLine("- Reduced memory allocation from O(n²) to O(n/4) in optimal version");
            Console.WriteLine("- Eliminated unnecessary Mat object creations");
            Console.WriteLine("- Added cache-friendly memory access patterns");
            Console.WriteLine("- Provided specialized versions for different use cases");
            Console.WriteLine("- Used unsafe code for maximum performance where beneficial");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}