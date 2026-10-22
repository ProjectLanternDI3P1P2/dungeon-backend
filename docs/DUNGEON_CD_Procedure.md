# Dungeon CD Procedure

## 1. Purpose

This document describes the Continuous Delivery (CD) workflow of the **Dungeon** backend.

The CD is deliberately **separate from CI**, so delivery runs are visible on their own in GitHub Actions. Its role is to take the code that has been validated by CI and deliver the Docker image of the service to the platform.

The workflow itself is very small: it does not contain the build logic. It calls a **shared reusable workflow** maintained in the `platform-gitops` repository, so that every squad delivers its service in the same way.

## 2. Where the pieces live

| Item | Location |
|------|----------|
| Dungeon CD workflow | `.github/workflows/cd.yaml` (Dungeon repository) |
| Shared delivery logic | `ProjectLanternDI3P1P2/platform-gitops/.github/workflows/publish-and-promote.yaml` |
| Image registry | Harbor: `registry.lantern.diiage` |
| Dungeon image | `registry.lantern.diiage/lantern/dungeon-backend` |
| Dockerfile | See `Dungeon_Dockerfile_Procedure.md` |

## 3. The workflow

```yaml
name: CD - Publish and promote image

on:
  push:
    branches: [main]
    tags: ["v*"]
  workflow_dispatch:

concurrency:
  group: cd-${{ github.ref }}
  cancel-in-progress: true

jobs:
  delivery:
    if: github.event_name != 'workflow_dispatch' || github.ref == 'refs/heads/main'
    permissions:
      contents: read
      packages: read
    uses: ProjectLanternDI3P1P2/platform-gitops/.github/workflows/publish-and-promote.yaml@06761f0b31acaa488e39c909bcb422a1649924d7 # main
    with:
      application: dungeon
      image: registry.lantern.diiage/lantern/dungeon-backend
    secrets:
      HARBOR_USERNAME: ${{ secrets.HARBOR_USERNAME }}
      HARBOR_TOKEN: ${{ secrets.HARBOR_TOKEN }}
      GITOPS_DISPATCH_TOKEN: ${{ secrets.GITOPS_DISPATCH_TOKEN }}
```

## 4. How it works

### 4.1 Triggers

| Trigger | Effect |
|---------|--------|
| Push on `main` | Starts a delivery |
| Push of a tag `v*` (release, e.g. `v1.2.0`) | Starts a delivery for the release |
| Manual run (`workflow_dispatch`) | Allowed **only from `main`**; the `if:` condition skips the job on any other branch |

### 4.2 Concurrency

```yaml
concurrency:
  group: cd-${{ github.ref }}
  cancel-in-progress: true
```

If a newer run starts on the same branch or tag, the older run is cancelled. This avoids delivering an outdated version after a newer one.

### 4.3 Delegation to the shared workflow

The job uses `uses: ...@<commit SHA>` to call the reusable workflow. It passes:

- `application: dungeon`: the name of the application being delivered
- `image: registry.lantern.diiage/lantern/dungeon-backend`: the target image in Harbor

The reusable workflow is the single place where the publish and promote steps are implemented, and it is maintained centrally. As indicated by its name and by the secrets it receives, it publishes the Dungeon image to Harbor and then promotes the new version through the GitOps repository.

### 4.4 Secrets

| Secret | Used for |
|--------|----------|
| `HARBOR_USERNAME` / `HARBOR_TOKEN` | Authentication to the Harbor registry to push the image |
| `GITOPS_DISPATCH_TOKEN` | Triggering the promotion in the GitOps repository |

Secrets are stored in the GitHub repository settings and passed **explicitly** to the reusable workflow (not inherited implicitly). They never appear in the code.

### 4.5 Permissions

```yaml
permissions:
  contents: read
  packages: read
```

The job gets read-only access. `packages: read` allows the build to fetch private packages from GitHub Packages (used by the Dockerfile through a BuildKit secret).

### 4.6 Pinned reusable workflow

The reusable workflow is referenced by its full **commit SHA** (the `# main` comment only tells which branch it came from). Pinning prevents a change on the `main` branch of `platform-gitops` from silently altering how Dungeon is delivered, and protects against supply-chain tampering. Updating the pin is a deliberate action, done with a pull request.

## 5. Delivery flow

```
push on main / tag vX.Y.Z / manual run (main only)
                 |
                 v
          GitHub Actions
     (cd.yaml, concurrency control)
                 |
                 v
     Shared workflow: publish-and-promote
     (platform-gitops, pinned by SHA)
                 |
        +--------+---------+
        |                  |
        v                  v
  Build & publish     Promote the new version
  image to Harbor     through the GitOps repository
  (dungeon-backend)
```

## 6. How to use it

- **Normal flow**: merge a pull request into `main`; CD starts automatically.
- **Release**: when a release tag `vX.Y.Z` is created, CD delivers that release.
- **Manual delivery**: in GitHub, go to *Actions*, select *CD - Publish and promote image*, then *Run workflow* on the `main` branch.
- **Follow a run**: open the run in the *Actions* tab and check the logs of the `delivery` job.

## 7. Points to keep in mind

- The tags applied to the image, the exact promotion mechanism and the environments targeted are defined in the shared `publish-and-promote` workflow, not in the Dungeon repository. Refer to that repository for the details.
- A push on `main` and a release tag each start their own run (the concurrency group depends on the ref), so both may deliver.
- To change the delivery logic for all services, modify the shared workflow in `platform-gitops`, then update the SHA pin in each application's `cd.yaml`.
