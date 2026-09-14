using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OtimizaPC.Helpers;
using OtimizaPC.Models;
using OtimizaPC.Services;

namespace OtimizaPC.ViewModels;

public partial class LimpezaViewModel : ObservableObject
{
    private readonly CleanupService _servico = new();
    private CancellationTokenSource? _cts;

    public TarefaSegundoPlanoStatus StatusTarefa { get; } = new("🧹");
    public ObservableCollection<CleanupCategory> Categorias { get; } = new();

    [ObservableProperty]
    private bool escaneando;

    [ObservableProperty]
    private bool limpando;

    [ObservableProperty]
    private string statusGeral = "Clique em \"Escanear\" para ver o que pode ser limpo.";

    [ObservableProperty]
    private long tamanhoTotalSelecionado;

    public string TamanhoTotalSelecionadoTexto => FormatHelper.FormatarBytes(TamanhoTotalSelecionado);

    public LimpezaViewModel()
    {
        foreach (var definicao in CleanupDefinitions.ObterCategorias())
        {
            var categoria = new CleanupCategory { Definicao = definicao, Selecionado = definicao.SelecionadoPorPadrao };
            categoria.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(CleanupCategory.Selecionado) or nameof(CleanupCategory.TamanhoBytes))
                    RecalcularTotal();
            };
            Categorias.Add(categoria);
        }
    }

    private void RecalcularTotal()
    {
        TamanhoTotalSelecionado = Categorias.Where(c => c.Selecionado).Sum(c => c.TamanhoBytes);
        OnPropertyChanged(nameof(TamanhoTotalSelecionadoTexto));
    }

    [RelayCommand]
    private async Task EscanearTudoAsync()
    {
        Escaneando = true;
        StatusTarefa.Ocupado = true;
        StatusTarefa.LimparConcluido();
        _cts = new CancellationTokenSource();
        try
        {
            foreach (var categoria in Categorias)
            {
                StatusGeral = $"Escaneando: {categoria.Nome}...";
                categoria.Status = "Escaneando...";
                await _servico.EscanearCategoriaAsync(categoria, _cts.Token);
            }
            StatusGeral = "Escaneamento concluído.";
            StatusTarefa.MarcarConcluido();
            NotificationService.Instancia.Notificar("Limpeza — escaneamento concluído",
                $"{FormatHelper.FormatarBytes(Categorias.Sum(c => c.TamanhoBytes))} disponíveis para limpar.");
        }
        catch (OperationCanceledException)
        {
            StatusGeral = "Escaneamento cancelado.";
        }
        finally
        {
            Escaneando = false;
            StatusTarefa.Ocupado = false;
            RecalcularTotal();
        }
    }

    public List<CleanupCategory> ObterSelecionadosComItens() =>
        Categorias.Where(c => c.Selecionado && c.QuantidadeArquivos > 0).ToList();

    public async Task<CleanupResult> LimparSelecionadosAsync()
    {
        Limpando = true;
        StatusTarefa.Ocupado = true;
        StatusTarefa.LimparConcluido();
        var progresso = new Progress<string>(msg => StatusGeral = msg);
        try
        {
            var resultado = await _servico.LimparAsync(ObterSelecionadosComItens(), progresso);
            foreach (var categoria in Categorias.Where(c => c.Selecionado))
            {
                categoria.TamanhoBytes = 0;
                categoria.QuantidadeArquivos = 0;
                categoria.Status = "Limpo";
                categoria.ArquivosEncontrados.Clear();
            }
            RecalcularTotal();
            StatusGeral = $"Concluído: {FormatHelper.FormatarBytes(resultado.BytesLiberados)} liberados, {resultado.ArquivosExcluidos} arquivos removidos.";
            StatusTarefa.MarcarConcluido();
            NotificationService.Instancia.Notificar("Limpeza concluída",
                $"{FormatHelper.FormatarBytes(resultado.BytesLiberados)} liberados, {resultado.ArquivosExcluidos} arquivos removidos.");
            return resultado;
        }
        finally
        {
            Limpando = false;
            StatusTarefa.Ocupado = false;
        }
    }
}
