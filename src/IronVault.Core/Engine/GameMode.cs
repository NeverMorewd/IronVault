namespace IronVault.Core.Engine;

/// <summary>
/// Selects the game ruleset.
/// <list type="bullet">
///   <item>Classic — infinite waves; no victory condition; survive as long as possible.</item>
///   <item>Defense — 10 pre-scripted waves with increasing pressure; survive all 10 to win.</item>
/// </list>
/// </summary>
public enum GameMode { Classic, Defense }
