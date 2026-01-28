using System;
using System.Collections.Generic;
using System.Linq;
using TenCrowns.GameCore;
using UnityEngine;

namespace OldWorldAPIEndpoint
{
    /// <summary>
    /// Diplomacy methods for teams and tribes.
    /// Builds diplomatic relationships and alliance data.
    /// </summary>
    public partial class APIEndpoint
    {
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
    }
}
