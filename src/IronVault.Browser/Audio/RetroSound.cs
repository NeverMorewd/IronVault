namespace IronVault.Desktop.Audio;

/// <summary>
/// Browser stub for <c>RetroSound</c>.
/// All methods are intentional no-ops: the original implementation uses the
/// Windows <c>waveOut</c> API (winmm.dll) which is not available in WebAssembly.
/// Web Audio API integration can be wired in later via JavaScript interop.
/// </summary>
public static class RetroSound
{
    public static void PlayClick()          { }
    public static void PlayShoot()          { }
    public static void PlayExplosion()      { }
    public static void PlayEnemyDestroyed() { }
    public static void PlayPlayerHurt()     { }
    public static void PlayPowerUp()        { }
    public static void PlayGameOver()       { }
    public static void PlayVictory()        { }
    public static void StartMovement()      { }
    public static void StopMovement()       { }
}
