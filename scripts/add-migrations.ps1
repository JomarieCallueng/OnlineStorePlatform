# Generates the initial EF Core migration for every service. Run once from the repo root.
# Requires: dotnet-ef  (dotnet tool install --global dotnet-ef)
$ErrorActionPreference = "Stop"
Set-Location (Join-Path $PSScriptRoot "..")

$contexts = @{
  "src/Services/Identity.API"  = "IdentityDbContext"
  "src/Services/Catalog.API"   = "CatalogDbContext"
  "src/Services/Inventory.API" = "InventoryDbContext"
  "src/Services/Order.API"     = "OrderDbContext"
  "src/Services/Payment.API"   = "PaymentDbContext"
}

foreach ($proj in $contexts.Keys) {
  $ctx = $contexts[$proj]
  Write-Host ">> $proj ($ctx)"
  dotnet ef migrations add InitialCreate --project $proj --startup-project $proj --context $ctx --output-dir Migrations
}
Write-Host "Done. Review and commit the Migrations/ folders."
