using System.Diagnostics;
using Microsoft.Win32;

namespace OtimizaPC.Services;

/// <summary>
/// Ajustes de desempenho mais avançados — os mesmos que aparecem em Propriedades do
/// Sistema &gt; Desempenho &gt; Avançado (prioridade de processos), no Gerenciador de
/// Dispositivos/registro (agendamento de GPU) e no powercfg (hibernação).
/// </summary>
public class AdvancedTweaksService
{
    private const string ChavePrioridade = @"SYSTEM\CurrentControlSet\Control\PriorityControl";
    private const string ChaveGpu = @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers";
    private const string ChaveHibernacao = @"SYSTEM\CurrentControlSet\Control\Power";

    public bool PriorizaProgramasEmPrimeiroPlano()
    {
        using var chave = Registry.LocalMachine.OpenSubKey(ChavePrioridade);
        return chave?.GetValue("Win32PrioritySeparation") is int valor && valor == 38;
    }

    public void DefinirPriorizarProgramas(bool priorizar)
    {
        try
        {
            using var chave = Registry.LocalMachine.OpenSubKey(ChavePrioridade, writable: true);
            chave?.SetValue("Win32PrioritySeparation", priorizar ? 38 : 24, RegistryValueKind.DWord);
        }
        catch (UnauthorizedAccessException) { throw ErroPermissao(); }
        catch (System.Security.SecurityException) { throw ErroPermissao(); }
    }

    public bool AgendamentoGpuAtivo()
    {
        using var chave = Registry.LocalMachine.OpenSubKey(ChaveGpu);
        return chave?.GetValue("HwSchMode") is int valor && valor == 2;
    }

    public void DefinirAgendamentoGpu(bool ativar)
    {
        try
        {
            using var chave = Registry.LocalMachine.OpenSubKey(ChaveGpu, writable: true);
            chave?.SetValue("HwSchMode", ativar ? 2 : 1, RegistryValueKind.DWord);
        }
        catch (UnauthorizedAccessException) { throw ErroPermissao(); }
        catch (System.Security.SecurityException) { throw ErroPermissao(); }
    }

    public bool HibernacaoAtiva()
    {
        using var chave = Registry.LocalMachine.OpenSubKey(ChaveHibernacao);
        return chave?.GetValue("HiberFileEnabled") is int valor && valor == 1;
    }

    public async Task<string> DefinirHibernacaoAsync(bool ativar)
    {
        return await Task.Run(() =>
        {
            var info = new ProcessStartInfo("powercfg", ativar ? "/hibernate on" : "/hibernate off")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            try
            {
                using var processo = Process.Start(info);
                if (processo == null) return "Não foi possível executar o powercfg.";
                processo.WaitForExit();
                return processo.ExitCode == 0
                    ? (ativar ? "Hibernação ativada." : "Hibernação desativada — o hiberfil.sys foi removido.")
                    : "Requer permissão de administrador.";
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return "Requer permissão de administrador.";
            }
        });
    }

    private static InvalidOperationException ErroPermissao() =>
        new("Sem permissão para alterar isto. Reinicie o programa como administrador.");
}
