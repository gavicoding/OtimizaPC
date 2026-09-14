using System.ServiceProcess;
using Microsoft.Win32;
using OtimizaPC.Models;

namespace OtimizaPC.Services;

/// <summary>
/// Expõe apenas uma lista curada de serviços conhecidos como seguros de desativar
/// para a maioria dos usuários domésticos — nunca a lista completa de serviços do
/// Windows, para evitar que alguém desative algo crítico sem saber.
/// </summary>
public class ServiceManagementService
{
    private static readonly (string Nome, string Descricao, string Recomendacao)[] ServicosCurados =
    {
        ("DiagTrack", "Serviço de Experiência do Usuário Conectada e Telemetria",
            "Coleta dados de diagnóstico para a Microsoft. Seguro desativar se você não usa recursos de diagnóstico corporativo."),
        ("SysMain", "SysMain (Superfetch)",
            "Pré-carrega programas usados com frequência na memória. Em HDs antigos pode ajudar; em SSDs geralmente não traz ganho e usa CPU/disco em segundo plano."),
        ("Fax", "Fax",
            "Serviço de envio/recebimento de fax. Praticamente ninguém usa isso hoje em dia."),
        ("WSearch", "Windows Search",
            "Indexa arquivos para buscas rápidas no Explorer/menu Iniciar. Desativar economiza recursos, mas deixa a busca de arquivos mais lenta."),
        ("PrintNotify", "Notificações de Impressão",
            "Gerencia notificações de fila de impressão. Seguro desativar se você não usa impressora."),
        ("RemoteRegistry", "Registro Remoto",
            "Permite que outros computadores da rede editem seu registro remotamente. Desativado por padrão em versões recentes; mantenha desativado por segurança."),
        ("MapsBroker", "Mapas Baixados",
            "Mantém mapas offline atualizados. Seguro desativar se você não usa o app Mapas."),
    };

    public List<ServiceItem> ObterServicos()
    {
        var itens = new List<ServiceItem>();

        foreach (var (nome, descricao, recomendacao) in ServicosCurados)
        {
            try
            {
                using var controlador = new ServiceController(nome);
                var emExecucao = controlador.Status == ServiceControllerStatus.Running;
                itens.Add(new ServiceItem
                {
                    NomeServico = nome,
                    NomeExibicao = controlador.DisplayName,
                    Descricao = descricao,
                    RecomendacaoSeguranca = recomendacao,
                    EmExecucao = emExecucao,
                    InicializacaoAutomatica = LerTipoInicializacao(nome) == 2
                });
            }
            catch (InvalidOperationException)
            {
                // Serviço não existe nesta edição do Windows — ignora.
            }
        }

        return itens;
    }

    public void DefinirInicializacaoAutomatica(ServiceItem item, bool automatico)
    {
        try
        {
            using var chave = Registry.LocalMachine.OpenSubKey(
                $@"SYSTEM\CurrentControlSet\Services\{item.NomeServico}", writable: true);
            // 2 = Automático, 3 = Manual, 4 = Desabilitado
            chave?.SetValue("Start", automatico ? 2 : 3, RegistryValueKind.DWord);
            item.InicializacaoAutomatica = automatico;

            if (!automatico)
            {
                using var controlador = new ServiceController(item.NomeServico);
                if (controlador.Status == ServiceControllerStatus.Running)
                {
                    controlador.Stop();
                    controlador.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                }
                item.EmExecucao = false;
            }
        }
        catch (System.Security.SecurityException)
        {
            throw new InvalidOperationException("Sem permissão para alterar este serviço. Reinicie como administrador.");
        }
        catch (UnauthorizedAccessException)
        {
            throw new InvalidOperationException("Sem permissão para alterar este serviço. Reinicie como administrador.");
        }
    }

    private static int LerTipoInicializacao(string nomeServico)
    {
        try
        {
            using var chave = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{nomeServico}");
            return chave?.GetValue("Start") is int valor ? valor : -1;
        }
        catch (System.Security.SecurityException)
        {
            return -1;
        }
    }
}
