<#
  Sube el repositorio a GitHub.

  El remoto sigue siendo SSH (git@github.com:Sgborbolla/QBASConverter.git).
  Hasta que haya una clave SSH configurada, este script hace el push por
  HTTPS con un token personal. El token se lee de la variable de entorno
  GITHUB_TOKEN, asi que no queda escrito en ningun fichero del repo.

    $env:GITHUB_TOKEN = "ghp_..."
    .\tools\push.ps1

  Cuando se configure una clave SSH, este script deja de hacer falta:
  basta con  git push origin main .
#>
param(
    [string]$Rama = "main",
    [string]$Mensaje = ""
)

$ErrorActionPreference = "Stop"

$raiz = Split-Path -Parent $PSScriptRoot
Push-Location $raiz

try {
    if ($Mensaje) {
        git add -A
        git commit -m $Mensaje
    }

    $token = $env:GITHUB_TOKEN
    if (-not $token) {
        Write-Host "GITHUB_TOKEN no esta definido." -ForegroundColor Yellow
        Write-Host "  `$env:GITHUB_TOKEN = 'ghp_...'"
        Write-Host "  (o configura una clave SSH y usa: git push origin $Rama)"
        exit 1
    }

    $b64 = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("Sgborbolla:$token"))
    $env:GIT_CONFIG_COUNT = 1
    $env:GIT_CONFIG_KEY_0 = "http.https://github.com/.extraheader"
    $env:GIT_CONFIG_VALUE_0 = "Authorization: Basic $b64"

    git push "https://github.com/Sgborbolla/QBASConverter.git" "HEAD:$Rama"
}
finally {
    Pop-Location
}
