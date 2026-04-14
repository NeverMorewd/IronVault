$PROJECT="../src/IronVault.Browser/IronVault.Browser.csproj"
$OUTPUT="publish"

dotnet publish $PROJECT `
  -c Release `
  -f net10.0-browser `
  -o $OUTPUT `
  -p:RunAOTCompilation=true `
  -p:WasmBuildNative=true `
  -p:PublishTrimmed=true `
  -p:TrimMode=full `
  -p:InvariantGlobalization=true `
  -p:IlcGenerateStackTraceData=false `
  -p:IlcOptimizationPreference=Speed `
  -p:DebuggerSupport=false

Write-Host "✅ AOT publish completed -> $OUTPUT"