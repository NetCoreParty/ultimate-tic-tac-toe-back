param(
	[Parameter(Mandatory = $false)]
	[ValidateSet('up', 'down', 'restart', 'status', 'logs')]
	[string]$Command = 'up'
)

$ErrorActionPreference = 'Stop'

$composeFile = Join-Path $PSScriptRoot '..\deploy\docker-compose.yaml'

function Invoke-Compose([string[]]$composeArgs) {
	# NOTE: Don't name this parameter '$args' (PowerShell has a built-in automatic variable named $args)
	& docker compose -f $composeFile @composeArgs
	if ($LASTEXITCODE -ne 0) {
		throw "docker compose failed (exit $LASTEXITCODE): docker compose -f `"$composeFile`" $($composeArgs -join ' ')"
	}
}

switch ($Command) {
	'up' {
		Invoke-Compose @('up', '-d')
		Invoke-Compose @('ps')
	}
	'down' {
		Invoke-Compose @('down')
	}
	'restart' {
		Invoke-Compose @('down')
		Invoke-Compose @('up', '-d')
		Invoke-Compose @('ps')
	}
	'status' {
		Invoke-Compose @('ps')
	}
	'logs' {
		Invoke-Compose @('logs', '--tail', '200')
	}
}

