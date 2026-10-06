# ADR 0001: Unity and deterministic domain architecture

Status: Accepted
Date: 2026-10-06

## Decision
Use Unity 6.3 LTS for the mobile client. Keep puzzle generation, solving, validation, and most session rules in pure deterministic C# separated from MonoBehaviour/UI.

## Why
The project targets iOS/Android and may later use IAP, ads, analytics, store services, and native mobile integrations. Unity provides a mature mobile ecosystem while pure C# domain logic keeps the hardest puzzle code fast to test and reproducible.

## Consequences
- Unity-specific code is an adapter/presentation layer.
- Seeds and generator versions are first-class debugging data.
- Puzzle correctness can be tested in EditMode without scenes.
