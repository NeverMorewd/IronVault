/**
 * IronVault Touch Controls
 *
 * Virtual joystick (left) + FIRE button (right) for mobile browsers.
 *
 * INPUT: Calls window.IronVaultInput.setMove / setFire ([JSExport] C# methods)
 *        directly — bypasses synthesized KeyboardEvents which are ignored by
 *        Avalonia (isTrusted = false filter).
 *
 * LAYOUT: Toggles body.iv-game which shrinks #out via CSS var(--ctrl-h) so
 *         the Avalonia canvas never renders behind the controls.
 *
 * AUX BUTTONS (START / PAUSE / MENU): dispatch KeyboardEvents as a best-
 *        effort fallback for one-shot actions (not held movement).
 */
(function () {
    'use strict';

    // ── Guard: desktop only shows controls if URL param ?touch=1 ─────────
    const forceTouch = new URLSearchParams(location.search).has('touch');
    const hasTouch   = forceTouch || ('ontouchstart' in window) || (navigator.maxTouchPoints > 0);
    if (!hasTouch) return;

    /* ── Aux key simulation (one-shot actions only) ──────────────────────
     * Movement and fire use [JSExport] interop instead (see IronVaultInput).
     * Aux keys are dispatched to window so they reach Avalonia's global
     * keyboard handler regardless of focused element.                      */
    function dispatchKey(type, code, key, keyCode) {
        window.dispatchEvent(new KeyboardEvent(type, {
            code, key, keyCode, which: keyCode, bubbles: true, cancelable: true
        }));
    }
    function tapKey(code, key, keyCode) {
        dispatchKey('keydown', code, key, keyCode);
        setTimeout(() => dispatchKey('keyup', code, key, keyCode), 80);
    }

    /* ── Joystick direction state ────────────────────────────────────────
     * Tracks active dirs so we can release them on screen change / blur.  */
    let _activeDirs = new Set();

    function setDirs(nextArr) {
        _activeDirs = new Set(nextArr);
        const inp = window.IronVaultInput;
        if (inp) inp.setMove(
            _activeDirs.has('up'),
            _activeDirs.has('down'),
            _activeDirs.has('left'),
            _activeDirs.has('right')
        );
    }

    /**
     * Map joystick displacement → direction names (8-way).
     * Screen coords: Y grows downward, so dy > 0 → "down".
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

    /* ── Inject CSS ──────────────────────────────────────────────────────*/
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
            width: 140px;
            height: 140px;
            border-radius: 50%;
            background: rgba(8, 6, 0, 0.62);
            border: 2px solid rgba(255, 165, 0, 0.32);
            box-shadow:
                0 0 24px rgba(255, 140, 0, 0.10),
                inset 0 0 28px rgba(0, 0, 0, 0.55);
            pointer-events: all;
            touch-action: none;
        }

        /* ── Joystick knob ── */
        #iv-stick-knob {
            position: absolute;
            width: 54px;
            height: 54px;
            border-radius: 50%;
            background: radial-gradient(
                circle at 36% 32%,
                rgba(255, 195, 60, 0.92),
                rgba(170, 90, 0, 0.80)
            );
            border: 1.5px solid rgba(255, 165, 0, 0.55);
            box-shadow: 0 0 14px rgba(255, 165, 0, 0.40);
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
            width: 100px;
            height: 100px;
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
                0 0 38px rgba(255, 140, 0, 0.65),
                inset 0 0 8px rgba(0, 0, 0, 0.20);
        }

        /* ── Auxiliary buttons ── */
        .iv-aux {
            position: absolute;
            height: 32px;
            padding: 0 12px;
            border-radius: 6px;
            background: rgba(8, 6, 0, 0.65);
            border: 1.5px solid rgba(255, 165, 0, 0.28);
            display: flex;
            align-items: center;
            justify-content: center;
            font-family: Consolas, 'Courier New', monospace;
            font-size: 9px;
            letter-spacing: 0.10em;
            color: rgba(255, 165, 0, 0.65);
            text-shadow: 0 0 6px rgba(255, 165, 0, 0.30);
            pointer-events: all;
            touch-action: none;
            cursor: pointer;
            transition: border-color 60ms, color 60ms;
            white-space: nowrap;
        }
        .iv-aux.iv-active {
            border-color: rgba(255, 165, 0, 0.65);
            color: rgba(255, 220, 120, 0.95);
        }

        #iv-start-btn { bottom: 180px; left:  38px; }
        #iv-pause-btn { bottom: 180px; right: 146px; }
        #iv-esc-btn   { top:    16px;  right:  16px; }

        /* ── Safe-area padding for notched phones ── */
        @supports (padding: env(safe-area-inset-bottom)) {
            #iv-stick-base { bottom: calc(24px  + env(safe-area-inset-bottom)); }
            #iv-fire-btn   { bottom: calc(36px  + env(safe-area-inset-bottom)); }
            #iv-start-btn  { bottom: calc(180px + env(safe-area-inset-bottom)); }
            #iv-pause-btn  { bottom: calc(180px + env(safe-area-inset-bottom)); }
        }
    `;
    document.head.appendChild(css);

    /* ── Build overlay DOM ───────────────────────────────────────────────*/
    function mkEl(tag, id, cls) {
        const e = document.createElement(tag);
        if (id)  e.id        = id;
        if (cls) e.className = cls;
        return e;
    }

    const overlay   = mkEl('div', 'iv-overlay');
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

    /* ── Hidden until the game screen is active ──────────────────────────*/
    overlay.style.display = 'none';

    /* ── Public screen API ───────────────────────────────────────────────
     * Called from BrowserNavBridge.cs via [JSImport] on every navigation. */
    window.IronVaultControls = {
        setScreen(screen) {
            const isGame = screen === 'game';

            // Show / hide controls overlay
            overlay.style.display = isGame ? '' : 'none';

            // Toggle body class → CSS adjusts #out bottom via --ctrl-h
            if (isGame) {
                document.body.classList.add('iv-game');
            } else {
                document.body.classList.remove('iv-game');
            }

            // Release all touch inputs when leaving the game
            if (!isGame) {
                setDirs([]);
                _firePids.clear();
                fireBtn.classList.remove('iv-active');
                const inp = window.IronVaultInput;
                if (inp) inp.releaseAll();
            }
        }
    };

    /* ── Joystick pointer handling ───────────────────────────────────────*/
    const BASE_R   = 70;    // half of stick base (140/2)
    const DEADZONE = 14;    // px before directions register
    const KNOB_MAX = BASE_R - 27; // max knob travel from center

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
        _stickOx  = rc.left + rc.width  / 2;
        _stickOy  = rc.top  + rc.height / 2;
        e.preventDefault();
    }, { passive: false });

    stickBase.addEventListener('pointermove', e => {
        if (e.pointerId !== _stickPid) return;
        const dx   = e.clientX - _stickOx;
        const dy   = e.clientY - _stickOy;
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

    /* ── Fire button ─────────────────────────────────────────────────────*/
    const _firePids = new Set();

    fireBtn.addEventListener('pointerdown', e => {
        fireBtn.setPointerCapture(e.pointerId);
        _firePids.add(e.pointerId);
        fireBtn.classList.add('iv-active');
        const inp = window.IronVaultInput;
        if (inp) inp.setFire(true);
        e.preventDefault();
    }, { passive: false });

    function fireEnd(e) {
        _firePids.delete(e.pointerId);
        if (_firePids.size === 0) {
            fireBtn.classList.remove('iv-active');
            const inp = window.IronVaultInput;
            if (inp) inp.setFire(false);
        }
    }
    fireBtn.addEventListener('pointerup',     fireEnd);
    fireBtn.addEventListener('pointercancel', fireEnd);

    /* ── Auxiliary buttons (best-effort keyboard dispatch) ───────────────*/
    function hookAux(btn, code, key, keyCode) {
        btn.addEventListener('pointerdown', e => {
            btn.setPointerCapture(e.pointerId);
            btn.classList.add('iv-active');
            tapKey(code, key, keyCode);
            e.preventDefault();
        }, { passive: false });
        const up = () => btn.classList.remove('iv-active');
        btn.addEventListener('pointerup',     up);
        btn.addEventListener('pointercancel', up);
    }

    hookAux(startBtn, 'Enter',  'Enter',  13);
    hookAux(pauseBtn, 'KeyP',   'p',      80);
    hookAux(escBtn,   'Escape', 'Escape', 27);

    /* ── Global guards ───────────────────────────────────────────────────*/
    // Prevent page scroll when touching the overlay
    overlay.addEventListener('touchmove', e => e.preventDefault(), { passive: false });

    // Release all inputs when app goes to background
    document.addEventListener('visibilitychange', () => {
        if (document.hidden) {
            setDirs([]);
            _firePids.clear();
            fireBtn.classList.remove('iv-active');
            const inp = window.IronVaultInput;
            if (inp) inp.releaseAll();
        }
    });

})();
