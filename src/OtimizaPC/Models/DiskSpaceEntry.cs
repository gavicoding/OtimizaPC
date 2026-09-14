namespace OtimizaPC.Models;

public class DiskSpaceEntry
{
    public required string Nome { get; init; }
    public required string CaminhoCompleto { get; init; }
    public required bool EhPasta { get; init; }
    public long TamanhoBytes { get; set; }
    public double PercentualDoPai { get; set; }
}

public class DiscoInfo
{
    public required string Unidade { get; init; }
    public required string RotuloVolume { get; init; }
    public required long EspacoTotalBytes { get; init; }
    public required long EspacoLivreBytes { get; init; }
    public long EspacoUsadoBytes => EspacoTotalBytes - EspacoLivreBytes;
    public double PercentualUsado => EspacoTotalBytes == 0 ? 0 : (double)EspacoUsadoBytes / EspacoTotalBytes * 100;

    public string StatusSaude { get; set; } = "Desconhecido";
    public bool SaudeOk { get; set; } = true;
}
