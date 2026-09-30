# Data Model: Latest NOK/EUR Exchange Rate

## Rate request

Represents a caller's request for the one supported latest rate.

| Field | Type | Constraints |
|---|---|---|
| Pair | Fixed currency pair | EUR base and NOK quote; no caller-supplied pair or date range in this feature |

## Norges Bank observation

Represents a single source row from the `EXR/B.EUR.NOK.SP` series.

| Field | Source column | Type | Validation |
|---|---|---|---|
| Frequency | `FREQ` | String | Must be `B` (business frequency) |
| Base currency | `BASE_CUR` | String | Must be `EUR` |
| Quote currency | `QUOTE_CUR` | String | Must be `NOK` |
| Tenor | `TENOR` | String | Must be `SP` (spot) |
| Effective date | `TIME_PERIOD` | ISO 8601 date | Required and parseable as a calendar date |
| Rate | `OBS_VALUE` | Decimal | Required, parseable, finite, and greater than zero |

Exactly one latest observation is requested. Empty responses, missing required columns/values, malformed rows, mismatched series dimensions, or invalid dates/rates are rejected.

## Exchange rate response

The public success representation returned by the HTTP function.

| Field | Type | Constraints |
|---|---|---|
| `baseCurrency` | String | Always `EUR` |
| `quoteCurrency` | String | Always `NOK` |
| `rate` | Number | Positive NOK amount for one EUR; preserve source decimal precision |
| `effectiveDate` | String | ISO 8601 date from the source; not necessarily today's date |

## Failure response

No exchange-rate entity is returned on failure. The HTTP response contains an error code and concise message, with a non-2xx status for upstream unavailability, timeout, or unusable source data.
