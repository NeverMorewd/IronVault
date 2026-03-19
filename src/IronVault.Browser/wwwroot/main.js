import { dotnet } from './_framework/dotnet.js'

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

const dotnetRuntime = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

const config = dotnetRuntime.getConfig();

// ── Expose C# touch-input bridge to JavaScript ────────────────────────────
// BrowserInput.[JSExport] methods are accessible once the runtime is created.
// touch-controls.js calls window.IronVaultInput.setMove / setFire directly,
// bypassing unreliable synthesized KeyboardEvents (isTrusted = false).
try {
    const asm = await dotnetRuntime.getAssemblyExports('IronVault.Browser');
    const bi  = asm.IronVault.Browser.Input.BrowserInput;
    window.IronVaultInput = {
        setMove:    (u, d, l, r) => bi.SetMove(u, d, l, r),
        setFire:    (f)          => bi.SetFire(f),
        releaseAll: ()           => bi.ReleaseAll(),
    };
} catch (ex) {
    console.warn('[IronVault] Touch input bridge unavailable:', ex);
}

const runPromise = dotnetRuntime.runMain(config.mainAssemblyName, [globalThis.location.href]);

// Hide splash overlay once Avalonia has had time to render its first frame
requestAnimationFrame(() => requestAnimationFrame(() => {
    const splash = document.querySelector('.avalonia-splash');
    if (splash) splash.classList.add('loaded');
}));

await runPromise;
