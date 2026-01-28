# Naming Conventions Audit Report

This report analyzes how well the OldWorldAPIEndpoint mod follows the naming conventions established in Old World's reference XML files.

## Executive Summary

**Overall Status: EXCELLENT COMPLIANCE**

The API endpoint code correctly uses the game's `mzType` field pattern for all game identifiers. All type strings are dynamically retrieved from the game's `Infos` system, ensuring perfect consistency with the game's internal naming.

## Reference XML Naming Conventions

The game's XML files in `$OLDWORLD_PATH/Reference/XML/Infos/` define type identifiers using a consistent `<zType>` field with `PREFIX_NAME` format:

### Type Categories and Prefixes

| Category | Prefix | Example Values |
|----------|--------|----------------|
| **Yields** | `YIELD_` | `YIELD_FOOD`, `YIELD_MONEY`, `YIELD_ORDERS`, `YIELD_CIVICS`, `YIELD_TRAINING`, `YIELD_GROWTH`, `YIELD_CULTURE`, `YIELD_SCIENCE`, `YIELD_HAPPINESS`, `YIELD_DISCONTENT`, `YIELD_MAINTENANCE`, `YIELD_IRON`, `YIELD_STONE`, `YIELD_WOOD` |
| **Nations** | `NATION_` | `NATION_ROME`, `NATION_PERSIA`, `NATION_GREECE`, `NATION_EGYPT`, `NATION_BABYLONIA`, `NATION_ASSYRIA` |
| **Units** | `UNIT_` | `UNIT_SETTLER`, `UNIT_WARRIOR`, `UNIT_SCOUT`, `UNIT_ZOROASTRIANISM_DISCIPLE` |
| **Improvements** | `IMPROVEMENT_` | `IMPROVEMENT_ANCIENT_RUINS`, `IMPROVEMENT_FARM` |
| **Improvement Classes** | `IMPROVEMENTCLASS_` | `IMPROVEMENTCLASS_FARM`, `IMPROVEMENTCLASS_MINE`, `IMPROVEMENTCLASS_QUARRY`, `IMPROVEMENTCLASS_BARRACKS` |
| **Projects** | `PROJECT_` | `PROJECT_REPAIR`, `PROJECT_GOVERNOR`, `PROJECT_IMPORT_HORSE` |
| **Specialists** | `SPECIALIST_` | `SPECIALIST_FARMER`, `SPECIALIST_MINER`, `SPECIALIST_STONECUTTER` |
| **Specialist Classes** | `SPECIALISTCLASS_` | `SPECIALISTCLASS_FARMER`, `SPECIALISTCLASS_ACOLYTE`, `SPECIALISTCLASS_PHILOSOPHER` |
| **Traits** | `TRAIT_` | `TRAIT_COMMANDER_ARCHETYPE`, `TRAIT_AFFABLE`, `TRAIT_CRUEL`, `TRAIT_ROBUST` |
| **Ratings** | `RATING_` | `RATING_WISDOM`, `RATING_CHARISMA`, `RATING_COURAGE`, `RATING_DISCIPLINE` |
| **Cultures** | `CULTURE_` | `CULTURE_WEAK`, `CULTURE_DEVELOPING`, `CULTURE_STRONG`, `CULTURE_LEGENDARY` |
| **Religions** | `RELIGION_` | `RELIGION_ZOROASTRIANISM`, `RELIGION_JUDAISM`, `RELIGION_CHRISTIANITY` |
| **Families** | `FAMILY_` | `FAMILY_SARGONID`, `FAMILY_KASSITE`, `FAMILY_CHALDEAN` |
| **Family Classes** | `FAMILYCLASS_` | `FAMILYCLASS_LANDOWNERS`, `FAMILYCLASS_CHAMPIONS`, `FAMILYCLASS_CLERICS` |
| **Tribes** | `TRIBE_` | `TRIBE_REBELS`, `TRIBE_BARBARIANS`, `TRIBE_GAULS`, `TRIBE_RAIDERS` |
| **Diplomacy** | `DIPLOMACY_` | `DIPLOMACY_WAR`, `DIPLOMACY_TRUCE`, `DIPLOMACY_PEACE`, `DIPLOMACY_TEAM` |
| **War States** | `WARSTATE_` | `WARSTATE_ROUTED`, `WARSTATE_LOSING`, `WARSTATE_NEUTRAL`, `WARSTATE_WINNING`, `WARSTATE_TRIUMPHANT` |
| **Characters** | `CHARACTER_` | `CHARACTER_ASHURBANIPAL`, `CHARACTER_NEBUCHADNEZZAR`, `CHARACTER_DIDO` |
| **Jobs** | `JOB_` | `JOB_AMBASSADOR`, `JOB_CHANCELLOR`, `JOB_GENERAL`, `JOB_GOVERNOR` |
| **Council** | `COUNCIL_` | `COUNCIL_AMBASSADOR`, `COUNCIL_CHANCELLOR`, `COUNCIL_SPYMASTER` |
| **Courtiers** | `COURTIER_` | `COURTIER_SOLDIER` |
| **Opinion (Character)** | `OPINIONCHARACTER_` | `OPINIONCHARACTER_FURIOUS`, `OPINIONCHARACTER_ANGRY`, `OPINIONCHARACTER_CAUTIOUS`, `OPINIONCHARACTER_PLEASED`, `OPINIONCHARACTER_FRIENDLY` |
| **Relationships** | `RELATIONSHIP_` | `RELATIONSHIP_IN_LOVE_WITH`, `RELATIONSHIP_PLOTTING_AGAINST`, `RELATIONSHIP_ESTRANGED_FROM` |
| **Cognomens** | `COGNOMEN_` | `COGNOMEN_NEW`, `COGNOMEN_FOUNDER`, `COGNOMEN_WARRIOR`, `COGNOMEN_BRAVE` |
| **Titles** | `TITLE_` | `TITLE_LEADER`, `TITLE_HEIR`, `TITLE_LEADER_CONSORT` |
| **Text** | `TEXT_` | `TEXT_TRIBE_REBELS_HELP`, `TEXT_YIELD_FOOD`, etc. |

## API Implementation Analysis

### Correct Usage Pattern

The API correctly retrieves type strings using the `mzType` field from Info objects:

```csharp
// Yields (APIEndpoint.cs:203)
string yieldName = infos.yield(yieldType).mzType;  // Returns "YIELD_FOOD"

// Nations (APIEndpoint.cs:212)
nation = infos.nation(player.getNation()).mzType;  // Returns "NATION_ROME"

// Units (APIEndpoint.cs:506, 1113, 1151)
itemType = infos.unit((UnitType)build.miType)?.mzType;  // Returns "UNIT_WARRIOR"

// Improvements (APIEndpoint.cs:430)
improvements[infos.improvement(impType).mzType] = c;  // Returns "IMPROVEMENT_FARM"

// Projects (APIEndpoint.cs:466)
projects[infos.project(projType).mzType] = c;  // Returns "PROJECT_GOVERNOR"

// Specialists (APIEndpoint.cs:516)
itemType = infos.specialist((SpecialistType)build.miType)?.mzType;

// Traits (APIEndpoint.cs:718, 732)
return infos.trait(archetype)?.mzType;  // Returns "TRAIT_COMMANDER_ARCHETYPE"

// Ratings (APIEndpoint.cs:750)
string name = infos.rating(ratingType).mzType;  // Returns "RATING_COURAGE"

// Cultures (APIEndpoint.cs:309)
culture = infos.culture(city.getCulture())?.mzType;  // Returns "CULTURE_STRONG"

// Religions (APIEndpoint.cs:372, 628)
religions.Add(infos.religion(relType).mzType);  // Returns "RELIGION_ZOROASTRIANISM"

// Families (APIEndpoint.cs:304, 623)
family = city.hasFamily() ? infos.family(city.getFamily())?.mzType : null;

// Family Classes (APIEndpoint.cs:624)
familyClass = character.hasFamily() ? infos.familyClass(character.getFamilyClass())?.mzType : null;

// Tribes (APIEndpoint.cs:589, 1403, 1591)
tribe = character.isTribe() ? infos.tribe(character.getTribe())?.mzType : null;

// Diplomacy (APIEndpoint.cs:1524, 1653)
diplomacy = diplomacyInfo?.mzType;  // Returns "DIPLOMACY_WAR"

// War States (APIEndpoint.cs:1529, 1658)
warState = warStateInfo?.mzType;  // Returns "WARSTATE_WINNING"

// Jobs (APIEndpoint.cs:607)
job = character.isJob() ? infos.job(character.getJob())?.mzType : null;

// Council (APIEndpoint.cs:608, 691)
council = character.isCouncil() ? infos.council(character.getCouncil())?.mzType : null;

// Courtiers (APIEndpoint.cs:609)
courtier = character.isCourtier() ? infos.courtier(character.getCourtier())?.mzType : null;

// Opinion Characters (APIEndpoint.cs:827)
opinion = opinion != OpinionCharacterType.NONE
    ? infos.opinionCharacter(opinion)?.mzType : null;  // Returns "OPINIONCHARACTER_PLEASED"

// Relationships (APIEndpoint.cs:801)
type = infos.relationship(rel.meType)?.mzType;  // Returns "RELATIONSHIP_IN_LOVE_WITH"

// Cognomens (APIEndpoint.cs:697)
cognomen = character.hasCognomen() ? infos.cognomen(character.getCognomen())?.mzType : null;

// Titles (APIEndpoint.cs:696)
title = character.hasTitle() ? infos.title(character.getTitle())?.mzType : null;

// Text (Death Reasons) (APIEndpoint.cs:693, 911)
deathReason = character.getDeathReason() != TextType.NONE
    ? infos.text(character.getDeathReason())?.mzType : null;

// Improvement Classes (APIEndpoint.cs:448)
classes[infos.improvementClass(classType).mzType] = c;  // Returns "IMPROVEMENTCLASS_FARM"

// Character Types (APIEndpoint.cs:584)
characterType = character.hasCharacter() ? infos.character(character.getCharacter())?.mzType : null;
```

### Verified Compliance Points

1. **All type strings use `mzType` field** - The code never uses `ToString()` on enums, which would return numeric values. Instead, it consistently retrieves the human-readable `mzType` string from Info objects.

2. **Null safety** - The code properly uses null-conditional operators (`?.mzType`) to handle cases where Info objects might not exist.

3. **Conditional exposure** - Type strings are only emitted when the entity actually has a value (e.g., `character.hasFamily()` check before accessing `infos.family()`).

4. **Dynamic iteration** - The code correctly iterates using `infos.*Num()` counts rather than hardcoding values.

## Minor Observations

### Build Type Handling (Non-Issue)

The API uses simplified build type strings ("UNIT", "PROJECT", "SPECIALIST") in `BuildQueueItemObject()` at line 505-517:

```csharp
if (buildTypeValue == infos.Globals.UNIT_BUILD)
{
    buildType = "UNIT";  // Simplified identifier
    itemType = infos.unit((UnitType)build.miType)?.mzType;  // Full game type string
}
```

This is **intentional and correct** - the `buildType` field indicates the category while `itemType` contains the full game type string (e.g., `UNIT_WARRIOR`). The game's internal `UNIT_BUILD` is a build category, not a unit type.

### Event Type Strings

Event types defined by the API (e.g., `characterBorn`, `characterDied`, `unitKilled`) are **API-specific identifiers**, not game types. These follow a different convention (camelCase) to distinguish them from game types. This is appropriate since these events are an API construct, not game data.

## Recommendations

The current implementation is correct and well-aligned with game conventions. No changes are needed.

For future development:

1. **Continue using `mzType`** - Always retrieve type strings via `infos.[category](typeEnum).mzType` rather than enum `ToString()`.

2. **Check for NONE values** - Before accessing Info objects, verify the enum is not the NONE value for that type.

3. **Use null-conditional operators** - Always use `?.mzType` pattern for safety.

4. **Document new types** - When adding support for new game types, reference the corresponding XML file in `Reference/XML/Infos/` to ensure correct prefix usage.

## Appendix: Game Type String Access Patterns

| Pattern | Usage |
|---------|-------|
| `infos.yield(yieldType).mzType` | Get yield type string |
| `infos.nation(nationType).mzType` | Get nation type string |
| `infos.unit(unitType).mzType` | Get unit type string |
| `infos.improvement(impType).mzType` | Get improvement type string |
| `infos.improvementClass(classType).mzType` | Get improvement class string |
| `infos.project(projectType).mzType` | Get project type string |
| `infos.specialist(specType).mzType` | Get specialist type string |
| `infos.trait(traitType).mzType` | Get trait type string |
| `infos.rating(ratingType).mzType` | Get rating type string |
| `infos.culture(cultureType).mzType` | Get culture type string |
| `infos.religion(religionType).mzType` | Get religion type string |
| `infos.family(familyType).mzType` | Get family type string |
| `infos.familyClass(familyClassType).mzType` | Get family class string |
| `infos.tribe(tribeType).mzType` | Get tribe type string |
| `infos.diplomacy(diplomacyType).mzType` | Get diplomacy type string |
| `infos.warState(warStateType).mzType` | Get war state string |
| `infos.job(jobType).mzType` | Get job type string |
| `infos.council(councilType).mzType` | Get council type string |
| `infos.courtier(courtierType).mzType` | Get courtier type string |
| `infos.opinionCharacter(opinionType).mzType` | Get character opinion string |
| `infos.relationship(relType).mzType` | Get relationship type string |
| `infos.cognomen(cognomenType).mzType` | Get cognomen type string |
| `infos.title(titleType).mzType` | Get title type string |
| `infos.text(textType).mzType` | Get text type string |
| `infos.character(characterType).mzType` | Get character archetype string |

---

*Report generated: January 2025*
*Source: Reference XML files and APIEndpoint.cs analysis*
