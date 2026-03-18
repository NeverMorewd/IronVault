namespace IronVault.Navigation;

public sealed class NavigationService : INavigationService
{
    public AppScreen CurrentScreen { get; private set; } = AppScreen.Menu;

    public event EventHandler<AppScreen>? Navigated;

    public void NavigateTo(AppScreen screen)
    {
        CurrentScreen = screen;
        Navigated?.Invoke(this, screen);
    }
}
