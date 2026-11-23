param(
    [Parameter(Mandatory=$false)]
    [string]$Token
)

$git = "C:\Users\swthanne\AppData\Local\Programs\Git\cmd\git.exe"
if (-not (Test-Path $git)) {
    $git = "git"
}

if ($Token) {
    Write-Host "Pushing to GitHub with Personal Access Token..." -ForegroundColor Cyan
    & $git push -u "https://${Token}@github.com/swarnanjalithanneeru/FlightBookingSystem.git" main
} else {
    Write-Host "Pushing to GitHub (https://github.com/swarnanjalithanneeru/FlightBookingSystem.git)..." -ForegroundColor Cyan
    Write-Host "If prompted, complete browser authentication or enter your GitHub credentials." -ForegroundColor Yellow
    & $git push -u origin main
}
