# Domino Majlis PRO Web Preview Deployment

## Safety boundary

The preview is isolated on `agent/web-preview-foundation`. It does not modify the existing .NET MAUI project or its local application data.

## Static web preview

1. Open repository Settings.
2. Open Pages.
3. Select GitHub Actions as the source.
4. Run the `Deploy Web Preview` workflow.

Expected URL:

`https://kingabdulla.github.io/DominoMajlisPRO/`

## Preview API on Render

1. Create a new Blueprint in Render.
2. Connect `kingAbdulla/DominoMajlisPRO`.
3. Select the `agent/web-preview-foundation` branch.
4. Render will read `render.yaml` and create the Docker web service plus persistent disk.
5. Copy the generated HTTPS service URL.

Open the web preview once with:

`https://kingabdulla.github.io/DominoMajlisPRO/?api=https://YOUR-RENDER-SERVICE.onrender.com`

The page stores the API address locally for later visits.

## Validation

The `Web Preview API CI` workflow builds the .NET project, builds the Docker image, starts the container, and runs `DominoMajlisPRO.Api/smoke-test.sh` to validate health, registration, bearer authentication, team creation, team retrieval, logout, and token revocation.

## Current limitation

This remains a separate preview identity and data store. It is not yet the production migration of the MAUI application's local JSON data.
