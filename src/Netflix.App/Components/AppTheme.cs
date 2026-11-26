using ApexCharts;
using MudBlazor;

namespace Netflix.App.Components;

/// <summary>Tema oscuro estilo Netflix (design D10) y opciones comunes de las gráficas.</summary>
public static class AppTheme
{
    public const string Movies = "#E50914";
    public const string Shows = "#F5C518";
    public const string Ok = "#46D369";
    public const string Error = "#FF5252";

    public static readonly MudTheme Mud = new()
    {
        PaletteDark = new PaletteDark
        {
            Primary = Movies,
            Secondary = Shows,
            Success = Ok,
            Error = Error,
            Background = "#141414",
            Surface = "#1F1F1F",
            DrawerBackground = "#1F1F1F",
            AppbarBackground = "#141414",
        },
    };

    /// <summary>Cada gráfica necesita su propia instancia de opciones.</summary>
    public static ApexChartOptions<T> Chart<T>(bool horizontal = false, bool stacked = false) where T : class => new()
    {
        Theme = new ApexCharts.Theme { Mode = Mode.Dark },
        Chart = new Chart { Background = "transparent", Stacked = stacked, Toolbar = new Toolbar { Show = false } },
        Colors = [Movies, Shows],
        DataLabels = new DataLabels { Enabled = false },
        Grid = new Grid { BorderColor = "#333333" },
        PlotOptions = new PlotOptions { Bar = new PlotOptionsBar { Horizontal = horizontal } },
    };

    public static string TypeName(string? type) => type == "SHOW" ? "Serie" : "Película";
    public static string RoleName(string role) => role == "DIRECTOR" ? "Dirección" : "Actuación";
    public static string Score(double? value) => value?.ToString("0.0") ?? "—";
}
