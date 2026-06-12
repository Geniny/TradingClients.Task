namespace Producer.Service.Domain;

public sealed class PayloadGenerationOptions
{
    public string[] Symbols { get; set; } = ["BTCUSDT", "ETHUSDT", "SOLUSDT", "XRPUSDT"];
    public string[] FuturesSymbols { get; set; } = ["ESM4", "NQM4", "CLM4", "GCM4"];
    public string[] FuturesSources { get; set; } = ["NYSE", "CME", "ICE"];
}