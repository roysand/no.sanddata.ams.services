# Feature Specification: Latest NOK/EUR Exchange Rate

**Feature Branch**: `main`

**Created**: 2026-09-30

**Status**: Draft

**Input**: User description: "Implement a Azure function to get NOK exchange rates against EURO from Bank of Norway API"

## User Scenarios & Testing

### User Story 1 - Retrieve the latest NOK/EUR rate (Priority: P1)

A system or service that needs a current exchange rate requests the latest published rate for Norwegian kroner against euros and receives the rate together with the currency pair and the date it applies to.

**Why this priority**: Providing a reliable, clearly identified rate is the core value of the feature.

**Independent Test**: Request the latest rate when the source has published a valid rate and verify that the response identifies the rate, both currencies, and its effective date.

**Acceptance Scenarios**:

1. **Given** a valid latest published rate is available, **When** a consumer requests the NOK/EUR rate, **Then** the result includes the numeric rate, identifies euros as the base currency and Norwegian kroner as the quote currency, and includes the rate's effective date.
2. **Given** the request occurs on a day with no new published rate, **When** a consumer requests the latest rate, **Then** the most recent valid published rate is returned with its actual effective date.

### User Story 2 - Handle unavailable or invalid rate data (Priority: P2)

A consumer receives a clear failure outcome when a valid rate cannot be obtained, rather than receiving an invented or misleading value.

**Why this priority**: Consumers must be able to distinguish a valid rate from unavailable or unusable source data.

**Independent Test**: Make the source unavailable or provide unusable rate data and verify that the request fails clearly without presenting a rate as valid.

**Acceptance Scenarios**:

1. **Given** the rate source is unavailable, **When** a consumer requests the latest rate, **Then** the result communicates that the rate could not be obtained.
2. **Given** the source returns missing, malformed, or mismatched currency data, **When** a consumer requests the latest rate, **Then** the data is rejected and no rate is presented as valid.

### Edge Cases

- The source has not published a new rate for the current day, including weekends and public holidays.
- The source is unavailable or does not respond to a request.
- The source returns no matching rate, a missing value, an invalid value, or a rate for a different currency pair.
- The source data does not include an effective date.

## Requirements

### Functional Requirements

- **FR-001**: The system MUST provide the latest valid published exchange rate for Norwegian kroner (NOK) against euros (EUR) from Bank of Norway's published exchange-rate data.
- **FR-002**: Each successful result MUST identify the base currency as EUR, the quote currency as NOK, the numeric rate, and the rate's effective date.
- **FR-003**: The system MUST use the most recent valid published rate when no rate has been published for the current day, and MUST identify the date to which that rate applies.
- **FR-004**: The system MUST reject source data that is missing a rate or effective date, is invalid, or does not match the requested currency pair.
- **FR-005**: When a valid rate cannot be obtained, the system MUST communicate a failure to the caller and MUST NOT present an invalid or fabricated value as a successful result.

### Key Entities

- **Exchange rate**: The published value relating EUR to NOK, including its base currency, quote currency, and effective date.
- **Rate request**: A consumer's request for the latest available exchange rate for the specified currency pair.

## Success Criteria

### Measurable Outcomes

- **SC-001**: 100% of successful results identify the base currency, quote currency, numeric rate, and effective date.
- **SC-002**: At least 95% of requests with valid source data return a usable result within 5 seconds under normal operating conditions.
- **SC-003**: 100% of requests with unavailable or invalid source data produce a clear failure outcome and do not report an invalid rate as successful.

## Assumptions

- Consumers request the latest available rate on demand; historical date or date-range queries are outside this feature's scope.
- The rate is expressed as the amount of NOK for one EUR, and the result makes that direction explicit.
- If no rate has been published for the current day, the latest valid published rate is acceptable as long as its effective date is included.
- Bank of Norway publishes the requested EUR/NOK rate and an effective date.
