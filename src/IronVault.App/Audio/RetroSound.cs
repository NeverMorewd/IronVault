using System.Runtime.InteropServices;

namespace IronVault.App.Audio;

/// <summary>
/// Synthesized game audio — zero asset files, zero NuGet dependencies.
///
/// Architecture:
///   • One-shots (click / shoot / explosion) → winmm.dll PlaySound  (channel A)
///   • Movement loop                         → waveOut API           (channel B, independent)
///
/// Two separate Windows audio channels means the engine-rumble loop runs
/// continuously in parallel with clicks, shots and explosions — no interruption.
/// Both paths are pure P/Invoke: AOT-safe and trimmer-safe.
/// </summary>
internal static class RetroSound
{
    // All synthesis uses 16-bit mono PCM at 22 050 Hz
    private const int Rate = 22_050;

    // ── Pre-built buffers (synthesised once at class load) ────────────────────
    // One-shots are wrapped in a WAV container so PlaySound can consume them.
    private static readonly byte[] _click           = WrapWav(SynthClick());
    private static readonly byte[] _shoot           = WrapWav(SynthShoot());
    private static readonly byte[] _explosion       = WrapWav(SynthExplosion());
    private static readonly byte[] _enemyDestroyed  = WrapWav(SynthEnemyDestroyed());
    private static readonly byte[] _playerHurt      = WrapWav(SynthPlayerHurt());
    private static readonly byte[] _gameOver        = WrapWav(SynthGameOver());
    private static readonly byte[] _victory         = WrapWav(SynthVictory());
    private static readonly byte[] _powerUp         = WrapWav(SynthPowerUp());
    // Movement is raw PCM fed directly to waveOut (no WAV header needed).
    private static readonly byte[] _movement        = SynthMovement();

    // ── Shoot debounce (prevents enemy-fire spam drowning the mix) ────────────
    private static long _lastShootTick;
    private const  long ShootCooldownMs = 130;

    // ── Public API ────────────────────────────────────────────────────────────

    public static void PlayClick()          => TryPlayWav(_click);
    public static void PlayExplosion()      => TryPlayWav(_explosion);
    public static void PlayPowerUp()        => TryPlayWav(_powerUp);
    public static void PlayEnemyDestroyed() => TryPlayWav(_enemyDestroyed);
    public static void PlayPlayerHurt()     => TryPlayWav(_playerHurt);
    public static void PlayGameOver()       => TryPlayWav(_gameOver);
    public static void PlayVictory()        => TryPlayWav(_victory);

    public static void PlayShoot()
    {
        long now = Environment.TickCount64;
        if (now - _lastShootTick < ShootCooldownMs) return;
        _lastShootTick = now;
        TryPlayWav(_shoot);
    }

    /// <summary>Begin looping the engine-rumble sound (no-op if already playing).</summary>
    public static void StartMovement()
    {
        if (!OperatingSystem.IsWindows()) return;
        MoveLoop.Start(_movement);
    }

    /// <summary>Stop the engine-rumble loop (no-op if already stopped).</summary>
    public static void StopMovement()
    {
        if (!OperatingSystem.IsWindows()) return;
        MoveLoop.Stop();
    }

    // ── PlaySound one-shot helper ─────────────────────────────────────────────

    private static void TryPlayWav(byte[] wav)
    {
        if (!OperatingSystem.IsWindows()) return;
        try { WinMM.PlaySound(wav, 0, WinMM.SND_MEMORY | WinMM.SND_ASYNC | WinMM.SND_NODEFAULT); }
        catch { /* audio subsystem unavailable */ }
    }

    // ── Sound synthesis ───────────────────────────────────────────────────────

    /// <summary>UI click: 1 kHz square-wave tick, 28 ms, cubic decay.</summary>
    private static byte[] SynthClick()
    {
        int n = Rate * 28 / 1000;
        var buf = new byte[n * 2];
        for (int i = 0; i < n; i++)
        {
            double t = (double)i / n;
            double amp   = Math.Pow(1 - t, 3) * 0.65;
            double phase = 2 * Math.PI * 1000 * i / Rate;
            Write16(buf, i, Math.Sign(Math.Sin(phase)) * amp);
        }
        return buf;
    }

    /// <summary>Player shoots: frequency-sweep chirp 680 → 110 Hz, 95 ms, square wave.</summary>
    private static byte[] SynthShoot()
    {
        int n = Rate * 95 / 1000;
        var buf = new byte[n * 2];
        double phase = 0;
        for (int i = 0; i < n; i++)
        {
            double t  = (double)i / n;
            double hz = 680 * Math.Pow(0.16, t);   // exponential sweep downward
            phase += 2 * Math.PI * hz / Rate;
            double amp = (1.0 - t) * 0.78;
            Write16(buf, i, Math.Sign(Math.Sin(phase)) * amp);
        }
        return buf;
    }

    /// <summary>
    /// Hit / explosion: white noise + 52 Hz sub-thud, 280 ms, sqrt-decay envelope.
    /// The low thud gives a physical "weight" the original square-wave lacked.
    /// </summary>
    private static byte[] SynthExplosion()
    {
        int n = Rate * 280 / 1000;
        var buf = new byte[n * 2];
        var rng = new Random(77);   // fixed seed → same sound every time
        for (int i = 0; i < n; i++)
        {
            double t    = (double)i / n;
            double amp  = Math.Pow(1 - t, 0.45) * 0.88;
            double noise = rng.NextDouble() * 2 - 1;
            double thud  = Math.Sin(2 * Math.PI * 52 * i / Rate);
            Write16(buf, i, (noise * 0.58 + thud * 0.42) * amp);
        }
        return buf;
    }

    /// <summary>
    /// Enemy tank destroyed: deep 40 Hz thud + sharp crack at front, 400 ms, sqrt-decay.
    /// Heavier than the bullet-hit explosion to mark a full tank kill.
    /// </summary>
    private static byte[] SynthEnemyDestroyed()
    {
        int n   = Rate * 400 / 1000;
        var buf = new byte[n * 2];
        var rng = new Random(42);
        for (int i = 0; i < n; i++)
        {
            double t     = (double)i / n;
            double amp   = Math.Pow(1 - t, 0.35) * 0.95;
            double noise = rng.NextDouble() * 2 - 1;
            double thud  = Math.Sin(2 * Math.PI * 40 * i / Rate);
            // Brief high crack in first 20 ms
            double crack = i < Rate * 20 / 1000
                           ? (rng.NextDouble() * 2 - 1) * 0.35
                           : 0;
            Write16(buf, i, (noise * 0.45 + thud * 0.55 + crack) * amp);
        }
        return buf;
    }

    /// <summary>
    /// Player hurt: harsh square-wave sweep 700 → 100 Hz, 160 ms, linear decay.
    /// Distinctive alert tone distinct from weapon sounds.
    /// </summary>
    private static byte[] SynthPlayerHurt()
    {
        int n   = Rate * 160 / 1000;
        var buf = new byte[n * 2];
        double phase = 0;
        for (int i = 0; i < n; i++)
        {
            double t   = (double)i / n;
            double hz  = 700 * Math.Pow(700.0 / 100.0, -t);  // 700 → 100 exponential
            phase += 2 * Math.PI * hz / Rate;
            double amp = (1.0 - t) * 0.80;
            Write16(buf, i, Math.Sign(Math.Sin(phase)) * amp);
        }
        return buf;
    }

    /// <summary>
    /// Game-over: descending three-note dirge G4 → E4 → C4 → G3 (~900 ms total).
    /// </summary>
    private static byte[] SynthGameOver()
    {
        // note durations and frequencies
        var pcm = Concat(
            SynthNote(392.0, 200, 0.70),   // G4
            Silence(40),
            SynthNote(329.6, 200, 0.65),   // E4
            Silence(40),
            SynthNote(261.6, 220, 0.60),   // C4
            Silence(40),
            SynthNote(196.0, 300, 0.55)    // G3 — long final note
        );
        return pcm;
    }

    /// <summary>
    /// Victory fanfare: ascending C4 → E4 → G4 → C5 (~700 ms total).
    /// </summary>
    private static byte[] SynthVictory()
    {
        var pcm = Concat(
            SynthNote(261.6, 140, 0.68),   // C4
            Silence(30),
            SynthNote(329.6, 140, 0.72),   // E4
            Silence(30),
            SynthNote(392.0, 140, 0.76),   // G4
            Silence(30),
            SynthNote(523.3, 280, 0.82)    // C5 — long final note
        );
        return pcm;
    }

    /// <summary>Synthesise a single square-wave note with a short cubic-decay envelope.</summary>
    private static byte[] SynthNote(double hz, int ms, double peakAmp)
    {
        int n   = Rate * ms / 1000;
        var buf = new byte[n * 2];
        double phase = 0;
        for (int i = 0; i < n; i++)
        {
            double t = (double)i / n;
            phase += 2 * Math.PI * hz / Rate;
            double amp = peakAmp * Math.Pow(1 - t, 1.5);
            Write16(buf, i, Math.Sign(Math.Sin(phase)) * amp);
        }
        return buf;
    }

    /// <summary>Returns a silent PCM buffer of the given duration.</summary>
    private static byte[] Silence(int ms)
        => new byte[Rate * ms / 1000 * 2];

    /// <summary>Concatenates raw PCM byte arrays in order.</summary>
    private static byte[] Concat(params byte[][] parts)
    {
        int total = 0;
        foreach (var p in parts) total += p.Length;
        var result = new byte[total];
        int offset = 0;
        foreach (var p in parts) { Buffer.BlockCopy(p, 0, result, offset, p.Length); offset += p.Length; }
        return result;
    }

    /// <summary>
    /// Power-up collected: bright ascending two-tone chime, 180 ms.
    /// Two quick square notes (C5 → E5) with sharp attack and fast decay.
    /// </summary>
    private static byte[] SynthPowerUp()
    {
        var pcm = Concat(
            SynthNote(523.3, 75, 0.75),   // C5
            Silence(15),
            SynthNote(659.3, 90, 0.80)    // E5
        );
        return pcm;
    }

    /// <summary>
    /// Engine rumble: 90 Hz fundamental + 3 harmonics, seamlessly loopable.
    /// 22 050 / 90 = 245 samples per cycle exactly → 100 cycles = 24 500 samples,
    /// zero phase error at the loop point.
    /// </summary>
    private static byte[] SynthMovement()
    {
        int cycleLen = Rate / 90;   // = 245 (exact integer, no rounding)
        int n        = cycleLen * 100;
        var buf      = new byte[n * 2];
        var rng      = new Random(13);
        for (int i = 0; i < n; i++)
        {
            double ph = 2 * Math.PI * 90 * i / Rate;
            double engine = Math.Sin(ph)     * 0.50
                          + Math.Sin(2 * ph) * 0.26
                          + Math.Sin(3 * ph) * 0.14
                          + Math.Sin(4 * ph) * 0.10;
            double noise  = (rng.NextDouble() * 2 - 1) * 0.055;
            Write16(buf, i, (engine + noise) * 0.48);
        }
        return buf;
    }

    // ── PCM / WAV helpers ─────────────────────────────────────────────────────

    /// <summary>Write a normalised sample [-1 .. 1] as a 16-bit LE integer into buf.</summary>
    private static void Write16(byte[] buf, int i, double sample)
    {
        short s = (short)Math.Clamp(sample * 28_000, -32_768, 32_767);
        buf[i * 2]     = (byte)(s & 0xFF);
        buf[i * 2 + 1] = (byte)(s >> 8);
    }

    /// <summary>Wrap raw 16-bit mono PCM in a minimal RIFF/WAV container.</summary>
    private static byte[] WrapWav(byte[] pcm16)
    {
        using var ms = new MemoryStream(44 + pcm16.Length);
        using var w  = new BinaryWriter(ms);
        w.Write("RIFF"u8);
        w.Write(36 + pcm16.Length);
        w.Write("WAVE"u8);
        w.Write("fmt "u8);
        w.Write(16);            // PCM fmt chunk is always 16 bytes
        w.Write((short)1);      // wFormatTag  = PCM
        w.Write((short)1);      // nChannels   = mono
        w.Write(Rate);          // nSamplesPerSec
        w.Write(Rate * 2);      // nAvgBytesPerSec (16-bit mono → rate × 2)
        w.Write((short)2);      // nBlockAlign
        w.Write((short)16);     // wBitsPerSample
        w.Write("data"u8);
        w.Write(pcm16.Length);
        w.Write(pcm16);
        return ms.ToArray();
    }

    // ── winmm.dll PlaySound wrapper ───────────────────────────────────────────

    private static class WinMM
    {
        public const uint SND_ASYNC     = 0x0001;
        public const uint SND_NODEFAULT = 0x0002;
        public const uint SND_MEMORY    = 0x0004;

        [DllImport("winmm.dll")]
        public static extern bool PlaySound(byte[] pszSound, nint hmod, uint fdwSound);
    }

    // ── waveOut movement loop (independent audio channel) ────────────────────

    /// <summary>
    /// Manages a persistent waveOut stream for seamless looping playback.
    /// Lives completely independently of the PlaySound channel so that
    /// clicks / shots / explosions never interrupt the engine rumble.
    /// </summary>
    private static class MoveLoop
    {
        // WAVEHDR flags
        private const uint WHDR_BEGINLOOP = 0x04;
        private const uint WHDR_ENDLOOP   = 0x08;

        // waveOut P/Invoke declarations
        [DllImport("winmm.dll")] static extern uint waveOutOpen(out nint hwo, uint dev, ref WAVEFORMATEX fmt, nint cb, nint inst, uint flags);
        [DllImport("winmm.dll")] static extern uint waveOutPrepareHeader(nint hwo, ref WAVEHDR hdr, uint sz);
        [DllImport("winmm.dll")] static extern uint waveOutWrite(nint hwo, ref WAVEHDR hdr, uint sz);
        [DllImport("winmm.dll")] static extern uint waveOutReset(nint hwo);
        [DllImport("winmm.dll")] static extern uint waveOutUnprepareHeader(nint hwo, ref WAVEHDR hdr, uint sz);
        [DllImport("winmm.dll")] static extern uint waveOutClose(nint hwo);

        [StructLayout(LayoutKind.Sequential)]
        private struct WAVEFORMATEX
        {
            public ushort wFormatTag, nChannels;
            public uint   nSamplesPerSec, nAvgBytesPerSec;
            public ushort nBlockAlign, wBitsPerSample, cbSize;
        }

        // WAVEHDR is a static field so it never moves in memory — safe for async P/Invoke.
        [StructLayout(LayoutKind.Sequential)]
        private struct WAVEHDR
        {
            public nint  lpData;
            public uint  dwBufferLength, dwBytesRecorded;
            public nint  dwUser;
            public uint  dwFlags, dwLoops;
            public nint  lpNext, reserved;
        }

        private static nint    _handle;
        private static GCHandle _pin;
        private static WAVEHDR  _hdr;                    // static → GC-stable address
        private static readonly object _lock = new();

        public static void Start(byte[] pcm)
        {
            lock (_lock)
            {
                if (_handle != 0) return;   // already running
                try
                {
                    var fmt = new WAVEFORMATEX
                    {
                        wFormatTag      = 1,             // PCM
                        nChannels       = 1,
                        nSamplesPerSec  = Rate,
                        nAvgBytesPerSec = (uint)(Rate * 2),
                        nBlockAlign     = 2,
                        wBitsPerSample  = 16,
                    };

                    if (waveOutOpen(out _handle, unchecked((uint)-1) /*WAVE_MAPPER*/, ref fmt, 0, 0, 0) != 0)
                        return;

                    // Pin the PCM array so the GC won't move it during playback
                    _pin = GCHandle.Alloc(pcm, GCHandleType.Pinned);
                    _hdr = new WAVEHDR
                    {
                        lpData         = _pin.AddrOfPinnedObject(),
                        dwBufferLength = (uint)pcm.Length,
                        dwFlags        = WHDR_BEGINLOOP | WHDR_ENDLOOP,
                        dwLoops        = uint.MaxValue,   // ~136 years at 1.1 s/loop
                    };

                    uint sz = (uint)Marshal.SizeOf<WAVEHDR>();
                    if (waveOutPrepareHeader(_handle, ref _hdr, sz) != 0) { Cleanup(); return; }
                    if (waveOutWrite(_handle, ref _hdr, sz) != 0)         { Cleanup(); return; }
                }
                catch { Cleanup(); }
            }
        }

        public static void Stop()
        {
            lock (_lock) { Cleanup(); }
        }

        private static void Cleanup()
        {
            if (_handle == 0) return;
            try
            {
                uint sz = (uint)Marshal.SizeOf<WAVEHDR>();
                waveOutReset(_handle);
                waveOutUnprepareHeader(_handle, ref _hdr, sz);
                waveOutClose(_handle);
            }
            catch { }
            finally
            {
                _handle = 0;
                if (_pin.IsAllocated) _pin.Free();
            }
        }
    }
}
