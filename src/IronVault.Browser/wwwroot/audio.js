/**
 * IronVault Web Audio synthesizer — mirrors the PCM synthesis in RetroSound.cs.
 * All sounds are generated procedurally; no audio files required.
 */
window.IronVaultAudio = (() => {
    let ctx = null;
    let movGain = null;
    let movOscs = [];

    function getCtx() {
        if (!ctx) ctx = new (window.AudioContext || window.webkitAudioContext)();
        if (ctx.state === 'suspended') ctx.resume();
        return ctx;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /** Play a square-wave frequency sweep from startHz → endHz over durationSec. */
    function playFreqSweep(startHz, endHz, durationSec, peakGain) {
        const actx = getCtx();
        const osc  = actx.createOscillator();
        const gain = actx.createGain();
        osc.connect(gain);
        gain.connect(actx.destination);
        osc.type = 'square';
        osc.frequency.setValueAtTime(startHz, actx.currentTime);
        osc.frequency.exponentialRampToValueAtTime(Math.max(endHz, 1), actx.currentTime + durationSec);
        gain.gain.setValueAtTime(peakGain, actx.currentTime);
        gain.gain.exponentialRampToValueAtTime(0.0001, actx.currentTime + durationSec);
        osc.start(actx.currentTime);
        osc.stop(actx.currentTime + durationSec);
    }

    /** Play a single square-wave note with cubic-decay envelope. */
    function playNote(hz, durationSec, peakGain) {
        const actx = getCtx();
        const osc  = actx.createOscillator();
        const gain = actx.createGain();
        osc.connect(gain);
        gain.connect(actx.destination);
        osc.type = 'square';
        osc.frequency.value = hz;
        gain.gain.setValueAtTime(peakGain, actx.currentTime);
        gain.gain.setTargetAtTime(0, actx.currentTime, durationSec / 3);
        osc.start(actx.currentTime);
        osc.stop(actx.currentTime + durationSec + 0.05);
    }

    /** Play a note at a specific future time (for sequencing). */
    function scheduleNote(hz, startTime, durationSec, peakGain) {
        const actx = getCtx();
        const osc  = actx.createOscillator();
        const gain = actx.createGain();
        osc.connect(gain);
        gain.connect(actx.destination);
        osc.type = 'square';
        osc.frequency.value = hz;
        gain.gain.setValueAtTime(0.0001, startTime);
        gain.gain.linearRampToValueAtTime(peakGain, startTime + 0.005);
        gain.gain.setTargetAtTime(0, startTime + 0.01, durationSec / 3);
        osc.start(startTime);
        osc.stop(startTime + durationSec + 0.05);
    }

    /** White-noise burst via AudioBuffer, starting at an optional scheduled time. */
    function playNoiseAt(startTime, durationSec, peakGain, lowHz) {
        const actx       = getCtx();
        const sampleRate = actx.sampleRate;
        const length     = Math.floor(sampleRate * durationSec);
        const buffer     = actx.createBuffer(1, length, sampleRate);
        const data       = buffer.getChannelData(0);
        for (let i = 0; i < length; i++) data[i] = Math.random() * 2 - 1;

        const src  = actx.createBufferSource();
        src.buffer = buffer;

        // Low-frequency thud underneath the noise
        const osc  = actx.createOscillator();
        const mix  = actx.createGain();
        const gain = actx.createGain();
        osc.type            = 'sine';
        osc.frequency.value = lowHz;
        mix.gain.value      = 0.45;
        gain.gain.setValueAtTime(peakGain, startTime);
        gain.gain.exponentialRampToValueAtTime(0.0001, startTime + durationSec);

        src.connect(mix);
        mix.connect(gain);
        osc.connect(gain);
        gain.connect(actx.destination);

        src.start(startTime);
        osc.start(startTime);
        osc.stop(startTime + durationSec);
    }

    /** White-noise burst (immediate). */
    function playNoise(durationSec, peakGain, lowHz) {
        playNoiseAt(getCtx().currentTime, durationSec, peakGain, lowHz);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /** UI click: 1 kHz square wave, 28 ms */
    function playClick() {
        playFreqSweep(1000, 900, 0.028, 0.25);
    }

    /** Player shoots: 680 → 110 Hz sweep, 95 ms */
    function playShoot() {
        playFreqSweep(680, 110, 0.095, 0.35);
    }

    /** Hit / explosion: noise + 52 Hz thud, 280 ms */
    function playExplosion() {
        playNoise(0.28, 0.50, 52);
    }

    /** Enemy tank destroyed: metallic clang + deep 35 Hz thud + secondary debris boom, 580 ms. */
    function playEnemyDestroyed() {
        const actx = getCtx();
        const t    = actx.currentTime;

        // Main explosion: longer, louder, deeper thud (35 Hz)
        playNoiseAt(t, 0.58, 0.82, 35);

        // Metallic clang: 820 Hz decaying ring
        const clangOsc  = actx.createOscillator();
        const clangGain = actx.createGain();
        clangOsc.connect(clangGain);
        clangGain.connect(actx.destination);
        clangOsc.type = 'sine';
        clangOsc.frequency.setValueAtTime(820, t);
        clangOsc.frequency.exponentialRampToValueAtTime(280, t + 0.028);
        clangGain.gain.setValueAtTime(0.30, t);
        clangGain.gain.exponentialRampToValueAtTime(0.0001, t + 0.030);
        clangOsc.start(t);
        clangOsc.stop(t + 0.032);

        // Secondary debris boom at 185 ms
        playNoiseAt(t + 0.185, 0.08, 0.40, 45);
    }

    /** Player hurt: 700 → 100 Hz square sweep, 160 ms */
    function playPlayerHurt() {
        playFreqSweep(700, 100, 0.16, 0.40);
    }

    /** Game-over: deep opening + descending minor dirge (~1 200 ms). */
    function playGameOver() {
        const actx = getCtx();
        const t    = actx.currentTime;
        scheduleNote(130.8, t + 0.00, 0.09, 0.38);  // C3 — deep boom
        scheduleNote(392.0, t + 0.12, 0.24, 0.48);  // G4
        scheduleNote(329.6, t + 0.40, 0.24, 0.44);  // E4
        scheduleNote(261.6, t + 0.68, 0.26, 0.40);  // C4
        scheduleNote(196.0, t + 0.98, 0.40, 0.36);  // G3 (long)
        scheduleNote(130.8, t + 1.48, 0.22, 0.26);  // C3 — low echo fade
    }

    /** Victory fanfare: 7-note ascending run to triumphant peak (~1 100 ms). */
    function playVictory() {
        const actx = getCtx();
        const t    = actx.currentTime;
        scheduleNote(261.6, t + 0.00, 0.13, 0.42);  // C4
        scheduleNote(329.6, t + 0.15, 0.13, 0.46);  // E4
        scheduleNote(392.0, t + 0.30, 0.13, 0.50);  // G4
        scheduleNote(523.3, t + 0.45, 0.13, 0.55);  // C5
        scheduleNote(659.3, t + 0.60, 0.13, 0.60);  // E5
        scheduleNote(784.0, t + 0.75, 0.32, 0.68);  // G5 — peak
        scheduleNote(523.3, t + 1.10, 0.22, 0.56);  // C5 — resolution
    }

    /** Stage-start: Battle-City-inspired ascending arpeggio + resolution. */
    function playStageStart() {
        const actx = getCtx();
        const t    = actx.currentTime;
        scheduleNote(329.6, t + 0.00, 0.08, 0.44);  // E4
        scheduleNote(392.0, t + 0.09, 0.08, 0.48);  // G4
        scheduleNote(493.9, t + 0.18, 0.08, 0.52);  // B4
        scheduleNote(659.3, t + 0.27, 0.16, 0.60);  // E5 (held)
        scheduleNote(587.3, t + 0.47, 0.08, 0.46);  // D5
        scheduleNote(523.3, t + 0.56, 0.14, 0.54);  // C5 (resolution)
    }

    /**
     * Power-up: different sound per type.
     * typeId mirrors PowerUpType enum: Star=0, BulletSpeed=1, ExtraBullet=2,
     * Shield=3, Clock=4, Shovel=5, Life=6
     */
    function playPowerUp(typeId) {
        const actx = getCtx();
        const t    = actx.currentTime;
        switch (typeId) {
            case 0: // Star — rapid sparkle E5 → G5 → B5 → E6
                scheduleNote( 659.3, t + 0.00, 0.05, 0.44);
                scheduleNote( 784.0, t + 0.06, 0.05, 0.48);
                scheduleNote( 987.8, t + 0.12, 0.05, 0.52);
                scheduleNote(1318.5, t + 0.18, 0.10, 0.58);
                break;
            case 6: // Life — warm ascending C4 → E4 → G4 → C5
                scheduleNote(261.6, t + 0.00, 0.10, 0.40);
                scheduleNote(329.6, t + 0.12, 0.10, 0.44);
                scheduleNote(392.0, t + 0.24, 0.10, 0.48);
                scheduleNote(523.3, t + 0.36, 0.20, 0.56);
                break;
            case 4: // Clock — cool descending C5 → A4 → F4
                scheduleNote(523.3, t + 0.00, 0.09, 0.38);
                scheduleNote(440.0, t + 0.10, 0.09, 0.34);
                scheduleNote(349.2, t + 0.20, 0.12, 0.30);
                break;
            case 3: // Shield — deep resonant G3 → C4
                scheduleNote(196.0, t + 0.00, 0.13, 0.44);
                scheduleNote(261.6, t + 0.14, 0.18, 0.50);
                break;
            default: // BulletSpeed / ExtraBullet / Shovel — default chime
                scheduleNote(523.3, t + 0.00, 0.08, 0.40);  // C5
                scheduleNote(659.3, t + 0.10, 0.10, 0.46);  // E5
                break;
        }
    }

    /** Begin looping engine-rumble: 90 Hz sawtooth + 180 Hz, softly mixed. */
    function startMovement() {
        if (movGain) return;
        const actx = getCtx();
        movGain = actx.createGain();
        movGain.gain.value = 0.12;
        movGain.connect(actx.destination);

        const osc1 = actx.createOscillator();
        osc1.type = 'sawtooth';
        osc1.frequency.value = 90;
        osc1.connect(movGain);
        osc1.start();

        const osc2 = actx.createOscillator();
        osc2.type = 'sine';
        osc2.frequency.value = 180;
        const g2 = actx.createGain();
        g2.gain.value = 0.3;
        osc2.connect(g2);
        g2.connect(movGain);
        osc2.start();

        movOscs = [osc1, osc2];
    }

    /** Stop the engine-rumble loop. */
    function stopMovement() {
        if (!movGain) return;
        try {
            movOscs.forEach(o => o.stop());
            movGain.disconnect();
        } catch (_) {}
        movGain = null;
        movOscs = [];
    }

    return {
        playClick, playShoot, playExplosion, playEnemyDestroyed,
        playPlayerHurt, playGameOver, playVictory, playStageStart,
        playPowerUp, startMovement, stopMovement,
    };
})();
