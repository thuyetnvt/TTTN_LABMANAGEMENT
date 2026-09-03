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

docker compose \
  --env-file .env.production \
  -f docker-compose.prod.yml \
  config --quiet

docker compose \
  --env-file .env.production \
  -f docker-compose.prod.yml \
  up -d --build backend frontend

docker compose \
  --env-file .env.production \
  -f docker-compose.prod.yml \
  ps
