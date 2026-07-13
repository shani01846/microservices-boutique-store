# Project Instructions

## Focus

- For microservices work, prefer the code under `microservices-new/`.
- Treat `legacy/` as reference only unless explicitly requested.

## Code Style

- Backend: .NET 8, keep APIs and controllers focused and small.
- Frontend: Next.js + TypeScript + Tailwind, build small reusable components.
- Avoid broad refactors unless requested.

## Git Safety

- Do not run destructive git commands unless explicitly requested.
- Before `git add .`, `git commit`, `git push` always:
  1. Review changed files.
  2. Confirm active branch.
  3. Use a clear commit message.

## Docs

- When behavior/UI changes, update docs.
- Keep `README.md` aligned with assets in `Images/`.

## Recommended Git Publish Flow

```bash
git status --short --branch
git add .
git commit -m "<clear-message>"
git push
```
