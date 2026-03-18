namespace IronVault.Navigation;

public interface INavigationService
{
    AppScreen CurrentScreen { get; }

    void NavigateTo(AppScreen screen);

    /// <summary>Fired after each successful navigation.</summary>
    event EventHandler<AppScreen>? Navigated;
}
