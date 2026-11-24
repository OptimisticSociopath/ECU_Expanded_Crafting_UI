param([Parameter(Mandatory=$true)][string]$UnityEditor)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if (-not (Test-Path -LiteralPath $UnityEditor -PathType Leaf)) { throw 'Unity Editor executable not found.' }
$run = Join-Path $PSScriptRoot ('artifacts\unity-' + [Guid]::NewGuid().ToString('N'))
$project = Join-Path $run 'project'
$audit = Join-Path $run 'audit'
New-Item -ItemType Directory -Path "$project\Assets\Editor","$project\Packages","$project\ProjectSettings",$audit -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'src\CenteredIngredientGrid.cs'),(Join-Path $PSScriptRoot 'src\CraftingLayout.cs') -Destination "$project\Assets"
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'tests\LayoutUnityTests.cs') -Destination "$project\Assets\Editor"
'{"dependencies":{"com.unity.ugui":"1.0.0","com.unity.modules.ui":"1.0.0","com.unity.modules.imgui":"1.0.0"}}' | Set-Content "$project\Packages\manifest.json"
'm_EditorVersion: 2022.3.43f1' | Set-Content "$project\ProjectSettings\ProjectVersion.txt"
$log = Join-Path $audit 'unity-editor.log'
$process = Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-nographics','-projectPath',('"'+$project+'"'),'-executeMethod','LayoutUnityTests.Run','-logFile',('"'+$log+'"')) -WindowStyle Hidden -PassThru
$process.WaitForExit()
$report = Join-Path $audit 'unity-layout-tests.txt'
if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $report)) { throw "Unity tests failed. See $log" }
Get-Content -LiteralPath $report
Write-Output "REPORT=$report"
