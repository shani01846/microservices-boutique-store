CI/CD Setup

- CI: `.github/workflows/ci.yml`
  - Runs on push and PR to `main`
  - Sets up .NET 8 and Node.js, restores and builds backend, runs tests, builds frontend

- CD: `.github/workflows/cd.yml`
  - Triggers on tag push (v*)
  - Builds Docker image for `api-gateway` and pushes to GitHub Container Registry

Usage

1. Ensure repository secrets are set for any external registries (Docker Hub) or adjust `cd.yml` to use `GITHUB_TOKEN` for `ghcr.io`.
2. Create a tag and push, e.g.:

```bash
git tag v1.0.0
git push origin v1.0.0
```

Next steps

- Add per-service Docker builds and push steps in `cd.yml`.
- Add integration test job using `docker-compose.test.yml` to run end-to-end tests in CI.
