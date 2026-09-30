# Implementation Plan: Latest NOK/EUR Exchange Rate

**Branch**: `001-nok-eur-rates` | **Date**: 2026-09-30 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-nok-eur-rates/spec.md`

## Summary

Add a standalone .NET 10 Azure Functions HTTP-triggered app that returns the latest published EUR/NOK spot observation from Norges Bank. Query the Norges Bank EXR series with its latest-observation option, validate the returned currencies, tenor, rate, and effective date, then return the normalized result as JSON. Invalid or unavailable upstream data must produce an HTTP failure, never a successful rate response.

## Technical Context

**Language/Version**: C# / .NET 10 (`net10.0`)

**Primary Dependencies**: Azure Functions v4 isolated worker, .NET HTTP client factory, built-in JSON/CSV parsing; use the currently supported Azure Functions worker and build SDK packages

**Storage**: None; rate data is fetched on demand and is not persisted or cached

**Testing**: xUnit, following the repository's existing .NET test conventions; mock the upstream HTTP response for deterministic tests

**Target Platform**: Azure Functions v4 isolated worker; local development with Azure Functions Core Tools v4

**Project Type**: HTTP-triggered web service, added as a separate function app alongside the existing MQTT worker

**Performance Goals**: Meet SC-002: at least 95% of requests with valid source data return within 5 seconds under normal operating conditions

**Constraints**: The response must preserve the source observation date and NOK-per-EUR direction. Bound the upstream request duration; do not cache or fabricate a value. Linux .NET 10 deployments require a supported plan such as Flex Consumption rather than Linux Consumption.

**Scale/Scope**: One latest-rate endpoint and one fixed currency series (EUR base, NOK quote, spot); no history/range queries, storage, or deployment provisioning in this feature

## Constitution Check

The checked-in constitution is still the unfilled template: it contains placeholder principle names and descriptions, not ratified normative rules. No applicable MUST gates or conflicts can be derived from it. **Gate: PASS (no actionable principles defined).**

Re-check after design: **PASS**; the design adds one independently testable function app, uses existing .NET/xUnit conventions, and introduces no persistence or additional service boundary beyond the requested HTTP function and its upstream API call.

## Project Structure

### Documentation (this feature)

```text
specs/001-nok-eur-rates/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── openapi.yaml
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── MqttForwarder/                 # Existing worker; unchanged
└── NokEurRateFunction/
    ├── Program.cs                 # Isolated-worker host and DI setup
    ├── Functions/                 # HTTP-trigger function
    ├── Services/                  # Norges Bank client and rate validation
    ├── Contracts/                 # Function response DTOs
    ├── host.json
    └── local.settings.json.example
tests/
├── MqttForwarder.Tests/           # Existing tests; unchanged
└── NokEurRateFunction.Tests/      # Function and source-client tests
```

**Structure Decision**: Use a new `src/NokEurRateFunction` isolated-worker project and a corresponding xUnit test project. This keeps Azure Functions hosting concerns out of the existing MQTT worker while fitting the repository's current `src/` and `tests/` layout. Add both projects to the solution.

## Complexity Tracking

No constitution violations or additional architectural complexity require justification.
