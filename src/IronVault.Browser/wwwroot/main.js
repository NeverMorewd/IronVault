import { dotnet } from './_framework/dotnet.js'

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

const dotnetRuntime = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

const config = dotnetRuntime.getConfig();

// Start the Avalonia app (never resolves — runs the main event loop).
// Touch input is handled entirely by Avalonia C# PointerEvents in
// GameView.axaml.cs; no JS→C# interop bridge is needed for controls.
const runPromise = dotnetRuntime.runMain(config.mainAssemblyName, [globalThis.location.href]);

// Hide the splash screen once Avalonia has rendered its first frame.
requestAnimationFrame(() => requestAnimationFrame(() => {
    const splash = document.querySelector('.avalonia-splash');
    if (splash) splash.classList.add('loaded');
}));

await runPromise;
