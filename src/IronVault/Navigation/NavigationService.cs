namespace IronVault.Navigation;

public sealed class NavigationService : INavigationService
{
    public AppScreen CurrentScreen { get; private set; } = AppScreen.Menu;

    public event EventHandler<AppScreen>? Navigated;

    /// <summary>
    /// Static hook so platform-specific projects (e.g. IronVault.Browser) can
    /// subscribe without needing access to the DI container.
    /// </summary>
    public static event EventHandler<AppScreen>? GlobalNavigated;

    public void NavigateTo(AppScreen screen)
    {
        CurrentScreen = screen;
        Navigated?.Invoke(this, screen);
        GlobalNavigated?.Invoke(this, screen);
    }
}
