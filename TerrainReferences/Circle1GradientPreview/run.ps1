$ErrorActionPreference = 'Stop'
$experiment=$PSScriptRoot
$project=Split-Path (Split-Path $experiment)
$src=Join-Path $project 'TerrainReferences/GradientTerrainExample/upstream/eric-guerin-gradient-terrains-68da08b'
$compiler=Join-Path $project 'Library/GradientTerrainExampleTools/toolchain/w64devkit/bin/g++.exe'
$python='C:/Users/Guanhao/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& $python "$experiment/prepare.py"
if($LASTEXITCODE -ne 0) { throw 'Input preparation failed' }
New-Item -ItemType Directory -Force -Path "$experiment/bin" | Out-Null
Copy-Item -LiteralPath "$src/shader" -Destination $experiment -Recurse -Force
Copy-Item "$src/dependency/dll/*.dll" -Destination "$experiment/bin" -Force
& $compiler -O2 -std=c++11 -DGLFW_DLL "-I$src/dependency/include" "-I$src/code/src" `
    "$experiment/terrain_solver.cpp" "$src/code/src/diffusionterrain.cpp" "$src/code/src/gpu-shader.cpp" `
    "$src/dependency/dll/glew32.dll" "$src/dependency/dll/glfw3.dll" -lopengl32 -static-libgcc -static-libstdc++ `
    -o "$experiment/bin/terrain-solver.exe"
if($LASTEXITCODE -ne 0) { throw 'Solver compilation failed' }
Push-Location "$experiment/bin"
try {
    foreach($name in @('A','B')) {
        & './terrain-solver.exe' "$experiment/$name" 1025 2>&1 | Tee-Object "$experiment/$name/solver.log"
        if($LASTEXITCODE -ne 0) { throw 'Terrain reconstruction failed' }
    }
} finally { Pop-Location }
& $python "$experiment/evaluate.py"
if($LASTEXITCODE -ne 0) { throw 'Candidate verification failed' }
& $python "$experiment/package_preview.py"
if($LASTEXITCODE -ne 0) { throw 'Preview packaging failed' }
