# Rebuilds (optional) and restarts the CoffeeNChill Functions container on its own
# (without docker compose). Function keys are mounted from docker\keys in the repo root.
#
#   .\run-docker.ps1          restart container using the existing image
#   .\run-docker.ps1 -Build   rebuild the image first

param([switch]$Build)

$ErrorActionPreference = 'Stop'

$image     = 'coffeenchill-functions:v2.0'
$container = 'coffeenchill-functions'
$network   = 'coffeenchill-network'
$keys      = Join-Path $PSScriptRoot '..\docker\keys'

if (-not (Test-Path (Join-Path $keys 'host.json'))) {
    throw "Missing $keys\host.json - the function keys file is required."
}

# Azurite's well-known development account key (public, not a real secret).
$storage = 'DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;' +
           'AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;' +
           'BlobEndpoint=http://azurite:10000/devstoreaccount1;' +
           'QueueEndpoint=http://azurite:10001/devstoreaccount1;' +
           'TableEndpoint=http://azurite:10002/devstoreaccount1;'

if ($Build) {
    docker build -t $image $PSScriptRoot
    if ($LASTEXITCODE -ne 0) { throw 'docker build failed' }
}

docker rm -f $container 2>$null | Out-Null

docker run -d --name $container --network $network -p 7071:80 `
    -v "${keys}:/azure-functions-host/Secrets" `
    -e AzureWebJobsSecretStorageType=files `
    -e "AzureWebJobsStorage=$storage" `
    $image
if ($LASTEXITCODE -ne 0) { throw 'docker run failed' }

Write-Host "Running on http://localhost:7071 (keys from $keys)"
