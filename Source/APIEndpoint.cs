using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using TenCrowns.AppCore;
using TenCrowns.GameCore;
using UnityEngine;

namespace OldWorldAPIEndpoint
{
    /// <summary>
    /// Old World mod that broadcasts game state over TCP for companion apps.
    /// </summary>
    public class APIEndpoint : ModEntryPointAdapter
    {
        private static TcpBroadcastServer _server;
        private static HttpRestServer _httpServer;
        private static ModSettings _modSettings;
        private static int _initCount = 0;

        // Cached game reference for HTTP access (updated from main thread)
        private static Game _cachedGame;

        // Cached ClientManager for command execution
        private static object _clientManager;

        // Command queue: HTTP thread enqueues, main thread (OnClientUpdate) dequeues and executes
        private static readonly ConcurrentQueue<(GameCommand cmd, ManualResetEventSlim signal, CommandResult result)> _commandQueue
            = new ConcurrentQueue<(GameCommand, ManualResetEventSlim, CommandResult)>();

        // JSON serializer settings
        // Note: We use DefaultContractResolver to preserve exact game type strings (e.g., YIELD_GROWTH)
        // Property names in anonymous objects are already camelCase as defined
        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver(),
            NullValueHandling = NullValueHandling.Include,
            Formatting = Formatting.None
        };

        // Cached reflection info for AppMain.gApp.Client.Game
        private static Type _appMainType;
        private static FieldInfo _gAppField;
        private static PropertyInfo _clientProperty;
        private static PropertyInfo _gameProperty;
        private static bool _reflectionInitialized;

        // Alternative reflection paths for headless mode
        private static PropertyInfo _serverProperty;
        private static MethodInfo _getLocalGameServerMethod;
        private static PropertyInfo _localGameProperty;

        private static void InitializeReflection()
        {
            if (_reflectionInitialized) return;

            try
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.GetName().Name == "Assembly-CSharp")
                    {
                        _appMainType = assembly.GetType("AppMain");

                        // Get GameServerBehaviour and GameNetworkBehaviour for headless access
                        var gameServerBehaviourType = assembly.GetType("GameServerBehaviour");
                        var gameNetworkBehaviourType = assembly.GetType("GameNetworkBehaviour");

                        if (gameNetworkBehaviourType != null)
                        {
                            _localGameProperty = gameNetworkBehaviourType.GetProperty("LocalGame",
                                BindingFlags.Public | BindingFlags.Instance);
                        }

                        break;
                    }
                }

                if (_appMainType != null)
                {
                    _gAppField = _appMainType.GetField("gApp", BindingFlags.Public | BindingFlags.Static);
                    _clientProperty = _appMainType.GetProperty("Client", BindingFlags.Public | BindingFlags.Instance);
                    _serverProperty = _appMainType.GetProperty("Server", BindingFlags.Public | BindingFlags.Instance);
                    _getLocalGameServerMethod = _appMainType.GetMethod("GetLocalGameServer",
                        BindingFlags.Public | BindingFlags.Instance);

                    if (_clientProperty != null)
                    {
                        var clientType = _clientProperty.PropertyType;
                        _gameProperty = clientType.GetProperty("Game", BindingFlags.Public | BindingFlags.Instance);
                    }

                    Debug.Log($"[APIEndpoint] Reflection initialized");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIEndpoint] Reflection init failed: {ex.Message}");
            }

            _reflectionInitialized = true;
        }

        private static object GetAppMain()
        {
            InitializeReflection();
            return _gAppField?.GetValue(null);
        }

        /// <summary>
        /// Try multiple paths to get the Game instance.
        /// Returns cached game for thread-safe access from HTTP requests.
        /// </summary>
        private static Game GetGame()
        {
            // Return cached game if available (for HTTP thread safety)
            if (_cachedGame != null) return _cachedGame;

            try
            {
                InitializeReflection();
                var appMain = GetAppMain();
                if (appMain == null) return null;

                // Path 1: AppMain.gApp.Client.Game (works in GUI mode)
                if (_clientProperty != null && _gameProperty != null)
                {
                    var client = _clientProperty.GetValue(appMain);
                    if (client != null)
                    {
                        var game = _gameProperty.GetValue(client) as Game;
                        if (game != null) return game;
                    }
                }

                // Path 2: AppMain.gApp.Server.Game (try server property)
                if (_serverProperty != null && _gameProperty != null)
                {
                    var server = _serverProperty.GetValue(appMain);
                    if (server != null)
                    {
                        // Server might have a Game property too
                        var serverGameProp = server.GetType().GetProperty("Game",
                            BindingFlags.Public | BindingFlags.Instance);
                        if (serverGameProp != null)
                        {
                            var game = serverGameProp.GetValue(server) as Game;
                            if (game != null) return game;
                        }
                    }
                }

                // Path 3: AppMain.GetLocalGameServer().LocalGame
                if (_getLocalGameServerMethod != null)
                {
                    var gameServer = _getLocalGameServerMethod.Invoke(appMain, null);
                    if (gameServer != null)
                    {
                        // Try to get LocalGame property dynamically from the gameServer instance
                        var localGameProp = gameServer.GetType().GetProperty("LocalGame",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (localGameProp != null)
                        {
                            var game = localGameProp.GetValue(gameServer) as Game;
                            if (game != null) return game;
                        }

                        // Also try Game property directly
                        var gameProp = gameServer.GetType().GetProperty("Game",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (gameProp != null)
                        {
                            var game = gameProp.GetValue(gameServer) as Game;
                            if (game != null) return game;
                        }
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

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
                gender = character.getGender().ToString(),
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

        #endregion

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

        #region Character Events

        // Snapshot class for storing previous turn's character state (used for event detection)
        private class CharacterSnapshot
        {
            public int Id;
            public bool IsAlive;
            public bool IsDead;
            public bool IsLeader;
            public bool IsHeir;
            public bool IsRegent;
            public int NumSpouses;
            public List<int> SpouseIds;
            public int PlayerId;  // -1 if none
        }

        // Storage for previous turn's character state
        private static Dictionary<int, CharacterSnapshot> _previousCharacters = new Dictionary<int, CharacterSnapshot>();
        private static int _previousTurn = -1;
        private static List<object> _lastCharacterEvents = new List<object>();

        /// <summary>
        /// Detect character events by diffing current state against previous turn's state.
        /// Returns list of event objects (births, deaths, marriages, leader changes, heir changes).
        /// </summary>
        public static List<object> DetectCharacterEvents(Game game, Infos infos)
        {
            var events = new List<object>();
            int currentTurn = game.getTurn();

            // Skip event detection on first turn or if turn hasn't changed
            if (_previousTurn < 0 || currentTurn <= _previousTurn)
            {
                UpdateCharacterSnapshots(game);
                _previousTurn = currentTurn;
                _lastCharacterEvents = events;
                return events;
            }

            var currentCharacters = game.getCharacters();

            foreach (var character in currentCharacters)
            {
                if (character == null) continue;
                int id = character.getID();

                if (!_previousCharacters.TryGetValue(id, out var prev))
                {
                    // New character = birth
                    var parentIds = new List<int>();
                    if (character.hasFather())
                        parentIds.Add(character.getFatherID());
                    if (character.hasMother())
                        parentIds.Add(character.getMotherID());

                    events.Add(new
                    {
                        eventType = "characterBorn",
                        characterId = id,
                        parentIds = parentIds
                    });
                }
                else
                {
                    // Check for death
                    if (character.isDead() && !prev.IsDead)
                    {
                        events.Add(new
                        {
                            eventType = "characterDied",
                            characterId = id,
                            deathReason = character.getDeathReason() != TextType.NONE
                                ? infos.text(character.getDeathReason())?.mzType : null
                        });
                    }

                    // Check for new leadership
                    if (character.isLeader() && !prev.IsLeader)
                    {
                        // Find old leader for this player
                        int? oldLeaderId = null;
                        int playerId = character.hasPlayer() ? (int)character.getPlayer() : -1;
                        foreach (var kvp in _previousCharacters)
                        {
                            if (kvp.Value.IsLeader && kvp.Value.PlayerId == playerId)
                            {
                                oldLeaderId = kvp.Key;
                                break;
                            }
                        }

                        events.Add(new
                        {
                            eventType = "leaderChanged",
                            playerId = playerId,
                            newLeaderId = id,
                            oldLeaderId = oldLeaderId
                        });
                    }

                    // Check for marriage (new spouse)
                    var currentSpouseIds = GetCharacterSpouseIds(character);
                    foreach (var spouseId in currentSpouseIds)
                    {
                        if (!prev.SpouseIds.Contains(spouseId))
                        {
                            // Only emit once per marriage (from lower ID character)
                            if (id < spouseId)
                            {
                                events.Add(new
                                {
                                    eventType = "characterMarried",
                                    character1Id = id,
                                    character2Id = spouseId
                                });
                            }
                        }
                    }

                    // Check for heir change
                    if (character.isHeir() && !prev.IsHeir)
                    {
                        int playerId = character.hasPlayer() ? (int)character.getPlayer() : -1;
                        int? oldHeirId = null;
                        foreach (var kvp in _previousCharacters)
                        {
                            if (kvp.Value.IsHeir && kvp.Value.PlayerId == playerId)
                            {
                                oldHeirId = kvp.Key;
                                break;
                            }
                        }

                        events.Add(new
                        {
                            eventType = "heirChanged",
                            playerId = playerId,
                            newHeirId = id,
                            oldHeirId = oldHeirId
                        });
                    }
                }
            }

            // Update snapshots for next turn
            UpdateCharacterSnapshots(game);
            _previousTurn = currentTurn;
            _lastCharacterEvents = events;

            return events;
        }

        /// <summary>
        /// Update the character snapshots for the next turn comparison.
        /// </summary>
        private static void UpdateCharacterSnapshots(Game game)
        {
            _previousCharacters.Clear();
            foreach (var character in game.getCharacters())
            {
                if (character == null) continue;
                _previousCharacters[character.getID()] = new CharacterSnapshot
                {
                    Id = character.getID(),
                    IsAlive = character.isAlive(),
                    IsDead = character.isDead(),
                    IsLeader = character.isLeader(),
                    IsHeir = character.isHeir(),
                    IsRegent = character.isRegent(),
                    NumSpouses = character.getNumSpouses(),
                    SpouseIds = GetCharacterSpouseIds(character),
                    PlayerId = character.hasPlayer() ? (int)character.getPlayer() : -1
                };
            }
        }

        /// <summary>
        /// Get the last detected character events (for HTTP endpoint).
        /// </summary>
        public static List<object> GetLastCharacterEvents()
        {
            return _lastCharacterEvents;
        }

        #endregion

        #region Unit Events

        // Snapshot class for storing previous turn's unit state (used for event detection)
        private class UnitSnapshot
        {
            public int Id;
            public bool IsAlive;
            public int PlayerId;        // -1 if none (tribe units)
            public string UnitType;     // e.g., "UNIT_WARRIOR"
            public int HP;
            public int TileId;
            public int X;
            public int Y;
        }

        // Storage for previous turn's unit state
        private static Dictionary<int, UnitSnapshot> _previousUnits = new Dictionary<int, UnitSnapshot>();
        private static List<object> _lastUnitEvents = new List<object>();

        /// <summary>
        /// Detect unit events by diffing current state against previous turn's state.
        /// Returns list of event objects (unitKilled, unitCreated).
        /// </summary>
        public static List<object> DetectUnitEvents(Game game, Infos infos)
        {
            var events = new List<object>();
            int currentTurn = game.getTurn();

            // Skip event detection on first turn or if turn hasn't changed
            if (_previousTurn < 0 || currentTurn <= _previousTurn)
            {
                UpdateUnitSnapshots(game, infos);
                _lastUnitEvents = events;
                return events;
            }

            // Build dictionary of current units
            var currentUnits = new Dictionary<int, Unit>();
            try
            {
                var units = game.getUnits();
                foreach (var unit in units)
                {
                    if (unit != null)
                        currentUnits[unit.getID()] = unit;
                }
            }
            catch { }

            // Check for killed units (in previous but not in current or now dead)
            foreach (var kvp in _previousUnits)
            {
                int unitId = kvp.Key;
                var prev = kvp.Value;

                if (!currentUnits.TryGetValue(unitId, out var currentUnit) || currentUnit.isDead())
                {
                    events.Add(new
                    {
                        eventType = "unitKilled",
                        unitId = unitId,
                        unitType = prev.UnitType,
                        lastOwnerId = prev.PlayerId,
                        lastLocation = new
                        {
                            tileId = prev.TileId,
                            x = prev.X,
                            y = prev.Y
                        }
                    });
                }
            }

            // Check for created units (in current but not in previous)
            foreach (var kvp in currentUnits)
            {
                int unitId = kvp.Key;
                var unit = kvp.Value;

                if (!_previousUnits.ContainsKey(unitId) && !unit.isDead())
                {
                    var tile = unit.tile();
                    int playerId = unit.hasPlayer() ? (int)unit.getPlayer() : -1;

                    events.Add(new
                    {
                        eventType = "unitCreated",
                        unitId = unitId,
                        unitType = infos.unit(unit.getType())?.mzType ?? "UNKNOWN",
                        playerId = playerId,
                        location = new
                        {
                            tileId = tile?.getID() ?? -1,
                            x = tile?.getX() ?? 0,
                            y = tile?.getY() ?? 0
                        }
                    });
                }
            }

            // Update snapshots for next turn
            UpdateUnitSnapshots(game, infos);
            _lastUnitEvents = events;

            return events;
        }

        /// <summary>
        /// Update the unit snapshots for the next turn comparison.
        /// </summary>
        private static void UpdateUnitSnapshots(Game game, Infos infos)
        {
            _previousUnits.Clear();
            try
            {
                var units = game.getUnits();
                foreach (var unit in units)
                {
                    if (unit == null || unit.isDead()) continue;

                    var tile = unit.tile();
                    _previousUnits[unit.getID()] = new UnitSnapshot
                    {
                        Id = unit.getID(),
                        IsAlive = !unit.isDead(),
                        PlayerId = unit.hasPlayer() ? (int)unit.getPlayer() : -1,
                        UnitType = infos.unit(unit.getType())?.mzType ?? "UNKNOWN",
                        HP = unit.getHP(),
                        TileId = tile?.getID() ?? -1,
                        X = tile?.getX() ?? 0,
                        Y = tile?.getY() ?? 0
                    };
                }
            }
            catch { }
        }

        /// <summary>
        /// Get the last detected unit events (for HTTP endpoint).
        /// </summary>
        public static List<object> GetLastUnitEvents()
        {
            return _lastUnitEvents;
        }

        #endregion

        #region City Events

        // Snapshot class for storing previous turn's city ownership state (used for event detection)
        private class CitySnapshot
        {
            public int Id;
            public int OwnerId;
            public string Name;
            public bool IsTribe;
        }

        // Storage for previous turn's city state
        private static Dictionary<int, CitySnapshot> _previousCities = new Dictionary<int, CitySnapshot>();
        private static List<object> _lastCityEvents = new List<object>();

        /// <summary>
        /// Detect city events by diffing current state against previous turn's state.
        /// Returns list of event objects (cityCapture, cityFounded).
        /// </summary>
        public static List<object> DetectCityEvents(Game game, Infos infos)
        {
            var events = new List<object>();
            int currentTurn = game.getTurn();

            // Skip event detection on first turn or if turn hasn't changed
            if (_previousTurn < 0 || currentTurn <= _previousTurn)
            {
                UpdateCitySnapshots(game);
                _lastCityEvents = events;
                return events;
            }

            // Build dictionary of current cities
            var currentCities = new Dictionary<int, City>();
            try
            {
                var cities = game.getCities();
                foreach (var city in cities)
                {
                    if (city != null)
                        currentCities[city.getID()] = city;
                }
            }
            catch { }

            // Check for city captures (owner changed)
            foreach (var kvp in _previousCities)
            {
                int cityId = kvp.Key;
                var prev = kvp.Value;

                if (currentCities.TryGetValue(cityId, out var currentCity))
                {
                    int currentOwnerId = (int)currentCity.getPlayer();
                    if (currentOwnerId != prev.OwnerId)
                    {
                        events.Add(new
                        {
                            eventType = "cityCapture",
                            cityId = cityId,
                            cityName = currentCity.getName(),
                            oldOwnerId = prev.OwnerId,
                            newOwnerId = currentOwnerId,
                            wasTribe = prev.IsTribe
                        });
                    }
                }
            }

            // Check for founded cities (in current but not in previous)
            foreach (var kvp in currentCities)
            {
                int cityId = kvp.Key;
                var city = kvp.Value;

                if (!_previousCities.ContainsKey(cityId))
                {
                    var tile = city.tile();
                    int playerId = (int)city.getPlayer();

                    events.Add(new
                    {
                        eventType = "cityFounded",
                        cityId = cityId,
                        cityName = city.getName(),
                        playerId = playerId,
                        location = new
                        {
                            tileId = tile?.getID() ?? -1,
                            x = tile?.getX() ?? 0,
                            y = tile?.getY() ?? 0
                        }
                    });
                }
            }

            // Update snapshots for next turn
            UpdateCitySnapshots(game);
            _lastCityEvents = events;

            return events;
        }

        /// <summary>
        /// Update the city snapshots for the next turn comparison.
        /// </summary>
        private static void UpdateCitySnapshots(Game game)
        {
            _previousCities.Clear();
            try
            {
                var cities = game.getCities();
                foreach (var city in cities)
                {
                    if (city == null) continue;

                    _previousCities[city.getID()] = new CitySnapshot
                    {
                        Id = city.getID(),
                        OwnerId = (int)city.getPlayer(),
                        Name = city.getName(),
                        IsTribe = city.isTribe()
                    };
                }
            }
            catch { }
        }

        /// <summary>
        /// Get the last detected city events (for HTTP endpoint).
        /// </summary>
        public static List<object> GetLastCityEvents()
        {
            return _lastCityEvents;
        }

        #endregion

        #region Wonder Events

        // Snapshot class for storing previous turn's wonder state (used for event detection)
        private class WonderSnapshot
        {
            public ImprovementType WonderType;
            public bool IsCompleted;
            public PlayerType OwnerPlayer;
            public TribeType OwnerTribe;
        }

        // Storage for previous turn's wonder state
        private static Dictionary<ImprovementType, WonderSnapshot> _previousWonders
            = new Dictionary<ImprovementType, WonderSnapshot>();
        private static List<object> _lastWonderEvents = new List<object>();

        /// <summary>
        /// Find which city contains a completed wonder for a player.
        /// </summary>
        private static int? FindPlayerWonderCity(Game game, ImprovementType wonderType, PlayerType ownerPlayer)
        {
            try
            {
                foreach (var city in game.getCities())
                {
                    if (city == null) continue;
                    if (city.getPlayer() != ownerPlayer) continue;

                    foreach (int tileId in city.getTerritoryTiles())
                    {
                        var tile = game.tile(tileId);
                        if (tile != null && tile.getImprovement() == wonderType)
                        {
                            return city.getID();
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Detect wonder completion events by diffing current state against previous turn's state.
        /// Returns list of event objects (wonderCompleted).
        /// </summary>
        public static List<object> DetectWonderEvents(Game game, Infos infos)
        {
            var events = new List<object>();
            int currentTurn = game.getTurn();

            // Skip event detection on first turn or if turn hasn't changed
            if (_previousTurn < 0 || currentTurn <= _previousTurn)
            {
                UpdateWonderSnapshots(game, infos);
                _lastWonderEvents = events;
                return events;
            }

            try
            {
                int improvementCount = (int)infos.improvementsNum();

                for (int i = 0; i < improvementCount; i++)
                {
                    var impType = (ImprovementType)i;
                    var impInfo = infos.improvement(impType);

                    // Only check wonders
                    if (!impInfo.mbWonder) continue;

                    // Check if wonder is now owned (finished)
                    bool isNowOwned = game.getWonderOwned(impType, true,
                        out PlayerType currentPlayer, out TribeType currentTribe);

                    // Get previous state
                    bool wasOwned = _previousWonders.TryGetValue(impType, out var prev)
                        && prev.IsCompleted;

                    // Detect new completion
                    if (isNowOwned && !wasOwned)
                    {
                        int? cityId = null;
                        int? playerId = null;
                        string tribeType = null;

                        if (currentPlayer != PlayerType.NONE)
                        {
                            playerId = (int)currentPlayer;
                            cityId = FindPlayerWonderCity(game, impType, currentPlayer);
                        }
                        else if (currentTribe != TribeType.NONE)
                        {
                            tribeType = infos.tribe(currentTribe).mzType;
                            var tribeCity = game.getTribeWonderCity(impType, currentTribe);
                            cityId = tribeCity?.getID();
                        }

                        events.Add(new
                        {
                            eventType = "wonderCompleted",
                            wonder = impInfo.mzType,
                            cityId = cityId,
                            playerId = playerId,
                            tribeType = tribeType
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIEndpoint] Error detecting wonder events: {ex.Message}");
            }

            // Update snapshots for next turn
            UpdateWonderSnapshots(game, infos);
            _lastWonderEvents = events;

            return events;
        }

        /// <summary>
        /// Update the wonder snapshots for the next turn comparison.
        /// </summary>
        private static void UpdateWonderSnapshots(Game game, Infos infos)
        {
            _previousWonders.Clear();
            try
            {
                int improvementCount = (int)infos.improvementsNum();

                for (int i = 0; i < improvementCount; i++)
                {
                    var impType = (ImprovementType)i;
                    var impInfo = infos.improvement(impType);

                    // Only track wonders
                    if (!impInfo.mbWonder) continue;

                    bool isOwned = game.getWonderOwned(impType, true,
                        out PlayerType ownerPlayer, out TribeType ownerTribe);

                    _previousWonders[impType] = new WonderSnapshot
                    {
                        WonderType = impType,
                        IsCompleted = isOwned,
                        OwnerPlayer = ownerPlayer,
                        OwnerTribe = ownerTribe
                    };
                }
            }
            catch { }
        }

        /// <summary>
        /// Get the last detected wonder events (for HTTP endpoint).
        /// </summary>
        public static List<object> GetLastWonderEvents()
        {
            return _lastWonderEvents;
        }

        #endregion

        #region Team Diplomacy Methods

        /// <summary>
        /// Build list of team diplomacy relationships for JSON serialization.
        /// Each entry represents a directed relationship from one team to another.
        /// </summary>
        public static List<object> BuildTeamDiplomacyObject(Game game)
        {
            Infos infos = game.infos();
            var diplomacyList = new List<object>();
            int numTeams = (int)game.getNumTeams();

            for (int fromTeam = 0; fromTeam < numTeams; fromTeam++)
            {
                var fromTeamType = (TeamType)fromTeam;
                if (!game.isTeamAlive(fromTeamType)) continue;

                for (int toTeam = 0; toTeam < numTeams; toTeam++)
                {
                    if (fromTeam == toTeam) continue;
                    var toTeamType = (TeamType)toTeam;
                    if (!game.isTeamAlive(toTeamType)) continue;

                    try
                    {
                        diplomacyList.Add(BuildTeamDiplomacyEntry(game, infos, fromTeamType, toTeamType));
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[APIEndpoint] Error building diplomacy {fromTeam}->{toTeam}: {ex.Message}");
                    }
                }
            }
            return diplomacyList;
        }

        /// <summary>
        /// Build a single team diplomacy entry.
        /// </summary>
        private static object BuildTeamDiplomacyEntry(Game game, Infos infos, TeamType fromTeam, TeamType toTeam)
        {
            var diplomacyType = game.getTeamDiplomacy(fromTeam, toTeam);
            var diplomacyInfo = infos.diplomacy(diplomacyType);
            var warStateType = game.getTeamWarState(fromTeam, toTeam);
            var warStateInfo = infos.warState(warStateType);

            return new
            {
                fromTeam = (int)fromTeam,
                toTeam = (int)toTeam,
                diplomacy = diplomacyInfo?.mzType,
                isHostile = diplomacyInfo?.mbHostile ?? false,
                isPeace = diplomacyInfo?.mbPeace ?? false,
                hasContact = game.isTeamContact(fromTeam, toTeam),
                warScore = game.getTeamWarScore(fromTeam, toTeam),
                warState = warStateInfo?.mzType,
                conflictTurn = game.getTeamConflictTurn(fromTeam, toTeam),
                conflictNumTurns = game.getTeamConflictNumTurns(fromTeam, toTeam),
                diplomacyTurn = game.getTeamDiplomacyTurn(fromTeam, toTeam),
                diplomacyNumTurns = game.getTeamDiplomacyNumTurns(fromTeam, toTeam),
                diplomacyBlockTurn = game.getTeamDiplomacyBlock(fromTeam, toTeam),
                diplomacyBlockTurns = game.getTeamDiplomacyBlockTurns(fromTeam, toTeam)
            };
        }

        /// <summary>
        /// Build list of team alliances for JSON serialization.
        /// </summary>
        public static List<object> BuildTeamAlliancesObject(Game game)
        {
            var allianceList = new List<object>();
            int numTeams = (int)game.getNumTeams();

            for (int team = 0; team < numTeams; team++)
            {
                var teamType = (TeamType)team;
                if (!game.isTeamAlive(teamType)) continue;
                if (!game.hasTeamAlliance(teamType)) continue;

                try
                {
                    allianceList.Add(new
                    {
                        team = team,
                        allyTeam = (int)game.getTeamAlliance(teamType)
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[APIEndpoint] Error building alliance for team {team}: {ex.Message}");
                }
            }
            return allianceList;
        }

        #endregion

        #region Tribe Diplomacy Methods

        /// <summary>
        /// Build list of tribe objects for JSON serialization.
        /// Includes all tribes that exist in the game (alive or dead).
        /// </summary>
        public static List<object> BuildTribesObject(Game game)
        {
            Infos infos = game.infos();
            var tribeList = new List<object>();
            int numTribes = (int)infos.tribesNum();

            for (int t = 0; t < numTribes; t++)
            {
                var tribeType = (TribeType)t;
                var tribe = game.tribe(tribeType);
                if (tribe == null) continue;

                try
                {
                    var infoTribe = infos.tribe(tribeType);
                    tribeList.Add(new
                    {
                        tribeType = infoTribe.mzType,
                        isAlive = tribe.isAlive(),
                        isDead = tribe.isDead(),
                        hasDiplomacy = infoTribe.mbDiplomacy,
                        leaderId = tribe.hasLeader() ? (int?)tribe.getLeaderID() : null,
                        hasLeader = tribe.hasLeader(),
                        religion = tribe.isReligion() ? infos.religion(tribe.getReligion())?.mzType : null,
                        hasReligion = tribe.isReligion(),
                        allyPlayerId = tribe.hasPlayerAlly() ? (int?)tribe.getPlayerAlly() : null,
                        allyTeam = tribe.hasPlayerAlly() ? (int?)tribe.getTeamAlly() : null,
                        hasPlayerAlly = tribe.hasPlayerAlly(),
                        numUnits = tribe.getNumUnits(),
                        numCities = tribe.getNumCities(),
                        strength = tribe.calculateStrength(),
                        cityIds = tribe.getCities().ToList(),
                        settlementTileIds = tribe.getSettlements().ToList(),
                        numTribeImprovements = tribe.getNumTribeImprovements()
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[APIEndpoint] Error building tribe {t}: {ex.Message}");
                }
            }
            return tribeList;
        }

        /// <summary>
        /// Build list of tribe diplomacy relationships for JSON serialization.
        /// Each entry represents a directed relationship from one tribe to a team.
        /// </summary>
        public static List<object> BuildTribeDiplomacyObject(Game game)
        {
            Infos infos = game.infos();
            var diplomacyList = new List<object>();
            int numTribes = (int)infos.tribesNum();
            int numTeams = (int)game.getNumTeams();

            for (int t = 0; t < numTribes; t++)
            {
                var tribeType = (TribeType)t;
                if (!game.isDiplomacyTribeAlive(tribeType)) continue;

                var infoTribe = infos.tribe(tribeType);

                for (int team = 0; team < numTeams; team++)
                {
                    var teamType = (TeamType)team;
                    if (!game.isTeamAlive(teamType)) continue;

                    try
                    {
                        var diplomacyInfo = game.tribeDiplomacy(tribeType, teamType);
                        var warStateInfo = game.tribeWarState(tribeType, teamType);

                        diplomacyList.Add(new
                        {
                            tribe = infoTribe.mzType,
                            toTeam = team,
                            diplomacy = diplomacyInfo?.mzType,
                            isHostile = diplomacyInfo?.mbHostile ?? false,
                            isPeace = diplomacyInfo?.mbPeace ?? false,
                            hasContact = game.isTribeContact(tribeType, teamType),
                            warScore = game.getTribeWarScore(tribeType, teamType),
                            warState = warStateInfo?.mzType,
                            conflictTurn = game.getTribeConflictTurn(tribeType, teamType),
                            conflictNumTurns = game.getTribeConflictNumTurns(tribeType, teamType),
                            diplomacyTurn = game.getTribeDiplomacyTurn(tribeType, teamType),
                            diplomacyNumTurns = game.getTribeDiplomacyNumTurns(tribeType, teamType),
                            diplomacyBlockTurn = game.getTribeDiplomacyBlock(tribeType, teamType),
                            diplomacyBlockTurns = game.getTribeDiplomacyBlockTurns(tribeType, teamType)
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[APIEndpoint] Error building tribe diplomacy {t}->{team}: {ex.Message}");
                    }
                }
            }
            return diplomacyList;
        }

        /// <summary>
        /// Build list of tribe alliances for JSON serialization.
        /// </summary>
        public static List<object> BuildTribeAlliancesObject(Game game)
        {
            Infos infos = game.infos();
            var allianceList = new List<object>();
            int numTribes = (int)infos.tribesNum();

            for (int t = 0; t < numTribes; t++)
            {
                var tribeType = (TribeType)t;
                if (!game.isDiplomacyTribeAlive(tribeType)) continue;
                if (!game.hasTribeAlly(tribeType)) continue;

                try
                {
                    var infoTribe = infos.tribe(tribeType);
                    allianceList.Add(new
                    {
                        tribe = infoTribe.mzType,
                        allyPlayerId = (int)game.getTribeAlly(tribeType),
                        allyTeam = (int)game.getTribeAllyTeam(tribeType)
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[APIEndpoint] Error building tribe alliance for tribe {t}: {ex.Message}");
                }
            }
            return allianceList;
        }

        #endregion

        #region Single Entity Lookup Methods

        /// <summary>
        /// Get a specific player by index.
        /// </summary>
        public static object GetPlayerByIndex(Game game, int index)
        {
            Player[] players = game.getPlayers();
            if (index < 0 || index >= players.Length || players[index] == null)
                return null;

            var player = players[index];
            Infos infos = game.infos();
            int yieldCount = (int)infos.yieldsNum();

            var stockpiles = new Dictionary<string, int>();
            var rates = new Dictionary<string, int>();
            for (int y = 0; y < yieldCount; y++)
            {
                var yieldType = (YieldType)y;
                string yieldName = infos.yield(yieldType).mzType;
                stockpiles[yieldName] = player.getYieldStockpileWhole(yieldType);
                rates[yieldName] = player.calculateYieldAfterUnits(yieldType, false) / 10;
            }

            return new
            {
                index = index,
                team = (int)player.getTeam(),
                nation = infos.nation(player.getNation()).mzType,
                leaderId = player.hasFounder() ? (int?)player.getFounderID() : null,
                cities = player.getNumCities(),
                units = player.getNumUnits(),
                legitimacy = player.getLegitimacy(),
                stockpiles = stockpiles,
                rates = rates
            };
        }

        /// <summary>
        /// Get a specific city by ID.
        /// </summary>
        public static object GetCityById(Game game, int cityId)
        {
            var cities = game.getCities();
            Infos infos = game.infos();

            foreach (var city in cities)
            {
                if (city != null && city.getID() == cityId)
                    return BuildCityObject(city, game, infos);
            }
            return null;
        }

        /// <summary>
        /// Get a specific character by ID.
        /// </summary>
        public static object GetCharacterById(Game game, int characterId)
        {
            var characters = game.getCharacters();
            Infos infos = game.infos();

            foreach (var character in characters)
            {
                if (character != null && character.getID() == characterId)
                    return BuildCharacterObject(character, game, infos);
            }
            return null;
        }

        /// <summary>
        /// Get a specific tribe by type string (e.g., "TRIBE_GAULS").
        /// </summary>
        public static object GetTribeByType(Game game, string tribeTypeStr)
        {
            Infos infos = game.infos();
            int numTribes = (int)infos.tribesNum();

            for (int t = 0; t < numTribes; t++)
            {
                var tribeType = (TribeType)t;
                var infoTribe = infos.tribe(tribeType);

                if (infoTribe.mzType.Equals(tribeTypeStr, StringComparison.OrdinalIgnoreCase))
                {
                    var tribe = game.tribe(tribeType);
                    if (tribe == null) return null;

                    return new
                    {
                        tribeType = infoTribe.mzType,
                        isAlive = tribe.isAlive(),
                        isDead = tribe.isDead(),
                        hasDiplomacy = infoTribe.mbDiplomacy,
                        leaderId = tribe.hasLeader() ? (int?)tribe.getLeaderID() : null,
                        hasLeader = tribe.hasLeader(),
                        religion = tribe.isReligion() ? infos.religion(tribe.getReligion())?.mzType : null,
                        hasReligion = tribe.isReligion(),
                        allyPlayerId = tribe.hasPlayerAlly() ? (int?)tribe.getPlayerAlly() : null,
                        allyTeam = tribe.hasPlayerAlly() ? (int?)tribe.getTeamAlly() : null,
                        hasPlayerAlly = tribe.hasPlayerAlly(),
                        numUnits = tribe.getNumUnits(),
                        numCities = tribe.getNumCities(),
                        strength = tribe.calculateStrength(),
                        cityIds = tribe.getCities().ToList(),
                        settlementTileIds = tribe.getSettlements().ToList(),
                        numTribeImprovements = tribe.getNumTribeImprovements()
                    };
                }
            }
            return null;
        }

        #endregion

        public override void Initialize(ModSettings modSettings)
        {
            base.Initialize(modSettings);
            _modSettings = modSettings;
            _initCount++;

            Debug.Log($"[APIEndpoint] Initialize() called (count={_initCount})");

            // Initialize reflection FIRST so GetGame() works
            InitializeReflection();

            // Start TCP server if not already running
            if (_server == null)
            {
                _server = new TcpBroadcastServer(9876);
                _server.Start();
            }

            // Start HTTP server if not already running
            if (_httpServer == null)
            {
                _httpServer = new HttpRestServer(9877, GetGame, _jsonSettings);
                _httpServer.Start();
            }
        }

        public override void Shutdown()
        {
            Debug.Log("[APIEndpoint] Shutdown() called");
            base.Shutdown();
        }

        public override void OnNewTurnServer()
        {
            try
            {
                // Get game and cache it for HTTP thread access
                InitializeReflection();
                var appMain = GetAppMain();
                Game game = null;

                // Try to get game directly from main thread and cache it
                if (appMain != null && _getLocalGameServerMethod != null)
                {
                    var gameServer = _getLocalGameServerMethod.Invoke(appMain, null);
                    if (gameServer != null)
                    {
                        var localGameProp = gameServer.GetType().GetProperty("LocalGame",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (localGameProp != null)
                            game = localGameProp.GetValue(gameServer) as Game;
                    }
                }

                // Fallback to standard GetGame if above failed
                if (game == null) game = GetGame();

                // Cache for HTTP access
                _cachedGame = game;

                string json;

                if (game != null)
                {
                    int turn = game.getTurn();
                    int year = game.getYear();
                    Infos infos = game.infos();

                    // Detect events by diffing against previous turn's state
                    var characterEvents = DetectCharacterEvents(game, infos);
                    var unitEvents = DetectUnitEvents(game, infos);
                    var cityEvents = DetectCityEvents(game, infos);
                    var wonderEvents = DetectWonderEvents(game, infos);

                    var message = new
                    {
                        @event = "newTurn",
                        turn = turn,
                        year = year,
                        currentPlayer = (int)game.getPlayerTurn(),
                        characterEvents = characterEvents,
                        unitEvents = unitEvents,
                        cityEvents = cityEvents,
                        wonderEvents = wonderEvents,
                        players = BuildPlayersObject(game),
                        characters = BuildCharactersObject(game),
                        cities = BuildCitiesObject(game),
                        teamDiplomacy = BuildTeamDiplomacyObject(game),
                        teamAlliances = BuildTeamAlliancesObject(game),
                        tribes = BuildTribesObject(game),
                        tribeDiplomacy = BuildTribeDiplomacyObject(game),
                        tribeAlliances = BuildTribeAlliancesObject(game)
                    };
                    json = JsonConvert.SerializeObject(message, _jsonSettings);

                    Debug.Log($"[APIEndpoint] OnNewTurnServer: turn={turn}, year={year}, charEvents={characterEvents.Count}, unitEvents={unitEvents.Count}, cityEvents={cityEvents.Count}, wonderEvents={wonderEvents.Count}");
                }
                else
                {
                    Debug.Log("[APIEndpoint] OnNewTurnServer: game=null");
                    json = JsonConvert.SerializeObject(new { @event = "newTurn", error = "game not available" }, _jsonSettings);
                }

                _server?.Broadcast(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIEndpoint] OnNewTurnServer error: {ex.Message}");
            }
        }

        public override void OnGameServerReady()
        {
            try
            {
                // Get game and cache it for HTTP thread access
                InitializeReflection();
                var appMain = GetAppMain();
                Game game = null;

                // Try to get game directly from main thread and cache it
                if (appMain != null && _getLocalGameServerMethod != null)
                {
                    var gameServer = _getLocalGameServerMethod.Invoke(appMain, null);
                    if (gameServer != null)
                    {
                        var localGameProp = gameServer.GetType().GetProperty("LocalGame",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (localGameProp != null)
                            game = localGameProp.GetValue(gameServer) as Game;
                    }
                }

                // Fallback to standard GetGame if above failed
                if (game == null) game = GetGame();

                // Cache for HTTP access
                _cachedGame = game;

                string json;

                if (game != null)
                {
                    int turn = game.getTurn();
                    int year = game.getYear();

                    Infos infos = game.infos();

                    // Initialize snapshots for event detection
                    // (no events emitted on game start, just baseline state)
                    UpdateCharacterSnapshots(game);
                    UpdateUnitSnapshots(game, infos);
                    UpdateCitySnapshots(game);
                    UpdateWonderSnapshots(game, infos);
                    _previousTurn = turn;

                    var message = new
                    {
                        @event = "gameReady",
                        turn = turn,
                        year = year,
                        characterEvents = new List<object>(),  // Empty on game start
                        unitEvents = new List<object>(),       // Empty on game start
                        cityEvents = new List<object>(),       // Empty on game start
                        wonderEvents = new List<object>(),     // Empty on game start
                        players = BuildPlayersObject(game),
                        characters = BuildCharactersObject(game),
                        cities = BuildCitiesObject(game),
                        teamDiplomacy = BuildTeamDiplomacyObject(game),
                        teamAlliances = BuildTeamAlliancesObject(game),
                        tribes = BuildTribesObject(game),
                        tribeDiplomacy = BuildTribeDiplomacyObject(game),
                        tribeAlliances = BuildTribeAlliancesObject(game)
                    };
                    json = JsonConvert.SerializeObject(message, _jsonSettings);

                    Debug.Log($"[APIEndpoint] OnGameServerReady: turn={turn}, year={year}");
                }
                else
                {
                    Debug.Log("[APIEndpoint] OnGameServerReady: game=null");
                    json = JsonConvert.SerializeObject(new { @event = "gameReady", error = "game not available" }, _jsonSettings);
                }

                _server?.Broadcast(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIEndpoint] OnGameServerReady error: {ex.Message}");
            }
        }

        /// <summary>
        /// Queue a command for execution on the main thread and wait for result.
        /// Called from HTTP thread.
        /// </summary>
        public static CommandResult ExecuteCommand(GameCommand cmd, int timeoutMs = 5000)
        {
            var result = new CommandResult { RequestId = cmd.RequestId };
            using var signal = new ManualResetEventSlim(false);

            // Queue the command for processing on the main thread
            _commandQueue.Enqueue((cmd, signal, result));

            // Wait for the main thread to process it
            if (!signal.Wait(timeoutMs))
            {
                result.Success = false;
                result.Error = "Command execution timed out (main thread may not be processing)";
            }

            return result;
        }

        /// <summary>
        /// Execute multiple commands in sequence.
        /// Each command is queued and executed on the main thread.
        /// </summary>
        public static BulkCommandResult ExecuteBulkCommand(BulkCommand bulkCmd)
        {
            var result = new BulkCommandResult
            {
                RequestId = bulkCmd.RequestId,
                Results = new List<BulkCommandItemResult>(),
                AllSucceeded = true
            };

            for (int i = 0; i < bulkCmd.Commands.Count; i++)
            {
                var cmd = bulkCmd.Commands[i];
                var itemResult = new BulkCommandItemResult
                {
                    Index = i,
                    Action = cmd.Action
                };

                var execResult = ExecuteCommand(cmd);
                itemResult.Success = execResult.Success;
                itemResult.Error = execResult.Error;

                result.Results.Add(itemResult);

                if (!itemResult.Success)
                {
                    result.AllSucceeded = false;
                    if (bulkCmd.StopOnError)
                    {
                        result.StoppedAtIndex = i;
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Get or create the ClientManager instance via reflection.
        /// Path: AppMain.gApp.Client (GameClientBehaviour) -> ClientMgr (ClientManager)
        /// </summary>
        private static object GetOrCreateClientManager()
        {
            if (_clientManager != null) return _clientManager;

            try
            {
                InitializeReflection();
                var appMain = GetAppMain();
                if (appMain == null)
                {
                    Debug.Log("[APIEndpoint] GetOrCreateClientManager: appMain is null");
                    return null;
                }

                // Step 1: Get GameClientBehaviour via AppMain.gApp.Client
                object gameClientBehaviour = null;
                if (_clientProperty != null)
                {
                    gameClientBehaviour = _clientProperty.GetValue(appMain);
                }

                if (gameClientBehaviour == null)
                {
                    Debug.LogWarning("[APIEndpoint] Could not get GameClientBehaviour");
                    return null;
                }

                // Step 2: Get ClientManager via GameClientBehaviour.ClientMgr
                var clientMgrProp = gameClientBehaviour.GetType().GetProperty("ClientMgr",
                    BindingFlags.Public | BindingFlags.Instance);
                if (clientMgrProp != null)
                {
                    _clientManager = clientMgrProp.GetValue(gameClientBehaviour);
                    if (_clientManager != null)
                    {
                        Debug.Log($"[APIEndpoint] Got ClientManager: {_clientManager.GetType().Name}");
                        return _clientManager;
                    }
                }

                Debug.LogWarning("[APIEndpoint] Could not find ClientManager via ClientMgr property");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIEndpoint] Failed to get ClientManager: {ex.Message}");
            }

            return _clientManager;
        }

        /// <summary>
        /// Get the cached ClientManager for command execution.
        /// </summary>
        public static object GetClientManager()
        {
            return _clientManager;
        }

        /// <summary>
        /// Get the cached game instance.
        /// </summary>
        public static Game GetCachedGame()
        {
            return _cachedGame;
        }

        // Legacy queue methods - kept for API compatibility but now execute synchronously
        public static CommandResult QueueAndWaitCommand(GameCommand cmd, int timeoutMs = 5000)
        {
            return ExecuteCommand(cmd);
        }

        public static BulkCommandResult QueueAndWaitBulkCommand(BulkCommand cmd, int timeoutMs = 30000)
        {
            return ExecuteBulkCommand(cmd);
        }

        /// <summary>
        /// Called every frame on the main Unity thread.
        /// Process queued commands here so they execute on the correct thread.
        /// </summary>
        public override void OnClientUpdate()
        {
            // Ensure we have the game reference
            if (_cachedGame == null)
            {
                _cachedGame = GetGame();
            }

            // Process up to 10 commands per frame to avoid blocking
            int processed = 0;
            while (processed < 10 && _commandQueue.TryDequeue(out var item))
            {
                var (cmd, signal, result) = item;
                try
                {
                    // Get ClientManager on main thread
                    var clientManager = GetOrCreateClientManager();
                    if (clientManager == null)
                    {
                        result.Success = false;
                        result.Error = "ClientManager not available";
                    }
                    else if (_cachedGame == null)
                    {
                        result.Success = false;
                        result.Error = "Game not available";
                    }
                    else
                    {
                        var execResult = CommandExecutor.Execute(clientManager, _cachedGame, cmd);
                        result.RequestId = execResult.RequestId;
                        result.Success = execResult.Success;
                        result.Error = execResult.Error;
                        result.Data = execResult.Data;
                    }
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.Error = $"Exception: {ex.Message}";
                    Debug.LogError($"[APIEndpoint] Command error in OnClientUpdate: {ex}");
                }
                signal.Set(); // Signal the waiting HTTP thread
                processed++;
            }
        }

        public override bool CallOnGUI()
        {
            return false;
        }
    }
}
