using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OtimizaPC.Helpers;
using OtimizaPC.Models;
using OtimizaPC.Services;

namespace OtimizaPC.ViewModels;

public partial class DashboardViewModel : ObservableObject, IDisposable
{
    private readonly SystemMaintenanceService _servico = new();
    private readonly DispatcherTimer _timer;

    [ObservableProperty]
    private double usoCpuPercentual;

    [ObservableProperty]
    private double usoMemoriaPercentual;

    [ObservableProperty]
    private string memoriaTexto = "";

    [ObservableProperty]
    private bool elevado;

    public bool NaoElevado => !Elevado;

    partial void OnElevadoChanged(bool value) => OnPropertyChanged(nameof(NaoElevado));

    [ObservableProperty]
    private string statusPontoRestauracao = "";

    public ObservableCollection<DiscoInfo> Discos { get; } = new();

    public DashboardViewModel()
    {
        Elevado = ElevationHelper.EstaElevado();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += (_, _) => AtualizarMetricas();
        _timer.Start();
        AtualizarMetricas();
        _ = AtualizarDiscosAsync();
    }

    private void AtualizarMetricas()
    {
        var cpu = _servico.ObterUsoCpu();
        if (cpu >= 0)
            UsoCpuPercentual = Math.Round(cpu, 0);

        var memoria = _servico.ObterInfoMemoria();
        if (memoria.TotalBytes > 0)
        {
            UsoMemoriaPercentual = Math.Round(memoria.PercentualUsado, 0);
            MemoriaTexto = $"{FormatHelper.FormatarBytes(memoria.UsadoBytes)} de {FormatHelper.FormatarBytes(memoria.TotalBytes)}";
        }
    }

    private async Task AtualizarDiscosAsync()
    {
        var discos = await Task.Run(() => _servico.ObterInfoDiscos());
        Discos.Clear();
        foreach (var disco in discos)
            Discos.Add(disco);
    }

    [RelayCommand]
    private void ReiniciarComoAdministrador() => ElevationHelper.ReiniciarComoAdministrador();

    [RelayCommand]
    private async Task CriarPontoRestauracaoAsync()
    {
        StatusPontoRestauracao = "Criando ponto de restauração...";
        var sucesso = await _servico.CriarPontoRestauracaoAsync("OtimizaPC — antes de ajustes de desempenho");
        StatusPontoRestauracao = sucesso
            ? "Ponto de restauração criado com sucesso."
            : "Não foi possível criar (o Windows limita a 1 por dia, ou a Proteção do Sistema está desativada).";
    }

    [RelayCommand]
    private async Task OtimizarUnidadeAsync(DiscoInfo? disco)
    {
        if (disco == null) return;
        StatusPontoRestauracao = $"Otimizando {disco.Unidade}...";
        var resultado = await _servico.OtimizarUnidadeAsync(disco.Unidade.TrimEnd('\\'));
        StatusPontoRestauracao = resultado;
    }

    public void Dispose() => _timer.Stop();
}
