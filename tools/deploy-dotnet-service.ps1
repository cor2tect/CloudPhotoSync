param(
    [ValidateNotNull()]
    [string]$sourceDirectory,
    [ValidateNotNull()]
    [string]$stagingDirectory,
    [ValidateNotNull()]
    [string]$targetMachine,
    [ValidateNotNull()]
    [string]$serviceName
)

$ErrorActionPreference = "Stop"

if (!(Test-Path $stagingDirectory)) {
    New-Item -Path "$stagingDirectory" -ItemType "directory"
}

dotnet publish `
    $sourceDirectory `
    -c:Release `
    -r:win-x64 `
    -o:$stagingDirectory `
    --self-contained:true

if ($LASTEXITCODE -ne 0) {
    throw "Compilation failed!"
}

Write-Host "Compilation succeeded"

Write-Host "Copying files to remote temp-directory"

$id = [guid]::NewGuid().ToString();
$tempDirectory = "$env:TMP\deployment\$id" #$env:TMP the same on the remote machine
$session = New-PSSession -ComputerName $targetMachine
New-Item -Path "$tempDirectory" -ItemType "directory"
Copy-Item -Path "$stagingDirectory" -Destination $tempDirectory -ToSession $session -Recurse -Force

Write-Host "Invoke Remote Command"
Invoke-Command -ComputerName $targetMachine -ScriptBlock {
    param($tempDirectory, $serviceName)

    if (!(Test-Path $tempDirectory)) {
        throw "Cannot access temp directory at $tempDirectory"
    }

    $serviceExecutable = Get-ChildItem "Path $tempDirectory -Filter "*.exe" -File
    If (!$serviceExecutable.Exists) {
        throw "Could not find service executable!"
    }

    $serviceDirectory = "$env:ProgramFiles\$serviceName"
    if (!(Test-Path $serviceDirectory)) {
        New-Item -Path "$serviceDirectory" -ItemType "directory"
    }
   
    $service = Get-Service $serviceName -ErrorAction SilentlyContinue
    
    if ($service) {
        Write-Host "Stopping service $serviceName"
        Stop-Service $service
    }

    Write-Host "Copying files to service-directory"
    Copy-Item "$tempDirectory\*" $serviceDirectory -Recurse -Force
     
    if (!$service) {
        Write-Host "Creating service $serviceName"
        $binPath = (Get-ChildItem –Path $serviceDirectory -Filter "*.exe" -File).FullName
        $service = New-Service -Name $serviceName -BinaryPathName $binPath
    }

    Write-Host "Starting service $serviceName"
    Start-Service -Name $serviceName
} -ArgumentList $tempDirectory, $serviceName

Write-Host "Completed"