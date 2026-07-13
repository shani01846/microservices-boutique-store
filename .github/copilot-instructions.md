# Copilot Project Instructions

This repository contains multiple e-commerce codebases (monolith and microservices).

## Scope and Intent

- Prefer working in `microservices-new/` when the user asks about microservices.
- Treat `legacy/` as older reference code unless the user explicitly requests edits there.
- Keep root `README.md` aligned with the current demo assets in `Images/`.

## Coding Conventions

- Backend services target .NET 8 and should keep minimal APIs/controllers clean and small.
- Frontend uses Next.js + TypeScript + Tailwind; keep components simple and composable.
- Avoid broad refactors unless requested.

## Safety and Git

- Never run destructive git commands (`git reset --hard`, `git checkout --`) unless explicitly requested.
- Before commit/push operations, always:
  1. Review changed files.
  2. Write a clear commit message.
  3. Confirm branch is correct.

## Documentation

- Update docs when behavior changes.
- For UI changes, include screenshot references when available under `Images/`.
