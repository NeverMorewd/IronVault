/**
 * IronVault Touch Controls — visual overlay only.
 *
 * This file renders the joystick and FIRE button visuals on mobile.
 * ALL pointer input is handled by Avalonia's C# pointer-event handlers
 * in GameView.axaml.cs, so the HTML overlay is pointer-events:none
 * throughout — no double-tap-zoom, no event conflicts.
 *
 * C# calls window.IronVaultControls.setScreen('game'|'menu') via
 * [JSImport] in BrowserNavBridge.cs to show/hide the overlay.
 */
(function () {
    'use strict';

    // Show controls on touch-capable devices (or ?touch=1 for debug)
    const hasTouch = ('ontouchstart' in window) || (navigator.maxTouchPoints > 0)
                  || new URLSearchParams(location.search).has('touch');
    if (!hasTouch) return;

    /* ── Styles ──────────────────────────────────────────────────────────── */
    const css = document.createElement('style');
    css.textContent = `
        /* ── Overlay shell: covers full viewport, never intercepts input ── */
        #iv-overlay {
            position: fixed;
            inset: 0;
            pointer-events: none;   /* ALL touches fall through to Avalonia */
            z-index: 500;
            user-select: none;
            -webkit-user-select: none;
        }

        /* ── Joystick base ── */
        #iv-stick-base {
            position: absolute;
            bottom: 24px;
            left: 24px;
            width: 140px;
            height: 140px;
            border-radius: 50%;
            background: rgba(8, 6, 0, 0.55);
            border: 2px solid rgba(255, 165, 0, 0.28);
            box-shadow:
                0 0 22px rgba(255, 140, 0, 0.08),
                inset 0 0 28px rgba(0, 0, 0, 0.50);
        }
        /* Cardinal hint arrows */
        #iv-stick-base::before { content: '▲'; position: absolute; top: 5px; left: 50%;
            transform: translateX(-50%); font-size: 9px; color: rgba(255,165,0,0.20); }
        #iv-stick-base::after  { content: '▼'; position: absolute; bottom: 5px; left: 50%;
            transform: translateX(-50%); font-size: 9px; color: rgba(255,165,0,0.20); }

        /* ── Joystick knob (static centre — C# handles movement) ── */
        #iv-stick-knob {
            position: absolute;
            width: 54px;
            height: 54px;
            border-radius: 50%;
            background: radial-gradient(
                circle at 36% 32%,
                rgba(255, 195, 60, 0.88),
                rgba(165, 85, 0, 0.78)
            );
            border: 1.5px solid rgba(255, 165, 0, 0.50);
            box-shadow: 0 0 12px rgba(255, 165, 0, 0.35);
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
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
                rgba(255, 110, 0, 0.88),
                rgba(135, 28, 0, 0.84)
            );
            border: 2px solid rgba(255, 90, 0, 0.60);
            box-shadow:
                0 0 22px rgba(255, 80, 0, 0.28),
                inset 0 0 16px rgba(0, 0, 0, 0.35);
            display: flex;
            align-items: center;
            justify-content: center;
            font-family: Consolas, 'Courier New', monospace;
            font-size: 14px;
            font-weight: bold;
            letter-spacing: 0.12em;
            color: rgba(255, 235, 195, 0.92);
            text-shadow: 0 0 8px rgba(255, 160, 0, 0.80);
        }

        /* ── Safe-area insets for notched phones ── */
        @supports (padding: env(safe-area-inset-bottom)) {
            #iv-stick-base { bottom: calc(24px + env(safe-area-inset-bottom)); }
            #iv-fire-btn   { bottom: calc(36px + env(safe-area-inset-bottom)); }
        }
    `;
    document.head.appendChild(css);

    /* ── Build DOM ───────────────────────────────────────────────────────── */
    const overlay   = document.createElement('div'); overlay.id = 'iv-overlay';
    const stickBase = document.createElement('div'); stickBase.id = 'iv-stick-base';
    const stickKnob = document.createElement('div'); stickKnob.id = 'iv-stick-knob';
    const fireBtn   = document.createElement('div'); fireBtn.id   = 'iv-fire-btn';

    fireBtn.textContent = 'FIRE';
    stickBase.appendChild(stickKnob);
    overlay.append(stickBase, fireBtn);
    document.body.appendChild(overlay);

    /* ── Hidden until C# navigates to game screen ────────────────────────── */
    overlay.style.display = 'none';

    /* ── Public API (called from BrowserNavBridge.cs via [JSImport]) ──────── */
    window.IronVaultControls = {
        /**
         * Show or hide the overlay.
         * Called by BrowserNavBridge whenever the active AppScreen changes.
         * @param {string} screen  'game' | 'menu'
         */
        setScreen(screen) {
            overlay.style.display = (screen === 'game') ? '' : 'none';
        },
    };

})();
