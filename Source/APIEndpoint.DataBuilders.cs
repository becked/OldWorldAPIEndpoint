using System;
using System.Collections.Generic;
using TenCrowns.GameCore;
using UnityEngine;

namespace OldWorldAPIEndpoint
{
    /// <summary>
    /// Data building methods for JSON serialization.
    /// Converts game entities to anonymous objects for JSON output.
    /// </summary>
    public partial class APIEndpoint
    {
        #region Player Methods

        /// <summary>
        /// Build list of player objects for JSON serialization.
        /// </summary>
        public static List<object> BuildPlayersObject(Game game)
        {
            Player[] players = game.getPlayers();
            Infos infos = game.infos();
            int yieldCount = (int)infos.yieldsNum();

            var playerList = new List<object>();

            for (int i = 0; i < players.Length; i++)
            {
                var player = players[i];
                if (player == null) continue;

                var stockpiles = new Dictionary<string, int>();
                var rates = new Dictionary<string, int>();
                for (int y = 0; y < yieldCount; y++)
                {
                    var yieldType = (YieldType)y;
                    string yieldName = infos.yield(yieldType).mzType;
                    stockpiles[yieldName] = player.getYieldStockpileWhole(yieldType);
                    rates[yieldName] = player.calculateYieldAfterUnits(yieldType, false) / 10;
                }

                playerList.Add(new
                {
                    index = i,
                    team = (int)player.getTeam(),
                    nation = infos.nation(player.getNation()).mzType,
                    leaderId = player.hasFounder() ? (int?)player.getFounderID() : null,
                    cities = player.getNumCities(),
                    units = player.getNumUnits(),
                    legitimacy = player.getLegitimacy(),
                    stockpiles = stockpiles,
                    rates = rates
                });
            }

            return playerList;
        }

        #endregion

        #region City Methods

        /// <summary>
        /// Build list of city objects for JSON serialization.
        /// </summary>
        public static List<object> BuildCitiesObject(Game game)
        {
            Infos infos = game.infos();
            var cityList = new List<object>();

            try
            {
                var cities = game.getCities();
                foreach (var city in cities)
                {
                    if (city == null) continue;
                    try
                    {
                        cityList.Add(BuildCityObject(city, game, infos));
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[APIEndpoint] Error building city {city.getID()}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIEndpoint] Error getting cities: {ex.Message}");
            }

            return cityList;
        }

        /// <summary>
        /// Build a single city object with all field groups.
        /// </summary>
        public static object BuildCityObject(City city, Game game, Infos infos)
        {
            var tile = city.tile();

            return new
            {
                // 1. Identity & Location
                id = city.getID(),
                name = city.getName(),
                ownerId = (int)city.getPlayer(),
                tileId = city.getTileID(),
                x = tile.getX(),
                y = tile.getY(),
                nation = infos.nation(city.getNation())?.mzType,
                team = (int)city.getTeam(),

                // 2. Status Flags
                isCapital = city.isCapital(),
                isTribe = city.isTribe(),
                isConnected = city.isConnected(),

                // 3. Founding & History
                foundedTurn = city.getFoundedTurn(),

                // 4. Population & Growth
                citizens = city.getCitizens(),

                // 5. Military & Defense
                hp = city.getHP(),
                hpMax = city.getHPMax(),
                damage = city.getDamage(),
                strength = city.strength(),

                // 6. Capture & Assimilation
                captureTurns = city.getCaptureTurns(),
                hasCapturePlayer = city.hasCapturePlayer(),
                hasCaptureTribe = city.hasCaptureTribe(),
                assimilateTurns = city.getAssimilateTurns(),

                // 7. Governor
                governorId = city.hasGovernor() ? (int?)city.getGovernorID() : null,
                hasGovernor = city.hasGovernor(),

                // 8. Family & Faction
                family = city.hasFamily() ? infos.family(city.getFamily())?.mzType : null,
                hasFamily = city.hasFamily(),
                isFamilySeat = city.isFamilySeat(),

                // 9. Culture
                culture = infos.culture(city.getCulture())?.mzType,
                cultureStep = city.getCultureStep(),

                // 10. Religion
                religions = GetCityReligions(city, infos),
                religionCount = city.getReligionCount(),
                hasStateReligion = city.hasStateReligion(),
                holyCity = GetCityHolyCityReligions(city, infos),
                isReligionHolyCityAny = city.isReligionHolyCityAny(),

                // 11. Production & Build Queue
                hasBuild = city.hasBuild(),
                buildCount = city.getBuildCount(),
                currentBuild = BuildQueueItemObject(city, city.getCurrentBuild(), infos),
                buildQueue = GetBuildQueue(city, infos),

                // 12. Yields
                yields = GetCityYields(city, infos),

                // 13. Specialists
                specialistCount = city.getSpecialistCount(),

                // 14. Improvements
                improvements = GetCityImprovements(city, infos),
                improvementClasses = GetCityImprovementClasses(city, infos),

                // 15. Projects
                projects = GetCityProjects(city, infos),

                // 16. Trade & Connectivity
                tradeNetwork = city.getTradeNetwork(),
                luxuryCount = city.getLuxuryCount(),

                // 17. Happiness
                happinessLevel = city.getHappinessLevel(),

                // 18. Cost Modifiers
                improvementCostModifier = city.getImprovementCostModifier(),
                specialistCostModifier = city.getSpecialistCostModifier(),
                projectCostModifier = city.getProjectCostModifier(),

                // 19. Territory
                urbanTiles = city.getUrbanTiles(),
                territoryTileCount = city.getTerritoryTiles().Count,

                // 20. Misc
                raidedTurn = city.getRaidedTurn(),
                buyTileCount = city.getBuyTileCount()
            };
        }

        #region City Helper Methods
        // Note: The helper methods in this region use bare catch blocks intentionally.
        // They implement graceful degradation - if any data field fails to read
        // (due to game API changes, null references, etc.), we return partial/empty
        // data rather than crashing the entire API broadcast. This is by design.

        private static List<string> GetCityReligions(City city, Infos infos)
        {
            var religions = new List<string>();
            try
            {
                int count = (int)infos.religionsNum();
                for (int r = 0; r < count; r++)
                {
                    var relType = (ReligionType)r;
                    if (city.isReligion(relType))
                        religions.Add(infos.religion(relType).mzType);
                }
            }
            catch { }
            return religions;
        }

        private static List<string> GetCityHolyCityReligions(City city, Infos infos)
        {
            var religions = new List<string>();
            try
            {
                int count = (int)infos.religionsNum();
                for (int r = 0; r < count; r++)
                {
                    var relType = (ReligionType)r;
                    if (city.isReligionHolyCity(relType))
                        religions.Add(infos.religion(relType).mzType);
                }
            }
            catch { }
            return religions;
        }

        private static Dictionary<string, object> GetCityYields(City city, Infos infos)
        {
            var yields = new Dictionary<string, object>();
            try
            {
                int count = (int)infos.yieldsNum();
                for (int y = 0; y < count; y++)
                {
                    var yieldType = (YieldType)y;
                    string name = infos.yield(yieldType).mzType;
                    yields[name] = new
                    {
                        perTurn = city.calculateCurrentYield(yieldType, true, true),
                        progress = city.getYieldProgress(yieldType),
                        threshold = city.getYieldThresholdWhole(yieldType),
                        overflow = city.getYieldOverflow(yieldType)
                    };
                }
            }
            catch { }
            return yields;
        }

        private static Dictionary<string, int> GetCityImprovements(City city, Infos infos)
        {
            var improvements = new Dictionary<string, int>();
            try
            {
                int count = (int)infos.improvementsNum();
                for (int i = 0; i < count; i++)
                {
                    var impType = (ImprovementType)i;
                    int c = city.getImprovementCount(impType);
                    if (c > 0)
                        improvements[infos.improvement(impType).mzType] = c;
                }
            }
            catch { }
            return improvements;
        }

        private static Dictionary<string, int> GetCityImprovementClasses(City city, Infos infos)
        {
            var classes = new Dictionary<string, int>();
            try
            {
                int count = (int)infos.improvementClassesNum();
                for (int i = 0; i < count; i++)
                {
                    var classType = (ImprovementClassType)i;
                    int c = city.getImprovementClassCount(classType);
                    if (c > 0)
                        classes[infos.improvementClass(classType).mzType] = c;
                }
            }
            catch { }
            return classes;
        }

        private static Dictionary<string, int> GetCityProjects(City city, Infos infos)
        {
            var projects = new Dictionary<string, int>();
            try
            {
                int count = (int)infos.projectsNum();
                for (int i = 0; i < count; i++)
                {
                    var projType = (ProjectType)i;
                    int c = city.getProjectCount(projType);
                    if (c > 0)
                        projects[infos.project(projType).mzType] = c;
                }
            }
            catch { }
            return projects;
        }

        private static List<object> GetBuildQueue(City city, Infos infos)
        {
            var queue = new List<object>();
            try
            {
                int count = city.getBuildCount();
                for (int q = 0; q < count; q++)
                {
                    var item = city.getBuildQueueNode(q);
                    var obj = BuildQueueItemObject(city, item, infos);
                    if (obj != null) queue.Add(obj);
                }
            }
            catch { }
            return queue;
        }

        private static object BuildQueueItemObject(City city, CityQueueData build, Infos infos)
        {
            if (build == null) return null;

            try
            {
                string buildType = "UNKNOWN";
                string itemType = "UNKNOWN";

                // Determine build type and item based on meBuild
                var buildTypeValue = build.meBuild;

                // Try to match against known build types
                if (buildTypeValue == infos.Globals.UNIT_BUILD)
                {
                    buildType = "UNIT";
                    itemType = infos.unit((UnitType)build.miType)?.mzType ?? "UNKNOWN";
                }
                else if (buildTypeValue == infos.Globals.PROJECT_BUILD)
                {
                    buildType = "PROJECT";
                    itemType = infos.project((ProjectType)build.miType)?.mzType ?? "UNKNOWN";
                }
                else if (buildTypeValue == infos.Globals.SPECIALIST_BUILD)
                {
                    buildType = "SPECIALIST";
                    itemType = infos.specialist((SpecialistType)build.miType)?.mzType ?? "UNKNOWN";
                }

                return new
                {
                    buildType = buildType,
                    itemType = itemType,
                    progress = city.getBuildProgress(build, true),
                    threshold = city.getBuildThreshold(build),
                    turnsLeft = city.getBuildTurnsLeft(build),
                    hurried = build.mbHurried
                };
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #endregion

        #region Character Methods

        /// <summary>
        /// Build list of character objects for JSON serialization.
        /// </summary>
        public static List<object> BuildCharactersObject(Game game)
        {
            Infos infos = game.infos();
            var characterList = new List<object>();

            try
            {
                var characters = game.getCharacters();
                foreach (var character in characters)
                {
                    if (character == null) continue;
                    try
                    {
                        characterList.Add(BuildCharacterObject(character, game, infos));
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[APIEndpoint] Error building character {character.getID()}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIEndpoint] Error getting characters: {ex.Message}");
            }

            return characterList;
        }

        /// <summary>
        /// Build a single character object with all field groups.
        /// </summary>
        public static object BuildCharacterObject(Character character, Game game, Infos infos)
        {
            return new
            {
                // 1. Identity
                id = character.getID(),
                name = character.getFirstName(),
                suffix = character.getSuffix(),
                gender = (int)character.getGender() == 0 ? "Male" : "Female",
                age = character.getAge(),
                characterType = character.hasCharacter() ? infos.character(character.getCharacter())?.mzType : null,

                // 2. Player & Nation
                playerId = character.hasPlayer() ? (int?)character.getPlayer() : null,
                nation = character.hasNation() ? infos.nation(character.getNation())?.mzType : null,
                tribe = character.isTribe() ? infos.tribe(character.getTribe())?.mzType : null,

                // 3. Status
                isAlive = character.isAlive(),
                isDead = character.isDead(),
                isRoyal = character.isRoyal(),
                isAdult = character.isAdult(),
                isTemporary = character.isTemporary(),

                // 4. Leadership & Succession
                isLeader = character.isLeader(),
                isHeir = character.isHeir(),
                isSuccessor = character.isSuccessor(),
                isLeaderSpouse = character.isLeaderSpouse(),
                isHeirSpouse = character.isHeirSpouse(),
                isRegent = character.isRegent(),

                // 5. Jobs & Positions
                job = character.isJob() ? infos.job(character.getJob())?.mzType : null,
                council = character.isCouncil() ? infos.council(character.getCouncil())?.mzType : null,
                courtier = character.isCourtier() ? infos.courtier(character.getCourtier())?.mzType : null,

                // 6. Governor/Agent
                isCityGovernor = character.isCityGovernor(),
                cityGovernorId = character.isCityGovernor() ? (int?)character.getCityGovernorID() : null,
                isCityAgent = character.isCityAgent(),
                cityAgentId = character.isCityAgent() ? (int?)character.getCityAgentID() : null,

                // 7. Military
                hasUnit = character.hasUnit(),
                unitId = character.hasUnit() ? (int?)character.getUnitID() : null,
                isGeneral = character.isUnitGeneral(),

                // 8. Family
                family = character.hasFamily() ? infos.family(character.getFamily())?.mzType : null,
                familyClass = character.hasFamily() ? infos.familyClass(character.getFamilyClass())?.mzType : null,
                isFamilyHead = character.isFamilyHead(),

                // 9. Religion
                religion = character.hasReligion() ? infos.religion(character.getReligion())?.mzType : null,
                isReligionHead = character.isReligionHead(),

                // 10. Parents (adoptive)
                fatherId = character.hasFather() ? (int?)character.getFatherID() : null,
                motherId = character.hasMother() ? (int?)character.getMotherID() : null,

                // 11. Traits
                archetype = GetCharacterArchetype(character, infos),
                traits = GetCharacterTraits(character, infos),

                // 12. Ratings (stats)
                ratings = GetCharacterRatings(character, infos),

                // 13. XP & Level
                xp = character.getXP(),
                level = character.getLevel(),

                // 14. Lifecycle Timeline (NEW)
                birthTurn = character.getBirthTurn(),
                deathTurn = character.getDeathTurn(),
                leaderTurn = character.getLeaderTurn(),
                abdicateTurn = character.getAbdicateTurn(),
                regentTurn = character.getRegentTurn(),
                safeTurn = character.getSafeTurn(),
                nationTurn = character.getNationTurn(),

                // 15. Extended Status Flags (NEW)
                isInfertile = character.isInfertile(),
                isRetired = character.isRetired(),
                isAbdicated = character.isAbdicated(),
                isOrWasLeader = character.isOrWasLeader(),
                isOrWasRegent = character.isOrWasRegent(),
                isOrWasLeaderSpouse = character.isOrWasLeaderSpouse(),
                isSafe = character.isSafe(),

                // 16. Biological Parents (NEW - distinct from adoptive)
                birthFatherId = character.hasBirthFather() ? (int?)character.getBirthFatherID() : null,
                birthMotherId = character.hasBirthMother() ? (int?)character.getBirthMotherID() : null,

                // 17. Birth Location (NEW)
                birthCityId = character.hasBirthCity() ? (int?)character.getBirthCityID() : null,

                // 18. Spouse & Marriage Data (NEW)
                spouseIds = GetCharacterSpouseIds(character),
                numSpouses = character.getNumSpouses(),
                spousesAlive = character.countSpousesAlive(),
                hasSpouseAlive = character.hasSpouseAlive(),
                hasSpouseForeign = character.hasSpouseForeign(),
                hasSpouseTribe = character.hasSpouseTribe(),

                // 19. Children Data (NEW)
                childrenIds = GetCharacterChildrenIds(character),
                numChildren = character.getNumChildren(),

                // 20. Former Positions (NEW)
                wasReligionHead = character.getWasReligionHead() != ReligionType.NONE
                    ? infos.religion(character.getWasReligionHead())?.mzType : null,
                wasFamilyHead = character.getWasFamilyHead() != FamilyType.NONE
                    ? infos.family(character.getWasFamilyHead())?.mzType : null,

                // 21. Death Info (NEW)
                deadCouncil = character.getDeadCouncil() != CouncilType.NONE
                    ? infos.council(character.getDeadCouncil())?.mzType : null,
                deathReason = character.getDeathReason() != TextType.NONE
                    ? infos.text(character.getDeathReason())?.mzType : null,

                // 22. Title & Cognomen (NEW)
                title = character.hasTitle() ? infos.title(character.getTitle())?.mzType : null,
                cognomen = character.hasCognomen() ? infos.cognomen(character.getCognomen())?.mzType : null,
                nickname = character.hasNickname() ? character.getNicknameText() : null,

                // 23. Relationships (NEW)
                relationships = GetCharacterRelationships(character, infos),

                // 24. Opinions towards players (NEW)
                opinions = GetCharacterOpinions(character, game, infos)
            };
        }

        #region Character Helper Methods

        private static string GetCharacterArchetype(Character character, Infos infos)
        {
            try
            {
                var archetype = character.getArchetype();
                if (archetype != TraitType.NONE)
                    return infos.trait(archetype)?.mzType;
            }
            catch { }
            return null;
        }

        private static List<string> GetCharacterTraits(Character character, Infos infos)
        {
            var traits = new List<string>();
            try
            {
                var traitList = character.getTraits();
                foreach (var traitType in traitList)
                {
                    var traitName = infos.trait(traitType)?.mzType;
                    if (traitName != null)
                        traits.Add(traitName);
                }
            }
            catch { }
            return traits;
        }

        private static Dictionary<string, int> GetCharacterRatings(Character character, Infos infos)
        {
            var ratings = new Dictionary<string, int>();
            try
            {
                int count = (int)infos.ratingsNum();
                for (int r = 0; r < count; r++)
                {
                    var ratingType = (RatingType)r;
                    string name = infos.rating(ratingType).mzType;
                    ratings[name] = character.getRating(ratingType);
                }
            }
            catch { }
            return ratings;
        }

        private static List<int> GetCharacterSpouseIds(Character character)
        {
            var spouseIds = new List<int>();
            try
            {
                for (int i = 0; i < character.getNumSpouses(); i++)
                {
                    var spouse = character.getSpouseAtIndex(i);
                    if (spouse != null)
                        spouseIds.Add(spouse.getID());
                }
            }
            catch { }
            return spouseIds;
        }

        private static List<int> GetCharacterChildrenIds(Character character)
        {
            var childrenIds = new List<int>();
            try
            {
                var children = character.getChildren();
                if (children != null)
                {
                    childrenIds.AddRange(children);
                }
            }
            catch { }
            return childrenIds;
        }

        private static List<object> GetCharacterRelationships(Character character, Infos infos)
        {
            var relationships = new List<object>();
            try
            {
                var relationshipList = character.getRelationshipList();
                if (relationshipList != null)
                {
                    foreach (var rel in relationshipList)
                    {
                        relationships.Add(new
                        {
                            type = infos.relationship(rel.meType)?.mzType,
                            characterId = rel.miCharacterID
                        });
                    }
                }
            }
            catch { }
            return relationships;
        }

        private static Dictionary<int, object> GetCharacterOpinions(Character character, Game game, Infos infos)
        {
            var opinions = new Dictionary<int, object>();
            try
            {
                int numPlayers = (int)game.getNumPlayers();
                for (int p = 0; p < numPlayers; p++)
                {
                    var playerType = (PlayerType)p;
                    var opinion = character.getOpinion(playerType);
                    var rate = character.getOpinionRate(playerType);
                    if (opinion != OpinionCharacterType.NONE || rate != 0)
                    {
                        opinions[p] = new
                        {
                            opinion = opinion != OpinionCharacterType.NONE
                                ? infos.opinionCharacter(opinion)?.mzType : null,
                            rate = rate
                        };
                    }
                }
            }
            catch { }
            return opinions;
        }

        #endregion

        #endregion

        #region Unit Data Methods

        /// <summary>
        /// Build list of unit objects for JSON serialization.
        /// </summary>
        public static List<object> BuildUnitsObject(Game game)
        {
            Infos infos = game.infos();
            var unitList = new List<object>();

            try
            {
                var units = game.getUnits();
                foreach (var unit in units)
                {
                    if (unit == null || unit.isDead()) continue;
                    try
                    {
                        unitList.Add(BuildUnitObject(unit, game, infos));
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[APIEndpoint] Error building unit {unit.getID()}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIEndpoint] Error getting units: {ex.Message}");
            }

            return unitList;
        }

        /// <summary>
        /// Build list of unit objects for a specific player.
        /// </summary>
        public static List<object> BuildPlayerUnitsObject(Game game, int playerIndex)
        {
            Infos infos = game.infos();
            var unitList = new List<object>();

            try
            {
                var units = game.getUnits();
                foreach (var unit in units)
                {
                    if (unit == null || unit.isDead()) continue;
                    if (!unit.hasPlayer()) continue;
                    if ((int)unit.getPlayer() != playerIndex) continue;

                    try
                    {
                        unitList.Add(BuildUnitObject(unit, game, infos));
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[APIEndpoint] Error building unit {unit.getID()}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIEndpoint] Error getting player units: {ex.Message}");
            }

            return unitList;
        }

        /// <summary>
        /// Build a single unit object with all field groups.
        /// </summary>
        public static object BuildUnitObject(Unit unit, Game game, Infos infos)
        {
            var tile = unit.tile();
            var unitInfo = infos.unit(unit.getType());

            // Build base object with confirmed methods
            var baseData = new Dictionary<string, object>
            {
                // 1. Identity
                ["id"] = unit.getID(),
                ["unitType"] = unitInfo?.mzType,

                // 2. Ownership
                ["ownerId"] = unit.hasPlayer() ? (int?)unit.getPlayer() : null,

                // 3. Position
                ["tileId"] = tile?.getID() ?? -1,
                ["x"] = tile?.getX() ?? 0,
                ["y"] = tile?.getY() ?? 0,

                // 4. Health (base)
                ["hp"] = unit.getHP(),

                // 5. Status (base)
                ["isAlive"] = !unit.isDead()
            };

            // Try to add additional fields that may exist
            TryAddUnitField(baseData, "hpMax", () => unit.getHPMax());
            TryAddUnitField(baseData, "damage", () => unit.getDamage());
            TryAddUnitField(baseData, "xp", () => unit.getXP());
            TryAddUnitField(baseData, "level", () => unit.getLevel());
            TryAddUnitField(baseData, "turnSteps", () => unit.getTurnSteps());
            TryAddUnitField(baseData, "cooldownTurns", () => unit.getCooldownTurns());
            TryAddUnitField(baseData, "fortifyTurns", () => unit.getFortifyTurns());
            TryAddUnitField(baseData, "createTurn", () => unit.getCreateTurn());

            // Character attachments
            TryAddUnitField(baseData, "generalId", () => unit.hasGeneral() ? (int?)unit.getGeneralID() : null);
            TryAddUnitField(baseData, "hasGeneral", () => unit.hasGeneral());

            // Note: Tribe ownership fields (hasTribe, getTribe) not available in public API

            // Status flags
            TryAddUnitField(baseData, "isSleep", () => unit.isSleep());
            TryAddUnitField(baseData, "isSentry", () => unit.isSentry());
            TryAddUnitField(baseData, "isPass", () => unit.isPass());

            // Family
            TryAddUnitField(baseData, "family", () => unit.hasFamily() ? infos.family(unit.getFamily())?.mzType : null);
            TryAddUnitField(baseData, "hasFamily", () => unit.hasFamily());

            // Religion
            TryAddUnitField(baseData, "religion", () => unit.hasReligion() ? infos.religion(unit.getReligion())?.mzType : null);
            TryAddUnitField(baseData, "hasReligion", () => unit.hasReligion());

            // Promotions
            TryAddUnitField(baseData, "promotions", () => GetUnitPromotions(unit, infos));

            return baseData;
        }

        #region Unit Helper Methods

        private static void TryAddUnitField(Dictionary<string, object> data, string key, Func<object> getValue)
        {
            try
            {
                data[key] = getValue();
            }
            catch
            {
                // Method doesn't exist or failed - skip this field
            }
        }

        private static List<string> GetUnitPromotions(Unit unit, Infos infos)
        {
            var promotions = new List<string>();
            try
            {
                int count = (int)infos.promotionsNum();
                for (int p = 0; p < count; p++)
                {
                    var promoType = (PromotionType)p;
                    if (unit.hasPromotion(promoType))
                    {
                        var promoName = infos.promotion(promoType)?.mzType;
                        if (promoName != null)
                            promotions.Add(promoName);
                    }
                }
            }
            catch { }
            return promotions;
        }

        #endregion

        #endregion

        #region Player Extension Methods

        /// <summary>
        /// Build player technology state for JSON serialization.
        /// </summary>
        public static object BuildPlayerTechs(Player player, Game game, Infos infos)
        {
            var data = new Dictionary<string, object>();

            try
            {
                // Current research
                var researching = player.getTechResearching();
                data["researching"] = researching != TechType.NONE ? infos.tech(researching)?.mzType : null;

                // Progress and status per tech
                var progress = new Dictionary<string, int>();
                var researched = new List<string>();
                var available = new List<string>();

                int techCount = (int)infos.techsNum();
                for (int t = 0; t < techCount; t++)
                {
                    var techType = (TechType)t;
                    var techInfo = infos.tech(techType);
                    if (techInfo == null) continue;

                    string techName = techInfo.mzType;

                    try
                    {
                        int prog = player.getTechProgress(techType);
                        if (prog > 0)
                            progress[techName] = prog;
                    }
                    catch { }

                    try
                    {
                        if (player.isTechAcquired(techType))
                            researched.Add(techName);
                    }
                    catch { }

                    try
                    {
                        if (player.isTechAvailable(techType))
                            available.Add(techName);
                    }
                    catch { }
                }

                data["progress"] = progress;
                data["researched"] = researched;
                data["available"] = available;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIEndpoint] Error building player techs: {ex.Message}");
            }

            return data;
        }

        /// <summary>
        /// Build player family relationships for JSON serialization.
        /// Note: Limited data available from public API
        /// </summary>
        public static object BuildPlayerFamilies(Player player, Game game, Infos infos)
        {
            var families = new List<object>();

            try
            {
                int familyCount = (int)infos.familiesNum();
                for (int f = 0; f < familyCount; f++)
                {
                    var familyType = (FamilyType)f;
                    var familyInfo = infos.family(familyType);
                    if (familyInfo == null) continue;

                    var familyData = new Dictionary<string, object>
                    {
                        ["family"] = familyInfo.mzType
                    };

                    // Try to get opinion rate - may not be available
                    try
                    {
                        familyData["opinionRate"] = player.getFamilyOpinionRate(familyType);
                    }
                    catch { }

                    // Only add if we got some data
                    if (familyData.Count > 1)
                        families.Add(familyData);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIEndpoint] Error building player families: {ex.Message}");
            }

            return new { families };
        }

        /// <summary>
        /// Build player religion state for JSON serialization.
        /// </summary>
        public static object BuildPlayerReligion(Player player, Game game, Infos infos)
        {
            var data = new Dictionary<string, object>();

            try
            {
                // State religion
                try
                {
                    var stateReligion = player.getStateReligion();
                    data["stateReligion"] = stateReligion != ReligionType.NONE ? infos.religion(stateReligion)?.mzType : null;
                }
                catch { data["stateReligion"] = null; }

                // Religion counts
                var religionCounts = new Dictionary<string, int>();
                int religionCount = (int)infos.religionsNum();
                for (int r = 0; r < religionCount; r++)
                {
                    var relType = (ReligionType)r;
                    try
                    {
                        int count = player.getReligionCount(relType);
                        if (count > 0)
                        {
                            var relInfo = infos.religion(relType);
                            if (relInfo != null)
                                religionCounts[relInfo.mzType] = count;
                        }
                    }
                    catch { }
                }
                data["religionCounts"] = religionCounts;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIEndpoint] Error building player religion: {ex.Message}");
            }

            return data;
        }

        /// <summary>
        /// Build player goals and ambitions for JSON serialization.
        /// Note: Goal data is not accessible from public API - returns empty structure
        /// </summary>
        public static object BuildPlayerGoals(Player player, Game game, Infos infos)
        {
            // Goal data (getGoalDataList) is protected - cannot access
            return new
            {
                goals = new List<object>(),
                note = "Goal data not accessible from mod API"
            };
        }

        /// <summary>
        /// Build player pending decisions for JSON serialization.
        /// Note: Decision data is not accessible from public API - returns empty structure
        /// </summary>
        public static object BuildPlayerDecisions(Player player, Game game, Infos infos)
        {
            // Decision data (getDecisionList) is protected - cannot access
            return new
            {
                decisions = new List<object>(),
                note = "Decision data not accessible from mod API"
            };
        }

        /// <summary>
        /// Build player laws state for JSON serialization.
        /// Note: Law data not accessible from mod API
        /// </summary>
        public static object BuildPlayerLaws(Player player, Game game, Infos infos)
        {
            // isLaw method is not available in public API
            return new
            {
                activeLaws = new Dictionary<string, string>(),
                note = "Law data not accessible from mod API"
            };
        }

        /// <summary>
        /// Build player missions state for JSON serialization.
        /// Note: Mission data not accessible from mod API
        /// </summary>
        public static object BuildPlayerMissions(Player player, Game game, Infos infos)
        {
            return new
            {
                missions = new List<object>(),
                note = "Mission data not accessible from mod API"
            };
        }

        /// <summary>
        /// Build player resources/luxuries state for JSON serialization.
        /// Note: Resource count data not accessible from mod API
        /// </summary>
        public static object BuildPlayerResources(Player player, Game game, Infos infos)
        {
            // countResource method is not available in public API
            return new
            {
                luxuries = new Dictionary<string, int>(),
                resources = new Dictionary<string, int>(),
                note = "Resource data not accessible from mod API"
            };
        }

        #endregion

        #region Global Data Methods

        /// <summary>
        /// Build global religions state for JSON serialization.
        /// </summary>
        public static object BuildReligionsObject(Game game)
        {
            Infos infos = game.infos();
            var religions = new List<object>();

            try
            {
                int religionCount = (int)infos.religionsNum();
                for (int r = 0; r < religionCount; r++)
                {
                    var religionType = (ReligionType)r;
                    var religionInfo = infos.religion(religionType);
                    if (religionInfo == null) continue;

                    var religionData = new Dictionary<string, object>
                    {
                        ["religionType"] = religionInfo.mzType
                    };

                    // Try to get founded status
                    try { religionData["isFounded"] = game.isReligionFounded(religionType); } catch { }

                    // Try to get head character
                    try
                    {
                        var head = game.religionHead(religionType);
                        religionData["headCharacterId"] = head?.getID();
                    }
                    catch { }

                    // Try to get holy city
                    try
                    {
                        var holyCity = game.religionHolyCity(religionType);
                        religionData["holyCityId"] = holyCity?.getID();
                    }
                    catch { }

                    religions.Add(religionData);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIEndpoint] Error building religions: {ex.Message}");
            }

            return religions;
        }

        /// <summary>
        /// Build map metadata for JSON serialization.
        /// </summary>
        public static object BuildMapObject(Game game)
        {
            var data = new Dictionary<string, object>();

            // Note: getMapWidth/getMapHeight not available in public API
            // Using numTiles as primary dimension indicator
            try { data["numTiles"] = game.getNumTiles(); } catch { }

            return data;
        }

        /// <summary>
        /// Build paginated tiles list for JSON serialization.
        /// </summary>
        public static object BuildTilesObjectPaginated(Game game, int offset, int limit)
        {
            Infos infos = game.infos();
            var tiles = new List<object>();
            int total = 0;

            try
            {
                var allTiles = game.allTiles();
                if (allTiles != null)
                {
                    var tileList = new List<Tile>(allTiles);
                    total = tileList.Count;
                    int end = Math.Min(offset + limit, total);

                    for (int i = offset; i < end; i++)
                    {
                        try
                        {
                            var tile = tileList[i];
                            if (tile != null)
                                tiles.Add(BuildTileObject(tile, game, infos));
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIEndpoint] Error building tiles: {ex.Message}");
            }

            return new
            {
                tiles,
                pagination = new
                {
                    offset,
                    limit,
                    total,
                    hasMore = offset + limit < total
                }
            };
        }

        /// <summary>
        /// Build a single tile object.
        /// </summary>
        public static object BuildTileObject(Tile tile, Game game, Infos infos)
        {
            var data = new Dictionary<string, object>
            {
                ["id"] = tile.getID(),
                ["x"] = tile.getX(),
                ["y"] = tile.getY()
            };

            // Geography
            try { data["terrain"] = infos.terrain(tile.getTerrain())?.mzType; } catch { }
            try { data["height"] = infos.height(tile.getHeight())?.mzType; } catch { }
            try
            {
                if (tile.hasVegetation())
                    data["vegetation"] = infos.vegetation(tile.getVegetation())?.mzType;
            }
            catch { }
            try
            {
                if (tile.hasResource())
                    data["resource"] = infos.resource(tile.getResource())?.mzType;
            }
            catch { }

            // Infrastructure
            // Note: hasRoad() not available in public API
            try
            {
                if (tile.hasImprovement())
                    data["improvement"] = infos.improvement(tile.getImprovement())?.mzType;
            }
            catch { }
            try { data["isPillaged"] = tile.isPillaged(); } catch { }

            // Ownership
            try { data["ownerId"] = tile.hasOwner() ? (int?)tile.getOwner() : null; } catch { }
            try { data["cityId"] = tile.hasCity() ? (int?)tile.getCityID() : null; } catch { }
            try { data["cityTerritoryId"] = tile.hasCityTerritory() ? (int?)tile.getCityTerritory() : null; } catch { }

            // Note: tile.getUnits() is protected - unitIds not available

            return data;
        }

        /// <summary>
        /// Build game configuration for JSON serialization.
        /// </summary>
        public static object BuildConfigObject(Game game)
        {
            var data = new Dictionary<string, object>();

            // Note: getMapWidth/getMapHeight not available in public API
            try { data["numTiles"] = game.getNumTiles(); } catch { }
            try { data["numPlayers"] = (int)game.getNumPlayers(); } catch { }
            try { data["numTeams"] = (int)game.getNumTeams(); } catch { }
            try { data["turn"] = game.getTurn(); } catch { }
            try { data["year"] = game.getYear(); } catch { }

            return data;
        }

        #endregion
    }
}
