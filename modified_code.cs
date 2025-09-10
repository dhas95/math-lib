private static void KeepTwoLargestComponentsOverall(Mat image, bool showImages)
{
    var allComponents = new List<(int area, byte value, Mat mask)>();

    // Find all black components (value 0)
    var blackMask = new Mat();
    Cv2.InRange(image, new Scalar(0), new Scalar(0), blackMask);
    var blackComponents = GetComponentsInfo(blackMask, 0);
    allComponents.AddRange(blackComponents);

    // Find all white components (value 255)
    var whiteMask = new Mat();
    Cv2.InRange(image, new Scalar(255), new Scalar(255), whiteMask);
    var whiteComponents = GetComponentsInfo(whiteMask, 255);
    allComponents.AddRange(whiteComponents);

    // Find the largest black component
    var largestBlack = blackComponents
        .OrderByDescending(x => x.area)
        .FirstOrDefault();

    // Find the largest white component
    var largestWhite = whiteComponents
        .OrderByDescending(x => x.area)
        .FirstOrDefault();

    // Create list of components to keep
    var componentsToKeep = new List<(int area, byte value, Mat mask)>();
    if (largestBlack.mask != null)
        componentsToKeep.Add(largestBlack);
    if (largestWhite.mask != null)
        componentsToKeep.Add(largestWhite);

    if (showImages)
    {
        Console.WriteLine($"Found {blackComponents.Count} black components, {whiteComponents.Count} white components");
        Console.WriteLine($"Keeping largest black (area: {largestBlack.area}) and largest white (area: {largestWhite.area})");
    }

    // Create a mask for pixels that belong to the largest black and white components
    var keepMask = Mat.Zeros(image.Size(), MatType.CV_8UC1).ToMat();
    foreach (var component in componentsToKeep)
    {
        Cv2.BitwiseOr(keepMask, component.mask, keepMask);
    }

    // Invert all pixels that don't belong to the largest black and white components
    for (int y = 0; y < image.Rows; y++)
    {
        for (int x = 0; x < image.Cols; x++)
        {
            if (keepMask.At<byte>(y, x) == 0) // This pixel is not part of largest black/white components
            {
                byte currentValue = image.At<byte>(y, x);
                byte invertedValue = (byte)(255 - currentValue); // 0->255, 255->0
                image.Set<byte>(y, x, invertedValue);
            }
        }
    }

    // Clean up temporary masks
    blackMask?.Dispose();
    whiteMask?.Dispose();
    keepMask?.Dispose();
    
    // Clean up component masks
    foreach (var component in allComponents)
    {
        component.mask?.Dispose();
    }
}