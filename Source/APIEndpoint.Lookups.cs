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
        /// Get a specific unit by ID.
        /// </summary>
        public static object GetUnitById(Game game, int unitId)
        {
            var units = game.getUnits();
            Infos infos = game.infos();

            foreach (var unit in units)
            {
                if (unit != null && !unit.isDead() && unit.getID() == unitId)
                    return BuildUnitObject(unit, game, infos);
            }
            return null;
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

        /// <summary>
        /// Get a single tile by ID.
        /// </summary>
        public static object GetTileById(Game game, int tileId)
        {
            try
            {
                // Try direct access first
                var tile = game.tile(tileId);
                if (tile != null)
                    return BuildTileObject(tile, game, game.infos());
            }
            catch { }

            // Fallback to iterating all tiles
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
        /// Get a single tile by coordinates.
        /// </summary>
        public static object GetTileByCoords(Game game, int x, int y)
        {
            try
            {
                // Try grid access first
                var tile = game.tileGrid(x, y);
                if (tile != null)
                    return BuildTileObject(tile, game, game.infos());
            }
            catch { }

            // Fallback to iterating all tiles
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
