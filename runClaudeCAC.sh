#!/usr/bin/env bash
set -Eeuo pipefail

CYAN='\033[1;36m'
GREEN='\033[1;32m'
YELLOW='\033[1;33m'
RED='\033[1;31m'
RESET='\033[0m'

# Updated path for your Congressional App Challenge workspace (WSL path convention)
PROJECT="/mnt/c/Users/Dhruveen/Desktop/CongressionalAppChallenge/Dhruveen-Project"

clear
echo -e "${CYAN}============================================================${RESET}"
echo -e "${GREEN}     Congressional App Challenge - MoneyMap AI Automation     ${RESET}"
echo -e "${CYAN}============================================================${RESET}"
echo -e "${YELLOW}[*] Model      :${RESET} opus (Claude Opus)"
echo -e "${YELLOW}[*] Effort     :${RESET} high"
echo -e "${YELLOW}[*] Mode       :${RESET} Unattended (--dangerously-skip-permissions)"
echo -e "${YELLOW}[*] Workspace  :${RESET} ${PROJECT}"
echo -e "${CYAN}============================================================${RESET}\n"

mkdir -p "$PROJECT"
cd "$PROJECT"

if [[ ! -f "PROMPT.md" ]]; then
    echo -e "${RED}[ERROR] Missing required file: PROMPT.md in $PROJECT${RESET}"
    echo -e "${YELLOW}[*] Please save your detailed application prompt into PROMPT.md before running this script.${RESET}"
    exit 1
fi

command -v claude >/dev/null 2>&1 || {
    echo -e "${RED}[ERROR] Claude Code CLI not found or not in PATH.${RESET}"
    exit 1
}

echo -e "${GREEN}[+] Prerequisites verified successfully.${RESET}"
echo -e "${YELLOW}[*] Starting autonomous engineering session...${RESET}\n"

set +e
claude \
  --model opus \
  --effort high \
  --dangerously-skip-permissions \
  --add-dir "$PROJECT" \
  "Read PROMPT.md completely before implementation. Treat it as the governing specification and mandatory execution policy for building the MoneyMap AI personal finance application for the Congressional App Challenge. Inspect the repository, establish the architecture, implement core features (onboarding, dashboard, budget builder, savings goals, investment simulator, AI assistant), ensure WCAG 2.2 AA accessibility, write automated tests, and produce the required deliverables and final report. Work autonomously, preserve unrelated changes, and deliver a production-ready application."
CLAUDE_STATUS=$?
set -e

if [[ "$CLAUDE_STATUS" -eq 0 ]]; then
    echo -e "${GREEN}[+] Claude process exited successfully (status 0).${RESET}"
else
    echo -e "${RED}[ERROR] Claude process exited with status ${CLAUDE_STATUS}.${RESET}"
fi

echo -e "${YELLOW}[*] Review generated source files, tests, and documentation in ${PROJECT}.${RESET}"