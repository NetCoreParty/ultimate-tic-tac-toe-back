$ErrorActionPreference = 'Stop'

$solution = Join-Path $PSScriptRoot '..\UltimateTicTacToe.sln'
$runsettings = Join-Path $PSScriptRoot '..\coverlet.runsettings'
$resultsDir = Join-Path $PSScriptRoot '..\artifacts\testresults'
$reportDir = Join-Path $PSScriptRoot '..\artifacts\coverage-report'

Write-Host "Restoring local dotnet tools..."
dotnet tool restore

Write-Host "Running tests with coverage (Cobertura)..."
dotnet test $solution -c Release `
	--collect:"XPlat Code Coverage" `
	--settings $runsettings `
	--results-directory $resultsDir

Write-Host "Searching for coverage reports..."
$reportFiles = Get-ChildItem -Path $resultsDir -Recurse -Filter "coverage.cobertura.xml" | ForEach-Object { $_.FullName }

if (-not $reportFiles -or $reportFiles.Count -eq 0) {
	throw "No 'coverage.cobertura.xml' files were found under '$resultsDir'."
}

$reportsArg = ($reportFiles -join ';')

Write-Host "Generating HTML coverage report..."
dotnet tool run reportgenerator `
	"-reports:$reportsArg" `
	"-targetdir:$reportDir" `
	"-reporttypes:Html"

Write-Host "Done."
Write-Host "Open: $reportDir\\index.html"
