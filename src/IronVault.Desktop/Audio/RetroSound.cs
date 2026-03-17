using System.Runtime.InteropServices;

namespace IronVault.Desktop.Audio;

/// <summary>
/// Retro synthesized sound effects via winmm.dll PlaySound (Windows only).
/// All sounds are generated from code — no audio file assets needed.
/// </summary>
internal static partial class RetroSound
{
    // Pre-built WAV buffers (synthesized once at startup)
    private static readonly byte[] _shoot     = MakeDecayBlip(hz: 900,  ms: 80);
    private static readonly byte[] _explosion = MakeNoiseBurst(ms: 180);
    private static readonly byte[] _click     = MakeDecayBlip(hz: 1200, ms: 40);

    public static void PlayShoot()     => TryPlay(_shoot);
    public static void PlayExplosion() => TryPlay(_explosion);
    public static void PlayClick()     => TryPlay(_click);

    // ── Windows PlaySound P/Invoke ────────────────────────────────────────────

    private static void TryPlay(byte[] wav)
    {
        if (!OperatingSystem.IsWindows()) return;
        try { PlaySound(wav, 0, SND_MEMORY | SND_ASYNC | SND_NODEFAULT); }
        catch { /* audio unavailable */ }
    }

    [DllImport("winmm.dll")]
    private static extern bool PlaySound(byte[] pszSound, nint hmod, uint fdwSound);

    private const uint SND_ASYNC     = 0x0001;
    private const uint SND_NODEFAULT = 0x0002;
    private const uint SND_MEMORY    = 0x0004;

    // ── Sound synthesis ───────────────────────────────────────────────────────

    /// <summary>Square wave blip with linear amplitude decay.</summary>
    private static byte[] MakeDecayBlip(double hz, int ms)
    {
        const int rate = 8000;
        int n = rate * ms / 1000;
        double period = rate / hz;
        var pcm = new byte[n];
        for (int i = 0; i < n; i++)
        {
            double amp = 1.0 - (double)i / n;           // linear decay
            bool hi = (i % (int)period) < period / 2;
            pcm[i] = (byte)(128 + (hi ? 1 : -1) * (int)(amp * 96));
        }
        return WavWrap(rate, pcm);
    }

    /// <summary>White-noise burst with amplitude decay (explosion).</summary>
    private static byte[] MakeNoiseBurst(int ms)
    {
        const int rate = 8000;
        int n = rate * ms / 1000;
        var rng = new Random(42);   // fixed seed → deterministic
        var pcm = new byte[n];
        for (int i = 0; i < n; i++)
        {
            double amp = 1.0 - (double)i / n;
            int noise = (int)((rng.NextDouble() * 2.0 - 1.0) * amp * 80);
            pcm[i] = (byte)Math.Clamp(128 + noise, 0, 255);
        }
        return WavWrap(rate, pcm);
    }

    /// <summary>Wraps raw 8-bit mono PCM bytes in a minimal WAV container.</summary>
    private static byte[] WavWrap(int sampleRate, byte[] pcm)
    {
        using var ms = new MemoryStream(44 + pcm.Length);
        using var w  = new BinaryWriter(ms);
        w.Write("RIFF"u8);
        w.Write(36 + pcm.Length);   // RIFF chunk size
        w.Write("WAVE"u8);
        w.Write("fmt "u8);
        w.Write(16);                // fmt chunk size
        w.Write((short)1);          // PCM
        w.Write((short)1);          // mono
        w.Write(sampleRate);
        w.Write(sampleRate);        // ByteRate = rate * 1 * 1
        w.Write((short)1);          // BlockAlign
        w.Write((short)8);          // BitsPerSample
        w.Write("data"u8);
        w.Write(pcm.Length);
        w.Write(pcm);
        return ms.ToArray();
    }
}
