namespace OtimizaPC.Helpers;

/// <summary>
/// Encapsula um IProgress&lt;string&gt; limitando a taxa de atualizações (por tempo, não por
/// contagem de itens). Sem isso, varreduras de "Todo o PC" com centenas de milhares de
/// arquivos podem gerar milhares de callbacks pra thread de UI em sequência rápida demais
/// pra ela processar, deixando a janela travada/lenta até a varredura terminar.
/// </summary>
public class ProgressoComLimite
{
    private readonly IProgress<string>? _interno;
    private readonly long _intervaloMinimoMs;
    private long _ultimoTicks = long.MinValue;

    public ProgressoComLimite(IProgress<string>? interno, int intervaloMinimoMs = 150)
    {
        _interno = interno;
        _intervaloMinimoMs = intervaloMinimoMs;
    }

    public void Report(string mensagem)
    {
        if (_interno == null) return;

        var agora = Environment.TickCount64;
        if (agora - _ultimoTicks < _intervaloMinimoMs) return;

        _ultimoTicks = agora;
        _interno.Report(mensagem);
    }
}
