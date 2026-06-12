namespace Producer.Service.Application.Ticks;

public static class RandomExtensions
{
    public static string GetRandomString(this Random random, string[] source)
    {
        return source.ElementAt(random.Next(minValue: 0, source.Length));
    }

    public static decimal GetRandomDecimal(this Random random, int decimals)
    {
        return decimal.Round((decimal)random.NextDouble() * 10m + 1m, decimals);
    }
}