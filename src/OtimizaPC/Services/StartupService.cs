using System.IO;
using Microsoft.Win32;
using OtimizaPC.Models;

namespace OtimizaPC.Services;

/// <summary>
/// Gerencia programas de inicialização. Itens de registro são desabilitados
/// removendo o valor da chave Run e guardando uma cópia em backup (nossa
/// própria chave) para poder reativar depois — não depende de flags internas
/// não documentadas do Windows.
/// </summary>
public class StartupService
{
    private const string ChaveRun = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string ChaveBackup = @"SOFTWARE\OtimizaPC\InicializacaoDesativada";

    public List<StartupItem> ObterItens()
    {
        var itens = new List<StartupItem>();

        LerRegistro(Registry.CurrentUser, ChaveRun, StartupOrigem.RegistroHKCU, itens, habilitado: true);
        LerRegistro(Registry.LocalMachine, ChaveRun, StartupOrigem.RegistroHKLM, itens, habilitado: true);

        using (var backupHKCU = Registry.CurrentUser.OpenSubKey(ChaveBackup))
        {
            if (backupHKCU != null)
                LerBackup(backupHKCU, StartupOrigem.RegistroHKCU, itens);
        }

        LerPastaInicializacao(Environment.SpecialFolder.Startup, StartupOrigem.PastaInicializacaoUsuario, itens);
        LerPastaInicializacao(Environment.SpecialFolder.CommonStartup, StartupOrigem.PastaInicializacaoComum, itens);

        return itens.OrderByDescending(i => i.Habilitado).ThenBy(i => i.Nome).ToList();
    }

    public void DefinirHabilitado(StartupItem item, bool habilitado)
    {
        switch (item.Origem)
        {
            case StartupOrigem.RegistroHKCU:
                AlterarRegistro(Registry.CurrentUser, item, habilitado);
                break;
            case StartupOrigem.RegistroHKLM:
                AlterarRegistro(Registry.LocalMachine, item, habilitado);
                break;
            case StartupOrigem.PastaInicializacaoUsuario:
                AlterarAtalho(Environment.SpecialFolder.Startup, item, habilitado);
                break;
            case StartupOrigem.PastaInicializacaoComum:
                AlterarAtalho(Environment.SpecialFolder.CommonStartup, item, habilitado);
                break;
        }
        item.Habilitado = habilitado;
    }

    private static void LerRegistro(RegistryKey raiz, string caminhoChave, StartupOrigem origem, List<StartupItem> itens, bool habilitado)
    {
        try
        {
            using var chave = raiz.OpenSubKey(caminhoChave);
            if (chave == null) return;

            foreach (var nome in chave.GetValueNames())
            {
                var comando = chave.GetValue(nome) as string ?? "";
                itens.Add(new StartupItem { Nome = nome, Comando = comando, Origem = origem, Habilitado = habilitado });
            }
        }
        catch (System.Security.SecurityException) { }
    }

    private static void LerBackup(RegistryKey backupChave, StartupOrigem origem, List<StartupItem> itens)
    {
        foreach (var nome in backupChave.GetValueNames())
        {
            var comando = backupChave.GetValue(nome) as string ?? "";
            itens.Add(new StartupItem { Nome = nome, Comando = comando, Origem = origem, Habilitado = false });
        }
    }

    private static void LerPastaInicializacao(Environment.SpecialFolder pasta, StartupOrigem origem, List<StartupItem> itens)
    {
        var caminhoPasta = Environment.GetFolderPath(pasta);
        if (string.IsNullOrEmpty(caminhoPasta) || !Directory.Exists(caminhoPasta)) return;

        foreach (var arquivo in Directory.EnumerateFiles(caminhoPasta))
        {
            var habilitado = !Path.GetExtension(arquivo).Equals(".desativado", StringComparison.OrdinalIgnoreCase);
            var nome = habilitado ? Path.GetFileNameWithoutExtension(arquivo) : Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(arquivo));
            itens.Add(new StartupItem { Nome = nome, Comando = arquivo, Origem = origem, Habilitado = habilitado });
        }
    }

    private static void AlterarRegistro(RegistryKey raiz, StartupItem item, bool habilitado)
    {
        try
        {
            if (habilitado)
            {
                using var run = raiz.CreateSubKey(ChaveRun);
                run?.SetValue(item.Nome, item.Comando);

                using var backup = Registry.CurrentUser.OpenSubKey(ChaveBackup, writable: true);
                backup?.DeleteValue(item.Nome, throwOnMissingValue: false);
            }
            else
            {
                using var backup = Registry.CurrentUser.CreateSubKey(ChaveBackup);
                backup?.SetValue(item.Nome, item.Comando);

                using var run = raiz.OpenSubKey(ChaveRun, writable: true);
                run?.DeleteValue(item.Nome, throwOnMissingValue: false);
            }
        }
        catch (System.Security.SecurityException)
        {
            throw new InvalidOperationException("Sem permissão para alterar este item. Reinicie como administrador.");
        }
    }

    private static void AlterarAtalho(Environment.SpecialFolder pasta, StartupItem item, bool habilitado)
    {
        var caminhoAtual = item.Comando;
        if (!File.Exists(caminhoAtual)) return;

        string novoCaminho = habilitado
            ? caminhoAtual.Replace(".desativado", "", StringComparison.OrdinalIgnoreCase)
            : caminhoAtual + ".desativado";

        try
        {
            File.Move(caminhoAtual, novoCaminho);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
