<#
    Loads sample data and US Core profiles into a FHIR server.

    Uses the Azure CLI, NPM, .NET 6+ SDK, and the FHIR Loader CLI tool.
#>
param (
    [Parameter(Mandatory=$false)]
    [string]$FhirUrl,
    
    [Parameter(Mandatory=$false)]
    [string]$FhirAudience,

    [Parameter(Mandatory=$false)]
    [string]$TenantId
)

$SCRIPT_PATH = Split-Path -parent $MyInvocation.MyCommand.Definition
$SAMPLE_ROOT = (Get-Item $SCRIPT_PATH).Parent.FullName

if ([string]::IsNullOrWhiteSpace($FhirUrl) -or [string]::IsNullOrWhiteSpace($FhirAudience) -or  [string]::IsNullOrWhiteSpace($TenantId)) {

    Write-Host "Required parameters parameter blank, looking in azd enviornment configuration...."

    # Load parameters from active Azure Developer CLI environment
    $AZD_ENVIRONMENT = azd env get-values --cwd $SAMPLE_ROOT
    $AZD_ENVIRONMENT | ForEach-Object {
        $name, $value = $_.split('=')
        if ([string]::IsNullOrWhiteSpace($name) -or $name.Contains('#')) {
            continue
        }
        
        if ([string]::IsNullOrWhiteSpace($FhirAudience) -and $name -eq "FhirAudience") {
            $FhirAudience = $value.Trim('"')
        }

        if ([string]::IsNullOrWhiteSpace($FhirUrl) -and $name -eq "FhirUrl") {
            $FhirUrl = $value.Trim('"')
        }

        if ([string]::IsNullOrWhiteSpace($TenantId) -and $name -eq "TenantId") {
            $TenantId = $value.Trim('"')
        }
    }
}

if (-not $FhirAudience) {
    Write-Error "FhirAudience is STILL not set. Exiting."
    exit
}

if (-not $FhirUrl) {
    Write-Error "FhirUrl is STILL not set. Exiting."
    exit
}

if (-not $TenantId) {
    Write-Error "TenantId is STILL not set. Exiting."
    exit
}

az login -t $TenantId --scope "$FhirAudience/user_impersonation"
#az login -t $TenantId --scope "$FhirAudience/patient.Organization.read"

$access_token = az account get-access-token --scope "$FhirAudience/user_impersonation" --query 'accessToken' -o tsv
#$access_token = az account get-access-token --scope "$FhirAudience/patient.Organization.read" --query 'accessToken' -o tsv

#Write-Host "Using token $access_token"

#$FilePath = "$SCRIPT_PATH/test-resources/V3.1.1_USCoreCompliantResources.json"
#az rest --uri $FhirUrl --method POST --body "@$FilePath" --headers "Authorization=Bearer $access_token" "Content-Type=application/json"

#$FilePath = "$SCRIPT_PATH/test-resources/CapabilityStatement-us-core-server.json"
#az rest --uri "$FhirUrl/CapabilityStatement/us-core-server" --method PUT --body "@$FilePath" --headers "Authorization=Bearer $access_token" "Content-Type=application/json"

#az rest --uri "$FhirUrl/Practitioner/PractitionerA1" --method GET  --headers "Authorization=Bearer $access_token" "Content-Type=application/json"
#az rest --uri "$FhirUrl/Practitioner/PractitionerA1" --method GET  --headers "Authorization=Bearer $access_token" "Content-Type=application/json"
az rest --uri "https://fhir-on-dev-ys-apim.azure-api.net/smart/Organization/OrganizationA" --method GET  --headers "Authorization=Bearer $access_token" "Content-Type=application/json"

#$access_token = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsImtpZCI6Ii1LSTNROW5OUjdiUm9meG1lWm9YcWJIWkdldyJ9.eyJhdWQiOiJiYTJkZjg2ZC0wYTg5LTRkYWMtOWE0OC05MWY4OWJmMjJiZTEiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vOTJmNWQ0ZmQtMzFiNi00ZTc1LWJkMjItMWE5NjFmNzZiYTQzL3YyLjAiLCJpYXQiOjE2OTY4OTk3MTMsIm5iZiI6MTY5Njg5OTcxMywiZXhwIjoxNjk2OTAzNjEzLCJyaCI6IjAuQVEwQV9kVDFrcll4ZFU2OUlocVdIM2E2UTIzNExicUpDcXhObWtpUi1KdnlLLUVOQUxJLiIsInN1YiI6InFvTThYeVF1OHgydmFVRVJuUk95SFdOb1djckNQdnByZlRfVEYtaDdjX0UiLCJ0aWQiOiI5MmY1ZDRmZC0zMWI2LTRlNzUtYmQyMi0xYTk2MWY3NmJhNDMiLCJ1dGkiOiJtb1o0Z2pxdHQwMktoSHZWdTZGaUFBIiwidmVyIjoiMi4wIiwiZmhpclVzZXIiOiJQYXRpZW50L1BhdGllbnRBIn0.GAp_eCkYZP65xpxKoAZpXYnzMeiR8_vGRh1h1roUTK9UcBcn-PgqgFD19ceZypkj79-hJLSzjRnzYDVv4OkZuHgR3C4NLgWgXWt3D5AchhKjrm1AgyQC91iHYD32br94l-wROt2ID3hYJ7EbJqVjw6cIRj0oSwa-67nQvnLbRVcd1f1wczzFU9RaoT1yz93LjaO8Qso4muFI0ezo6qNTzMn0lOQe-90bFVPjgLaODTywQhSB8XlD3uo9H0fz33fcH0Oub_fvX-jHyT3xq0YVVqxZpHlKOUmIc3qBUxcWOed0Zb3S4frWZzFUXFUHUvb-Av9YcpsbLCWCKhD7tailQA"

#Invoke-RestMethod -Method Get -Uri "$FhirUrl/Practitioner/PractitionerA1" -Headers @{Authorization=$access_token;"Content-Type"="application/json"}


