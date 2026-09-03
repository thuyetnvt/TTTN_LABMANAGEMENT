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

docker compose \
  --env-file "$COMPOSE_ENV_FILE" \
  -f "$COMPOSE_FILE" \
  config --quiet

docker compose \
  --env-file "$COMPOSE_ENV_FILE" \
  -f "$COMPOSE_FILE" \
  up -d --build backend frontend

docker compose \
  --env-file "$COMPOSE_ENV_FILE" \
  -f "$COMPOSE_FILE" \
  ps
