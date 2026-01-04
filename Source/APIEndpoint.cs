using System;
using System.Collections.Generic;
using System.Reflection;
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
        private static ModSettings _modSettings;
        private static int _initCount = 0;

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
        /// </summary>
        private static Game GetGame()
        {
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
        private static List<object> BuildPlayersObject(Game game)
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
        private static List<object> BuildCitiesObject(Game game)
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
        private static object BuildCityObject(City city, Game game, Infos infos)
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
        private static List<object> BuildCharactersObject(Game game)
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
        private static object BuildCharacterObject(Character character, Game game, Infos infos)
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

                // 10. Parents
                fatherId = character.hasFather() ? (int?)character.getFatherID() : null,
                motherId = character.hasMother() ? (int?)character.getMotherID() : null,

                // 11. Traits
                archetype = GetCharacterArchetype(character, infos),
                traits = GetCharacterTraits(character, infos),

                // 12. Ratings (stats)
                ratings = GetCharacterRatings(character, infos),

                // 13. XP & Level
                xp = character.getXP(),
                level = character.getLevel()
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

        #endregion

        #region Team Diplomacy Methods

        /// <summary>
        /// Build list of team diplomacy relationships for JSON serialization.
        /// Each entry represents a directed relationship from one team to another.
        /// </summary>
        private static List<object> BuildTeamDiplomacyObject(Game game)
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
        private static List<object> BuildTeamAlliancesObject(Game game)
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

        public override void Initialize(ModSettings modSettings)
        {
            base.Initialize(modSettings);
            _modSettings = modSettings;
            _initCount++;

            Debug.Log($"[APIEndpoint] Initialize() called (count={_initCount})");

            // Start TCP server if not already running
            if (_server == null)
            {
                _server = new TcpBroadcastServer(9876);
                _server.Start();
            }

            InitializeReflection();
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
                var game = GetGame();
                string json;

                if (game != null)
                {
                    int turn = game.getTurn();
                    int year = game.getYear();

                    var message = new
                    {
                        @event = "newTurn",
                        turn = turn,
                        year = year,
                        currentPlayer = (int)game.getPlayerTurn(),
                        players = BuildPlayersObject(game),
                        characters = BuildCharactersObject(game),
                        cities = BuildCitiesObject(game),
                        teamDiplomacy = BuildTeamDiplomacyObject(game),
                        teamAlliances = BuildTeamAlliancesObject(game)
                    };
                    json = JsonConvert.SerializeObject(message, _jsonSettings);

                    Debug.Log($"[APIEndpoint] OnNewTurnServer: turn={turn}, year={year}");
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
                var game = GetGame();
                string json;

                if (game != null)
                {
                    int turn = game.getTurn();
                    int year = game.getYear();

                    var message = new
                    {
                        @event = "gameReady",
                        turn = turn,
                        year = year,
                        players = BuildPlayersObject(game),
                        characters = BuildCharactersObject(game),
                        cities = BuildCitiesObject(game),
                        teamDiplomacy = BuildTeamDiplomacyObject(game),
                        teamAlliances = BuildTeamAlliancesObject(game)
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

        public override bool CallOnGUI()
        {
            return false;
        }
    }
}
