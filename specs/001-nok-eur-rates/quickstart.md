# Quickstart: Latest NOK/EUR Exchange Rate

## Prerequisites

- .NET 10 SDK
- Azure Functions Core Tools v4

## Run locally

1. Restore and build the solution:

   ```sh
   dotnet restore
   dotnet build
   ```

2. Start the function app:

   ```sh
   func start --project src/NokEurRateFunction
   ```

3. Request the latest rate using the function key printed by Core Tools:

   ```sh
   curl -H "x-functions-key: <function-key>" \
     http://localhost:7071/api/exchange-rate/latest
   ```

   A successful request returns HTTP 200 and JSON with EUR as `baseCurrency`, NOK as `quoteCurrency`, a positive numeric `rate`, and an `effectiveDate` supplied by Norges Bank. The effective date may be earlier than the request date.

## Validate behavior

Run the function's tests:

```sh
dotnet test tests/NokEurRateFunction.Tests
```

The test suite should cover a valid source row, an older latest observation, malformed or mismatched source data, an empty response, an upstream failure, and an upstream timeout. Those failure cases must return non-2xx responses and must not include a successful rate payload.

## Contract and model

See [contracts/openapi.yaml](contracts/openapi.yaml) for the HTTP contract and [data-model.md](data-model.md) for source-field validation and response semantics.
