using System;
using System.Collections.Generic;
using TenCrowns.GameCore;
using UnityEngine;

namespace OldWorldAPIEndpoint
{
    /// <summary>
    /// Event detection systems using snapshot diffing.
    /// Detects changes between turns for characters, units, cities, and wonders.
    /// </summary>
    public partial class APIEndpoint
    {
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
    }
}
