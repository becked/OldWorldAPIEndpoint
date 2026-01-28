# Unit Schema

Units represent military and civilian units in the game.

## Endpoints

| Endpoint | Description |
|----------|-------------|
| `GET /units` | All living units |
| `GET /unit/{id}` | Single unit by ID |
| `GET /player/{index}/units` | Units owned by player |

## Fields

| Field | Type | Description |
|-------|------|-------------|
| `id` | integer | Unique unit ID |
| `unitType` | string | Unit type (e.g., `UNIT_WARRIOR`, `UNIT_WORKER`) |
| `ownerId` | integer? | Owning player index, null if no owner |
| `tileId` | integer | Current tile ID |
| `x` | integer | X coordinate |
| `y` | integer | Y coordinate |
| `hp` | integer | Current hit points |
| `hpMax` | integer | Maximum hit points |
| `damage` | integer | Accumulated damage |
| `isAlive` | boolean | Whether unit is alive |
| `xp` | integer | Experience points |
| `level` | integer | Unit level |
| `turnSteps` | integer | Steps taken this turn |
| `cooldownTurns` | integer | Turns until cooldown expires |
| `fortifyTurns` | integer | Turns fortified |
| `createTurn` | integer | Turn unit was created |
| `generalId` | integer? | Attached general character ID |
| `hasGeneral` | boolean | Whether unit has a general |
| `isSleep` | boolean | Whether unit is sleeping |
| `isSentry` | boolean | Whether unit is on sentry |
| `isPass` | boolean | Whether unit has passed |
| `family` | string? | Family type if unit has family |
| `hasFamily` | boolean | Whether unit has family |
| `religion` | string? | Religion type if unit has religion |
| `hasReligion` | boolean | Whether unit has religion |
| `promotions` | string[] | List of promotion types |

## Example

```json
{
  "id": 1,
  "unitType": "UNIT_WORKER",
  "ownerId": 1,
  "tileId": 661,
  "x": 69,
  "y": 8,
  "hp": 20,
  "hpMax": 20,
  "damage": 0,
  "isAlive": true,
  "xp": 0,
  "level": 1,
  "turnSteps": 0,
  "cooldownTurns": 0,
  "fortifyTurns": 0,
  "createTurn": 1,
  "generalId": null,
  "hasGeneral": false,
  "isSleep": false,
  "isSentry": false,
  "isPass": false,
  "family": "FAMILY_IRTJET",
  "hasFamily": true,
  "religion": null,
  "hasReligion": false,
  "promotions": []
}
```
