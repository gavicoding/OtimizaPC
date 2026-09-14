using System.IO;
using Microsoft.Win32;
using OtimizaPC.Helpers;
using OtimizaPC.Models;

namespace OtimizaPC.Services;

public class ResidueService
{
    private static readonly string[] PastasIgnoradas =
    {
        "windowsapps", "common files", "commonfiles", "packages", "internet explorer",
        "windows defender", "windows defender advanced threat protection", "windows mail",
        "windows media player", "windows multimedia platform", "windows nt", "windows photo viewer",
        "windowspowershell", "microsoft.net", "dotnet", "google", "microsoft", "mozilla",
        "modifiablewindowsapps",
        // Ruído comum em %APPDATA%, %LOCALAPPDATA% e %ProgramData% que não é "resíduo de programa":
        // são pastas de infraestrutura do próprio Windows, sem entrada de desinstalação.
        "temp", "temporary internet files", "connecteddevicesplatform", "comms",
        "elevateddiagnostics", "diagnostics", "d3dscache", "crashdumps", "iconcache",
        "nethood", "printhood", "recent", "sendto", "templates", "usoprivate", "usoshared",
        "package cache", "regid.1991-06.com.microsoft.com", "programs", "publishers",
        "application data", "local", "locallow", "roaming", "history", "caches",
        // Mecanismos do próprio Windows — nunca são "resíduo de programa", mesmo que não
        // batam com nada no registro de desinstalação.
        "virtualstore", "squirreltemp"
    };

    /// <summary>
    /// Uma pasta só é sinalizada se, além de não bater com nenhum programa instalado,
    /// ela também não foi tocada há pelo menos esse tempo. Isso evita marcar como
    /// "resíduo" caches e ferramentas em uso ativo (npm, NuGet, Playwright, etc.) que
    /// raramente têm entrada tradicional de desinstalação mas são reescritas o tempo todo.
    /// </summary>
    private static readonly TimeSpan TempoMinimoSemUso = TimeSpan.FromDays(90);

    public async Task<List<ResidueItem>> EncontrarResiduosAsync(IProgress<string>? progresso = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            var progressoLimitado = new ProgressoComLimite(progresso);
            progressoLimitado.Report("Lendo programas instalados...");
            var nomesInstalados = ObterNomesInstalados();

            var pastasParaChecar = new List<string>();
            AdicionarSeExistir(pastasParaChecar, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
            AdicionarSeExistir(pastasParaChecar, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));
            AdicionarSeExistir(pastasParaChecar, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs"));
            // Muitos instaladores (Electron, apps portáteis, etc.) deixam a pasta do programa
            // direto aqui, sem passar por "\Programs" — é onde mais aparecem resíduos ocultos.
            AdicionarSeExistir(pastasParaChecar, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            AdicionarSeExistir(pastasParaChecar, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
            AdicionarSeExistir(pastasParaChecar, Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));

            // Outras contas desta máquina também acumulam resíduo. Sem administrador, o
            // Windows bloqueia o acesso a essas pastas e elas são puladas silenciosamente.
            foreach (var outroPerfil in UserProfileHelper.ObterOutrosPerfisDeUsuarios())
            {
                AdicionarSeExistir(pastasParaChecar, Path.Combine(outroPerfil, "AppData", "Local"));
                AdicionarSeExistir(pastasParaChecar, Path.Combine(outroPerfil, "AppData", "Roaming"));
            }

            var resultado = new List<ResidueItem>();

            foreach (var raiz in pastasParaChecar.Distinct())
            {
                ct.ThrowIfCancellationRequested();
                IEnumerable<string> subpastas;
                try { subpastas = Directory.EnumerateDirectories(raiz); }
                catch (IOException) { continue; }
                catch (UnauthorizedAccessException) { continue; }

                foreach (var pasta in subpastas)
                {
                    ct.ThrowIfCancellationRequested();
                    var nomePasta = Path.GetFileName(pasta);
                    if (PastasIgnoradas.Contains(nomePasta, StringComparer.OrdinalIgnoreCase))
                        continue;

                    if (TemCorrespondencia(nomePasta, nomesInstalados))
                        continue;

                    progressoLimitado.Report($"Verificando: {nomePasta}...");
                    var (tamanho, ultimaModificacao) = FileSystemHelper.AnalisarPasta(pasta, ct);
                    if (tamanho <= 0) continue;

                    var tempoParado = DateTime.UtcNow - ultimaModificacao;
                    if (tempoParado < TempoMinimoSemUso)
                        continue; // algo ainda escreve aqui — não é resíduo abandonado

                    var oculta = EhOculta(pasta);
                    var diasParado = (int)tempoParado.TotalDays;
                    resultado.Add(new ResidueItem
                    {
                        Caminho = pasta,
                        TamanhoBytes = tamanho,
                        Motivo = oculta
                            ? $"Pasta oculta, sem programa instalado correspondente e sem uso há {diasParado} dias"
                            : $"Nenhum programa instalado corresponde a esta pasta, e ela está sem uso há {diasParado} dias"
                    });
                }
            }

            return resultado.OrderByDescending(r => r.TamanhoBytes).ToList();
        }, ct);
    }

    private static void AdicionarSeExistir(List<string> lista, string? caminho)
    {
        if (!string.IsNullOrEmpty(caminho) && Directory.Exists(caminho))
            lista.Add(caminho);
    }

    private const int TamanhoMinimoParaComparar = 3;

    private static bool TemCorrespondencia(string nomePasta, HashSet<string> nomesInstalados)
    {
        var normalizado = Normalizar(nomePasta);
        if (normalizado.Length < TamanhoMinimoParaComparar) return true; // nome curto demais p/ comparar com segurança — não afirma ser resíduo

        return nomesInstalados.Any(nomeInstalado =>
            nomeInstalado.Length >= TamanhoMinimoParaComparar &&
            (normalizado.Contains(nomeInstalado) || nomeInstalado.Contains(normalizado)));
    }

    private static bool EhOculta(string caminhoPasta)
    {
        try { return new DirectoryInfo(caminhoPasta).Attributes.HasFlag(FileAttributes.Hidden); }
        catch (IOException) { return false; }
    }

    private static string Normalizar(string texto) =>
        new string(texto.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    private static HashSet<string> ObterNomesInstalados()
    {
        var nomes = new HashSet<string>();
        string[] chaves =
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
        };

        foreach (var chave in chaves)
        {
            LerChaveDesinstalacao(Registry.LocalMachine, chave, nomes);
        }
        LerChaveDesinstalacao(Registry.CurrentUser, chaves[0], nomes);

        return nomes;
    }

    private static void LerChaveDesinstalacao(RegistryKey raiz, string caminhoChave, HashSet<string> nomes)
    {
        try
        {
            using var chave = raiz.OpenSubKey(caminhoChave);
            if (chave == null) return;

            foreach (var subChaveNome in chave.GetSubKeyNames())
            {
                using var subChave = chave.OpenSubKey(subChaveNome);
                var nomeExibicao = subChave?.GetValue("DisplayName") as string;
                if (!string.IsNullOrWhiteSpace(nomeExibicao))
                    nomes.Add(Normalizar(nomeExibicao));

                var editor = subChave?.GetValue("Publisher") as string;
                if (!string.IsNullOrWhiteSpace(editor))
                    nomes.Add(Normalizar(editor));
            }
        }
        catch (System.Security.SecurityException) { }
    }
}
