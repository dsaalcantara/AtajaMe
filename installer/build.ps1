<#
.SYNOPSIS
    Script automatizado de compilación, ejecución de pruebas y generación del instalador de AtajaMe.

.DESCRIPTION
    1. Ejecuta la suite de pruebas unitarias (tests/AtajaMe.Tests).
    2. Publica los binarios optimizados en modo Release para win-x64 (self-contained).
    3. Localiza el compilador de Inno Setup (ISCC.exe).
    4. Compila installer/setup.iss para producir AtajaMe_Setup.exe en installer/output/.
#>

[CmdletBinding()]
param(
    [switch]$SkipTests,
    # Versión X.Y.Z. Si se omite, se lee de <Version> en AtajaMe.UI.csproj.
    [string]$Version
)

$ErrorActionPreference = "Stop"
$rootDir = (Get-Item -LiteralPath $PSScriptRoot).Parent.FullName

if (-not $Version) {
    [xml]$csproj = Get-Content -LiteralPath (Join-Path $rootDir "src\AtajaMe.UI\AtajaMe.UI.csproj")
    $Version = ($csproj.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1).Version
}
if (-not ($Version -match '^\d+\.\d+\.\d+$')) {
    Write-Error "Versión no válida: '$Version'. Formato esperado: X.Y.Z"
}
Write-Host "-> Versión a empaquetar: $Version" -ForegroundColor DarkCyan

Write-Host "===========================================================" -ForegroundColor Cyan
Write-Host "  AtajaMe · Pipeline de Compilación y Empaquetado" -ForegroundColor Cyan
Write-Host "===========================================================" -ForegroundColor Cyan

# 1. Pruebas Unitarias
if (-not $SkipTests) {
    Write-Host "`n[1/3] Ejecutando suite de pruebas unitarias..." -ForegroundColor Yellow
    dotnet test "$rootDir\tests\AtajaMe.Tests\AtajaMe.Tests.csproj" -c Release --nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Las pruebas unitarias fallaron. Abortando empaquetado."
    }
    Write-Host "-> Todas las pruebas pasaron satisfactoriamente." -ForegroundColor Green
} else {
    Write-Host "`n[1/3] Pruebas unitarias omitidas (-SkipTests)." -ForegroundColor DarkGray
}

# 2. Publicación de Binarios en Release
Write-Host "`n[2/3] Publicando binarios para win-x64 (self-contained)..." -ForegroundColor Yellow
$publishDir = Join-Path $rootDir "publish\win-x64"
if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

dotnet publish "$rootDir\src\AtajaMe.UI\AtajaMe.UI.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -o $publishDir `
    -p:Version=$Version `
    --nologo

if ($LASTEXITCODE -ne 0) {
    Write-Error "Fallo en la publicación de dotnet. Abortando."
}
Write-Host "-> Binarios publicados en: $publishDir" -ForegroundColor Green

# 3. Compilación del Instalador con Inno Setup
Write-Host "`n[3/3] Compilando instalador nativo con Inno Setup..." -ForegroundColor Yellow

$isccCandidates = @(
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "ISCC.exe"
)

$isccPath = $null
foreach ($candidate in $isccCandidates) {
    if (Test-Path $candidate) {
        $isccPath = $candidate
        break
    }
    $cmd = Get-Command $candidate -ErrorAction SilentlyContinue
    if ($cmd) {
        $isccPath = $cmd.Source
        break
    }
}

if (-not $isccPath) {
    Write-Error "No se encontró el compilador de Inno Setup (ISCC.exe). Instálelo con: winget install JRSoftware.InnoSetup"
}

Write-Host "-> Usando compilador: $isccPath" -ForegroundColor DarkCyan
$issFile = Join-Path $PSScriptRoot "setup.iss"
& $isccPath "/DMyAppVersion=$Version" $issFile

if ($LASTEXITCODE -ne 0) {
    Write-Error "Error durante la compilación con Inno Setup."
}

$outputExe = Join-Path $PSScriptRoot "output\AtajaMe_Setup.exe"
if (Test-Path -LiteralPath $outputExe) {
    $item = Get-Item -LiteralPath $outputExe
    $hash = (Get-FileHash -LiteralPath $outputExe -Algorithm SHA256).Hash
    $sizeMB = [math]::Round($item.Length / 1MB, 2)

    Write-Host "`n===========================================================" -ForegroundColor Green
    Write-Host "  ¡Instalador generado con éxito!" -ForegroundColor Green
    Write-Host "===========================================================" -ForegroundColor Green
    Write-Host "  Archivo : $outputExe"
    Write-Host "  Tamaño  : $sizeMB MB ($($item.Length) bytes)"
    Write-Host "  SHA-256 : $hash"
    Write-Host "===========================================================`n" -ForegroundColor Green
} else {
    Write-Error "El ejecutable del instalador no se encontró en: $outputExe"
}
