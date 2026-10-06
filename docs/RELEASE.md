# Environments, Releases, and Rollback

## Branch/environment model
- Feature branches: isolated development.
- `develop`: integration branch; produces internal/dev artifacts.
- release candidate tag/branch: staging/TestFlight/internal testing when introduced.
- `main`: production-ready history only; human-approved merge required.

Environments should differ through build profiles/configuration, not divergent code copies.

## Production control
Agents may prepare lower-environment artifacts only when explicitly instructed. Production actions require explicit human instruction in the current task.

## Versioning
Use semantic product versions once external testing begins (`0.x.y`). Store build numbers separately for iOS/Android.

## Immutable artifacts
Release artifacts must be tied to a commit SHA/tag and retained long enough to reproduce/diagnose a release.

## Rollback
For client regressions:
1. disable remotely configurable risky behavior if available;
2. prepare a hotfix from the last known good tag or forward-fix branch;
3. never rewrite an already published tag;
4. preserve affected puzzle seeds and telemetry context for reproduction.

App stores limit instantaneous binary rollback, so risky monetization/live-service behavior should be kill-switchable independently of app binaries when those systems are later added.
