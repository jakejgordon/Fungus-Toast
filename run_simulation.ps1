param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Args
)

# Helper: Get output filename from args or generate one
function Get-OutputFilename {
    param([string[]]$args)
    $outputIndex = $args.IndexOf('--output')
    if ($outputIndex -ge 0 -and $outputIndex + 1 -lt $args.Length) {
        return $args[$outputIndex + 1]
    } else {
        $timestamp = (Get-Date -Format 'yyyy-MM-ddTHH-mm-ss')
        return "sim_output_$timestamp.txt"
    }
}

# Build FungusToast.Core
Write-Host "Building FungusToast.Core..."
dotnet build "FungusToast.Core/FungusToast.Core.csproj"
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed for FungusToast.Core. Exiting."
    exit 1
}

# Build FungusToast.Simulation
Write-Host "Building FungusToast.Simulation..."
dotnet build "FungusToast.Simulation/FungusToast.Simulation.csproj"
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed for FungusToast.Simulation. Exiting."
    exit 1
}

# Determine output filename
$outputFile = Get-OutputFilename $Args

# If --output not in args, add it
if ($Args.IndexOf('--output') -lt 0) {
    $Args += @('--output', $outputFile)
}

# Prepare argument string for dotnet run
$argString = $Args -join ' '

# Calculate the full path that the simulation will use
$simulationOutputDir = "FungusToast.Simulation\bin\Debug\net8.0\SimulationOutput"
$fullOutputPath = Join-Path $simulationOutputDir $outputFile

Write-Host "Output will be written to: $fullOutputPath"

# Launch simulation in a new PowerShell window
Write-Host "Launching simulation in a new window..."
$simProcess = Start-Process powershell -ArgumentList '-NoExit', '-Command', "dotnet run --project FungusToast.Simulation -- $argString" -PassThru
Write-Host "Simulation started in new window. Waiting for it to finish..."
$simProcess.WaitForExit()
Write-Host "Simulation process has exited." 