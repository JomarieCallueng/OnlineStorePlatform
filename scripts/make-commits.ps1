# Creates a small-step commit history on the feature branch, under YOUR git identity.
# Run from the repo root on your machine (where git can delete its lock files).
# TIP: first set your own identity:  git config user.name "You";  git config user.email "you@students..."
# TIP: run scripts/add-migrations.ps1 BEFORE this so the Migrations/ folders are included.
$ErrorActionPreference = "Stop"
Set-Location (Join-Path $PSScriptRoot "..")

$branch = (git rev-parse --abbrev-ref HEAD).Trim()
if ($branch -ne "feature/complete-capstone") { git checkout -B feature/complete-capstone }

$trailer = "`n`nCo-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`nClaude-Session: https://claude.ai/code/session_01TcDSuLRtehy3t4WLWECyPM"

function Commit([string[]]$paths, [string]$msg) {
  git add -- $paths
  git diff --cached --quiet
  if ($LASTEXITCODE -ne 0) { git commit -m "$msg$trailer" | Out-Null; Write-Host "committed: $msg" }
  else { Write-Host "nothing to commit for: $msg" }
}

Commit @("src/Shared/Store.Shared.Events") "feat(shared): integration event contracts and bus topology"
Commit @("src/Shared/Store.Shared.Common") "feat(shared): messaging, correlation, problem details, JWT, db bootstrap"
Commit @("src/Services/Identity.API","contracts/identity.yaml") "feat(identity): auth service with JWT issuance and roles"
Commit @("src/Services/Catalog.API","contracts/catalog.yaml") "feat(catalog): products with versioned, contract-first API"
Commit @("src/Services/Inventory.API","contracts/inventory.yaml") "feat(inventory): stock, reservations and reserve/release consumers"
Commit @("src/Services/Payment.API","contracts/payment.yaml") "feat(payment): charge/refund consumers and runtime failure switch"
Commit @("src/Services/Order.API","contracts/order.yaml") "feat(order): saga coordinator with resilient typed catalog client"
Commit @("src/Gateway/ApiGateway") "feat(gateway): ocelot routing, JWT validation and login rate limiting"
Commit @("src/Storefront/Storefront.UI") "feat(storefront): browse, auth, order tracking and admin pages"
Commit @("docker-compose.yml",".dockerignore",".env.example") "build: docker compose for the full stack with health checks"
Commit @("OnlineStorePlatform.slnx","README.md","CONTRIBUTIONS.md","Postman","Evidence","scripts",".gitignore") "docs: readme, postman, evidence guide and helper scripts"

Write-Host ""
Write-Host "Done. Review: git log --oneline"
Write-Host "Next: push the branch, open a reviewed PR to main, then tag v1.0 at submission."
