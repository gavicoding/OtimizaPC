using CommunityToolkit.Mvvm.ComponentModel;

namespace OtimizaPC.Models;

public enum StartupOrigem
{
    RegistroHKCU,
    RegistroHKLM,
    PastaInicializacaoUsuario,
    PastaInicializacaoComum
}

public partial class StartupItem : ObservableObject
{
    public required string Nome { get; init; }
    public required string Comando { get; init; }
    public required StartupOrigem Origem { get; init; }

    [ObservableProperty]
    private bool habilitado;
}

public partial class ServiceItem : ObservableObject
{
    public required string NomeServico { get; init; }
    public required string NomeExibicao { get; init; }
    public required string Descricao { get; init; }
    public required string RecomendacaoSeguranca { get; init; }

    [ObservableProperty]
    private bool emExecucao;

    [ObservableProperty]
    private bool inicializacaoAutomatica;
}

public class PowerPlanInfo
{
    public required Guid Guid { get; init; }
    public required string Nome { get; init; }
    public bool Ativo { get; set; }
}
