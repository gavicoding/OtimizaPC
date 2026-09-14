namespace OtimizaPC.Helpers;

public static class FormatHelper
{
    private static readonly string[] Unidades = { "B", "KB", "MB", "GB", "TB" };

    public static string FormatarBytes(long bytes)
    {
        if (bytes <= 0) return "0 B";

        double tamanho = bytes;
        int indice = 0;
        while (tamanho >= 1024 && indice < Unidades.Length - 1)
        {
            tamanho /= 1024;
            indice++;
        }

        return $"{tamanho:0.##} {Unidades[indice]}";
    }
}
