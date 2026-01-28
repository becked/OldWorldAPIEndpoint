# Events Schema

Events are detected by comparing game state between turns. The API identifies 10 event types across 4 categories.

## Character Events

### characterBorn

A new character was born.

```json
{
  "eventType": "characterBorn",
  "characterId": 150,
  "parentIds": [42, 43]
}
```

### characterDied

A character died.

```json
{
  "eventType": "characterDied",
  "characterId": 50,
  "deathReason": "TEXT_DEATH_OLD_AGE"
}
```

### leaderChanged

A nation's leader changed.

```json
{
  "eventType": "leaderChanged",
  "playerId": 0,
  "newLeaderId": 100,
  "oldLeaderId": 42
}
```

### heirChanged

A nation's heir changed.

```json
{
  "eventType": "heirChanged",
  "playerId": 0,
  "newHeirId": 101,
  "oldHeirId": 100
}
```

### characterMarried

Two characters married.

```json
{
  "eventType": "characterMarried",
  "character1Id": 42,
  "character2Id": 200
}
```

## Unit Events

### unitCreated

A new unit was created.

```json
{
  "eventType": "unitCreated",
  "unitId": 50,
  "unitType": "UNIT_WARRIOR",
  "playerId": 0,
  "location": {
    "tileId": 123,
    "x": 10,
    "y": 15
  }
}
```

### unitKilled

A unit was destroyed.

```json
{
  "eventType": "unitKilled",
  "unitId": 42,
  "unitType": "UNIT_WARRIOR",
  "lastOwnerId": 0,
  "lastLocation": {
    "tileId": 123,
    "x": 10,
    "y": 15
  }
}
```

## City Events

### cityFounded

A new city was founded.

```json
{
  "eventType": "cityFounded",
  "cityId": 10,
  "cityName": "Alexandria",
  "playerId": 2,
  "location": {
    "tileId": 456,
    "x": 20,
    "y": 25
  }
}
```

### cityCapture

A city was captured.

```json
{
  "eventType": "cityCapture",
  "cityId": 5,
  "cityName": "Memphis",
  "oldOwnerId": 0,
  "newOwnerId": 1,
  "wasTribe": false
}
```

## Wonder Events

### wonderCompleted

A wonder was completed.

```json
{
  "eventType": "wonderCompleted",
  "wonder": "IMPROVEMENT_GREAT_PYRAMID",
  "cityId": 0,
  "playerId": 0,
  "tribeType": null
}
```

## API Endpoints

- `GET /character-events` - Character events from last turn
- `GET /unit-events` - Unit events from last turn
- `GET /city-events` - City events from last turn

Events are also included in the TCP broadcast message:

```json
{
  "event": "newTurn",
  "turn": 5,
  "characterEvents": [...],
  "unitEvents": [...],
  "cityEvents": [...],
  "wonderEvents": [...]
}
```

## Example Queries

```bash
# Get character events
curl -s localhost:9877/character-events | jq

# Get deaths from last turn
curl -s localhost:9877/character-events | jq '.[] | select(.eventType == "characterDied")'

# Get unit casualties
curl -s localhost:9877/unit-events | jq '.[] | select(.eventType == "unitKilled")'
```

## JSON Schema

See [events.schema.json](events.schema.json)
