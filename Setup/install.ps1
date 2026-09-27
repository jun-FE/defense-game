# Defense Game - 개발 환경 원클릭 설치 (Windows 10/11)
# 설치 항목: Git, Git LFS, Unity Hub, Unity 6.3 LTS(+Windows IL2CPP), Visual Studio 2022(Unity 워크로드), Steam
# 이미 설치된 항목은 건너뛴다. 여러 번 실행해도 안전하다.

param(
    [string]$UnityStream = '6000.3',   # 설치할 Unity LTS 계열 (6000.3 = Unity 6.3 LTS)
    [switch]$SkipVisualStudio,
    [switch]$SkipSteam
)

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# 관리자 권한으로 다시 실행
$principal = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    $argList = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$PSCommandPath`"")
    foreach ($entry in $PSBoundParameters.GetEnumerator()) {
        if ($entry.Value -is [switch]) { if ($entry.Value) { $argList += "-$($entry.Key)" } }
        else { $argList += "-$($entry.Key)", "$($entry.Value)" }
    }
    Start-Process powershell.exe -Verb RunAs -ArgumentList $argList
    exit
}

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$HubExe = Join-Path $env:ProgramFiles 'Unity Hub\Unity Hub.exe'
$EditorRoot = Join-Path $env:ProgramFiles 'Unity\Hub\Editor'
$Warnings = New-Object System.Collections.Generic.List[string]

function Write-Step([string]$Text) { Write-Host "`n==> $Text" -ForegroundColor Cyan }
function Write-Ok([string]$Text) { Write-Host "    $Text" -ForegroundColor Green }
function Write-Warn([string]$Text) { Write-Host "    $Text" -ForegroundColor Yellow; $Warnings.Add($Text) }

function Update-PathFromRegistry {
    $env:Path = [Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' +
                [Environment]::GetEnvironmentVariable('Path', 'User')
}

function Install-Package([string]$Id, [string]$Name, [string]$Override) {
    $listed = winget list --id $Id -e --accept-source-agreements 2>$null | Out-String
    if ($listed -match [regex]::Escape($Id)) { Write-Ok "$Name - 이미 설치됨"; return }

    Write-Host "    $Name 설치 중... (몇 분 걸릴 수 있습니다)"
    $wingetArgs = @('install', '-e', '--id', $Id, '--accept-package-agreements', '--accept-source-agreements', '--silent')
    if ($Override) { $wingetArgs += @('--override', $Override) }
    winget @wingetArgs
    if ($LASTEXITCODE -eq 0) { Write-Ok "$Name - 설치 완료" }
    else { Write-Warn "$Name 설치 실패 (winget 코드 $LASTEXITCODE). 직접 설치해 주세요." }
}

function Invoke-Hub([string[]]$HubArgs) {
    # Unity Hub는 GUI 앱이라 출력을 파이프로 받아야 끝날 때까지 기다린다.
    & $HubExe -- --headless @HubArgs 2>&1 | Out-String
}

function Get-InstalledEditor {
    if (-not (Test-Path $EditorRoot)) { return $null }
    Get-ChildItem $EditorRoot -Directory |
        Where-Object { $_.Name -like "$UnityStream.*" -and (Test-Path (Join-Path $_.FullName 'Editor\Unity.exe')) } |
        Sort-Object { [version]($_.Name -replace '[a-z].*$', '') } -Descending |
        Select-Object -First 1
}

Write-Host '================================================' -ForegroundColor Cyan
Write-Host '  Defense Game 개발 환경 설치' -ForegroundColor Cyan
Write-Host '================================================' -ForegroundColor Cyan
Write-Host "프로젝트 위치: $ProjectRoot"

# 1. winget 확인
Write-Step '1/5 winget 확인'
if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
    Write-Host '    winget이 없습니다. Microsoft Store에서 "앱 설치 관리자(App Installer)"를 설치한 뒤 다시 실행하세요.' -ForegroundColor Red
    Start-Process 'ms-windows-store://pdp/?ProductId=9NBLGGH4NNS1'
    Read-Host '엔터를 누르면 종료합니다'
    exit 1
}
Write-Ok 'winget 사용 가능'

# 2. 기본 도구
Write-Step '2/5 Git / Git LFS / Unity Hub'
Install-Package 'Git.Git' 'Git'
Install-Package 'GitHub.GitLFS' 'Git LFS'
Install-Package 'Unity.UnityHub' 'Unity Hub'
Update-PathFromRegistry
if (Get-Command git -ErrorAction SilentlyContinue) {
    git lfs install | Out-Null
    Write-Ok 'Git LFS 활성화'
}

# 3. 코드 편집기 + Steam
Write-Step '3/5 Visual Studio 2022 / Steam'
if ($SkipVisualStudio) { Write-Ok 'Visual Studio - 건너뜀' }
else {
    Install-Package 'Microsoft.VisualStudio.2022.Community' 'Visual Studio 2022 Community' `
        '--quiet --wait --norestart --add Microsoft.VisualStudio.Workload.ManagedGame --includeRecommended'
}
if ($SkipSteam) { Write-Ok 'Steam - 건너뜀' }
else { Install-Package 'Valve.Steam' 'Steam 클라이언트' }

# 4. Unity 에디터
Write-Step "4/5 Unity $UnityStream LTS 에디터 (+ Windows IL2CPP 빌드 모듈)"
$editor = Get-InstalledEditor
if ($editor) {
    Write-Ok "Unity $($editor.Name) - 이미 설치됨"
}
elseif (-not (Test-Path $HubExe)) {
    Write-Warn 'Unity Hub를 찾을 수 없어 에디터를 설치하지 못했습니다.'
}
else {
    $releases = Invoke-Hub @('editors', '--releases')
    $version = [regex]::Matches($releases, [regex]::Escape($UnityStream) + '\.\d+f\d+') |
        ForEach-Object { $_.Value } | Sort-Object -Unique |
        Sort-Object { [version]($_ -replace 'f\d+$', '') } -Descending |
        Select-Object -First 1

    if (-not $version) {
        Write-Warn "Unity Hub에서 $UnityStream 버전 목록을 가져오지 못했습니다. Hub > 설치 > 에디터 설치에서 'Unity 6.3 LTS'를 직접 설치해 주세요."
    }
    else {
        Write-Host "    Unity $version 다운로드 및 설치 중... (10~30분, 용량 약 10GB)"
        Invoke-Hub @('install', '--version', $version, '--module', 'windows-il2cpp') | Write-Host
        $editor = Get-InstalledEditor
        if ($editor) { Write-Ok "Unity $($editor.Name) - 설치 완료" }
        else { Write-Warn "Unity $version 설치를 확인하지 못했습니다. Unity Hub에서 설치 상태를 확인해 주세요." }
    }
}

# 5. 프로젝트 버전 맞추기
Write-Step '5/5 프로젝트 설정'
if ($editor) {
    $versionFile = Join-Path $ProjectRoot 'ProjectSettings\ProjectVersion.txt'
    [IO.File]::WriteAllText($versionFile, "m_EditorVersion: $($editor.Name)`n")
    Write-Ok "프로젝트 에디터 버전을 $($editor.Name)(으)로 설정"
}

Write-Host "`n================================================" -ForegroundColor Cyan
if ($Warnings.Count -gt 0) {
    Write-Host '  완료 (확인 필요한 항목이 있습니다)' -ForegroundColor Yellow
    $Warnings | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
}
else {
    Write-Host '  설치 완료!' -ForegroundColor Green
}
Write-Host '================================================' -ForegroundColor Cyan
Set-Clipboard -Value $ProjectRoot
Write-Host @"

다음 단계:
  1. Unity Hub에 로그인하고 무료 라이선스(Personal)를 활성화하세요. (처음 한 번)
  2. Hub의 [프로젝트] > [추가] > [디스크에서 프로젝트 추가]에서 아래 폴더를 선택하세요.
     $ProjectRoot
     (경로가 클립보드에 복사돼 있습니다)
  3. 프로젝트가 열리면 데모 씬이 자동으로 만들어집니다. 상단의 Play 버튼을 누르세요.

"@

# 관리자 권한이 아닌 일반 권한으로 Unity Hub를 연다.
if (Test-Path $HubExe) { Start-Process explorer.exe -ArgumentList "`"$HubExe`"" }
Read-Host '엔터를 누르면 창을 닫습니다'
