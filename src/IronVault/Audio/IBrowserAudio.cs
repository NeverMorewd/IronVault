namespace IronVault.Audio;

/// <summary>
/// Audio backend injected by the browser host.
/// Allows RetroSound to delegate to Web Audio API without
/// taking a compile-time dependency on browser-only APIs.
/// </summary>
internal interface IBrowserAudio
{
    void PlayClick();
    void PlayShoot();
    void PlayExplosion();
    void PlayEnemyDestroyed();
    void PlayPlayerHurt();
    void PlayGameOver();
    void PlayVictory();
    void PlayStageStart();
    void PlayPowerUp(int typeId);
    void StartMovement();
    void StopMovement();
}
