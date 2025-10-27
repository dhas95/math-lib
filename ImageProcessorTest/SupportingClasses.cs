using System;
using OpenCvSharp;

public interface ITransformableObject : IDisposable
{
    Mat GetMat();
}

public class TransformableObject : ITransformableObject
{
    private Mat _mat;
    private MatFormat _format;
    
    public TransformableObject(Mat mat, MatFormat format)
    {
        _mat = mat;
        _format = format;
    }
    
    public Mat GetMat() => _mat;
    
    public void Dispose()
    {
        _mat?.Dispose();
    }
}

public enum MatFormat
{
    TwoDimensional8BitUnsignedIntOneChannel
}