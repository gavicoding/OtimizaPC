using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OtimizaPC.Helpers;
using OtimizaPC.Models;
using OtimizaPC.Services;

namespace OtimizaPC.ViewModels;

public partial class DesempenhoViewModel : ObservableObject
{
    private readonly StartupService _startupServico = new();
    private readonly ServiceManagementService _servicosServico = new();
    private readonly PowerPlanService _energiaServico = new();
    private readonly VisualEffectsService _visualServico = new();
    private readonly AdvancedTweaksService _avancadoServico = new();
    private bool _carregando;

    public ObservableCollection<StartupItem> ItensInicializacao { get; } = new();
    public ObservableCollection<ServiceItem> Servicos { get; } = new();
    public ObservableCollection<PowerPlanInfo> PlanosEnergia { get; } = new();

    [ObservableProperty]
    private string status = "Carregando...";

    [ObservableProperty]
    private bool sincronizando;

    [ObservableProperty]
    private bool modoMelhorDesempenho;

    [ObservableProperty]
    private PowerPlanInfo? planoSelecionado;

    [ObservableProperty]
    private bool priorizarProgramas;

    [ObservableProperty]
    private bool agendamentoGpu;

    [ObservableProperty]
    private bool hibernacaoLigada;

    public DesempenhoViewModel()
    {
        _ = CarregarTudoAsync();
    }

    /// <summary>
    /// Consultar serviços do Windows (ServiceController) e rodar o powercfg é lento
    /// (pode passar de 1-2s) — isso tudo roda fora da thread de UI pra não travar a
    /// janela na abertura do programa nem ao clicar em "Recarregar".
    /// </summary>
    private async Task CarregarTudoAsync()
    {
        Sincronizando = true;
        Status = "Carregando informações de desempenho...";
        try
        {
            var dados = await Task.Run(() => new
            {
                Inicializacao = _startupServico.ObterItens(),
                Servicos = _servicosServico.ObterServicos(),
                Planos = _energiaServico.ObterPlanos(),
                ModoDesempenho = _visualServico.EstaEmModoDesempenho(),
                Priorizar = _avancadoServico.PriorizaProgramasEmPrimeiroPlano(),
                Gpu = _avancadoServico.AgendamentoGpuAtivo(),
                Hibernacao = _avancadoServico.HibernacaoAtiva()
            });

            _carregando = true;
            try
            {
                ItensInicializacao.Clear();
                foreach (var item in dados.Inicializacao)
                    ItensInicializacao.Add(item);

                Servicos.Clear();
                foreach (var item in dados.Servicos)
                    Servicos.Add(item);

                PlanosEnergia.Clear();
                foreach (var plano in dados.Planos)
                    PlanosEnergia.Add(plano);
                PlanoSelecionado = PlanosEnergia.FirstOrDefault(p => p.Ativo);

                ModoMelhorDesempenho = dados.ModoDesempenho;
                PriorizarProgramas = dados.Priorizar;
                AgendamentoGpu = dados.Gpu;
                HibernacaoLigada = dados.Hibernacao;
            }
            finally
            {
                _carregando = false;
            }

            Status = "";
        }
        finally
        {
            Sincronizando = false;
        }
    }

    [RelayCommand]
    private async Task RecarregarAsync() => await CarregarTudoAsync();

    [RelayCommand]
    private void AlternarInicializacao(StartupItem? item)
    {
        if (item == null) return;
        try
        {
            _startupServico.DefinirHabilitado(item, !item.Habilitado);
            Status = item.Habilitado ? $"'{item.Nome}' habilitado na inicialização." : $"'{item.Nome}' removido da inicialização.";
        }
        catch (InvalidOperationException ex)
        {
            Status = ex.Message;
        }
    }

    [RelayCommand]
    private void AlternarServico(ServiceItem? item)
    {
        if (item == null) return;
        try
        {
            _servicosServico.DefinirInicializacaoAutomatica(item, !item.InicializacaoAutomatica);
            Status = item.InicializacaoAutomatica
                ? $"'{item.NomeExibicao}' definido como automático."
                : $"'{item.NomeExibicao}' desativado.";
        }
        catch (InvalidOperationException ex)
        {
            Status = ex.Message;
        }
    }

    partial void OnPlanoSelecionadoChanged(PowerPlanInfo? value)
    {
        if (value == null || _carregando) return;
        _energiaServico.AtivarPlano(value.Guid);
        foreach (var plano in PlanosEnergia)
            plano.Ativo = plano.Guid == value.Guid;
        Status = $"Plano de energia alterado para '{value.Nome}'.";
    }

    partial void OnModoMelhorDesempenhoChanged(bool value)
    {
        if (_carregando) return;
        _visualServico.DefinirModoDesempenho(value);
        Status = value
            ? "Efeitos visuais ajustados para melhor desempenho."
            : "Efeitos visuais restaurados para melhor aparência.";
    }

    partial void OnPriorizarProgramasChanged(bool value)
    {
        if (_carregando) return;
        try
        {
            _avancadoServico.DefinirPriorizarProgramas(value);
            Status = value
                ? "Priorizando o programa em uso — pode ajudar a responsividade em primeiro plano."
                : "Voltou a dividir o processador igualmente entre programas e serviços em segundo plano.";
        }
        catch (InvalidOperationException ex)
        {
            Status = ex.Message;
            PriorizarProgramas = !value;
        }
    }

    partial void OnAgendamentoGpuChanged(bool value)
    {
        if (_carregando) return;
        try
        {
            _avancadoServico.DefinirAgendamentoGpu(value);
            Status = value
                ? "Agendamento de GPU por hardware ativado — requer reiniciar o Windows para valer, e só faz efeito se a placa de vídeo suportar."
                : "Agendamento de GPU por hardware desativado — requer reiniciar o Windows para valer.";
        }
        catch (InvalidOperationException ex)
        {
            Status = ex.Message;
            AgendamentoGpu = !value;
        }
    }

    partial void OnHibernacaoLigadaChanged(bool value)
    {
        if (_carregando) return;
        _ = AlterarHibernacaoAsync(value);
    }

    private async Task AlterarHibernacaoAsync(bool ativar)
    {
        Status = ativar ? "Ativando hibernação..." : "Desativando hibernação...";
        var resultado = await _avancadoServico.DefinirHibernacaoAsync(ativar);
        Status = resultado;
        if (resultado.Contains("permissão", StringComparison.OrdinalIgnoreCase))
        {
            _carregando = true;
            HibernacaoLigada = !ativar;
            _carregando = false;
        }
    }
}
