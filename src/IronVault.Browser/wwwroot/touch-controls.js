/**
 * IronVault Touch Controls
 * Virtual joystick (left thumb) + FIRE button (right thumb) for mobile browsers.
 * Simulates Arrow-key + Space keyboard events so Avalonia receives standard input.
 */
(function () {
    'use strict';

    // ── Guard: desktop only shows controls if URL param ?touch=1 ─────────
    const forceTouch = new URLSearchParams(location.search).has('touch');
    const hasTouch   = forceTouch || ('ontouchstart' in window) || (navigator.maxTouchPoints > 0);
    if (!hasTouch) return;

    // ── Key simulation ────────────────────────────────────────────────────
    const _held = new Set();

    /** Dispatch a keydown to document (Avalonia Browser listens at document/window). */
    function kd(code, key, keyCode) {
        if (_held.has(code)) return;
        _held.add(code);
        document.dispatchEvent(new KeyboardEvent('keydown', {
            code, key, keyCode, which: keyCode, bubbles: true, cancelable: true
        }));
    }

    /** Dispatch a keyup to document. */
    function ku(code, key, keyCode) {
        if (!_held.has(code)) return;
        _held.delete(code);
        document.dispatchEvent(new KeyboardEvent('keyup', {
            code, key, keyCode, which: keyCode, bubbles: true, cancelable: true
        }));
    }

    // Key descriptor tuples [code, key, keyCode]
    const K = {
        up:    ['ArrowUp',    'ArrowUp',    38],
        down:  ['ArrowDown',  'ArrowDown',  40],
        left:  ['ArrowLeft',  'ArrowLeft',  37],
        right: ['ArrowRight', 'ArrowRight', 39],
        fire:  ['Space',      ' ',          32],
        enter: ['Enter',      'Enter',      13],
        pause: ['KeyP',       'p',          80],
        esc:   ['Escape',     'Escape',     27],
    };

    // ── Joystick direction state ──────────────────────────────────────────
    let _activeDirs = new Set();

    function setDirs(nextArr) {
        const next = new Set(nextArr);
        for (const d of _activeDirs) if (!next.has(d)) ku(...K[d]);
        for (const d of next)        if (!_activeDirs.has(d)) kd(...K[d]);
        _activeDirs = next;
    }

    /**
     * Map a joystick displacement (dx, dy) → array of direction names.
     * Uses 8-way detection: 45° sectors with slight diagonal tolerance.
     *   angle 0° = right, 90° = down (screen coords, Y grows down)
     */
    function dirsFromDelta(dx, dy, dist, deadzone) {
        if (dist < deadzone) return [];
        const a = Math.atan2(dy, dx) * (180 / Math.PI); // -180..180
        const dirs = [];
        if (a > -67.5  && a <  67.5)  dirs.push('right');
        if (a >  22.5  && a < 157.5)  dirs.push('down');
        if (a >  112.5 || a < -112.5) dirs.push('left');
        if (a < -22.5  && a > -157.5) dirs.push('up');
        return dirs;
    }

    // ── Inject styles ─────────────────────────────────────────────────────
    const css = document.createElement('style');
    css.textContent = `
        /* ── Overlay shell ── */
        #iv-overlay {
            position: fixed;
            inset: 0;
            pointer-events: none;
            z-index: 500;
            user-select: none;
            -webkit-user-select: none;
            -webkit-tap-highlight-color: transparent;
        }

        /* ── Joystick base ── */
        #iv-stick-base {
            position: absolute;
            bottom: 24px;
            left: 24px;
            width: 136px;
            height: 136px;
            border-radius: 50%;
            background: rgba(8, 6, 0, 0.60);
            border: 2px solid rgba(255, 165, 0, 0.30);
            box-shadow:
                0 0 24px rgba(255, 140, 0, 0.10),
                inset 0 0 28px rgba(0, 0, 0, 0.55);
            pointer-events: all;
            touch-action: none;
        }
        /* subtle cardinal arrows */
        #iv-stick-base::before {
            content: '▲  ▼';
            position: absolute;
            inset: 0;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 8px;
            color: rgba(255,165,0,0.18);
            letter-spacing: 44px;
            pointer-events: none;
        }

        /* ── Joystick knob ── */
        #iv-stick-knob {
            position: absolute;
            width: 52px;
            height: 52px;
            border-radius: 50%;
            background: radial-gradient(
                circle at 36% 32%,
                rgba(255, 195, 60, 0.92),
                rgba(170, 90, 0, 0.80)
            );
            border: 1.5px solid rgba(255, 165, 0, 0.55);
            box-shadow: 0 0 12px rgba(255, 165, 0, 0.38);
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            pointer-events: none;
            will-change: transform;
        }

        /* ── FIRE button ── */
        #iv-fire-btn {
            position: absolute;
            bottom: 36px;
            right: 28px;
            width: 96px;
            height: 96px;
            border-radius: 50%;
            background: radial-gradient(
                circle at 36% 32%,
                rgba(255, 110, 0, 0.92),
                rgba(140, 30, 0, 0.88)
            );
            border: 2px solid rgba(255, 90, 0, 0.65);
            box-shadow:
                0 0 22px rgba(255, 80, 0, 0.32),
                inset 0 0 16px rgba(0, 0, 0, 0.38);
            display: flex;
            align-items: center;
            justify-content: center;
            font-family: Consolas, 'Courier New', monospace;
            font-size: 14px;
            font-weight: bold;
            letter-spacing: 0.12em;
            color: rgba(255, 235, 195, 0.95);
            text-shadow: 0 0 8px rgba(255, 160, 0, 0.85);
            pointer-events: all;
            touch-action: none;
            cursor: pointer;
            transition: box-shadow 60ms, background 60ms;
        }
        #iv-fire-btn.iv-active {
            background: radial-gradient(
                circle at 36% 32%,
                rgba(255, 210, 100, 0.97),
                rgba(200, 70, 0, 0.92)
            );
            box-shadow:
                0 0 36px rgba(255, 140, 0, 0.60),
                inset 0 0 8px rgba(0, 0, 0, 0.20);
        }

        /* ── Auxiliary buttons (START / PAUSE / MENU) ── */
        .iv-aux {
            position: absolute;
            width: 52px;
            height: 28px;
            border-radius: 6px;
            background: rgba(8, 6, 0, 0.62);
            border: 1.5px solid rgba(255, 165, 0, 0.28);
            display: flex;
            align-items: center;
            justify-content: center;
            font-family: Consolas, 'Courier New', monospace;
            font-size: 8px;
            letter-spacing: 0.10em;
            color: rgba(255, 165, 0, 0.65);
            text-shadow: 0 0 6px rgba(255,165,0,0.35);
            pointer-events: all;
            touch-action: none;
            cursor: pointer;
            transition: border-color 60ms, color 60ms;
        }
        .iv-aux.iv-active {
            border-color: rgba(255, 165, 0, 0.65);
            color: rgba(255, 220, 120, 0.95);
        }

        #iv-start-btn  { bottom: 172px; left: 42px; }
        #iv-pause-btn  { bottom: 172px; right: 74px; }
        #iv-esc-btn    { top: 18px;    right: 18px; }

        /* ── Safe area padding for notched phones ── */
        @supports (padding: env(safe-area-inset-bottom)) {
            #iv-stick-base { bottom: calc(24px + env(safe-area-inset-bottom)); }
            #iv-fire-btn   { bottom: calc(36px + env(safe-area-inset-bottom)); }
            #iv-start-btn  { bottom: calc(172px + env(safe-area-inset-bottom)); }
            #iv-pause-btn  { bottom: calc(172px + env(safe-area-inset-bottom)); }
        }
    `;
    document.head.appendChild(css);

    // ── Build overlay DOM ─────────────────────────────────────────────────
    function mkEl(tag, id, cls) {
        const e = document.createElement(tag);
        if (id)  e.id = id;
        if (cls) e.className = cls;
        return e;
    }

    const overlay  = mkEl('div', 'iv-overlay');
    const stickBase = mkEl('div', 'iv-stick-base');
    const stickKnob = mkEl('div', 'iv-stick-knob');
    const fireBtn   = mkEl('div', 'iv-fire-btn');
    const startBtn  = mkEl('div', 'iv-start-btn', 'iv-aux');
    const pauseBtn  = mkEl('div', 'iv-pause-btn', 'iv-aux');
    const escBtn    = mkEl('div', 'iv-esc-btn',   'iv-aux');

    fireBtn.textContent  = 'FIRE';
    startBtn.textContent = 'START';
    pauseBtn.textContent = 'PAUSE';
    escBtn.textContent   = 'MENU';

    stickBase.appendChild(stickKnob);
    overlay.append(stickBase, fireBtn, startBtn, pauseBtn, escBtn);
    document.body.appendChild(overlay);

    // ── Joystick pointer handling ─────────────────────────────────────────
    const BASE_R  = 68;   // radius of stick base (136/2)
    const DEADZONE = 14;  // px before directions register
    const KNOB_MAX = BASE_R - 26; // max knob travel from center

    let _stickPid = null;
    let _stickOx  = 0, _stickOy = 0;

    function knobTo(dx, dy, dist) {
        const r = Math.min(dist, KNOB_MAX);
        const a = Math.atan2(dy, dx);
        stickKnob.style.transform =
            `translate(calc(-50% + ${(Math.cos(a) * r).toFixed(1)}px), ` +
            `calc(-50% + ${(Math.sin(a) * r).toFixed(1)}px))`;
    }

    stickBase.addEventListener('pointerdown', e => {
        if (_stickPid !== null) return;
        _stickPid = e.pointerId;
        stickBase.setPointerCapture(e.pointerId);
        const rc = stickBase.getBoundingClientRect();
        _stickOx = rc.left + rc.width  / 2;
        _stickOy = rc.top  + rc.height / 2;
        e.preventDefault();
    }, { passive: false });

    stickBase.addEventListener('pointermove', e => {
        if (e.pointerId !== _stickPid) return;
        const dx = e.clientX - _stickOx;
        const dy = e.clientY - _stickOy;
        const dist = Math.hypot(dx, dy);
        knobTo(dx, dy, dist);
        setDirs(dirsFromDelta(dx, dy, dist, DEADZONE));
        e.preventDefault();
    }, { passive: false });

    function stickEnd(e) {
        if (e.pointerId !== _stickPid) return;
        _stickPid = null;
        stickKnob.style.transform = 'translate(-50%, -50%)';
        setDirs([]);
    }
    stickBase.addEventListener('pointerup',     stickEnd);
    stickBase.addEventListener('pointercancel', stickEnd);

    // ── Fire button ───────────────────────────────────────────────────────
    let _firePids = new Set();

    fireBtn.addEventListener('pointerdown', e => {
        fireBtn.setPointerCapture(e.pointerId);
        _firePids.add(e.pointerId);
        fireBtn.classList.add('iv-active');
        kd(...K.fire);
        e.preventDefault();
    }, { passive: false });

    function fireEnd(e) {
        _firePids.delete(e.pointerId);
        if (_firePids.size === 0) {
            fireBtn.classList.remove('iv-active');
            ku(...K.fire);
        }
    }
    fireBtn.addEventListener('pointerup',     fireEnd);
    fireBtn.addEventListener('pointercancel', fireEnd);

    // ── Auxiliary buttons ─────────────────────────────────────────────────
    function hookAux(btn, keyTuple) {
        btn.addEventListener('pointerdown', e => {
            btn.setPointerCapture(e.pointerId);
            btn.classList.add('iv-active');
            kd(...keyTuple);
            e.preventDefault();
        }, { passive: false });
        const up = e => { btn.classList.remove('iv-active'); ku(...keyTuple); };
        btn.addEventListener('pointerup',     up);
        btn.addEventListener('pointercancel', up);
    }

    hookAux(startBtn, K.enter);
    hookAux(pauseBtn, K.pause);
    hookAux(escBtn,   K.esc);

    // ── Prevent accidental page scroll on controls ────────────────────────
    overlay.addEventListener('touchmove', e => e.preventDefault(), { passive: false });

    // ── Release all held keys on visibility change (app switch, etc.) ─────
    document.addEventListener('visibilitychange', () => {
        if (document.hidden) {
            setDirs([]);
            ku(...K.fire);
            ku(...K.enter);
            ku(...K.pause);
        }
    });

})();
