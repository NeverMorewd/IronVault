using System.Runtime.InteropServices.JavaScript;
using IronVault.Audio;

namespace IronVault.Browser.Audio;

/// <summary>
/// Browser audio backend: delegates to Web Audio API via JS interop.
/// Registered in Program.cs as RetroSound.BrowserBackend before the
/// Avalonia app starts.
/// </summary>
internal sealed partial class BrowserAudio : IBrowserAudio
{
    public void PlayClick()          => JsPlayClick();
    public void PlayShoot()          => JsPlayShoot();
    public void PlayExplosion()      => JsPlayExplosion();
    public void PlayEnemyDestroyed() => JsPlayEnemyDestroyed();
    public void PlayPlayerHurt()     => JsPlayPlayerHurt();
    public void PlayGameOver()       => JsPlayGameOver();
    public void PlayVictory()        => JsPlayVictory();
    public void PlayPowerUp()        => JsPlayPowerUp();
    public void StartMovement()      => JsStartMovement();
    public void StopMovement()       => JsStopMovement();

    [JSImport("globalThis.IronVaultAudio.playClick")]
    private static partial void JsPlayClick();

    [JSImport("globalThis.IronVaultAudio.playShoot")]
    private static partial void JsPlayShoot();

    [JSImport("globalThis.IronVaultAudio.playExplosion")]
    private static partial void JsPlayExplosion();

    [JSImport("globalThis.IronVaultAudio.playEnemyDestroyed")]
    private static partial void JsPlayEnemyDestroyed();

    [JSImport("globalThis.IronVaultAudio.playPlayerHurt")]
    private static partial void JsPlayPlayerHurt();

    [JSImport("globalThis.IronVaultAudio.playGameOver")]
    private static partial void JsPlayGameOver();

    [JSImport("globalThis.IronVaultAudio.playVictory")]
    private static partial void JsPlayVictory();

    [JSImport("globalThis.IronVaultAudio.playPowerUp")]
    private static partial void JsPlayPowerUp();

    [JSImport("globalThis.IronVaultAudio.startMovement")]
    private static partial void JsStartMovement();

    [JSImport("globalThis.IronVaultAudio.stopMovement")]
    private static partial void JsStopMovement();
}
