using Avalonia.Threading;
using IronVault.Core.Engine;
using IronVault.Core.Engine.Systems;

namespace IronVault.App.ViewModels;

public sealed class GameViewModel
{
    public GameEngine Engine { get; } = new();

    private readonly DispatcherTimer _timer;
    private DateTime _lastTick;

    /// <summary>Fired each frame with delta time (seconds). View calls GameCanvas.Tick(dt).</summary>
    public event EventHandler<float>? FrameTick;

    public GameViewModel()
    {
        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(1000.0 / 60),
        };
        _timer.Tick += OnTimerTick;
    }

    public void StartGame(AIDifficulty difficulty = AIDifficulty.Normal, GameMode mode = GameMode.Classic)
    {
        Engine.Difficulty = difficulty;
        Engine.Mode       = mode;
        Engine.StartGame();
        _lastTick = DateTime.UtcNow;
        _timer.Start();
    }

    public void TogglePause() => Engine.TogglePause();

    public void Stop() => _timer.Stop();

    private void OnTimerTick(object? sender, EventArgs e)
    {
        var now = DateTime.UtcNow;
        float dt = (float)(now - _lastTick).TotalSeconds;
        _lastTick = now;

        if (dt > 0.1f) dt = 0.1f;

        FrameTick?.Invoke(this, dt);
    }
}
