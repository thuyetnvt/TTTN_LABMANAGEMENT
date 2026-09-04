#!/usr/bin/env bash
set -Eeuo pipefail

DEPLOY_DIR="${DEPLOY_DIR:-/opt/labmanagement}"
DEPLOY_BRANCH="${DEPLOY_BRANCH:?DEPLOY_BRANCH is required}"

cd "$DEPLOY_DIR"

# Keep deployment safe: local edits to tracked files must be handled manually.
# Untracked runtime files such as .env.save are allowed to remain on the VPS.
tracked_changes="$(git diff --name-only; git diff --cached --name-only)"
if [ -n "$tracked_changes" ]; then
  echo "VPS has local changes in tracked files; deployment stopped."
  git status --short
  exit 1
fi

git fetch origin "$DEPLOY_BRANCH"

if git show-ref --verify --quiet "refs/heads/$DEPLOY_BRANCH"; then
  git checkout "$DEPLOY_BRANCH"
else
  git checkout -b "$DEPLOY_BRANCH" "origin/$DEPLOY_BRANCH"
fi

git pull --ff-only origin "$DEPLOY_BRANCH"

# The current VPS uses the default compose stack. Use the hardened production
# stack automatically once .env.production has been configured there.
if [ -f .env.production ]; then
  COMPOSE_ENV_FILE=".env.production"
  COMPOSE_FILE="docker-compose.prod.yml"
else
  COMPOSE_ENV_FILE=".env"
  COMPOSE_FILE="docker-compose.yml"
  echo "Warning: .env.production is missing; using .env with docker-compose.yml."
fi

if [ ! -f "$COMPOSE_ENV_FILE" ]; then
  echo "Missing environment file: $DEPLOY_DIR/$COMPOSE_ENV_FILE"
  exit 1
fi

COMPOSE_PROJECT_NAME="${COMPOSE_PROJECT_NAME:-labmanagement}"

compose() {
  docker compose \
    --project-name "$COMPOSE_PROJECT_NAME" \
    --env-file "$COMPOSE_ENV_FILE" \
    -f "$COMPOSE_FILE" \
    "$@"
}

compose config --quiet

compose_up_log="$(mktemp)"
trap 'rm -f "$compose_up_log"' EXIT

if ! compose up -d --build --remove-orphans backend frontend 2>&1 | tee "$compose_up_log"; then
  if ! grep -Eq 'Conflict|already in use' "$compose_up_log"; then
    exit 1
  fi

  echo "Found stale LabManagement containers; removing them and retrying deployment."
  mapfile -t stale_containers < <(
    docker ps -aq --format '{{.ID}}\t{{.Names}}' |
      awk -F '\t' '$2 ~ /(^|_)labmanagement-(db|backend|frontend|caddy)-[0-9]+$/ { print $1 }'
  )

  if [ "${#stale_containers[@]}" -eq 0 ]; then
    echo "No stale LabManagement containers were found."
    exit 1
  fi

  docker rm -f "${stale_containers[@]}"
  compose up -d --build --remove-orphans backend frontend
fi

compose ps
