using System.IO;
using OtimizaPC.Helpers;
using OtimizaPC.Models;

namespace OtimizaPC.Services;

public static class CleanupDefinitions
{
    public static List<CleanupCategoryDefinition> ObterCategorias()
    {
        var temp = Path.GetTempPath().TrimEnd('\\');
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var sistemDrive = Path.GetPathRoot(windows) ?? "C:\\";
        var outrosPerfis = UserProfileHelper.ObterOutrosPerfisDeUsuarios();

        var categorias = new List<CleanupCategoryDefinition>
        {
            new()
            {
                Id = "temp_usuario",
                Nome = "Arquivos temporários do usuário",
                Descricao = "Arquivos deixados por programas em %TEMP% — geralmente seguros de apagar.",
                Tipo = CleanupTargetKind.Folder,
                Caminhos = new() { temp },
                SelecionadoPorPadrao = true,
                IdadeMinimaDias = 1
            },
            new()
            {
                Id = "temp_windows",
                Nome = "Arquivos temporários do Windows",
                Descricao = "Pasta Temp do sistema (C:\\Windows\\Temp). Requer administrador.",
                Tipo = CleanupTargetKind.Folder,
                Caminhos = new() { Path.Combine(windows, "Temp") },
                SelecionadoPorPadrao = true,
                RequerAdministrador = true,
                IdadeMinimaDias = 1
            },
            new()
            {
                Id = "prefetch",
                Nome = "Prefetch",
                Descricao = "Cache de pré-carregamento do Windows. O próprio Windows recria o que precisa; seguro de limpar. Requer administrador.",
                Tipo = CleanupTargetKind.Folder,
                Caminhos = new() { Path.Combine(windows, "Prefetch") },
                SelecionadoPorPadrao = false,
                RequerAdministrador = true
            },
            new()
            {
                Id = "windows_update_cache",
                Nome = "Cache do Windows Update",
                Descricao = "Instaladores já baixados e aplicados pelo Windows Update. Requer administrador.",
                Tipo = CleanupTargetKind.Folder,
                Caminhos = new() { Path.Combine(windows, "SoftwareDistribution", "Download") },
                SelecionadoPorPadrao = true,
                RequerAdministrador = true
            },
            new()
            {
                Id = "lixeira",
                Nome = "Lixeira",
                Descricao = "Esvazia a Lixeira de todas as unidades.",
                Tipo = CleanupTargetKind.RecycleBin,
                SelecionadoPorPadrao = true
            },
            new()
            {
                Id = "miniaturas",
                Nome = "Cache de miniaturas",
                Descricao = "Miniaturas de imagens/vídeos geradas pelo Explorer. São recriadas automaticamente.",
                Tipo = CleanupTargetKind.Folder,
                Caminhos = new() { Path.Combine(localAppData, "Microsoft", "Windows", "Explorer") },
                ExtensoesPermitidas = new[] { ".db" },
                SelecionadoPorPadrao = true
            },
            new()
            {
                Id = "relatorios_erro",
                Nome = "Relatórios de erro do Windows",
                Descricao = "Dumps e relatórios de travamentos de programas.",
                Tipo = CleanupTargetKind.Folder,
                Caminhos = new()
                {
                    Path.Combine(localAppData, "CrashDumps"),
                    Path.Combine(programData, "Microsoft", "Windows", "WER")
                },
                SelecionadoPorPadrao = true,
                RequerAdministrador = true
            },
            new()
            {
                Id = "cache_chrome",
                Nome = "Cache do Google Chrome",
                Descricao = "Cache de navegação do Chrome (não apaga senhas, histórico ou favoritos).",
                Tipo = CleanupTargetKind.Folder,
                Caminhos = new() { Path.Combine(localAppData, "Google", "Chrome", "User Data", "*", "Cache") },
                SelecionadoPorPadrao = true
            },
            new()
            {
                Id = "cache_edge",
                Nome = "Cache do Microsoft Edge",
                Descricao = "Cache de navegação do Edge (não apaga senhas, histórico ou favoritos).",
                Tipo = CleanupTargetKind.Folder,
                Caminhos = new() { Path.Combine(localAppData, "Microsoft", "Edge", "User Data", "*", "Cache") },
                SelecionadoPorPadrao = true
            },
            new()
            {
                Id = "cache_firefox",
                Nome = "Cache do Firefox",
                Descricao = "Cache de navegação do Firefox (não apaga senhas, histórico ou favoritos).",
                Tipo = CleanupTargetKind.Folder,
                Caminhos = new() { Path.Combine(localAppData, "Mozilla", "Firefox", "Profiles", "*", "cache2") },
                SelecionadoPorPadrao = true
            },
            new()
            {
                Id = "logs_windows",
                Nome = "Logs do Windows",
                Descricao = "Arquivos de log gerados pelo sistema e por instalações. Requer administrador.",
                Tipo = CleanupTargetKind.Folder,
                Caminhos = new() { Path.Combine(windows, "Logs") },
                SelecionadoPorPadrao = false,
                RequerAdministrador = true
            },
            new()
            {
                Id = "restos_upgrade",
                Nome = "Restos de atualizações antigas do Windows",
                Descricao = "Pastas $Windows.~BT / $Windows.~WS deixadas por atualizações de versão do Windows. Requer administrador.",
                Tipo = CleanupTargetKind.Folder,
                Caminhos = new()
                {
                    Path.Combine(sistemDrive, "$Windows.~BT"),
                    Path.Combine(sistemDrive, "$Windows.~WS")
                },
                SelecionadoPorPadrao = false,
                RequerAdministrador = true
            }
        };

        if (outrosPerfis.Count > 0)
        {
            var caminhosOutrosUsuarios = new List<string>();
            foreach (var perfil in outrosPerfis)
            {
                caminhosOutrosUsuarios.Add(Path.Combine(perfil, "AppData", "Local", "Temp"));
                caminhosOutrosUsuarios.Add(Path.Combine(perfil, "AppData", "Local", "Google", "Chrome", "User Data", "*", "Cache"));
                caminhosOutrosUsuarios.Add(Path.Combine(perfil, "AppData", "Local", "Microsoft", "Edge", "User Data", "*", "Cache"));
                caminhosOutrosUsuarios.Add(Path.Combine(perfil, "AppData", "Local", "Mozilla", "Firefox", "Profiles", "*", "cache2"));
            }

            categorias.Add(new CleanupCategoryDefinition
            {
                Id = "temp_outros_usuarios",
                Nome = $"Arquivos temporários de outros usuários ({outrosPerfis.Count})",
                Descricao = "Temp e cache de navegador das outras contas desta máquina. Requer administrador — sem elevação, o Windows bloqueia o acesso e esta categoria aparece vazia.",
                Tipo = CleanupTargetKind.Folder,
                Caminhos = caminhosOutrosUsuarios,
                SelecionadoPorPadrao = false,
                RequerAdministrador = true
            });
        }

        return categorias;
    }

    /// <summary>
    /// Expande caminhos com "*" (ex.: perfis de navegador) em caminhos reais existentes.
    /// </summary>
    public static IEnumerable<string> ExpandirCaminho(string caminhoComCoringa)
    {
        if (!caminhoComCoringa.Contains('*'))
        {
            if (Directory.Exists(caminhoComCoringa))
                yield return caminhoComCoringa;
            yield break;
        }

        var partes = caminhoComCoringa.Split('*', 2);
        var basePasta = partes[0].TrimEnd('\\');
        var sufixo = partes.Length > 1 ? partes[1].TrimStart('\\') : "";

        if (!Directory.Exists(basePasta))
            yield break;

        IEnumerable<string> candidatos;
        try
        {
            candidatos = Directory.EnumerateDirectories(basePasta);
        }
        catch (UnauthorizedAccessException) { yield break; }
        catch (IOException) { yield break; }

        foreach (var candidato in candidatos)
        {
            var caminhoFinal = string.IsNullOrEmpty(sufixo) ? candidato : Path.Combine(candidato, sufixo);
            if (Directory.Exists(caminhoFinal))
                yield return caminhoFinal;
        }
    }
}
