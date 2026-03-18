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

    /** White-noise burst via AudioBuffer. */
    function playNoise(durationSec, peakGain, lowHz) {
        const actx      = getCtx();
        const sampleRate = actx.sampleRate;
        const length     = Math.floor(sampleRate * durationSec);
        const buffer     = actx.createBuffer(1, length, sampleRate);
        const data       = buffer.getChannelData(0);
        for (let i = 0; i < length; i++) data[i] = (Math.random() * 2 - 1);

        const src  = actx.createBufferSource();
        src.buffer = buffer;

        // Low-frequency thud underneath the noise
        const osc  = actx.createOscillator();
        const mix  = actx.createGain();
        const gain = actx.createGain();
        osc.type           = 'sine';
        osc.frequency.value = lowHz;
        mix.gain.value     = 0.45;   // noise share
        gain.gain.setValueAtTime(peakGain, actx.currentTime);
        gain.gain.exponentialRampToValueAtTime(0.0001, actx.currentTime + durationSec);

        src.connect(mix);
        mix.connect(gain);
        osc.connect(gain);
        gain.connect(actx.destination);

        src.start(actx.currentTime);
        osc.start(actx.currentTime);
        osc.stop(actx.currentTime + durationSec);
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

    /** Enemy tank destroyed: heavier noise + 40 Hz thud, 400 ms */
    function playEnemyDestroyed() {
        playNoise(0.40, 0.65, 40);
    }

    /** Player hurt: 700 → 100 Hz square sweep, 160 ms */
    function playPlayerHurt() {
        playFreqSweep(700, 100, 0.16, 0.40);
    }

    /** Game-over: G4 → E4 → C4 → G3 descending dirge */
    function playGameOver() {
        const actx = getCtx();
        const t    = actx.currentTime;
        scheduleNote(392.0, t + 0.00, 0.22, 0.40);  // G4
        scheduleNote(329.6, t + 0.24, 0.22, 0.37);  // E4
        scheduleNote(261.6, t + 0.48, 0.24, 0.34);  // C4
        scheduleNote(196.0, t + 0.74, 0.32, 0.30);  // G3
    }

    /** Victory fanfare: C4 → E4 → G4 → C5 ascending */
    function playVictory() {
        const actx = getCtx();
        const t    = actx.currentTime;
        scheduleNote(261.6, t + 0.00, 0.15, 0.40);  // C4
        scheduleNote(329.6, t + 0.17, 0.15, 0.42);  // E4
        scheduleNote(392.0, t + 0.34, 0.15, 0.45);  // G4
        scheduleNote(523.3, t + 0.51, 0.30, 0.50);  // C5
    }

    /** Power-up: bright C5 → E5 chime, 180 ms */
    function playPowerUp() {
        const actx = getCtx();
        const t    = actx.currentTime;
        scheduleNote(523.3, t + 0.00, 0.08, 0.40);  // C5
        scheduleNote(659.3, t + 0.10, 0.10, 0.45);  // E5
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
        playPlayerHurt, playGameOver, playVictory, playPowerUp,
        startMovement, stopMovement,
    };
})();
