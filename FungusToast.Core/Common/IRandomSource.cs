namespace FungusToast.Core.Common;

public interface IRandomSource
{
    double NextDouble();
    int Next(int maxValue);
}
