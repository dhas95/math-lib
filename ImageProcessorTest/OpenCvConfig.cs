using System;
using System.Runtime.InteropServices;
using OpenCvSharp;

public static class OpenCvConfig
{
    static OpenCvConfig()
    {
        // Set library path for Linux
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Try to load system OpenCV libraries
            try
            {
                Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", 
                    "/usr/lib/x86_64-linux-gnu:" + Environment.GetEnvironmentVariable("LD_LIBRARY_PATH"));
                
                // Force initialization
                var _ = new Mat();
                Console.WriteLine("OpenCV initialized successfully with system libraries");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize OpenCV: {ex.Message}");
                throw;
            }
        }
    }
    
    public static void Initialize()
    {
        // This method just ensures the static constructor runs
    }
}