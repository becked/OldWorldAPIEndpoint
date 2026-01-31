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
        /// Delegates to auto-generated null-safe code.
        /// </summary>
        public static List<object> BuildPlayersObject(Game game)
        {
            Player[] players = game.getPlayers();
            Infos infos = game.infos();
            var playerList = new List<object>();

            foreach (var player in players)
            {
                if (player == null) continue;
                try
                {
                    playerList.Add(DataBuilders.BuildPlayerObjectGenerated(player, game, infos));
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[APIEndpoint] Error building player: {ex.Message}");
                }
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
        /// Delegates to auto-generated null-safe code.
        /// </summary>
        public static object BuildCityObject(City city, Game game, Infos infos)
        {
            return DataBuilders.BuildCityObjectGenerated(city, game, infos);
        }

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
        /// Delegates to auto-generated null-safe code.
        /// </summary>
        public static object BuildCharacterObject(Character character, Game game, Infos infos)
        {
            return DataBuilders.BuildCharacterObjectGenerated(character, game, infos);
        }

        /// <summary>
        /// Get list of spouse IDs for a character.
        /// Used by both character data and event tracking.
        /// </summary>
        public static List<int> GetCharacterSpouseIds(Character character)
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
        /// Delegates to auto-generated null-safe code.
        /// </summary>
        public static object BuildUnitObject(Unit unit, Game game, Infos infos)
        {
            return DataBuilders.BuildUnitObjectGenerated(unit, game, infos);
        }

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
        /// Only includes families that belong to this player (from getFamilies()).
        /// </summary>
        public static object BuildPlayerFamilies(Player player, Game game, Infos infos)
        {
            var families = new List<object>();

            try
            {
                // Get only families that belong to this player
                var playerFamilies = player.getFamilies();
                if (playerFamilies == null)
                    return new { families };

                foreach (var familyType in playerFamilies)
                {
                    var familyInfo = infos.family(familyType);
                    if (familyInfo == null) continue;

                    var familyData = new Dictionary<string, object>
                    {
                        ["family"] = familyInfo.mzType
                    };

                    // Opinion rate
                    try
                    {
                        familyData["opinionRate"] = player.getFamilyOpinionRate(familyType);
                    }
                    catch { }

                    // Seat city ID
                    try
                    {
                        int seatCityId = player.getFamilySeatCityID(familyType);
                        if (seatCityId >= 0)
                            familyData["seatCityId"] = seatCityId;
                    }
                    catch { }

                    // Family head character ID
                    try
                    {
                        int headId = player.getFamilyHeadID(familyType);
                        if (headId >= 0)
                            familyData["headId"] = headId;
                    }
                    catch { }

                    // Family opinion level
                    try
                    {
                        var opinionLevel = player.getFamilyOpinion(familyType);
                        if (opinionLevel != OpinionFamilyType.NONE)
                        {
                            var opinionInfo = infos.opinionFamily(opinionLevel);
                            familyData["opinion"] = opinionInfo?.mzType ?? opinionLevel.ToString();
                        }
                    }
                    catch { }

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
        /// </summary>
        public static object BuildPlayerGoals(Player player, Game game, Infos infos)
        {
            var goals = new List<object>();

            try
            {
                int numGoals = player.getNumGoals();
                for (int i = 0; i < numGoals; i++)
                {
                    try
                    {
                        var goalData = player.getGoalDataAtIndex(i);
                        if (goalData == null) continue;

                        var goalInfo = infos.goal(goalData.meType);
                        var goalObj = new Dictionary<string, object>
                        {
                            ["id"] = goalData.miID,
                            ["type"] = goalInfo?.mzType ?? goalData.meType.ToString(),
                            ["turn"] = goalData.miTurn,
                            ["maxTurns"] = goalData.miMaxTurns,
                            ["finished"] = goalData.mbFinished,
                            ["isLegacy"] = goalData.mbLegacy,
                            ["isQuest"] = goalData.mbQuest
                        };

                        // Add related entity IDs if present
                        if (goalData.miCityID >= 0)
                            goalObj["cityId"] = goalData.miCityID;
                        if (goalData.miCharacterID >= 0)
                            goalObj["characterId"] = goalData.miCharacterID;
                        if (goalData.mePlayer != PlayerType.NONE)
                            goalObj["targetPlayer"] = (int)goalData.mePlayer;
                        if (goalData.meTribe != TribeType.NONE)
                            goalObj["targetTribe"] = infos.tribe(goalData.meTribe)?.mzType;
                        if (goalData.meFamily != FamilyType.NONE)
                            goalObj["targetFamily"] = infos.family(goalData.meFamily)?.mzType;
                        if (goalData.meReligion != ReligionType.NONE)
                            goalObj["targetReligion"] = infos.religion(goalData.meReligion)?.mzType;

                        goals.Add(goalObj);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[APIEndpoint] Error reading goal at index {i}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIEndpoint] Error building player goals: {ex.Message}");
            }

            return new { goals };
        }

        /// <summary>
        /// Build player pending decisions for JSON serialization.
        /// </summary>
        public static object BuildPlayerDecisions(Player player, Game game, Infos infos)
        {
            var decisions = new List<object>();

            try
            {
                int numDecisions = player.getNumDecisions();
                for (int i = 0; i < numDecisions; i++)
                {
                    try
                    {
                        var decisionData = player.getDecisionAt(i);
                        if (decisionData == null) continue;

                        var decisionObj = new Dictionary<string, object>
                        {
                            ["id"] = decisionData.ID,
                            ["type"] = decisionData.Type.ToString(),
                            ["sortOrder"] = decisionData.SortOrder,
                            ["modal"] = decisionData.Modal,
                            ["prevTurn"] = decisionData.PrevTurn
                        };

                        if (!string.IsNullOrEmpty(decisionData.Bonus))
                            decisionObj["bonus"] = decisionData.Bonus;

                        decisions.Add(decisionObj);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[APIEndpoint] Error reading decision at index {i}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIEndpoint] Error building player decisions: {ex.Message}");
            }

            return new { decisions, hasDecisions = player.hasDecisions() };
        }

        /// <summary>
        /// Build player laws state for JSON serialization.
        /// </summary>
        public static object BuildPlayerLaws(Player player, Game game, Infos infos)
        {
            var activeLaws = new Dictionary<string, string>();

            try
            {
                int lawClassCount = (int)infos.lawClassesNum();
                for (int i = 0; i < lawClassCount; i++)
                {
                    var lawClassType = (LawClassType)i;
                    var lawClassInfo = infos.lawClass(lawClassType);
                    if (lawClassInfo == null) continue;

                    try
                    {
                        var activeLaw = player.getActiveLaw(lawClassType);
                        if (activeLaw != LawType.NONE)
                        {
                            var lawInfo = infos.law(activeLaw);
                            activeLaws[lawClassInfo.mzType] = lawInfo?.mzType ?? activeLaw.ToString();
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIEndpoint] Error building player laws: {ex.Message}");
            }

            return new
            {
                activeLaws,
                activeLawCount = player.countActiveLaws()
            };
        }

        /// <summary>
        /// Build player missions state for JSON serialization.
        /// </summary>
        public static object BuildPlayerMissions(Player player, Game game, Infos infos)
        {
            var missions = new List<object>();
            var cooldowns = new Dictionary<string, int>();

            try
            {
                // Active missions
                int numMissions = player.getNumMissions();
                for (int i = 0; i < numMissions; i++)
                {
                    try
                    {
                        var missionData = player.getMissionAt(i);
                        if (missionData == null) continue;

                        var missionInfo = infos.mission(missionData.meType);
                        var missionObj = new Dictionary<string, object>
                        {
                            ["type"] = missionInfo?.mzType ?? missionData.meType.ToString(),
                            ["turn"] = missionData.miTurn,
                            ["characterId"] = missionData.miCharacterID
                        };

                        if (!string.IsNullOrEmpty(missionData.mzTarget))
                            missionObj["target"] = missionData.mzTarget;

                        if (missionData.mlSubjects != null && missionData.mlSubjects.Count > 0)
                            missionObj["subjects"] = missionData.mlSubjects;

                        missions.Add(missionObj);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[APIEndpoint] Error reading mission at index {i}: {ex.Message}");
                    }
                }

                // Mission cooldowns
                int missionCount = (int)infos.missionsNum();
                for (int m = 0; m < missionCount; m++)
                {
                    var missionType = (MissionType)m;
                    var missionInfo = infos.mission(missionType);
                    if (missionInfo == null) continue;

                    try
                    {
                        int cooldownLeft = player.getMissionCooldownTurnsLeft(missionType);
                        if (cooldownLeft > 0)
                            cooldowns[missionInfo.mzType] = cooldownLeft;
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIEndpoint] Error building player missions: {ex.Message}");
            }

            return new { missions, cooldowns };
        }

        /// <summary>
        /// Build player resources/luxuries state for JSON serialization.
        /// </summary>
        public static object BuildPlayerResources(Player player, Game game, Infos infos)
        {
            var luxuries = new Dictionary<string, int>();
            var revealed = new Dictionary<string, int>();

            try
            {
                int resourceCount = (int)infos.resourcesNum();
                for (int i = 0; i < resourceCount; i++)
                {
                    var resourceType = (ResourceType)i;
                    var resourceInfo = infos.resource(resourceType);
                    if (resourceInfo == null) continue;

                    string resourceName = resourceInfo.mzType;

                    // Luxury count (how many of this resource the player has available)
                    try
                    {
                        int luxuryCount = player.getLuxuryCount(resourceType);
                        if (luxuryCount != 0)
                            luxuries[resourceName] = luxuryCount;
                    }
                    catch { }

                    // Revealed count (how many tiles with this resource have been revealed)
                    try
                    {
                        int revealedCount = player.getResourceRevealed(resourceType);
                        if (revealedCount > 0)
                            revealed[resourceName] = revealedCount;
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIEndpoint] Error building player resources: {ex.Message}");
            }

            return new { luxuries, revealed };
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
        /// Delegates to auto-generated null-safe code.
        /// </summary>
        public static object BuildTileObject(Tile tile, Game game, Infos infos)
        {
            return DataBuilders.BuildTileObjectGenerated(tile, game, infos);
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
