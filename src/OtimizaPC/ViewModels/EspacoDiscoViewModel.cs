using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OtimizaPC.Helpers;
using OtimizaPC.Models;
using OtimizaPC.Services;

namespace OtimizaPC.ViewModels;

public partial class EspacoDiscoViewModel : ObservableObject
{
    private readonly DiskSpaceScanService _servico = new();
    private CancellationTokenSource? _cts;
    private Dictionary<string, List<DiskSpaceEntry>>? _arvore;
    private string? _raizEscaneada;

    public TarefaSegundoPlanoStatus StatusTarefa { get; } = new("💽");
    public ObservableCollection<DiskSpaceEntry> Itens { get; } = new();
    public ObservableCollection<string> Unidades { get; } = new();

    [ObservableProperty]
    private string caminhoAtual = "";

    [ObservableProperty]
    private bool escaneando;

    [ObservableProperty]
    private bool arvoreCarregada;

    [ObservableProperty]
    private string status = "Escolha uma unidade para escanear.";

    public string TamanhoTotalTexto => FormatHelper.FormatarBytes(Itens.Sum(i => i.TamanhoBytes));

    public EspacoDiscoViewModel()
    {
        foreach (var unidade in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed && d.IsReady))
            Unidades.Add(unidade.Name);
    }

    [RelayCommand]
    private async Task EscanearUnidadeAsync(string? unidade)
    {
        if (string.IsNullOrEmpty(unidade) || !Directory.Exists(unidade)) return;

        Escaneando = true;
        StatusTarefa.Ocupado = true;
        StatusTarefa.LimparConcluido();
        ArvoreCarregada = false;
        Itens.Clear();
        _cts = new CancellationTokenSource();
        var progresso = new Progress<string>(msg => Status = msg);

        try
        {
            _arvore = await _servico.EscanearArvoreCompletaAsync(unidade, progresso, _cts.Token);
            _raizEscaneada = unidade;
            ArvoreCarregada = true;
            AbrirPasta(unidade);
            Status = $"Unidade {unidade} analisada — navegação instantânea a partir daqui.";
            StatusTarefa.MarcarConcluido();
            NotificationService.Instancia.Notificar("Análise de espaço em disco concluída", Status);
        }
        catch (OperationCanceledException)
        {
            Status = "Escaneamento cancelado.";
        }
        finally
        {
            Escaneando = false;
            StatusTarefa.Ocupado = false;
        }
    }

    [RelayCommand]
    private void Cancelar() => _cts?.Cancel();

    [RelayCommand]
    private void AbrirPasta(string? caminho)
    {
        if (string.IsNullOrEmpty(caminho) || _arvore == null || !_arvore.TryGetValue(caminho, out var filhos))
            return;

        CaminhoAtual = caminho;
        Itens.Clear();
        foreach (var item in filhos)
            Itens.Add(item);

        Status = $"{filhos.Count} itens em {caminho}";
        OnPropertyChanged(nameof(TamanhoTotalTexto));
    }

    [RelayCommand]
    private void SubirNivel()
    {
        if (_raizEscaneada == null) return;
        if (string.Equals(CaminhoAtual.TrimEnd('\\'), _raizEscaneada.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
            return;

        var pai = Directory.GetParent(CaminhoAtual)?.FullName;
        if (pai != null)
            AbrirPasta(pai);
    }

    [RelayCommand]
    private void AbrirNoExplorer(DiskSpaceEntry? item)
    {
        if (item == null) return;
        try
        {
            Process.Start("explorer.exe", $"/select,\"{item.CaminhoCompleto}\"");
        }
        catch (System.ComponentModel.Win32Exception) { }
    }

    [RelayCommand]
    private void ExcluirItem(DiskSpaceEntry? item)
    {
        if (item == null) return;

        bool sucesso = item.EhPasta
            ? FileSystemHelper.TentarExcluirPasta(item.CaminhoCompleto)
            : FileSystemHelper.TentarExcluirArquivo(item.CaminhoCompleto);

        if (sucesso)
        {
            Itens.Remove(item);
            AtualizarArvoreAposExclusao(item.CaminhoCompleto, item.TamanhoBytes);
            Status = $"'{item.Nome}' excluído ({FormatHelper.FormatarBytes(item.TamanhoBytes)} liberados).";
            OnPropertyChanged(nameof(TamanhoTotalTexto));
        }
        else
        {
            Status = $"Não foi possível excluir '{item.Nome}' (sem permissão ou em uso).";
        }
    }

    private void AtualizarArvoreAposExclusao(string caminhoExcluido, long tamanhoLiberado)
    {
        if (_arvore == null) return;

        if (TentarObterPai(caminhoExcluido, out var paiDireto) && _arvore.TryGetValue(paiDireto, out var filhosDoPai))
        {
            var entrada = filhosDoPai.FirstOrDefault(e => string.Equals(e.CaminhoCompleto, caminhoExcluido, StringComparison.OrdinalIgnoreCase));
            if (entrada != null) filhosDoPai.Remove(entrada);
        }

        // Propaga a redução de tamanho pelos ancestrais, já que o total deles inclui este item.
        var atual = paiDireto;
        while (TentarObterPai(atual, out var avo) && _arvore.TryGetValue(avo, out var filhosDoAvo))
        {
            var entradaPai = filhosDoAvo.FirstOrDefault(e => string.Equals(e.CaminhoCompleto, atual, StringComparison.OrdinalIgnoreCase));
            if (entradaPai != null) entradaPai.TamanhoBytes = Math.Max(0, entradaPai.TamanhoBytes - tamanhoLiberado);
            atual = avo;
        }
    }

    private static bool TentarObterPai(string caminho, out string pai)
    {
        pai = Directory.GetParent(caminho)?.FullName ?? "";
        return !string.IsNullOrEmpty(pai);
    }
}
