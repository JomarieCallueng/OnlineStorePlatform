#!/usr/bin/env bash
# Creates a small-step commit history on the feature branch, under YOUR git identity.
# Run from the repo root. First: git config user.name/user.email to your own account.
# Run scripts/add-migrations.sh BEFORE this so Migrations/ folders are included.
set -euo pipefail
cd "$(dirname "$0")/.."

branch="$(git rev-parse --abbrev-ref HEAD)"
[ "$branch" = "feature/complete-capstone" ] || git checkout -B feature/complete-capstone

trailer=$'\n\nCo-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01TcDSuLRtehy3t4WLWECyPM'

commit() {
  local msg="$1"; shift
  git add -- "$@"
  if ! git diff --cached --quiet; then
    git commit -m "${msg}${trailer}" >/dev/null
    echo "committed: $msg"
  else
    echo "nothing to commit for: $msg"
  fi
}

commit "feat(shared): integration event contracts and bus topology" src/Shared/Store.Shared.Events
commit "feat(shared): messaging, correlation, problem details, JWT, db bootstrap" src/Shared/Store.Shared.Common
commit "feat(identity): auth service with JWT issuance and roles" src/Services/Identity.API contracts/identity.yaml
commit "feat(catalog): products with versioned, contract-first API" src/Services/Catalog.API contracts/catalog.yaml
commit "feat(inventory): stock, reservations and reserve/release consumers" src/Services/Inventory.API contracts/inventory.yaml
commit "feat(payment): charge/refund consumers and runtime failure switch" src/Services/Payment.API contracts/payment.yaml
commit "feat(order): saga coordinator with resilient typed catalog client" src/Services/Order.API contracts/order.yaml
commit "feat(gateway): ocelot routing, JWT validation and login rate limiting" src/Gateway/ApiGateway
commit "feat(storefront): browse, auth, order tracking and admin pages" src/Storefront/Storefront.UI
commit "build: docker compose for the full stack with health checks" docker-compose.yml .dockerignore .env.example
commit "docs: readme, postman, evidence guide and helper scripts" OnlineStorePlatform.slnx README.md CONTRIBUTIONS.md Postman Evidence scripts .gitignore

echo ""
echo "Done. Review: git log --oneline"
