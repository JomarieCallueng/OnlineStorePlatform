#!/usr/bin/env bash
# Generates the initial EF Core migration for every service. Run once from the repo root.
# Requires: dotnet-ef  (dotnet tool install --global dotnet-ef)
set -euo pipefail
cd "$(dirname "$0")/.."

declare -A CTX=(
  ["src/Services/Identity.API"]="IdentityDbContext"
  ["src/Services/Catalog.API"]="CatalogDbContext"
  ["src/Services/Inventory.API"]="InventoryDbContext"
  ["src/Services/Order.API"]="OrderDbContext"
  ["src/Services/Payment.API"]="PaymentDbContext"
)

for proj in "${!CTX[@]}"; do
  ctx="${CTX[$proj]}"
  echo ">> $proj ($ctx)"
  dotnet ef migrations add InitialCreate --project "$proj" --startup-project "$proj" --context "$ctx" --output-dir Migrations
done
echo "Done. Review and commit the Migrations/ folders."
