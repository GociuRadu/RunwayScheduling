namespace Modules.Aircrafts.Application.Generators;

public static class TailNumberGenerator
{
    public static string Generate(int current)
    {
        return string.Format("N{0:0000}", current);
    }
}
