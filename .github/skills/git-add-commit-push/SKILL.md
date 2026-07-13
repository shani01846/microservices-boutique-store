---
name: git-add-commit-push
user-invocable: true
description: Use when the user asks to stage all changes, create a commit, and push to remote (phrases like "git add .", "commit", "push", "תעשה push", "תעלה לגיטהאב").
---

# Git Add Commit Push Skill

Use this workflow when the user asks to send local changes to GitHub.

## Steps

1. Check repository status:

```bash
git status --short --branch
```

2. Stage all tracked and untracked changes:

```bash
git add .
```

3. Create a commit with a meaningful message supplied by the user. If no message is provided, ask for one:

```bash
git commit -m "<message>"
```

4. Push current branch to origin:

```bash
git push
```

## Validation

After push, show a short summary:

- Current branch name
- Commit hash and subject of latest commit
- Push result (success/failure)

## Safety Rules

- Never force push unless user explicitly asks.
- Do not amend commits unless user explicitly asks.
- If merge conflicts or rejected push occur, report exact issue and suggest next command.
