using System.Diagnostics;
using System.IO;
using System.Management;
using OtimizaPC.Helpers;
using OtimizaPC.Models;

namespace OtimizaPC.Services;

public class MemoriaInfo
{
    public long TotalBytes { get; init; }
    public long DisponivelBytes { get; init; }
    public long UsadoBytes => TotalBytes - DisponivelBytes;
    public double PercentualUsado => TotalBytes == 0 ? 0 : (double)UsadoBytes / TotalBytes * 100;
}

public class SystemMaintenanceService
{
    private readonly PerformanceCounter? _contadorCpu;

    public SystemMaintenanceService()
    {
        try
        {
            _contadorCpu = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _contadorCpu.NextValue();
        }
        catch (InvalidOperationException)
        {
            _contadorCpu = null;
        }
    }

    public float ObterUsoCpu() => _contadorCpu?.NextValue() ?? -1;

    public MemoriaInfo ObterInfoMemoria()
    {
        var status = NativeMethods.MEMORYSTATUSEX.Criar();
        if (!NativeMethods.GlobalMemoryStatusEx(ref status))
            return new MemoriaInfo { TotalBytes = 0, DisponivelBytes = 0 };

        return new MemoriaInfo
        {
            TotalBytes = (long)status.ullTotalPhys,
            DisponivelBytes = (long)status.ullAvailPhys
        };
    }

    public List<DiscoInfo> ObterInfoDiscos()
    {
        var saudePorModelo = ObterSaudeDiscos();
        var discos = new List<DiscoInfo>();

        foreach (var unidade in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed && d.IsReady))
        {
            var disco = new DiscoInfo
            {
                Unidade = unidade.Name,
                RotuloVolume = string.IsNullOrWhiteSpace(unidade.VolumeLabel) ? "(sem rótulo)" : unidade.VolumeLabel,
                EspacoTotalBytes = unidade.TotalSize,
                EspacoLivreBytes = unidade.AvailableFreeSpace
            };

            if (saudePorModelo.Count > 0)
            {
                disco.SaudeOk = !saudePorModelo.Values.Any(falha => falha);
                disco.StatusSaude = disco.SaudeOk ? "OK" : "Atenção — possível falha prevista";
            }

            discos.Add(disco);
        }

        return discos;
    }

    private static Dictionary<string, bool> ObterSaudeDiscos()
    {
        var resultado = new Dictionary<string, bool>();
        if (!ElevationHelper.EstaElevado())
            return resultado;

        try
        {
            using var pesquisador = new ManagementObjectSearcher(
                @"root\wmi", "SELECT InstanceName, PredictFailure FROM MSStorageDriver_FailurePredictStatus");

            foreach (ManagementObject item in pesquisador.Get())
            {
                var nome = item["InstanceName"]?.ToString() ?? Guid.NewGuid().ToString();
                var falha = item["PredictFailure"] is bool b && b;
                resultado[nome] = falha;
            }
        }
        catch (ManagementException) { }
        catch (UnauthorizedAccessException) { }

        return resultado;
    }

    public async Task<bool> CriarPontoRestauracaoAsync(string descricao)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var classeRestauracao = new ManagementClass(@"root\default", "SystemRestore", null);
                using var parametros = classeRestauracao.GetMethodParameters("CreateRestorePoint");
                parametros["Description"] = descricao;
                parametros["RestorePointType"] = 12; // MODIFY_SETTINGS
                parametros["EventType"] = 100; // BEGIN_SYSTEM_CHANGE

                using var resultado = classeRestauracao.InvokeMethod("CreateRestorePoint", parametros, null);
                var retorno = resultado?["ReturnValue"];
                return retorno is uint codigo && codigo == 0;
            }
            catch (ManagementException)
            {
                return false;
            }
        });
    }

    public async Task<string> OtimizarUnidadeAsync(string letraUnidade)
    {
        return await Task.Run(() =>
        {
            var info = new ProcessStartInfo("defrag.exe", $"{letraUnidade} /O")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            try
            {
                using var processo = Process.Start(info);
                if (processo == null) return "Não foi possível iniciar o defrag.";
                var saida = processo.StandardOutput.ReadToEnd();
                processo.WaitForExit();
                return processo.ExitCode == 0 ? "Otimização concluída." : $"Defrag retornou código {processo.ExitCode}.";
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return "Requer permissão de administrador.";
            }
        });
    }
}
