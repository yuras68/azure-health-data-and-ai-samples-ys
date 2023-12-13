#Connect-MgGraph

"Get grants" | Out-Host
$grants = Get-MgBetaUserOauth2PermissionGrant -UserId 2a0765a4-a2e4-4709-b42c-c5353108fb78
$grant = ($grants | ?{$_.Scope -like '*user.Patient*' -and $_.ResourceId -eq '321c2d68-372d-46f5-afb6-80d58ed951f7'})

"Original scope" | Out-Host
$grant.Scope
$grant.Scope = $grant.Scope + ' patient.Patient.read'
"Modified scope" | Out-Host
$grant.Scope

Update-MgBetaOauth2PermissionGrant -OAuth2PermissionGrantId $grant.Id
