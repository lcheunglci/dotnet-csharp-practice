using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace Budget_Desktop_App;

public partial class App : Application
{
    /// <summary>True when the dark theme is active.</summary>
    public bool IsDarkMode { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>Switches the whole application between the light and dark theme.</summary>
    public void ToggleTheme()
    {
        IsDarkMode = !IsDarkMode;
        RequestedThemeVariant = IsDarkMode ? ThemeVariant.Dark : ThemeVariant.Light;
    }
}
