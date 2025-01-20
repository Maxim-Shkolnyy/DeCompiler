param (
    [string]$TargetPath
)

if ([string]::IsNullOrWhiteSpace($TargetPath)) {{
    $TargetPath = Get-Location
}}

$ilspyVersion = & ilspycmd --version
if ($ilspyVersion -match 'ilspycmd: \d+\.\d+\.\d+\.\d+') {{
    Write-Host 'ilspycmd is already installed.'
}} else {{
    Write-Host 'ilspycmd not found. Installing...'
    & dotnet tool install --global ilspycmd
}}

$executionPolicy = Get-ExecutionPolicy
if ($executionPolicy -eq 'Restricted') {{
    Write-Host 'Setting execution policy to RemoteSigned...'
    Set-ExecutionPolicy RemoteSigned -Scope CurrentUser
}}

$timestamp = Get-Date -Format 'HH-mm_dd.MM.yyyy'
$outputFile = Join-Path -Path $TargetPath -ChildPath 'AssemblyVersions-info_$timestamp.txt'

if (Test-Path $outputFile) {{
    'Cleaned at $(Get-Date -Format ""HH:mm:ss_dd.MM.yyyy"")' | Set-Content $outputFile
}}

Get-ChildItem -Path $TargetPath | Where-Object {{ $_.Extension -in '.dll', '.exe' }} | ForEach-Object {{
    $file = $_
    try {{
        $assemblyInfo = & ilspycmd $file.FullName
        $versionLine = ($assemblyInfo | Select-String -Pattern 'AssemblyVersion\("(\d+(\.\d+)*)"\)').Line
        
        if ($versionLine) {{
            $version = $versionLine -replace '.*AssemblyVersion\(""([^""]+)""\).*', '$1'
            $lineOutput = ""{{0,-55}} - AssemblyVersion('{{1}}') - {{2}}"" -f $file.Name, $version, $file.LastWriteTime
            Write-Host $lineOutput
            $lineOutput | Add-Content $outputFile
        }} else {{
            ""$($file.Name) - ERROR ""Version not found"" - $($file.LastWriteTime)"" | Add-Content $outputFile
        }}
    }} catch {{
        ""$($file.Name) - ERROR ""$_"" - $($file.LastWriteTime)"" | Add-Content $outputFile
    }}
}}

Write-Host 'Processing completed. Results saved to $outputFile.'