# fix-required.ps1
# Run from the repo root

param(
    [string]$Root = "src/ModelContextProtocol.Core"
)

Write-Host "Scanning for C# files under: $Root"
Write-Host ""

Get-ChildItem -Path $Root -Recurse -Filter *.cs | ForEach-Object {
    $path = $_.FullName

    # Create a backup next to the file (one-time overwrite allowed)
    Copy-Item $path "$path.bak" -Force

    $text = Get-Content $path -Raw

    # Replace "public required" → "public"
    $text = $text -replace '\bpublic\s+required\b', 'public'

    # Replace "get; init;" → "get; set;"
    $text = $text -replace 'get;\s*init;', 'get; set;'

    Set-Content $path $text -Encoding UTF8

    Write-Host "Updated $path"
}

Write-Host ""
Write-Host "Done. Backups created as *.bak next to each file."
