namespace Washington.Services;

public sealed class BicepCompilationException : Exception
{
    public BicepCompilationException(string message)
        : base(message)
    {
    }
}
