$localProps = @("Guid", "FilePath", "IsServerMod", "PairedComponentPath", "AlternateGuids", "LocalName", "LocalAuthor", "LocalVersion", "LocalSptVersion")
$apiProps = @("ApiModId", "ApiName", "ApiAuthor", "ApiSlug", "ApiUrl", "ApiSourceCodeUrl", "ApiVersions")
$updateProps = @("LatestVersion", "UpdateStatus", "DownloadLink", "BlockingMods", "BlockReason", "IncompatibilityReason", "IsLocalSptIncompatible", "CompatibleVersionString", "UpdateSuppressed", "UpdateDependencyChanges")

$files = Get-Content build_errors.txt | Select-String -Pattern "^(.*\.cs)\(\d+,\d+\)" | ForEach-Object { $_.Matches.Groups[1].Value } | Select-Object -Unique

foreach ($file in $files) {
    if ($file -match "Mod\.cs" -or $file -match "ApiErrors\.cs") { continue }
    
    $content = Get-Content $file -Raw
    
    foreach ($prop in $localProps) {
        # Replace .PropName with .Local.PropName, avoiding already replaced .Local.LocalName
        $content = [System.Text.RegularExpressions.Regex]::Replace($content, "(?<!\.Local)\.$prop\b", ".Local.$prop")
        # Replace property assignments in object initializers: PropName = ... -> Local = new LocalModIdentity { PropName = ... }
        # This requires more complex parsing. We'll handle property accesses first.
    }
    foreach ($prop in $apiProps) {
        $content = [System.Text.RegularExpressions.Regex]::Replace($content, "(?<!\.Api)\.$prop\b", ".Api.$prop")
    }
    foreach ($prop in $updateProps) {
        $content = [System.Text.RegularExpressions.Regex]::Replace($content, "(?<!\.Update)\.$prop\b", ".Update.$prop")
    }
    
    Set-Content $file $content
}
