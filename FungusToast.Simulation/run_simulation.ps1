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
        # The simulation now automatically generates a timestamped filename
        # when no --output is specified, so we'll use the same format here for consistency
        $timestamp = (Get-Date -Format 'yyyy-MM-ddTHH-mm-ss')
        return "Simulation_output_$timestamp.txt"
    }
}

# Build FungusToast.Core (navigate relative to FungusToast.Simulation)
Write-Host "Building FungusToast.Core..."
dotnet build "../FungusToast.Core/FungusToast.Core.csproj"
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed for FungusToast.Core. Exiting."
    exit 1
}

# Build FungusToast.Simulation (current directory)
Write-Host "Building FungusToast.Simulation..."
dotnet build "FungusToast.Simulation.csproj"
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed for FungusToast.Simulation. Exiting."
    exit 1
}

# Determine expected output filename
$outputFile = Get-OutputFilename $Args

# Prepare argument string for dotnet run
$argString = $Args -join ' '

# Calculate the full path that the simulation will use
$simulationOutputDir = "bin\Debug\net8.0\SimulationOutput"
$fullOutputPath = Join-Path $simulationOutputDir $outputFile

# Note: The simulation will now always create an output file, even without --output parameter
if ($Args.IndexOf('--output') -lt 0) {
    Write-Host "No --output specified. Simulation will auto-generate a timestamped filename."
} else {
    Write-Host "Output will be written to: $fullOutputPath"
}

# Check if we're running from GitHub Copilot tools
$isAutomated = $env:COPILOT_AUTOMATED -eq "true" -or $args -contains "--automated"

if ($isAutomated) {
    # For automated execution: run directly in current console (no new window)
    Write-Host "Running simulation in current console (automated mode)..."
    dotnet run -- $argString
    Write-Host "Simulation process has completed."
} else {
    # For manual execution: launch in new window as before
    Write-Host "Launching simulation in a new window..."
    $simProcess = Start-Process powershell -ArgumentList '-NoExit', '-Command', "Set-Location '$PWD'; dotnet run -- $argString" -PassThru
    Write-Host "Simulation started in new window. Waiting for it to finish..."
    $simProcess.WaitForExit()
    Write-Host "Simulation process has exited."
}