# Religion Schema

Religion data for global state and per-player religion.

## Endpoints

| Endpoint | Description |
|----------|-------------|
| `GET /religions` | Global religion state |
| `GET /player/{index}/religion` | Player's religion state |

## Global Religion Fields

| Field | Type | Description |
|-------|------|-------------|
| `religionType` | string | Religion type (e.g., `RELIGION_ZOROASTRIANISM`) |
| `isFounded` | boolean | Whether religion has been founded |
| `headCharacterId` | integer? | Head of religion character ID |
| `holyCityId` | integer? | Holy city ID |

## Example Global Religion

```json
[
  {
    "religionType": "RELIGION_ZOROASTRIANISM",
    "isFounded": true,
    "headCharacterId": 45,
    "holyCityId": 3
  },
  {
    "religionType": "RELIGION_JUDAISM",
    "isFounded": false,
    "headCharacterId": null,
    "holyCityId": null
  }
]
```

## Player Religion Fields

| Field | Type | Description |
|-------|------|-------------|
| `stateReligion` | string? | Player's state religion type |
| `religionCount` | object | Count of tiles/cities per religion |

## Example Player Religion

```json
{
  "stateReligion": "RELIGION_ZOROASTRIANISM",
  "religionCount": {
    "RELIGION_ZOROASTRIANISM": 12,
    "RELIGION_JUDAISM": 3
  }
}
```
