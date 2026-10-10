$ErrorActionPreference = 'Stop'
$experiment = $PSScriptRoot
$source = Join-Path $experiment 'upstream/eric-guerin-gradient-terrains-68da08b'
$project = Split-Path (Split-Path $experiment)
$compiler = Join-Path $project 'Library/GradientTerrainExampleTools/toolchain/w64devkit/bin/g++.exe'
$run = Join-Path $experiment 'run'
New-Item -ItemType Directory -Force -Path "$run/bin", "$run/results" | Out-Null
Copy-Item -LiteralPath "$source/data", "$source/shader" -Destination $run -Recurse -Force
Copy-Item "$source/dependency/dll/*.dll" -Destination "$run/bin" -Force
$sources = Get-ChildItem "$source/code/src/*.cpp" | ForEach-Object FullName
$buildArguments = @('-O2', '-std=c++11', '-DGLFW_DLL', "-I$source/dependency/include") + $sources + @(
    "$source/dependency/dll/glew32.dll",
    "$source/dependency/dll/glfw3.dll",
    '-lopengl32', '-static-libgcc', '-static-libstdc++', '-o', "$run/bin/gradient-original.exe"
)
& $compiler --version | Set-Content "$experiment/compiler.txt"
& $compiler @buildArguments 2>&1 | Tee-Object "$experiment/build.log"
if ($LASTEXITCODE -ne 0) { throw 'Original example compilation failed' }
Push-Location "$run/bin"
try {
    $watch = [Diagnostics.Stopwatch]::StartNew()
    & './gradient-original.exe' 2>&1 | Tee-Object "$experiment/run.log"
    $runExitCode = $LASTEXITCODE
    $watch.Stop()
    [ordered]@{ exitCode=$runExitCode; elapsedSeconds=$watch.Elapsed.TotalSeconds; utc=[DateTime]::UtcNow.ToString('o') } |
        ConvertTo-Json | Set-Content "$experiment/run-status.json"
    if ($runExitCode -ne 0) { throw 'Original example execution failed' }
} finally { Pop-Location }
