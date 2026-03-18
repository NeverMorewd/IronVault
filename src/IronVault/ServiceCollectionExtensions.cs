using Microsoft.Extensions.DependencyInjection;
using IronVault.Core.Engine;
using IronVault.Navigation;
using IronVault.ViewModels;
using IronVault.Views;

namespace IronVault;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIronVaultServices(this IServiceCollection services)
    {
        // Core engine — one instance for the lifetime of the app
        services.AddSingleton<GameEngine>();

        // ViewModel wraps the engine and drives the game loop timer
        services.AddSingleton<GameViewModel>(sp =>
            new GameViewModel(sp.GetRequiredService<GameEngine>()));

        // Navigation
        services.AddSingleton<INavigationService, NavigationService>();

        // Views — singletons because each screen exists exactly once
        services.AddSingleton<MenuView>();
        services.AddSingleton<GameView>(sp =>
            new GameView(sp.GetRequiredService<GameViewModel>()));
        services.AddSingleton<UpgradeView>(sp =>
            new UpgradeView(sp.GetRequiredService<GameEngine>()));
        services.AddSingleton<MainView>(sp =>
            new MainView(
                sp.GetRequiredService<INavigationService>(),
                sp.GetRequiredService<GameViewModel>(),
                sp.GetRequiredService<MenuView>(),
                sp.GetRequiredService<GameView>(),
                sp.GetRequiredService<UpgradeView>()));

        return services;
    }
}
