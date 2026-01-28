using System;
using System.Collections.Generic;
using System.Linq;
using TenCrowns.GameCore;

namespace OldWorldAPIEndpoint
{
    /// <summary>
    /// Single entity lookup methods for HTTP endpoints.
    /// Returns individual game entities by ID or type.
    /// </summary>
    public partial class APIEndpoint
    {
        #region Player Validation Helpers

        /// <summary>
        /// Safely retrieves a player by index with bounds and null checking.
        /// </summary>
        public static bool TryGetPlayer(Game game, int index, out Player player)
        {
            player = null;
            if (game == null) return false;

            var players = game.getPlayers();
            if (index < 0 || index >= players.Length) return false;

            player = players[index];
            return player != null;
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
        /// Get a specific unit by ID using Game API O(log n) lookup.
        /// </summary>
        public static object GetUnitById(Game game, int unitId)
        {
            var unit = game.unit(unitId);
            if (unit != null && !unit.isDead())
                return BuildUnitObject(unit, game, game.infos());
            return null;
        }

        /// <summary>
        /// Get a specific city by ID using Game API O(log n) lookup.
        /// </summary>
        public static object GetCityById(Game game, int cityId)
        {
            var city = game.city(cityId);
            if (city != null)
                return BuildCityObject(city, game, game.infos());
            return null;
        }

        /// <summary>
        /// Get a specific character by ID using Game API O(log n) lookup.
        /// </summary>
        public static object GetCharacterById(Game game, int characterId)
        {
            var character = game.character(characterId);
            if (character != null)
                return BuildCharacterObject(character, game, game.infos());
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

        /// <summary>
        /// Get a single tile by ID using Game API O(1) direct array lookup.
        /// Falls back to iteration if direct access fails (edge cases).
        /// </summary>
        public static object GetTileById(Game game, int tileId)
        {
            // Try direct O(1) array access first
            try
            {
                var tile = game.tile(tileId);
                if (tile != null)
                    return BuildTileObject(tile, game, game.infos());
            }
            catch { }

            // Fallback for edge cases where direct access fails
            try
            {
                foreach (var tile in game.allTiles())
                {
                    if (tile != null && tile.getID() == tileId)
                        return BuildTileObject(tile, game, game.infos());
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Get a single tile by coordinates using Game API O(1) grid lookup.
        /// Falls back to iteration if grid access fails (edge cases).
        /// </summary>
        public static object GetTileByCoords(Game game, int x, int y)
        {
            // Try direct O(1) grid access first
            try
            {
                var tile = game.tileGrid(x, y);
                if (tile != null)
                    return BuildTileObject(tile, game, game.infos());
            }
            catch { }

            // Fallback for edge cases where grid access fails
            try
            {
                foreach (var tile in game.allTiles())
                {
                    if (tile != null && tile.getX() == x && tile.getY() == y)
                        return BuildTileObject(tile, game, game.infos());
                }
            }
            catch { }
            return null;
        }

        #endregion
    }
}
