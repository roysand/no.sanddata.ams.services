# Research: Latest NOK/EUR Exchange Rate

## Decision: Use Norges Bank's latest-observation CSV endpoint

- **Decision**: Request `https://data.norges-bank.no/api/data/EXR/B.EUR.NOK.SP?format=csvfile&lastNObservations=1`.
- **Rationale**: A live request returned one flat CSV record with explicit `BASE_CUR`, `QUOTE_CUR`, `TENOR`, `TIME_PERIOD`, and `OBS_VALUE` fields. The selected series is business-frequency EUR/NOK spot, and the latest-observation option naturally returns the most recent business-day observation when there is no current-day publication.
- **Alternatives considered**: SDMX-JSON is also available, but its series and observation dimensions are represented by indexed keys and require more complex mapping. Querying a date range is unnecessary for the latest-only requirement.
- **Source**: [Norges Bank API request](https://data.norges-bank.no/api/data/EXR/B.EUR.NOK.SP?format=csvfile&lastNObservations=1); [Norges Bank Open Data](https://www.norges-bank.no/en/topics/statistics/open-data/).
- **Observed response shape**:

  ```csv
  FREQ,BASE_CUR,QUOTE_CUR,TENOR,DECIMALS,CALCULATED,UNIT_MULT,COLLECTION,TIME_PERIOD,OBS_VALUE
  B,EUR,NOK,SP,4,false,0,C,2026-09-30,10.9015
  ```

  This is a dated daily spot observation, not an intraday quote. The rate is the number of NOK per EUR. The API may return an earlier effective date on weekends, holidays, or before a new observation is published.

## Decision: Implement as a .NET 10 isolated Azure Function

- **Decision**: Add a distinct .NET 10 Azure Functions v4 isolated-worker app with an HTTP trigger.
- **Rationale**: The feature explicitly requests an Azure Function; the repository uses .NET 10 and separates services under `src/`. Microsoft's current isolated-worker guidance supports .NET 10 on Functions v4 and standard .NET dependency injection.
- **Alternatives considered**: Extending the existing MQTT worker would mix an HTTP function runtime with an unrelated background worker. An in-process function app is not selected; isolated worker is Microsoft's supported model for .NET 10.
- **Source**: [Microsoft isolated-worker guide](https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide).
- **Hosting note**: .NET 10 is not supported on Linux Consumption; use Flex Consumption or another supported plan if deploying to Linux. Hosting-plan provisioning is outside the feature scope.

## Decision: Normalize success and failure responses

- **Decision**: Return JSON containing `baseCurrency`, `quoteCurrency`, `rate`, and `effectiveDate`; report upstream unavailable/invalid data as a non-success HTTP response with a concise error body.
- **Rationale**: This directly expresses the required currency direction and date while preventing consumers from interpreting absent or invalid data as a rate.
- **Alternatives considered**: Returning provider CSV or SDMX-JSON would leak provider-specific formatting into the consumer contract. Returning a stale cached rate would require storage and freshness rules not specified in the feature.

## Implementation constraints resolved

- Do not assume the observation date is today.
- Validate that the returned row is EUR/NOK spot, that the rate parses as a finite positive number, and that `TIME_PERIOD` is a valid date.
- Keep the source call bounded by a timeout and surface timeout/upstream/invalid-data failures distinctly at the HTTP boundary.
- No API credential is required for the tested public endpoint; the function itself uses Azure Functions' function-level authorization by default rather than adding a separate identity system.
