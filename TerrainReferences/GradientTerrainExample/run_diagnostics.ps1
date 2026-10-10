$ErrorActionPreference = 'Stop'
$experiment = $PSScriptRoot
$project = Split-Path (Split-Path $experiment)
$python = 'C:/Users/Guanhao/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$src = Join-Path $experiment 'upstream/eric-guerin-gradient-terrains-68da08b'
$compiler = Join-Path $project 'Library/GradientTerrainExampleTools/toolchain/w64devkit/bin/g++.exe'
& $python "$experiment/prepare_diagnostics.py"
& $compiler -O2 -std=c++11 -DGLFW_DLL "-I$src/dependency/include" "-I$src/code/src" `
    "$experiment/diagnostic_main.cpp" "$src/code/src/diffusionterrain.cpp" "$src/code/src/gpu-shader.cpp" `
    "$src/dependency/dll/glew32.dll" "$src/dependency/dll/glfw3.dll" -lopengl32 -static-libgcc -static-libstdc++ `
    -o "$experiment/run/bin/gradient-diagnostic.exe"
if ($LASTEXITCODE -ne 0) { throw 'Diagnostic compile failed' }
Push-Location "$experiment/run/bin"
try {
    & './gradient-diagnostic.exe' 2>&1 | Tee-Object "$experiment/diagnostic.log"
    if ($LASTEXITCODE -ne 0) { throw 'Diagnostic execution failed' }
} finally { Pop-Location }
& $python "$experiment/analyze.py"
if ($LASTEXITCODE -ne 0) { throw 'Analysis failed' }
