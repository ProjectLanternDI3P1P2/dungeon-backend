# Dungeon Continuous Delivery Procedure

## 1. Purpose

This document describes the Continuous Delivery (CD) workflow used by the **Dungeon backend**.

The CD workflow is separated from CI so Docker image publication runs are clearly visible in GitHub Actions.

Its purpose is to:
- build the Dungeon Docker image;
- publish it to the private Harbor registry;
- publish development tags from `dev`;
- publish release tags created from application releases;
- verify the published image;
- provide a delivery summary with tags and digest.

## 2. Workflow file

The CD workflow is stored in:

```text
.github/workflows/cd.yaml
```

## 3. Docker registry

Dungeon images are published to Harbor:

```text
registry.lantern.diiage
```

Image name:

```text
registry.lantern.diiage/lantern/dungeon-backend
```

The workflow uses:

```yaml
env:
  REGISTRY: registry.lantern.diiage
  IMAGE: registry.lantern.diiage/lantern/dungeon-backend
```

## 4. Triggers

The workflow can be started manually with:

```yaml
workflow_dispatch:
```

Manual publication is restricted to the `dev` branch.

The workflow also starts automatically for application release tags:

```yaml
push:
  tags: ["v*"]
```

Examples:

```text
v1.0.0
v1.1.0
v2.0.0
```

Contract tags such as `contracts-v*` are not intended to publish the Dungeon application image.

## 5. Development publication

A manual run on `dev` publishes:

```text
registry.lantern.diiage/lantern/dungeon-backend:dev-latest
```

The workflow also creates an immutable digest-based tag:

```text
registry.lantern.diiage/lantern/dungeon-backend:dev-<digest>
```

`dev-latest` can move to a newer development image, while the digest-based tag always identifies one exact image.

## 6. Release publication

When an application release tag such as `v1.2.0` is created, the workflow publishes:

```text
registry.lantern.diiage/lantern/dungeon-backend:v1.2.0
registry.lantern.diiage/lantern/dungeon-backend:main-latest
```

Both tags are created from the same Docker build and point to the same image digest.

## 7. Concurrency

The workflow uses:

```yaml
concurrency:
  group: cd-docker-${{ github.ref }}
  cancel-in-progress: true
```

This prevents unnecessary concurrent publication runs for the same Git reference.

## 8. Runner

The publication job runs on:

```yaml
runs-on: lantern-k3s-builders
```

This runner is used by the Lantern infrastructure and can access the private Harbor registry.

## 9. Checkout

The workflow checks out the repository source code with `actions/checkout`.

Credentials are not persisted:

```yaml
with:
  persist-credentials: false
```

## 10. Image tag computation

The workflow reads the current Git commit SHA:

```bash
sha="$(git rev-parse HEAD)"
```

For a release tag, it creates:

```text
vX.Y.Z
main-latest
```

For a manual development run, it creates:

```text
dev-latest
```

## 11. Docker Buildx

Docker Buildx is configured before the image build.

The workflow uses:

```yaml
driver: docker
```

The Lantern runner Docker daemon trusts Harbor's private CA.

## 12. Harbor authentication

The workflow authenticates to Harbor with GitHub secrets:

```yaml
registry: ${{ env.REGISTRY }}
username: ${{ secrets.HARBOR_USERNAME }}
password: ${{ secrets.HARBOR_TOKEN }}
```

Required secrets:

```text
HARBOR_USERNAME
HARBOR_TOKEN
```

Secret values must never be committed to the repository.

## 13. Build and push

The workflow uses `docker/build-push-action` to build and push the image.

Main settings:

```yaml
context: .
platforms: linux/amd64
push: true
tags: ${{ steps.image.outputs.tags }}
```

The workflow also adds OCI metadata linking the image to its GitHub repository and Git commit.

## 14. Digest tag

For a `dev` publication, the workflow validates that the returned image digest follows the expected `sha256` format.

It then creates:

```text
dev-<digest>
```

The workflow checks that the digest of this new tag matches the original image digest exactly.

If it does not match, the workflow fails.

## 15. Published image verification

After publication, every generated image tag is inspected.

For development:

```text
dev-latest
dev-<digest>
```

For a release:

```text
vX.Y.Z
main-latest
```

If an image cannot be inspected, the workflow fails.

## 16. Delivery summary

At the end of a successful run, GitHub Actions writes a summary containing:
- the publication source;
- the Docker image digest;
- all published tags.

Example:

```text
Docker image published

Source: dev commit <sha>
Digest: sha256:<digest>

Tags:
- registry.lantern.diiage/lantern/dungeon-backend:dev-latest
- registry.lantern.diiage/lantern/dungeon-backend:dev-<digest>
```

## 17. Development flow

```text
Developer
   |
   v
feature branch
   |
   v
Pull Request to dev
   |
   v
CI validation
   |
   v
Merge into dev
   |
   v
Manual CD workflow
   |
   v
lantern-k3s-builders
   |
   v
Build Docker image
   |
   v
Harbor
   |
   +-- dungeon-backend:dev-latest
   |
   +-- dungeon-backend:dev-<digest>
```

## 18. Release flow

```text
dev
 |
 v
Promotion to main
 |
 v
Release Please
 |
 v
Application tag vX.Y.Z
 |
 v
CD workflow
 |
 v
Build Docker image
 |
 v
Harbor
 |
 +-- dungeon-backend:vX.Y.Z
 |
 +-- dungeon-backend:main-latest
```

## 19. Result

The Dungeon CD workflow provides:
- development publication with `dev-latest`;
- immutable development images with `dev-<digest>`;
- versioned releases with `vX.Y.Z`;
- a latest release pointer with `main-latest`;
- publication to the private Harbor registry;
- digest verification;
- traceability between Docker images and Git commits.

The workflow publishes Docker images only. Deployment to Kubernetes or another runtime is handled separately.
