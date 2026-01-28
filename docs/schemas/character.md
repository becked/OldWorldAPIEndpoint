# Character Schema

Characters are the individuals in Old World - leaders, heirs, governors, generals, and family members. Each character has ~85 fields organized into 24 categories.

## Field Categories

### 1. Identity

| Field | Type | Description |
|-------|------|-------------|
| `id` | integer | Unique character ID |
| `name` | string | First name |
| `suffix` | string | Dynasty/family name |
| `gender` | string | `Male` or `Female` |
| `age` | integer | Current age |
| `characterType` | string \| null | Predefined type (e.g., `CHARACTER_CLEOPATRA`) |

### 2. Player & Nation

| Field | Type | Description |
|-------|------|-------------|
| `playerId` | integer \| null | Owning player index |
| `nation` | string \| null | Nation type |
| `tribe` | string \| null | Tribe type if tribal |

### 3. Status

| Field | Type | Description |
|-------|------|-------------|
| `isAlive` | boolean | Currently alive |
| `isDead` | boolean | Currently dead |
| `isRoyal` | boolean | Royal family member |
| `isAdult` | boolean | Adult (can hold positions) |
| `isTemporary` | boolean | Temporary character |

### 4. Leadership & Succession

| Field | Type | Description |
|-------|------|-------------|
| `isLeader` | boolean | Current nation leader |
| `isHeir` | boolean | Designated heir |
| `isSuccessor` | boolean | Potential successor |
| `isLeaderSpouse` | boolean | Spouse of leader |
| `isHeirSpouse` | boolean | Spouse of heir |
| `isRegent` | boolean | Serving as regent |

### 5. Jobs & Positions

| Field | Type | Description |
|-------|------|-------------|
| `job` | string \| null | Job type (e.g., `JOB_AMBASSADOR`) |
| `council` | string \| null | Council position (e.g., `COUNCIL_CHANCELLOR`) |
| `courtier` | string \| null | Courtier role |

### 6. Governor/Agent

| Field | Type | Description |
|-------|------|-------------|
| `isCityGovernor` | boolean | Is governing a city |
| `cityGovernorId` | integer \| null | City being governed |
| `isCityAgent` | boolean | Is an agent |
| `cityAgentId` | integer \| null | City where agent |

### 7. Military

| Field | Type | Description |
|-------|------|-------------|
| `hasUnit` | boolean | Commands a unit |
| `unitId` | integer \| null | Commanded unit ID |
| `isGeneral` | boolean | Is a general |

### 8. Family

| Field | Type | Description |
|-------|------|-------------|
| `family` | string \| null | Family type |
| `familyClass` | string \| null | Family class |
| `isFamilyHead` | boolean | Head of family |

### 9. Religion

| Field | Type | Description |
|-------|------|-------------|
| `religion` | string \| null | Personal religion |
| `isReligionHead` | boolean | Head of religion |

### 10. Parents (Adoptive)

| Field | Type | Description |
|-------|------|-------------|
| `fatherId` | integer \| null | Adoptive father ID |
| `motherId` | integer \| null | Adoptive mother ID |

### 11. Traits

| Field | Type | Description |
|-------|------|-------------|
| `archetype` | string \| null | Primary trait |
| `traits` | array | All trait types (e.g., `TRAIT_WARRIOR`) |

### 12. Ratings

| Field | Type | Description |
|-------|------|-------------|
| `ratings` | object | Map of rating type to value |

Rating types: `RATING_COURAGE`, `RATING_DISCIPLINE`, `RATING_CHARISMA`, `RATING_WISDOM`

### 13. XP & Level

| Field | Type | Description |
|-------|------|-------------|
| `xp` | integer | Experience points |
| `level` | integer | Character level |

### 14. Lifecycle Timeline

| Field | Type | Description |
|-------|------|-------------|
| `birthTurn` | integer | Turn born |
| `deathTurn` | integer | Turn died (-1 if alive) |
| `leaderTurn` | integer | Turn became leader |
| `abdicateTurn` | integer | Turn abdicated |
| `regentTurn` | integer | Turn became regent |
| `safeTurn` | integer | Turn became safe |
| `nationTurn` | integer | Turn joined nation |

### 15-24. Extended Fields

| Field | Type | Description |
|-------|------|-------------|
| `isInfertile` | boolean | Cannot have children |
| `isRetired` | boolean | Retired from service |
| `isAbdicated` | boolean | Abdicated leadership |
| `isOrWasLeader` | boolean | Was ever leader |
| `birthFatherId` | integer \| null | Biological father |
| `birthMotherId` | integer \| null | Biological mother |
| `birthCityId` | integer \| null | Birth city |
| `spouseIds` | array | Spouse character IDs |
| `numSpouses` | integer | Total spouses |
| `spousesAlive` | integer | Living spouses |
| `childrenIds` | array | Children character IDs |
| `numChildren` | integer | Total children |
| `title` | string \| null | Title (e.g., `TITLE_AUGUSTUS`) |
| `cognomen` | string \| null | Cognomen (e.g., `COGNOMEN_GREAT`) |
| `nickname` | string \| null | Nickname text |

### Relationships

```json
{
  "relationships": [
    { "type": "RELATIONSHIP_FRIEND", "characterId": 50 },
    { "type": "RELATIONSHIP_RIVAL", "characterId": 51 }
  ]
}
```

### Opinions

```json
{
  "opinions": {
    "0": { "opinion": "OPINION_CHARACTER_LOYAL", "rate": 5 },
    "1": { "opinion": "OPINION_CHARACTER_UPSET", "rate": -3 }
  }
}
```

## Example

```json
{
  "id": 42,
  "name": "Julius",
  "suffix": "Caesar",
  "gender": "Male",
  "age": 45,
  "nation": "NATION_ROME",
  "isLeader": true,
  "isAlive": true,
  "traits": ["TRAIT_WARRIOR", "TRAIT_AMBITIOUS"],
  "ratings": {
    "RATING_COURAGE": 8,
    "RATING_DISCIPLINE": 7,
    "RATING_CHARISMA": 9,
    "RATING_WISDOM": 6
  },
  "spouseIds": [43],
  "childrenIds": [100, 101, 102]
}
```

## API Endpoints

- `GET /characters` - All characters
- `GET /character/{id}` - Single character by ID
- `GET /character-events` - Character events from last turn

## Example Queries

```bash
# All living leaders
curl -s localhost:9877/characters | jq '.[] | select(.isLeader and .isAlive) | {name, nation, age}'

# Generals with ratings
curl -s localhost:9877/characters | jq '.[] | select(.isGeneral) | {name, ratings}'

# Characters with specific trait
curl -s localhost:9877/characters | jq '.[] | select(.traits | index("TRAIT_WARRIOR")) | .name'

# Family tree
curl -s localhost:9877/characters | jq '.[] | select(.isLeader) | {name, spouseIds, childrenIds}'
```

## JSON Schema

See [character.schema.json](character.schema.json)
