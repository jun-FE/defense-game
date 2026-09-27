# Builds/Windows 폴더를 Steam에 업로드한다(SteamPipe).
# 준비: Steamworks SDK를 받아 Tools/Steam/sdk 폴더에 압축을 푼다.
#       (https://partner.steamgames.com/downloads/list → steamworks_sdk_xxx.zip)
# 사용: .\upload.ps1 -AppId 1234560 -DepotId 1234561 -SteamUser 내빌드계정 -Desc "v0.1"
# 업로드 후 Steamworks 사이트 > SteamPipe > Builds 에서 브랜치에 라이브로 설정한다.

param(
    [Parameter(Mandatory)] [string]$AppId,
    [Parameter(Mandatory)] [string]$DepotId,
    [Parameter(Mandatory)] [string]$SteamUser,
    [string]$Desc = "build $(Get-Date -Format 'yyyy-MM-dd HH:mm')"
)

$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
$steamcmd = Join-Path $here 'sdk\tools\ContentBuilder\builder\steamcmd.exe'
$exe = Join-Path $here '..\..\Builds\Windows\DefenseGame.exe'

if (-not (Test-Path $steamcmd)) { throw "steamcmd를 찾을 수 없습니다: $steamcmd (Steamworks SDK를 Tools\Steam\sdk 에 풀어 주세요)" }
if (-not (Test-Path $exe)) { throw "빌드가 없습니다: $exe (Unity 메뉴 Defense > Windows 빌드를 먼저 실행하세요)" }

$script = Join-Path $here "output\app_build_$AppId.vdf"
New-Item -ItemType Directory -Force (Join-Path $here 'output') | Out-Null
(Get-Content (Join-Path $here 'app_build.vdf') -Raw) `
    -replace '__APP_ID__', $AppId -replace '__DEPOT_ID__', $DepotId -replace '__DESC__', $Desc `
    -replace '"output"', "`"$((Join-Path $here 'output') -replace '\\', '/')`"" `
    -replace '"\.\./\.\./Builds/Windows"', "`"$((Resolve-Path (Join-Path $here '..\..\Builds\Windows')).Path -replace '\\', '/')`"" |
    Set-Content -Encoding ASCII $script

& $steamcmd +login $SteamUser +run_app_build $script +quit
