using System;
using System.Collections.Generic;
using System.Reflection;
using TenCrowns.GameCore;
using UnityEngine;

namespace OldWorldAPIEndpoint
{
    /// <summary>
    /// Executes game commands via ClientManager.
    /// Uses reflection to access ClientManager methods since Assembly-CSharp
    /// cannot be directly referenced by mods.
    /// </summary>
    public static class CommandExecutor
    {
        // Cached reflection info for ClientManager methods
        private static Type _clientManagerType;
        private static MethodInfo _canDoActionsMethod;
        private static MethodInfo _sendUnitMoveMethod;
        private static MethodInfo _sendUnitAttackMethod;
        private static MethodInfo _sendUnitFortifyMethod;
        private static MethodInfo _sendUnitPassMethod;
        private static MethodInfo _sendUnitSleepMethod;
        private static MethodInfo _sendUnitSentryMethod;
        private static MethodInfo _sendUnitWakeMethod;
        private static MethodInfo _sendUnitDisbandMethod;
        private static MethodInfo _sendUnitPromoteMethod;
        private static MethodInfo _sendCityBuildUnitMethod;
        private static MethodInfo _sendCityBuildProjectMethod;
        private static MethodInfo _sendHurryCivicsMethod;
        private static MethodInfo _sendHurryTrainingMethod;
        private static MethodInfo _sendHurryMoneyMethod;
        private static MethodInfo _sendHurryPopulationMethod;
        private static MethodInfo _sendHurryOrdersMethod;
        private static MethodInfo _sendResearchMethod;
        private static MethodInfo _sendEndTurnMethod;
        private static PropertyInfo _gameClientProperty;

        // Phase 1: Unit commands
        private static MethodInfo _sendHealMethod;
        private static MethodInfo _sendMarchMethod;
        private static MethodInfo _sendLockMethod;
        private static MethodInfo _sendPillageMethod;
        private static MethodInfo _sendBurnMethod;
        private static MethodInfo _sendUpgradeMethod;
        private static MethodInfo _sendSpreadReligionMethod;

        // Phase 2: Worker commands
        private static MethodInfo _sendBuildImprovementMethod;
        private static MethodInfo _sendUpgradeImprovementMethod;
        private static MethodInfo _sendAddRoadMethod;

        // Phase 3: City foundation commands
        private static MethodInfo _sendFoundCityMethod;
        private static MethodInfo _sendJoinCityMethod;

        // Phase 4: City production commands
        private static MethodInfo _sendBuildQueueMethod;

        // Phase 5: Research & decisions commands
        private static MethodInfo _sendRedrawTechMethod;
        private static MethodInfo _sendTargetTechMethod;
        private static MethodInfo _sendMakeDecisionMethod;
        private static MethodInfo _sendRemoveDecisionMethod;

        // Phase 6: Diplomacy commands
        private static MethodInfo _sendDiplomacyPlayerMethod;
        private static MethodInfo _sendDiplomacyTribeMethod;
        private static MethodInfo _sendGiftCityMethod;
        private static MethodInfo _sendGiftYieldMethod;
        private static MethodInfo _sendAllyTribeMethod;
        private static MethodInfo _getActivePlayerMethod;

        // Phase 7: Character management commands
        private static MethodInfo _sendMakeGovernorMethod;
        private static MethodInfo _sendReleaseGovernorMethod;
        private static MethodInfo _sendMakeUnitCharacterMethod;
        private static MethodInfo _sendReleaseUnitCharacterMethod;
        private static MethodInfo _sendMakeAgentMethod;
        private static MethodInfo _sendReleaseAgentMethod;
        private static MethodInfo _sendStartMissionMethod;

        // Batch A: Laws & Economy
        private static MethodInfo _sendChooseLawMethod;
        private static MethodInfo _sendCancelLawMethod;
        private static MethodInfo _sendBuyYieldMethod;
        private static MethodInfo _sendSellYieldMethod;
        private static MethodInfo _sendConvertOrdersMethod;
        private static MethodInfo _sendConvertLegitimacyMethod;
        private static MethodInfo _sendConvertOrdersToScienceMethod;

        // Batch B: Luxury Trading
        private static MethodInfo _sendTradeCityLuxuryMethod;
        private static MethodInfo _sendTradeFamilyLuxuryMethod;
        private static MethodInfo _sendTradeTribeLuxuryMethod;
        private static MethodInfo _sendTradePlayerLuxuryMethod;
        private static MethodInfo _sendTributeMethod;

        // Batch C: Unit Special Actions
        private static MethodInfo _sendSwapMethod;
        private static MethodInfo _sendDoUnitQueueMethod;
        private static MethodInfo _sendCancelUnitQueueMethod;
        private static MethodInfo _sendFormationMethod;
        private static MethodInfo _sendUnlimberMethod;
        private static MethodInfo _sendAnchorMethod;
        private static MethodInfo _sendRepairMethod;
        private static MethodInfo _sendCancelImprovementMethod;
        private static MethodInfo _sendRemoveVegetationMethod;
        private static MethodInfo _sendHarvestResourceMethod;
        private static MethodInfo _sendUnitAutomateMethod;
        private static MethodInfo _sendAddUrbanMethod;
        private static MethodInfo _sendRoadToMethod;
        private static MethodInfo _sendBuyTileMethod;
        private static MethodInfo _sendRecruitMercenaryMethod;
        private static MethodInfo _sendHireMercenaryMethod;
        private static MethodInfo _sendGiftUnitMethod;
        private static MethodInfo _sendLaunchOffensiveMethod;
        private static MethodInfo _sendApplyEffectUnitMethod;
        private static MethodInfo _sendSelectUnitMethod;

        // Batch D: Agent & Caravan Units
        private static MethodInfo _sendCreateAgentNetworkMethod;
        private static MethodInfo _sendCreateTradeOutpostMethod;
        private static MethodInfo _sendCaravanMissionStartMethod;
        private static MethodInfo _sendCaravanMissionCancelMethod;

        // Batch E: Religious Units
        private static MethodInfo _sendPurgeReligionMethod;
        private static MethodInfo _sendSpreadReligionTribeMethod;
        private static MethodInfo _sendEstablishTheologyMethod;

        // Batch F: Character Management
        private static MethodInfo _sendCharacterNameMethod;
        private static MethodInfo _sendAddCharacterTraitMethod;
        private static MethodInfo _sendSetCharacterRatingMethod;
        private static MethodInfo _sendSetCharacterExperienceMethod;
        private static MethodInfo _sendSetCharacterCognomenMethod;
        private static MethodInfo _sendSetCharacterNationMethod;
        private static MethodInfo _sendSetCharacterFamilyMethod;
        private static MethodInfo _sendSetCharacterReligionMethod;
        private static MethodInfo _sendSetCharacterCourtierMethod;
        private static MethodInfo _sendSetCharacterCouncilMethod;
        private static MethodInfo _sendPlayerLeaderMethod;
        private static MethodInfo _sendFamilyHeadMethod;
        private static MethodInfo _sendPinCharacterMethod;

        // Batch G: City Management
        private static MethodInfo _sendCityRenameMethod;
        private static MethodInfo _sendCityAutomateMethod;
        private static MethodInfo _sendBuildSpecialistMethod;
        private static MethodInfo _sendSetSpecialistMethod;
        private static MethodInfo _sendChangeCitizensMethod;
        private static MethodInfo _sendChangeReligionMethod;
        private static MethodInfo _sendChangeFamilyMethod;
        private static MethodInfo _sendChangeFamilySeatMethod;

        // Batch H: Goals & Communication
        private static MethodInfo _sendAbandonAmbitionMethod;
        private static MethodInfo _sendAddPlayerGoalMethod;
        private static MethodInfo _sendRemovePlayerGoalMethod;
        private static MethodInfo _sendEventStoryMethod;
        private static MethodInfo _sendFinishGoalMethod;
        private static MethodInfo _sendChatMethod;
        private static MethodInfo _sendPingMethod;
        private static MethodInfo _sendCustomReminderMethod;
        private static MethodInfo _sendClearChatMethod;

        // Batch I: Game State & Turn
        private static MethodInfo _sendExtendTimeMethod;
        private static MethodInfo _sendPauseMethod;
        private static MethodInfo _sendUndoMethod;
        private static MethodInfo _sendRedoMethod;
        private static MethodInfo _sendReplayTurnMethod;
        private static MethodInfo _sendAIFinishTurnMethod;
        private static MethodInfo _sendToggleNoReplayMethod;

        // Batch J: Diplomacy Extended
        private static MethodInfo _sendTeamAllianceMethod;
        private static MethodInfo _sendTribeInvasionMethod;
        private static MethodInfo _sendVictoryTeamMethod;

        // Batch K: Editor/Debug
        private static MethodInfo _sendCreateUnitMethod;
        private static MethodInfo _sendCreateCityMethod;
        private static MethodInfo _sendRemoveCityMethod;
        private static MethodInfo _sendCityOwnerMethod;
        private static MethodInfo _sendTerrainMethod;
        private static MethodInfo _sendTerrainHeightMethod;
        private static MethodInfo _sendVegetationMethod;
        private static MethodInfo _sendResourceMethod;
        private static MethodInfo _sendRoadMethod;
        private static MethodInfo _sendSetImprovementMethod;
        private static MethodInfo _sendTileOwnerMethod;
        private static MethodInfo _sendMapRevealMethod;
        private static MethodInfo _sendMapUnrevealMethod;
        private static MethodInfo _sendAddTechMethod;
        private static MethodInfo _sendAddYieldMethod;
        private static MethodInfo _sendAddMoneyMethod;
        private static MethodInfo _sendCheatMethod;
        private static MethodInfo _sendMakeCharacterDeadMethod;
        private static MethodInfo _sendMakeCharacterSafeMethod;
        private static MethodInfo _sendNewCharacterMethod;
        private static MethodInfo _sendAddCharacterMethod;
        private static MethodInfo _sendUnitNameMethod;
        private static MethodInfo _sendSetUnitFamilyMethod;
        private static MethodInfo _sendChangeUnitOwnerMethod;
        private static MethodInfo _sendChangeCooldownMethod;
        private static MethodInfo _sendChangeDamageMethod;
        private static MethodInfo _sendUnitIncrementLevelMethod;
        private static MethodInfo _sendUnitChangePromotionMethod;
        private static MethodInfo _sendTribeLeaderMethod;
        private static MethodInfo _sendSetCitySiteMethod;
        private static MethodInfo _sendImprovementBuildTurnsMethod;
        private static MethodInfo _sendChangeCityDamageMethod;
        private static MethodInfo _sendChangeCultureMethod;
        private static MethodInfo _sendChangeCityBuildTurnsMethod;
        private static MethodInfo _sendChangeCityDiscontentLevelMethod;
        private static MethodInfo _sendChangeProjectMethod;

        private static bool _reflectionInitialized;

        /// <summary>
        /// Initialize reflection for ClientManager access.
        /// </summary>
        private static void InitializeReflection(object clientManager)
        {
            if (_reflectionInitialized || clientManager == null) return;

            try
            {
                _clientManagerType = clientManager.GetType();

                // Core action check
                _canDoActionsMethod = _clientManagerType.GetMethod("canDoActions",
                    BindingFlags.Public | BindingFlags.Instance);

                // GameClient property
                _gameClientProperty = _clientManagerType.GetProperty("GameClient",
                    BindingFlags.Public | BindingFlags.Instance);

                // Unit commands - methods take Unit/Tile objects, not IDs
                // sendMoveUnit(Unit, Tile, Boolean, Boolean, Tile)
                _sendUnitMoveMethod = _clientManagerType.GetMethod("sendMoveUnit",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendAttack(Unit, Tile)
                _sendUnitAttackMethod = _clientManagerType.GetMethod("sendAttack",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendFortify(Unit)
                _sendUnitFortifyMethod = _clientManagerType.GetMethod("sendFortify",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendPass(Unit)
                _sendUnitPassMethod = _clientManagerType.GetMethod("sendPass",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendSleep(Unit)
                _sendUnitSleepMethod = _clientManagerType.GetMethod("sendSleep",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendSentry(Unit)
                _sendUnitSentryMethod = _clientManagerType.GetMethod("sendSentry",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendWake(Unit)
                _sendUnitWakeMethod = _clientManagerType.GetMethod("sendWake",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendDisband(Unit, Boolean)
                _sendUnitDisbandMethod = _clientManagerType.GetMethod("sendDisband",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendPromote(Unit, PromotionType)
                _sendUnitPromoteMethod = _clientManagerType.GetMethod("sendPromote",
                    BindingFlags.Public | BindingFlags.Instance);

                // City commands
                // sendBuildUnit(City, UnitType, Boolean, Tile, Boolean)
                _sendCityBuildUnitMethod = _clientManagerType.GetMethod("sendBuildUnit",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendBuildProject(City, ProjectType, Boolean, Boolean, Boolean)
                _sendCityBuildProjectMethod = _clientManagerType.GetMethod("sendBuildProject",
                    BindingFlags.Public | BindingFlags.Instance);
                // Hurry methods - each takes just (City)
                _sendHurryCivicsMethod = _clientManagerType.GetMethod("sendHurryCivics",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendHurryTrainingMethod = _clientManagerType.GetMethod("sendHurryTraining",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendHurryMoneyMethod = _clientManagerType.GetMethod("sendHurryMoney",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendHurryPopulationMethod = _clientManagerType.GetMethod("sendHurryPopulation",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendHurryOrdersMethod = _clientManagerType.GetMethod("sendHurryOrders",
                    BindingFlags.Public | BindingFlags.Instance);

                // Research - sendResearchTech(TechType)
                _sendResearchMethod = _clientManagerType.GetMethod("sendResearchTech",
                    BindingFlags.Public | BindingFlags.Instance);

                // Turn
                _sendEndTurnMethod = FindMethod(_clientManagerType, "sendEndTurn",
                    Type.EmptyTypes);

                // Phase 1: Unit commands
                // sendHeal(Unit, Boolean)
                _sendHealMethod = _clientManagerType.GetMethod("sendHeal",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendMarch(Unit)
                _sendMarchMethod = _clientManagerType.GetMethod("sendMarch",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendLock(Unit)
                _sendLockMethod = _clientManagerType.GetMethod("sendLock",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendPillage(Unit)
                _sendPillageMethod = _clientManagerType.GetMethod("sendPillage",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendBurn(Unit)
                _sendBurnMethod = _clientManagerType.GetMethod("sendBurn",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendUpgrade(Unit, UnitType, Boolean)
                _sendUpgradeMethod = _clientManagerType.GetMethod("sendUpgrade",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendSpreadReligion(Unit, Int32)
                _sendSpreadReligionMethod = _clientManagerType.GetMethod("sendSpreadReligion",
                    BindingFlags.Public | BindingFlags.Instance);

                // Phase 2: Worker commands
                // sendBuildImprovement(Unit, ImprovementType, Boolean, Boolean, Tile)
                _sendBuildImprovementMethod = _clientManagerType.GetMethod("sendBuildImprovement",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendUpgradeImprovement(Unit, Boolean)
                _sendUpgradeImprovementMethod = _clientManagerType.GetMethod("sendUpgradeImprovement",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendAddRoad(Unit, Boolean, Boolean, Tile)
                _sendAddRoadMethod = _clientManagerType.GetMethod("sendAddRoad",
                    BindingFlags.Public | BindingFlags.Instance);

                // Phase 3: City foundation commands
                // sendFoundCity(Unit, FamilyType, NationType)
                _sendFoundCityMethod = _clientManagerType.GetMethod("sendFoundCity",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendJoinCity(Unit)
                _sendJoinCityMethod = _clientManagerType.GetMethod("sendJoinCity",
                    BindingFlags.Public | BindingFlags.Instance);

                // Phase 4: City production commands
                // sendBuildQueue(City, Int32, Int32)
                _sendBuildQueueMethod = _clientManagerType.GetMethod("sendBuildQueue",
                    BindingFlags.Public | BindingFlags.Instance);

                // Phase 5: Research & decisions commands
                // sendRedrawTech()
                _sendRedrawTechMethod = _clientManagerType.GetMethod("sendRedrawTech",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendTargetTech(TechType)
                _sendTargetTechMethod = _clientManagerType.GetMethod("sendTargetTech",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendMakeDecision(Int32, Int32, Int32)
                _sendMakeDecisionMethod = _clientManagerType.GetMethod("sendMakeDecision",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendRemoveDecision(Int32)
                _sendRemoveDecisionMethod = _clientManagerType.GetMethod("sendRemoveDecision",
                    BindingFlags.Public | BindingFlags.Instance);

                // Phase 6: Diplomacy commands
                // sendDiplomacyPlayer(PlayerType, PlayerType, ActionType)
                _sendDiplomacyPlayerMethod = _clientManagerType.GetMethod("sendDiplomacyPlayer",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendDiplomacyTribe(TribeType, PlayerType, ActionType)
                _sendDiplomacyTribeMethod = _clientManagerType.GetMethod("sendDiplomacyTribe",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendGiftCity(City, PlayerType)
                _sendGiftCityMethod = _clientManagerType.GetMethod("sendGiftCity",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendGiftYield(YieldType, PlayerType, Boolean)
                _sendGiftYieldMethod = _clientManagerType.GetMethod("sendGiftYield",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendAllyTribe(TribeType, PlayerType)
                _sendAllyTribeMethod = _clientManagerType.GetMethod("sendAllyTribe",
                    BindingFlags.Public | BindingFlags.Instance);
                // getActivePlayer()
                _getActivePlayerMethod = _clientManagerType.GetMethod("getActivePlayer",
                    BindingFlags.Public | BindingFlags.Instance);

                // Phase 7: Character management commands
                // sendMakeGovernor(City, Character)
                _sendMakeGovernorMethod = _clientManagerType.GetMethod("sendMakeGovernor",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendReleaseGovernor(City)
                _sendReleaseGovernorMethod = _clientManagerType.GetMethod("sendReleaseGovernor",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendMakeUnitCharacter(Unit, Character, Boolean)
                _sendMakeUnitCharacterMethod = _clientManagerType.GetMethod("sendMakeUnitCharacter",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendReleaseUnitCharacter(Unit)
                _sendReleaseUnitCharacterMethod = _clientManagerType.GetMethod("sendReleaseUnitCharacter",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendMakeAgent(City, Character)
                _sendMakeAgentMethod = _clientManagerType.GetMethod("sendMakeAgent",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendReleaseAgent(City)
                _sendReleaseAgentMethod = _clientManagerType.GetMethod("sendReleaseAgent",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendStartMission(MissionType, Int32, String, Boolean)
                _sendStartMissionMethod = _clientManagerType.GetMethod("sendStartMission",
                    BindingFlags.Public | BindingFlags.Instance);

                // Batch A: Laws & Economy
                // sendChooseLaw(LawType)
                _sendChooseLawMethod = _clientManagerType.GetMethod("sendChooseLaw",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendCancelLaw(LawType)
                _sendCancelLawMethod = _clientManagerType.GetMethod("sendCancelLaw",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendBuyYield(YieldType, int)
                _sendBuyYieldMethod = _clientManagerType.GetMethod("sendBuyYield",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendSellYield(YieldType, int)
                _sendSellYieldMethod = _clientManagerType.GetMethod("sendSellYield",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendConvertOrders()
                _sendConvertOrdersMethod = _clientManagerType.GetMethod("sendConvertOrders",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendConvertLegitimacy()
                _sendConvertLegitimacyMethod = _clientManagerType.GetMethod("sendConvertLegitimacy",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendConvertOrdersToScience()
                _sendConvertOrdersToScienceMethod = _clientManagerType.GetMethod("sendConvertOrdersToScience",
                    BindingFlags.Public | BindingFlags.Instance);

                // Batch B: Luxury Trading
                // sendTradeCityLuxury(City, ResourceType, bool)
                _sendTradeCityLuxuryMethod = _clientManagerType.GetMethod("sendTradeCityLuxury",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendTradeFamilyLuxury(FamilyType, ResourceType, bool)
                _sendTradeFamilyLuxuryMethod = _clientManagerType.GetMethod("sendTradeFamilyLuxury",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendTradeTribeLuxury(TribeType, ResourceType, bool)
                _sendTradeTribeLuxuryMethod = _clientManagerType.GetMethod("sendTradeTribeLuxury",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendTradePlayerLuxury(PlayerType, ResourceType, bool)
                _sendTradePlayerLuxuryMethod = _clientManagerType.GetMethod("sendTradePlayerLuxury",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendTribute(PlayerType, TribeType, YieldType, int, PlayerType)
                _sendTributeMethod = _clientManagerType.GetMethod("sendTribute",
                    BindingFlags.Public | BindingFlags.Instance);

                // Batch C: Unit Special Actions
                // sendSwap(Unit, Tile, bool)
                _sendSwapMethod = _clientManagerType.GetMethod("sendSwap",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendDoUnitQueue(Unit)
                _sendDoUnitQueueMethod = _clientManagerType.GetMethod("sendDoUnitQueue",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendCancelUnitQueue(Unit, bool)
                _sendCancelUnitQueueMethod = _clientManagerType.GetMethod("sendCancelUnitQueue",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendFormation(Unit, EffectUnitType)
                _sendFormationMethod = _clientManagerType.GetMethod("sendFormation",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendUnlimber(Unit)
                _sendUnlimberMethod = _clientManagerType.GetMethod("sendUnlimber",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendAnchor(Unit)
                _sendAnchorMethod = _clientManagerType.GetMethod("sendAnchor",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendRepair(Unit, bool, bool, Tile)
                _sendRepairMethod = _clientManagerType.GetMethod("sendRepair",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendCancelImprovement(Unit)
                _sendCancelImprovementMethod = _clientManagerType.GetMethod("sendCancelImprovement",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendRemoveVegetation(Unit)
                _sendRemoveVegetationMethod = _clientManagerType.GetMethod("sendRemoveVegetation",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendHarvestResource(Unit, bool)
                _sendHarvestResourceMethod = _clientManagerType.GetMethod("sendHarvestResource",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendUnitAutomate(Unit)
                _sendUnitAutomateMethod = _clientManagerType.GetMethod("sendUnitAutomate",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendAddUrban(Unit, bool)
                _sendAddUrbanMethod = _clientManagerType.GetMethod("sendAddUrban",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendRoadTo(Unit, bool, int[])
                _sendRoadToMethod = _clientManagerType.GetMethod("sendRoadTo",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendBuyTile(Unit, int, YieldType)
                _sendBuyTileMethod = _clientManagerType.GetMethod("sendBuyTile",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendRecruitMercenary(Unit)
                _sendRecruitMercenaryMethod = _clientManagerType.GetMethod("sendRecruitMercenary",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendHireMercenary(Unit)
                _sendHireMercenaryMethod = _clientManagerType.GetMethod("sendHireMercenary",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendGiftUnit(Unit, PlayerType)
                _sendGiftUnitMethod = _clientManagerType.GetMethod("sendGiftUnit",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendLaunchOffensive(Unit)
                _sendLaunchOffensiveMethod = _clientManagerType.GetMethod("sendLaunchOffensive",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendApplyEffectUnit(Unit, EffectUnitType)
                _sendApplyEffectUnitMethod = _clientManagerType.GetMethod("sendApplyEffectUnit",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendSelectUnit(Unit)
                _sendSelectUnitMethod = _clientManagerType.GetMethod("sendSelectUnit",
                    BindingFlags.Public | BindingFlags.Instance);

                // Batch D: Agent & Caravan Units
                // sendCreateAgentNetwork(Unit, int)
                _sendCreateAgentNetworkMethod = _clientManagerType.GetMethod("sendCreateAgentNetwork",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendCreateTradeOutpost(Unit, int)
                _sendCreateTradeOutpostMethod = _clientManagerType.GetMethod("sendCreateTradeOutpost",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendCaravanMissionStart(Unit, PlayerType)
                _sendCaravanMissionStartMethod = _clientManagerType.GetMethod("sendCaravanMissionStart",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendCaravanMissionCancel(Unit)
                _sendCaravanMissionCancelMethod = _clientManagerType.GetMethod("sendCaravanMissionCancel",
                    BindingFlags.Public | BindingFlags.Instance);

                // Batch E: Religious Units
                // sendPurgeReligion(Unit, ReligionType)
                _sendPurgeReligionMethod = _clientManagerType.GetMethod("sendPurgeReligion",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendSpreadReligionTribe(Unit, TribeType)
                _sendSpreadReligionTribeMethod = _clientManagerType.GetMethod("sendSpreadReligionTribe",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendEstablishTheology(Unit, TheologyType)
                _sendEstablishTheologyMethod = _clientManagerType.GetMethod("sendEstablishTheology",
                    BindingFlags.Public | BindingFlags.Instance);

                // Batch F: Character Management
                // sendCharacterName(int, string)
                _sendCharacterNameMethod = _clientManagerType.GetMethod("sendCharacterName",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendAddCharacterTrait(int, TraitType, bool)
                _sendAddCharacterTraitMethod = _clientManagerType.GetMethod("sendAddCharacterTrait",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendSetCharacterRating(int, RatingType, int)
                _sendSetCharacterRatingMethod = _clientManagerType.GetMethod("sendSetCharacterRating",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendSetCharacterExperience(int, int)
                _sendSetCharacterExperienceMethod = _clientManagerType.GetMethod("sendSetCharacterExperience",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendSetCharacterCognomen(int, CognomenType)
                _sendSetCharacterCognomenMethod = _clientManagerType.GetMethod("sendSetCharacterCognomen",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendSetCharacterNation(int, NationType)
                _sendSetCharacterNationMethod = _clientManagerType.GetMethod("sendSetCharacterNation",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendSetCharacterFamily(int, FamilyType)
                _sendSetCharacterFamilyMethod = _clientManagerType.GetMethod("sendSetCharacterFamily",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendSetCharacterReligion(ReligionType, int)
                _sendSetCharacterReligionMethod = _clientManagerType.GetMethod("sendSetCharacterReligion",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendSetCharacterCourtier(int, CourtierType)
                _sendSetCharacterCourtierMethod = _clientManagerType.GetMethod("sendSetCharacterCourtier",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendSetCharacterCouncil(int, CouncilType)
                _sendSetCharacterCouncilMethod = _clientManagerType.GetMethod("sendSetCharacterCouncil",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendPlayerLeader(PlayerType, int)
                _sendPlayerLeaderMethod = _clientManagerType.GetMethod("sendPlayerLeader",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendFamilyHead(PlayerType, FamilyType, int)
                _sendFamilyHeadMethod = _clientManagerType.GetMethod("sendFamilyHead",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendPinCharacter(int)
                _sendPinCharacterMethod = _clientManagerType.GetMethod("sendPinCharacter",
                    BindingFlags.Public | BindingFlags.Instance);

                // Batch G: City Management
                // sendCityRename(int, string)
                _sendCityRenameMethod = _clientManagerType.GetMethod("sendCityRename",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendCityAutomate(City, bool)
                _sendCityAutomateMethod = _clientManagerType.GetMethod("sendCityAutomate",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendBuildSpecialist(Tile, SpecialistType, bool, bool)
                _sendBuildSpecialistMethod = _clientManagerType.GetMethod("sendBuildSpecialist",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendSetSpecialist(Tile, SpecialistType)
                _sendSetSpecialistMethod = _clientManagerType.GetMethod("sendSetSpecialist",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendChangeCitizens(City, int)
                _sendChangeCitizensMethod = _clientManagerType.GetMethod("sendChangeCitizens",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendChangeReligion(City, ReligionType, bool)
                _sendChangeReligionMethod = _clientManagerType.GetMethod("sendChangeReligion",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendChangeFamily(City, FamilyType)
                _sendChangeFamilyMethod = _clientManagerType.GetMethod("sendChangeFamily",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendChangeFamilySeat(City, FamilyType)
                _sendChangeFamilySeatMethod = _clientManagerType.GetMethod("sendChangeFamilySeat",
                    BindingFlags.Public | BindingFlags.Instance);

                // Batch H: Goals & Communication
                // sendAbandonAmbition(int)
                _sendAbandonAmbitionMethod = _clientManagerType.GetMethod("sendAbandonAmbition",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendAddPlayerGoal(PlayerType, GoalType)
                _sendAddPlayerGoalMethod = _clientManagerType.GetMethod("sendAddPlayerGoal",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendRemovePlayerGoal(PlayerType, int)
                _sendRemovePlayerGoalMethod = _clientManagerType.GetMethod("sendRemovePlayerGoal",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendEventStory(PlayerType, EventStoryType)
                _sendEventStoryMethod = _clientManagerType.GetMethod("sendEventStory",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendFinishGoal(GoalType, bool)
                _sendFinishGoalMethod = _clientManagerType.GetMethod("sendFinishGoal",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendChat(ChatType, PlayerType, string)
                _sendChatMethod = _clientManagerType.GetMethod("sendChat",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendPing(int, PingType, string, int, ImprovementType)
                _sendPingMethod = _clientManagerType.GetMethod("sendPing",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendCustomReminder(string)
                _sendCustomReminderMethod = _clientManagerType.GetMethod("sendCustomReminder",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendClearChat()
                _sendClearChatMethod = _clientManagerType.GetMethod("sendClearChat",
                    BindingFlags.Public | BindingFlags.Instance);

                // Batch I: Game State & Turn
                // sendExtendTime()
                _sendExtendTimeMethod = _clientManagerType.GetMethod("sendExtendTime",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendPause()
                _sendPauseMethod = _clientManagerType.GetMethod("sendPause",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendUndo(bool)
                _sendUndoMethod = _clientManagerType.GetMethod("sendUndo",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendRedo()
                _sendRedoMethod = _clientManagerType.GetMethod("sendRedo",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendReplayTurn(int, bool)
                _sendReplayTurnMethod = _clientManagerType.GetMethod("sendReplayTurn",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendAIFinishTurn(int)
                _sendAIFinishTurnMethod = _clientManagerType.GetMethod("sendAIFinishTurn",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendToggleNoReplay()
                _sendToggleNoReplayMethod = _clientManagerType.GetMethod("sendToggleNoReplay",
                    BindingFlags.Public | BindingFlags.Instance);

                // Batch J: Diplomacy Extended
                // sendTeamAlliance(PlayerType, PlayerType)
                _sendTeamAllianceMethod = _clientManagerType.GetMethod("sendTeamAlliance",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendTribeInvasion(TribeType, PlayerType)
                _sendTribeInvasionMethod = _clientManagerType.GetMethod("sendTribeInvasion",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendVictoryTeam(VictoryType, TeamType, ActionType)
                _sendVictoryTeamMethod = _clientManagerType.GetMethod("sendVictoryTeam",
                    BindingFlags.Public | BindingFlags.Instance);

                // Batch K: Editor/Debug
                _sendCreateUnitMethod = _clientManagerType.GetMethod("sendCreateUnit",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendCreateCityMethod = _clientManagerType.GetMethod("sendCreateCity",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendRemoveCityMethod = _clientManagerType.GetMethod("sendRemoveCity",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendCityOwnerMethod = _clientManagerType.GetMethod("sendCityOwner",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendTerrainMethod = _clientManagerType.GetMethod("sendTerrain",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendTerrainHeightMethod = _clientManagerType.GetMethod("sendTerrainHeight",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendVegetationMethod = _clientManagerType.GetMethod("sendVegetation",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendResourceMethod = _clientManagerType.GetMethod("sendResource",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendRoadMethod = _clientManagerType.GetMethod("sendRoad",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendSetImprovementMethod = _clientManagerType.GetMethod("sendSetImprovement",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendTileOwnerMethod = _clientManagerType.GetMethod("sendTileOwner",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendMapRevealMethod = _clientManagerType.GetMethod("sendMapReveal",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendMapUnrevealMethod = _clientManagerType.GetMethod("sendMapUnreveal",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendAddTechMethod = _clientManagerType.GetMethod("sendAddTech",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendAddYieldMethod = _clientManagerType.GetMethod("sendAddYield",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendAddMoneyMethod = _clientManagerType.GetMethod("sendAddMoney",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendCheatMethod = _clientManagerType.GetMethod("sendCheat",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendMakeCharacterDeadMethod = _clientManagerType.GetMethod("sendMakeCharacterDead",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendMakeCharacterSafeMethod = _clientManagerType.GetMethod("sendMakeCharacterSafe",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendNewCharacterMethod = _clientManagerType.GetMethod("sendNewCharacter",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendAddCharacterMethod = _clientManagerType.GetMethod("sendAddCharacter",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendUnitNameMethod = _clientManagerType.GetMethod("sendUnitName",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendSetUnitFamilyMethod = _clientManagerType.GetMethod("sendSetUnitFamily",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendChangeUnitOwnerMethod = _clientManagerType.GetMethod("sendChangeUnitOwner",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendChangeCooldownMethod = _clientManagerType.GetMethod("sendChangeCooldown",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendChangeDamageMethod = _clientManagerType.GetMethod("sendChangeDamage",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendUnitIncrementLevelMethod = _clientManagerType.GetMethod("sendUnitIncrementLevel",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendUnitChangePromotionMethod = _clientManagerType.GetMethod("sendUnitChangePromotion",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendTribeLeaderMethod = _clientManagerType.GetMethod("sendTribeLeader",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendSetCitySiteMethod = _clientManagerType.GetMethod("sendSetCitySite",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendImprovementBuildTurnsMethod = _clientManagerType.GetMethod("sendImprovementBuildTurns",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendChangeCityDamageMethod = _clientManagerType.GetMethod("sendChangeCityDamage",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendChangeCultureMethod = _clientManagerType.GetMethod("sendChangeCulture",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendChangeCityBuildTurnsMethod = _clientManagerType.GetMethod("sendChangeCityBuildTurns",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendChangeCityDiscontentLevelMethod = _clientManagerType.GetMethod("sendChangeCityDiscontentLevel",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendChangeProjectMethod = _clientManagerType.GetMethod("sendChangeProject",
                    BindingFlags.Public | BindingFlags.Instance);

                // Log what we found
                Debug.Log($"[APIEndpoint] CommandExecutor reflection on {_clientManagerType.Name}:");
                Debug.Log($"[APIEndpoint]   canDoActions: {_canDoActionsMethod != null}");
                Debug.Log($"[APIEndpoint]   sendMoveUnit: {_sendUnitMoveMethod != null}");
                Debug.Log($"[APIEndpoint]   sendBuildUnit: {_sendCityBuildUnitMethod != null}");
                Debug.Log($"[APIEndpoint]   sendBuildProject: {_sendCityBuildProjectMethod != null}");
                Debug.Log($"[APIEndpoint]   sendHurryCivics: {_sendHurryCivicsMethod != null}");
                Debug.Log($"[APIEndpoint]   sendEndTurn: {_sendEndTurnMethod != null}");

                // Log methods containing "unit", "move", "send", "attack"
                Debug.Log($"[APIEndpoint] Methods on {_clientManagerType.Name}:");
                var methods = _clientManagerType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                foreach (var m in methods)
                {
                    string nameLower = m.Name.ToLower();
                    if (nameLower.Contains("unit") || nameLower.Contains("move") ||
                        nameLower.Contains("attack") || nameLower.Contains("send") ||
                        nameLower.Contains("action") || nameLower.Contains("order"))
                    {
                        var paramStr = string.Join(", ", Array.ConvertAll(m.GetParameters(), p => p.ParameterType.Name));
                        Debug.Log($"[APIEndpoint]   Method: {m.Name}({paramStr})");
                    }
                }

                Debug.Log($"[APIEndpoint] CommandExecutor reflection initialized");
                _reflectionInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIEndpoint] CommandExecutor reflection failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Find a method with the given name and parameter types, or any matching name if not found.
        /// </summary>
        private static MethodInfo FindMethod(Type type, string name, Type[] paramTypes)
        {
            // Try exact match first
            var method = type.GetMethod(name, BindingFlags.Public | BindingFlags.Instance, null, paramTypes, null);
            if (method != null) return method;

            // Fall back to any method with that name
            return type.GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
        }

        /// <summary>
        /// Execute a game command via ClientManager.
        /// Must be called from Unity's main thread.
        /// </summary>
        public static CommandResult Execute(object clientManager, Game game, GameCommand cmd)
        {
            var result = new CommandResult { RequestId = cmd.RequestId };

            if (clientManager == null)
            {
                result.Error = "ClientManager not available";
                return result;
            }

            InitializeReflection(clientManager);

            // Check if player can perform actions
            if (_canDoActionsMethod != null)
            {
                try
                {
                    bool canAct = (bool)_canDoActionsMethod.Invoke(clientManager, null);
                    if (!canAct)
                    {
                        result.Error = "Cannot perform actions (not player's turn or action blocked)";
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CommandExecutor] canDoActions check failed: {ex.Message}");
                }
            }

            // Check for multiplayer (refuse commands in MP)
            if (game != null && game.isMultiplayer())
            {
                result.Error = "Commands not supported in multiplayer games";
                return result;
            }

            // Dispatch based on action
            string action = cmd.Action?.ToLowerInvariant() ?? "";

            switch (action)
            {
                // Unit Movement & Combat
                case "moveunit":
                    return ExecuteMoveUnit(clientManager, game, cmd, result);
                case "attack":
                    return ExecuteAttack(clientManager, game, cmd, result);
                case "fortify":
                    return ExecuteFortify(clientManager, game, cmd, result);
                case "pass":
                case "skip":
                    return ExecutePass(clientManager, game, cmd, result);
                case "sleep":
                    return ExecuteSleep(clientManager, game, cmd, result);
                case "sentry":
                    return ExecuteSentry(clientManager, game, cmd, result);
                case "wake":
                    return ExecuteWake(clientManager, game, cmd, result);
                case "disband":
                    return ExecuteDisband(clientManager, game, cmd, result);
                case "promote":
                    return ExecutePromote(clientManager, game, cmd, result);

                // Unit Commands - Phase 1
                case "heal":
                    return ExecuteHeal(clientManager, game, cmd, result);
                case "march":
                    return ExecuteMarch(clientManager, game, cmd, result);
                case "lock":
                    return ExecuteLock(clientManager, game, cmd, result);
                case "pillage":
                    return ExecutePillage(clientManager, game, cmd, result);
                case "burn":
                    return ExecuteBurn(clientManager, game, cmd, result);
                case "upgrade":
                    return ExecuteUpgrade(clientManager, game, cmd, result);
                case "spreadreligion":
                    return ExecuteSpreadReligion(clientManager, game, cmd, result);

                // Worker Commands - Phase 2
                case "buildimprovement":
                    return ExecuteBuildImprovement(clientManager, game, cmd, result);
                case "upgradeimprovement":
                    return ExecuteUpgradeImprovement(clientManager, game, cmd, result);
                case "addroad":
                    return ExecuteAddRoad(clientManager, game, cmd, result);

                // City Foundation - Phase 3
                case "foundcity":
                    return ExecuteFoundCity(clientManager, game, cmd, result);
                case "joincity":
                    return ExecuteJoinCity(clientManager, game, cmd, result);

                // City Production
                case "build":
                case "buildunit":
                    return ExecuteBuildUnit(clientManager, game, cmd, result);
                case "buildproject":
                    return ExecuteBuildProject(clientManager, game, cmd, result);
                case "hurry":
                case "hurryCivics":
                case "hurrycivics":
                    return ExecuteHurryCivics(clientManager, game, cmd, result);
                case "hurryTraining":
                case "hurrytraining":
                    return ExecuteHurryTraining(clientManager, game, cmd, result);
                case "hurryMoney":
                case "hurrymoney":
                    return ExecuteHurryMoney(clientManager, game, cmd, result);
                case "hurryPopulation":
                case "hurrypopulation":
                    return ExecuteHurryPopulation(clientManager, game, cmd, result);
                case "hurryOrders":
                case "hurryorders":
                    return ExecuteHurryOrders(clientManager, game, cmd, result);

                // Research
                case "research":
                    return ExecuteResearch(clientManager, game, cmd, result);

                // Phase 4: City Production
                case "buildqueue":
                    return ExecuteBuildQueue(clientManager, game, cmd, result);

                // Phase 5: Research & Decisions
                case "redrawtech":
                    return ExecuteRedrawTech(clientManager, game, cmd, result);
                case "targettech":
                    return ExecuteTargetTech(clientManager, game, cmd, result);
                case "makedecision":
                    return ExecuteMakeDecision(clientManager, game, cmd, result);
                case "removedecision":
                    return ExecuteRemoveDecision(clientManager, game, cmd, result);

                // Turn Management
                case "endturn":
                    return ExecuteEndTurn(clientManager, game, cmd, result);

                // Phase 6: Diplomacy - Player
                case "declarewar":
                    return ExecuteDeclareWar(clientManager, game, cmd, result);
                case "makepeace":
                    return ExecuteMakePeace(clientManager, game, cmd, result);
                case "declaretruce":
                    return ExecuteDeclareTruce(clientManager, game, cmd, result);

                // Phase 6: Diplomacy - Tribe
                case "declarewartribe":
                    return ExecuteDeclareWarTribe(clientManager, game, cmd, result);
                case "makepeacetribe":
                    return ExecuteMakePeaceTribe(clientManager, game, cmd, result);
                case "declaretrucetribe":
                    return ExecuteDeclareTruceTribe(clientManager, game, cmd, result);

                // Phase 6: Diplomacy - Gifts & Alliance
                case "giftcity":
                    return ExecuteGiftCity(clientManager, game, cmd, result);
                case "giftyield":
                    return ExecuteGiftYield(clientManager, game, cmd, result);
                case "allytribe":
                    return ExecuteAllyTribe(clientManager, game, cmd, result);

                // Phase 7: Character Management - Governor
                case "assigngovernor":
                    return ExecuteAssignGovernor(clientManager, game, cmd, result);
                case "releasegovernor":
                    return ExecuteReleaseGovernor(clientManager, game, cmd, result);

                // Phase 7: Character Management - General
                case "assigngeneral":
                    return ExecuteAssignGeneral(clientManager, game, cmd, result);
                case "releasegeneral":
                    return ExecuteReleaseGeneral(clientManager, game, cmd, result);

                // Phase 7: Character Management - Agent
                case "assignagent":
                    return ExecuteAssignAgent(clientManager, game, cmd, result);
                case "releaseagent":
                    return ExecuteReleaseAgent(clientManager, game, cmd, result);

                // Phase 7: Character Management - Mission
                case "startmission":
                    return ExecuteStartMission(clientManager, game, cmd, result);

                // Batch A: Laws & Economy
                case "chooselaw":
                    return ExecuteChooseLaw(clientManager, game, cmd, result);
                case "cancellaw":
                    return ExecuteCancelLaw(clientManager, game, cmd, result);
                case "buyyield":
                    return ExecuteBuyYield(clientManager, game, cmd, result);
                case "sellyield":
                    return ExecuteSellYield(clientManager, game, cmd, result);
                case "convertorders":
                    return ExecuteConvertOrders(clientManager, game, cmd, result);
                case "convertlegitimacy":
                    return ExecuteConvertLegitimacy(clientManager, game, cmd, result);
                case "convertorderstoscience":
                    return ExecuteConvertOrdersToScience(clientManager, game, cmd, result);

                // Batch B: Luxury Trading
                case "tradecityluxury":
                    return ExecuteTradeCityLuxury(clientManager, game, cmd, result);
                case "tradefamilyluxury":
                    return ExecuteTradeFamilyLuxury(clientManager, game, cmd, result);
                case "tradetribeluxury":
                    return ExecuteTradeTribeLuxury(clientManager, game, cmd, result);
                case "tradeplayerluxury":
                    return ExecuteTradePlayerLuxury(clientManager, game, cmd, result);
                case "tribute":
                    return ExecuteTribute(clientManager, game, cmd, result);

                // Batch C: Unit Special Actions
                case "swap":
                    return ExecuteSwap(clientManager, game, cmd, result);
                case "dounitqueue":
                    return ExecuteDoUnitQueue(clientManager, game, cmd, result);
                case "cancelunitqueue":
                    return ExecuteCancelUnitQueue(clientManager, game, cmd, result);
                case "formation":
                    return ExecuteFormation(clientManager, game, cmd, result);
                case "unlimber":
                    return ExecuteUnlimber(clientManager, game, cmd, result);
                case "anchor":
                    return ExecuteAnchor(clientManager, game, cmd, result);
                case "repair":
                    return ExecuteRepair(clientManager, game, cmd, result);
                case "cancelimprovement":
                    return ExecuteCancelImprovement(clientManager, game, cmd, result);
                case "removevegetation":
                    return ExecuteRemoveVegetation(clientManager, game, cmd, result);
                case "harvestresource":
                    return ExecuteHarvestResource(clientManager, game, cmd, result);
                case "unitautomate":
                    return ExecuteUnitAutomate(clientManager, game, cmd, result);
                case "addurban":
                    return ExecuteAddUrban(clientManager, game, cmd, result);
                case "roadto":
                    return ExecuteRoadTo(clientManager, game, cmd, result);
                case "buytile":
                    return ExecuteBuyTile(clientManager, game, cmd, result);
                case "recruitmercenary":
                    return ExecuteRecruitMercenary(clientManager, game, cmd, result);
                case "hiremercenary":
                    return ExecuteHireMercenary(clientManager, game, cmd, result);
                case "giftunit":
                    return ExecuteGiftUnit(clientManager, game, cmd, result);
                case "launchoffensive":
                    return ExecuteLaunchOffensive(clientManager, game, cmd, result);
                case "applyeffectunit":
                    return ExecuteApplyEffectUnit(clientManager, game, cmd, result);
                case "selectunit":
                    return ExecuteSelectUnit(clientManager, game, cmd, result);

                // Batch D: Agent & Caravan Units
                case "createagentnetwork":
                    return ExecuteCreateAgentNetwork(clientManager, game, cmd, result);
                case "createtradeoutpost":
                    return ExecuteCreateTradeOutpost(clientManager, game, cmd, result);
                case "caravanmissionstart":
                    return ExecuteCaravanMissionStart(clientManager, game, cmd, result);
                case "caravanmissioncancel":
                    return ExecuteCaravanMissionCancel(clientManager, game, cmd, result);

                // Batch E: Religious Units
                case "purgereligion":
                    return ExecutePurgeReligion(clientManager, game, cmd, result);
                case "spreadreligiontribe":
                    return ExecuteSpreadReligionTribe(clientManager, game, cmd, result);
                case "establishtheology":
                    return ExecuteEstablishTheology(clientManager, game, cmd, result);

                // Batch F: Character Management
                case "charactername":
                    return ExecuteCharacterName(clientManager, game, cmd, result);
                case "addcharactertrait":
                    return ExecuteAddCharacterTrait(clientManager, game, cmd, result);
                case "setcharacterrating":
                    return ExecuteSetCharacterRating(clientManager, game, cmd, result);
                case "setcharacterexperience":
                    return ExecuteSetCharacterExperience(clientManager, game, cmd, result);
                case "setcharactercognomen":
                    return ExecuteSetCharacterCognomen(clientManager, game, cmd, result);
                case "setcharacternation":
                    return ExecuteSetCharacterNation(clientManager, game, cmd, result);
                case "setcharacterfamily":
                    return ExecuteSetCharacterFamily(clientManager, game, cmd, result);
                case "setcharacterreligion":
                    return ExecuteSetCharacterReligion(clientManager, game, cmd, result);
                case "setcharactercourtier":
                    return ExecuteSetCharacterCourtier(clientManager, game, cmd, result);
                case "setcharactercouncil":
                    return ExecuteSetCharacterCouncil(clientManager, game, cmd, result);
                case "playerleader":
                    return ExecutePlayerLeader(clientManager, game, cmd, result);
                case "familyhead":
                    return ExecuteFamilyHead(clientManager, game, cmd, result);
                case "pincharacter":
                    return ExecutePinCharacter(clientManager, game, cmd, result);

                // Batch G: City Management
                case "cityrename":
                    return ExecuteCityRename(clientManager, game, cmd, result);
                case "cityautomate":
                    return ExecuteCityAutomate(clientManager, game, cmd, result);
                case "buildspecialist":
                    return ExecuteBuildSpecialist(clientManager, game, cmd, result);
                case "setspecialist":
                    return ExecuteSetSpecialist(clientManager, game, cmd, result);
                case "changecitizens":
                    return ExecuteChangeCitizens(clientManager, game, cmd, result);
                case "changereligion":
                    return ExecuteChangeReligion(clientManager, game, cmd, result);
                case "changefamily":
                    return ExecuteChangeFamily(clientManager, game, cmd, result);
                case "changefamilyseat":
                    return ExecuteChangeFamilySeat(clientManager, game, cmd, result);

                // Batch H: Goals & Communication
                case "abandonambition":
                    return ExecuteAbandonAmbition(clientManager, game, cmd, result);
                case "addplayergoal":
                    return ExecuteAddPlayerGoal(clientManager, game, cmd, result);
                case "removeplayergoal":
                    return ExecuteRemovePlayerGoal(clientManager, game, cmd, result);
                case "eventstory":
                    return ExecuteEventStory(clientManager, game, cmd, result);
                case "finishgoal":
                    return ExecuteFinishGoal(clientManager, game, cmd, result);
                case "chat":
                    return ExecuteChat(clientManager, game, cmd, result);
                case "ping":
                    return ExecutePing(clientManager, game, cmd, result);
                case "customreminder":
                    return ExecuteCustomReminder(clientManager, game, cmd, result);
                case "clearchat":
                    return ExecuteClearChat(clientManager, game, cmd, result);

                // Batch I: Game State & Turn
                case "extendtime":
                    return ExecuteExtendTime(clientManager, game, cmd, result);
                case "pause":
                    return ExecutePause(clientManager, game, cmd, result);
                case "undo":
                    return ExecuteUndo(clientManager, game, cmd, result);
                case "redo":
                    return ExecuteRedo(clientManager, game, cmd, result);
                case "replayturn":
                    return ExecuteReplayTurn(clientManager, game, cmd, result);
                case "aifinishturn":
                    return ExecuteAIFinishTurn(clientManager, game, cmd, result);
                case "togglenoreplay":
                    return ExecuteToggleNoReplay(clientManager, game, cmd, result);

                // Batch J: Diplomacy Extended
                case "teamalliance":
                    return ExecuteTeamAlliance(clientManager, game, cmd, result);
                case "tribeinvasion":
                    return ExecuteTribeInvasion(clientManager, game, cmd, result);
                case "victoryteam":
                    return ExecuteVictoryTeam(clientManager, game, cmd, result);

                // Batch K: Editor/Debug - Unit Creation & Management
                case "createunit":
                    return ExecuteCreateUnit(clientManager, game, cmd, result);
                case "unitname":
                    return ExecuteUnitName(clientManager, game, cmd, result);
                case "setunitfamily":
                    return ExecuteSetUnitFamily(clientManager, game, cmd, result);
                case "changeunitowner":
                    return ExecuteChangeUnitOwner(clientManager, game, cmd, result);
                case "changecooldown":
                    return ExecuteChangeCooldown(clientManager, game, cmd, result);
                case "changedamage":
                    return ExecuteChangeDamage(clientManager, game, cmd, result);
                case "unitincrementlevel":
                    return ExecuteUnitIncrementLevel(clientManager, game, cmd, result);
                case "unitchangepromotion":
                    return ExecuteUnitChangePromotion(clientManager, game, cmd, result);

                // Batch K: Editor/Debug - City Management
                case "createcity":
                    return ExecuteCreateCity(clientManager, game, cmd, result);
                case "removecity":
                    return ExecuteRemoveCity(clientManager, game, cmd, result);
                case "cityowner":
                    return ExecuteCityOwner(clientManager, game, cmd, result);
                case "changecitydamage":
                    return ExecuteChangeCityDamage(clientManager, game, cmd, result);
                case "changeculture":
                    return ExecuteChangeCulture(clientManager, game, cmd, result);
                case "changecitybuildturns":
                    return ExecuteChangeCityBuildTurns(clientManager, game, cmd, result);
                case "changecitydiscontentlevel":
                    return ExecuteChangeCityDiscontentLevel(clientManager, game, cmd, result);
                case "changeproject":
                    return ExecuteChangeProject(clientManager, game, cmd, result);

                // Batch K: Editor/Debug - Tile Manipulation
                case "setterrain":
                    return ExecuteSetTerrain(clientManager, game, cmd, result);
                case "setterrainheight":
                    return ExecuteSetTerrainHeight(clientManager, game, cmd, result);
                case "setvegetation":
                    return ExecuteSetVegetation(clientManager, game, cmd, result);
                case "setresource":
                    return ExecuteSetResource(clientManager, game, cmd, result);
                case "setroad":
                    return ExecuteSetRoad(clientManager, game, cmd, result);
                case "setimprovement":
                    return ExecuteSetImprovement(clientManager, game, cmd, result);
                case "settileowner":
                    return ExecuteSetTileOwner(clientManager, game, cmd, result);
                case "setcitysite":
                    return ExecuteSetCitySite(clientManager, game, cmd, result);
                case "improvementbuildturns":
                    return ExecuteImprovementBuildTurns(clientManager, game, cmd, result);

                // Batch K: Editor/Debug - Map & Player
                case "mapreveal":
                    return ExecuteMapReveal(clientManager, game, cmd, result);
                case "mapunreveal":
                    return ExecuteMapUnreveal(clientManager, game, cmd, result);
                case "addtech":
                    return ExecuteAddTech(clientManager, game, cmd, result);
                case "addyield":
                    return ExecuteAddYield(clientManager, game, cmd, result);
                case "addmoney":
                    return ExecuteAddMoney(clientManager, game, cmd, result);
                case "cheat":
                    return ExecuteCheat(clientManager, game, cmd, result);

                // Batch K: Editor/Debug - Character Management
                case "makecharacterdead":
                    return ExecuteMakeCharacterDead(clientManager, game, cmd, result);
                case "makecharactersafe":
                    return ExecuteMakeCharacterSafe(clientManager, game, cmd, result);
                case "newcharacter":
                    return ExecuteNewCharacter(clientManager, game, cmd, result);
                case "addcharacter":
                    return ExecuteAddCharacter(clientManager, game, cmd, result);
                case "tribeleader":
                    return ExecuteTribeLeader(clientManager, game, cmd, result);

                default:
                    result.Error = $"Unknown action: {cmd.Action}";
                    return result;
            }
        }

        #region Parameter Extraction Helpers

        private static int GetIntParam(GameCommand cmd, string key, int defaultValue = -1)
        {
            if (cmd.Params == null || !cmd.Params.TryGetValue(key, out var value))
                return defaultValue;

            if (value is int i) return i;
            if (value is long l) return (int)l;
            if (value is double d) return (int)d;
            if (int.TryParse(value?.ToString(), out int parsed)) return parsed;

            return defaultValue;
        }

        private static string GetStringParam(GameCommand cmd, string key, string defaultValue = null)
        {
            if (cmd.Params == null || !cmd.Params.TryGetValue(key, out var value))
                return defaultValue;

            return value?.ToString() ?? defaultValue;
        }

        private static bool GetBoolParam(GameCommand cmd, string key, bool defaultValue = false)
        {
            if (cmd.Params == null || !cmd.Params.TryGetValue(key, out var value))
                return defaultValue;

            if (value is bool b) return b;
            if (bool.TryParse(value?.ToString(), out bool parsed)) return parsed;

            return defaultValue;
        }

        /// <summary>
        /// Try to get an integer parameter with detailed parse result.
        /// Distinguishes between missing parameters and invalid types.
        /// </summary>
        private static bool TryGetIntParam(GameCommand cmd, string key, out ParseResult<int> result)
        {
            result = new ParseResult<int>();

            if (cmd.Params == null || !cmd.Params.TryGetValue(key, out var value))
            {
                result.Found = false;
                return false;
            }

            result.Found = true;
            result.RawValue = value?.ToString() ?? "null";

            if (value is int i)
            {
                result.Valid = true;
                result.Value = i;
                return true;
            }
            if (value is long l)
            {
                result.Valid = true;
                result.Value = (int)l;
                return true;
            }
            if (value is double d)
            {
                result.Valid = true;
                result.Value = (int)d;
                return true;
            }
            if (int.TryParse(value?.ToString(), out int parsed))
            {
                result.Valid = true;
                result.Value = parsed;
                return true;
            }

            Debug.LogWarning($"[CommandExecutor] Parse failed for '{key}': expected int, got '{result.RawValue}'");
            result.Valid = false;
            return false;
        }

        /// <summary>
        /// Try to get a string parameter with detailed parse result.
        /// </summary>
        private static bool TryGetStringParam(GameCommand cmd, string key, out ParseResult<string> result)
        {
            result = new ParseResult<string>();

            if (cmd.Params == null || !cmd.Params.TryGetValue(key, out var value))
            {
                result.Found = false;
                return false;
            }

            result.Found = true;
            result.RawValue = value?.ToString() ?? "null";
            result.Value = result.RawValue;
            result.Valid = !string.IsNullOrEmpty(result.Value);

            if (!result.Valid)
            {
                Debug.LogWarning($"[CommandExecutor] Parse failed for '{key}': expected non-empty string, got '{result.RawValue}'");
            }

            return result.Valid;
        }

        /// <summary>
        /// Generate an appropriate error message based on parse result.
        /// </summary>
        private static string GetParamError<T>(string key, ParseResult<T> result, string expectedType)
        {
            if (!result.Found)
                return $"Missing required parameter: {key}";
            return $"Invalid type for parameter '{key}': expected {expectedType}, got '{result.RawValue}'";
        }

        /// <summary>
        /// Resolve a unit type string (e.g., "UNIT_WARRIOR") to UnitType enum.
        /// </summary>
        private static UnitType ResolveUnitType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return UnitType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.unitsNum();

            for (int i = 0; i < count; i++)
            {
                var unitType = (UnitType)i;
                if (infos.unit(unitType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return unitType;
            }

            return UnitType.NONE;
        }

        /// <summary>
        /// Resolve a tech type string (e.g., "TECH_FORESTRY") to TechType enum.
        /// </summary>
        private static TechType ResolveTechType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return TechType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.techsNum();

            for (int i = 0; i < count; i++)
            {
                var techType = (TechType)i;
                if (infos.tech(techType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return techType;
            }

            return TechType.NONE;
        }

        /// <summary>
        /// Resolve a project type string (e.g., "PROJECT_TREASURE") to ProjectType enum.
        /// </summary>
        private static ProjectType ResolveProjectType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return ProjectType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.projectsNum();

            for (int i = 0; i < count; i++)
            {
                var projType = (ProjectType)i;
                if (infos.project(projType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return projType;
            }

            return ProjectType.NONE;
        }

        /// <summary>
        /// Resolve a promotion type string (e.g., "PROMOTION_FIERCE") to PromotionType enum.
        /// </summary>
        private static PromotionType ResolvePromotionType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return PromotionType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.promotionsNum();

            for (int i = 0; i < count; i++)
            {
                var promoType = (PromotionType)i;
                if (infos.promotion(promoType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return promoType;
            }

            return PromotionType.NONE;
        }

        /// <summary>
        /// Resolve a yield type string (e.g., "YIELD_CIVICS") to YieldType enum.
        /// </summary>
        private static YieldType ResolveYieldType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return YieldType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.yieldsNum();

            for (int i = 0; i < count; i++)
            {
                var yieldType = (YieldType)i;
                if (infos.yield(yieldType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return yieldType;
            }

            return YieldType.NONE;
        }

        /// <summary>
        /// Resolve an improvement type string (e.g., "IMPROVEMENT_FARM") to ImprovementType enum.
        /// </summary>
        private static ImprovementType ResolveImprovementType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return ImprovementType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.improvementsNum();

            for (int i = 0; i < count; i++)
            {
                var impType = (ImprovementType)i;
                if (infos.improvement(impType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return impType;
            }

            return ImprovementType.NONE;
        }

        /// <summary>
        /// Resolve a family type string (e.g., "FAMILY_ARTISANS") to FamilyType enum.
        /// </summary>
        private static FamilyType ResolveFamilyType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return FamilyType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.familiesNum();

            for (int i = 0; i < count; i++)
            {
                var famType = (FamilyType)i;
                if (infos.family(famType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return famType;
            }

            return FamilyType.NONE;
        }

        /// <summary>
        /// Resolve a nation type string (e.g., "NATION_ROME") to NationType enum.
        /// </summary>
        private static NationType ResolveNationType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return NationType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.nationsNum();

            for (int i = 0; i < count; i++)
            {
                var natType = (NationType)i;
                if (infos.nation(natType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return natType;
            }

            return NationType.NONE;
        }

        /// <summary>
        /// Resolve a tribe type string (e.g., "TRIBE_GAULS") to TribeType enum.
        /// </summary>
        private static TribeType ResolveTribeType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return TribeType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.tribesNum();

            for (int i = 0; i < count; i++)
            {
                var tribeType = (TribeType)i;
                if (infos.tribe(tribeType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return tribeType;
            }

            return TribeType.NONE;
        }

        /// <summary>
        /// Resolve a mission type string (e.g., "MISSION_NETWORK") to MissionType enum.
        /// </summary>
        private static MissionType ResolveMissionType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return MissionType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.missionsNum();

            for (int i = 0; i < count; i++)
            {
                var misType = (MissionType)i;
                if (infos.mission(misType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return misType;
            }

            return MissionType.NONE;
        }

        /// <summary>
        /// Resolve a law type string (e.g., "LAW_SLAVERY") to LawType enum.
        /// </summary>
        private static LawType ResolveLawType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return LawType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.lawsNum();

            for (int i = 0; i < count; i++)
            {
                var lawType = (LawType)i;
                if (infos.law(lawType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return lawType;
            }

            return LawType.NONE;
        }

        /// <summary>
        /// Resolve a resource type string (e.g., "RESOURCE_IRON") to ResourceType enum.
        /// </summary>
        private static ResourceType ResolveResourceType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return ResourceType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.resourcesNum();

            for (int i = 0; i < count; i++)
            {
                var resType = (ResourceType)i;
                if (infos.resource(resType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return resType;
            }

            return ResourceType.NONE;
        }

        /// <summary>
        /// Resolve an effect unit type string (e.g., "EFFECTUNIT_TESTUDO") to EffectUnitType enum.
        /// </summary>
        private static EffectUnitType ResolveEffectUnitType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return EffectUnitType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.effectUnitsNum();

            for (int i = 0; i < count; i++)
            {
                var effectType = (EffectUnitType)i;
                if (infos.effectUnit(effectType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return effectType;
            }

            return EffectUnitType.NONE;
        }

        /// <summary>
        /// Resolve a religion type string (e.g., "RELIGION_ZOROASTRIANISM") to ReligionType enum.
        /// </summary>
        private static ReligionType ResolveReligionType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return ReligionType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.religionsNum();

            for (int i = 0; i < count; i++)
            {
                var relType = (ReligionType)i;
                if (infos.religion(relType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return relType;
            }

            return ReligionType.NONE;
        }

        /// <summary>
        /// Resolve a theology type string (e.g., "THEOLOGY_CLERGY") to TheologyType enum.
        /// </summary>
        private static TheologyType ResolveTheologyType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return TheologyType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.theologiesNum();

            for (int i = 0; i < count; i++)
            {
                var theoType = (TheologyType)i;
                if (infos.theology(theoType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return theoType;
            }

            return TheologyType.NONE;
        }

        /// <summary>
        /// Resolve a trait type string (e.g., "TRAIT_WARRIOR") to TraitType enum.
        /// </summary>
        private static TraitType ResolveTraitType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return TraitType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.traitsNum();

            for (int i = 0; i < count; i++)
            {
                var traitType = (TraitType)i;
                if (infos.trait(traitType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return traitType;
            }

            return TraitType.NONE;
        }

        /// <summary>
        /// Resolve a rating type string (e.g., "RATING_COURAGE") to RatingType enum.
        /// </summary>
        private static RatingType ResolveRatingType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return RatingType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.ratingsNum();

            for (int i = 0; i < count; i++)
            {
                var ratingType = (RatingType)i;
                if (infos.rating(ratingType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return ratingType;
            }

            return RatingType.NONE;
        }

        /// <summary>
        /// Resolve a cognomen type string (e.g., "COGNOMEN_THE_GREAT") to CognomenType enum.
        /// </summary>
        private static CognomenType ResolveCognomenType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return CognomenType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.cognomensNum();

            for (int i = 0; i < count; i++)
            {
                var cogType = (CognomenType)i;
                if (infos.cognomen(cogType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return cogType;
            }

            return CognomenType.NONE;
        }

        /// <summary>
        /// Resolve a council type string (e.g., "COUNCIL_CHANCELLOR") to CouncilType enum.
        /// </summary>
        private static CouncilType ResolveCouncilType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return CouncilType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.councilsNum();

            for (int i = 0; i < count; i++)
            {
                var councilType = (CouncilType)i;
                if (infos.council(councilType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return councilType;
            }

            return CouncilType.NONE;
        }

        /// <summary>
        /// Resolve a courtier type string (e.g., "COURTIER_ARCHITECT") to CourtierType enum.
        /// </summary>
        private static CourtierType ResolveCourtierType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return CourtierType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.courtiersNum();

            for (int i = 0; i < count; i++)
            {
                var courtierType = (CourtierType)i;
                if (infos.courtier(courtierType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return courtierType;
            }

            return CourtierType.NONE;
        }

        /// <summary>
        /// Resolve a specialist type string (e.g., "SPECIALIST_SAGE") to SpecialistType enum.
        /// </summary>
        private static SpecialistType ResolveSpecialistType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return SpecialistType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.specialistsNum();

            for (int i = 0; i < count; i++)
            {
                var specType = (SpecialistType)i;
                if (infos.specialist(specType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return specType;
            }

            return SpecialistType.NONE;
        }

        /// <summary>
        /// Resolve a goal type string (e.g., "GOAL_BUILD_WONDER") to GoalType enum.
        /// </summary>
        private static GoalType ResolveGoalType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return GoalType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.goalsNum();

            for (int i = 0; i < count; i++)
            {
                var goalType = (GoalType)i;
                if (infos.goal(goalType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return goalType;
            }

            return GoalType.NONE;
        }

        /// <summary>
        /// Resolve an event story type string (e.g., "EVENTSTORY_MIGRATION") to EventStoryType enum.
        /// </summary>
        private static EventStoryType ResolveEventStoryType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return EventStoryType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.eventStoriesNum();

            for (int i = 0; i < count; i++)
            {
                var eventType = (EventStoryType)i;
                if (infos.eventStory(eventType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return eventType;
            }

            return EventStoryType.NONE;
        }

        /// <summary>
        /// Resolve a ping type string (e.g., "PING_LOOK") to PingType enum.
        /// </summary>
        private static PingType ResolvePingType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return PingType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.pingsNum();

            for (int i = 0; i < count; i++)
            {
                var pingType = (PingType)i;
                if (infos.ping(pingType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return pingType;
            }

            return PingType.NONE;
        }

        /// <summary>
        /// Resolve a victory type string (e.g., "VICTORY_AMBITION") to VictoryType enum.
        /// </summary>
        private static VictoryType ResolveVictoryType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return VictoryType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.victoriesNum();

            for (int i = 0; i < count; i++)
            {
                var vicType = (VictoryType)i;
                if (infos.victory(vicType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return vicType;
            }

            return VictoryType.NONE;
        }

        /// <summary>
        /// Resolve a terrain type string (e.g., "TERRAIN_PLAINS") to TerrainType enum.
        /// </summary>
        private static TerrainType ResolveTerrainType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return TerrainType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.terrainsNum();

            for (int i = 0; i < count; i++)
            {
                var terrainType = (TerrainType)i;
                if (infos.terrain(terrainType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return terrainType;
            }

            return TerrainType.NONE;
        }

        /// <summary>
        /// Resolve a height type string (e.g., "HEIGHT_MOUNTAIN") to HeightType enum.
        /// </summary>
        private static HeightType ResolveHeightType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return HeightType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.heightsNum();

            for (int i = 0; i < count; i++)
            {
                var heightType = (HeightType)i;
                if (infos.height(heightType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return heightType;
            }

            return HeightType.NONE;
        }

        /// <summary>
        /// Resolve a vegetation type string (e.g., "VEGETATION_FOREST") to VegetationType enum.
        /// </summary>
        private static VegetationType ResolveVegetationType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return VegetationType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.vegetationNum();

            for (int i = 0; i < count; i++)
            {
                var vegType = (VegetationType)i;
                if (infos.vegetation(vegType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return vegType;
            }

            return VegetationType.NONE;
        }

        /// <summary>
        /// Resolve a hotkey type string (e.g., "HOTKEY_CHEAT_MONEY") to HotkeyType enum.
        /// </summary>
        private static HotkeyType ResolveHotkeyType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return HotkeyType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.hotkeysNum();

            for (int i = 0; i < count; i++)
            {
                var hotkeyType = (HotkeyType)i;
                if (infos.hotkey(hotkeyType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return hotkeyType;
            }

            return HotkeyType.NONE;
        }

        /// <summary>
        /// Resolve a character type string (e.g., "CHARACTER_RULER") to CharacterType enum.
        /// </summary>
        private static CharacterType ResolveCharacterType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return CharacterType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.charactersNum();

            for (int i = 0; i < count; i++)
            {
                var charType = (CharacterType)i;
                if (infos.character(charType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return charType;
            }

            return CharacterType.NONE;
        }

        /// <summary>
        /// Resolve a city site type string (e.g., "ACTIVE") to CitySiteType enum.
        /// CitySiteType is a simple enum, not Info-based.
        /// </summary>
        private static CitySiteType ResolveCitySiteType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr)) return CitySiteType.NONE;

            // Try direct enum parse (case-insensitive)
            if (Enum.TryParse<CitySiteType>(typeStr, true, out var siteType))
                return siteType;

            // Also try with CITYSITE_ prefix stripped if present
            if (typeStr.StartsWith("CITYSITE_", StringComparison.OrdinalIgnoreCase))
            {
                string stripped = typeStr.Substring(9);
                if (Enum.TryParse<CitySiteType>(stripped, true, out siteType))
                    return siteType;
            }

            return CitySiteType.NONE;
        }

        #endregion

        #region Command Implementations

        private static CommandResult ExecuteMoveUnit(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "targetTileId", out var targetTileIdResult))
            {
                result.Error = GetParamError("targetTileId", targetTileIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;
            int targetTileId = targetTileIdResult.Value;
            bool queueMove = GetBoolParam(cmd, "queue", false);
            bool marchMove = GetBoolParam(cmd, "march", false);
            int waypointTileId = GetIntParam(cmd, "waypointTileId", -1);

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            if (_sendUnitMoveMethod == null)
            {
                result.Error = "Move command not available";
                return result;
            }

            try
            {
                // Get Unit and Tile objects from game
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }

                Tile targetTile = game.tile(targetTileId);
                if (targetTile == null)
                {
                    result.Error = $"Tile not found: {targetTileId}";
                    return result;
                }

                // Optional waypoint tile
                Tile waypointTile = waypointTileId >= 0 ? game.tile(waypointTileId) : null;

                // sendMoveUnit(Unit, Tile, Boolean march, Boolean queue, Tile waypoint)
                _sendUnitMoveMethod.Invoke(clientManager, new object[] { unit, targetTile, marchMove, queueMove, waypointTile });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Moved unit {unitId} to tile {targetTileId}{(waypointTile != null ? $" via waypoint {waypointTileId}" : "")}");
            }
            catch (Exception ex)
            {
                result.Error = $"Move failed: {ex.InnerException?.Message ?? ex.Message}";
                Debug.LogError($"[APIEndpoint] Move error: {ex}");
            }

            return result;
        }

        private static CommandResult ExecuteAttack(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "targetTileId", out var targetTileIdResult))
            {
                result.Error = GetParamError("targetTileId", targetTileIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;
            int targetTileId = targetTileIdResult.Value;

            if (_sendUnitAttackMethod == null)
            {
                result.Error = "Attack command not available";
                return result;
            }

            try
            {
                // sendAttack(Unit, Tile)
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                Tile targetTile = game.tile(targetTileId);
                if (targetTile == null)
                {
                    result.Error = $"Tile not found: {targetTileId}";
                    return result;
                }
                _sendUnitAttackMethod.Invoke(clientManager, new object[] { unit, targetTile });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"Attack failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteFortify(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;

            if (_sendUnitFortifyMethod == null)
            {
                result.Error = "Fortify command not available";
                return result;
            }

            try
            {
                // sendFortify(Unit)
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                _sendUnitFortifyMethod.Invoke(clientManager, new object[] { unit });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"Fortify failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecutePass(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;

            if (_sendUnitPassMethod == null)
            {
                result.Error = "Pass command not available";
                return result;
            }

            try
            {
                // sendPass(Unit)
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                _sendUnitPassMethod.Invoke(clientManager, new object[] { unit });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"Pass failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteSleep(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;

            if (_sendUnitSleepMethod == null)
            {
                result.Error = "Sleep command not available";
                return result;
            }

            try
            {
                // sendSleep(Unit)
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                _sendUnitSleepMethod.Invoke(clientManager, new object[] { unit });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"Sleep failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteSentry(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;

            if (_sendUnitSentryMethod == null)
            {
                result.Error = "Sentry command not available";
                return result;
            }

            try
            {
                // sendSentry(Unit)
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                _sendUnitSentryMethod.Invoke(clientManager, new object[] { unit });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"Sentry failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteWake(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;

            if (_sendUnitWakeMethod == null)
            {
                result.Error = "Wake command not available";
                return result;
            }

            try
            {
                // sendWake(Unit)
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                _sendUnitWakeMethod.Invoke(clientManager, new object[] { unit });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"Wake failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteDisband(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;
            bool force = GetBoolParam(cmd, "force", false);

            if (_sendUnitDisbandMethod == null)
            {
                result.Error = "Disband command not available";
                return result;
            }

            try
            {
                // sendDisband(Unit, Boolean)
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                _sendUnitDisbandMethod.Invoke(clientManager, new object[] { unit, force });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"Disband failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecutePromote(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "promotion", out var promotionResult))
            {
                result.Error = GetParamError("promotion", promotionResult, "string");
                return result;
            }

            int unitId = unitIdResult.Value;
            string promotionStr = promotionResult.Value;

            PromotionType promotionType = ResolvePromotionType(game, promotionStr);
            if (promotionType == PromotionType.NONE)
            {
                result.Error = $"Unknown promotion type: {promotionStr}";
                return result;
            }

            if (_sendUnitPromoteMethod == null)
            {
                result.Error = "Promote command not available";
                return result;
            }

            try
            {
                // sendPromote(Unit, PromotionType)
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                _sendUnitPromoteMethod.Invoke(clientManager, new object[] { unit, promotionType });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"Promote failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        #region Phase 1: Unit Commands

        private static CommandResult ExecuteHeal(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;
            bool auto = GetBoolParam(cmd, "auto", false);

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            if (_sendHealMethod == null)
            {
                result.Error = "Heal command not available";
                return result;
            }

            try
            {
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                _sendHealMethod.Invoke(clientManager, new object[] { unit, auto });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"Heal failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteMarch(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            if (_sendMarchMethod == null)
            {
                result.Error = "March command not available";
                return result;
            }

            try
            {
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                _sendMarchMethod.Invoke(clientManager, new object[] { unit });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"March failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteLock(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            if (_sendLockMethod == null)
            {
                result.Error = "Lock command not available";
                return result;
            }

            try
            {
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                _sendLockMethod.Invoke(clientManager, new object[] { unit });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"Lock failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecutePillage(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            if (_sendPillageMethod == null)
            {
                result.Error = "Pillage command not available";
                return result;
            }

            try
            {
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                _sendPillageMethod.Invoke(clientManager, new object[] { unit });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"Pillage failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteBurn(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            if (_sendBurnMethod == null)
            {
                result.Error = "Burn command not available";
                return result;
            }

            try
            {
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                _sendBurnMethod.Invoke(clientManager, new object[] { unit });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"Burn failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteUpgrade(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "unitType", out var unitTypeResult))
            {
                result.Error = GetParamError("unitType", unitTypeResult, "string");
                return result;
            }

            int unitId = unitIdResult.Value;
            string unitTypeStr = unitTypeResult.Value;
            bool buyGoods = GetBoolParam(cmd, "buyGoods", false);

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            UnitType unitType = ResolveUnitType(game, unitTypeStr);
            if (unitType == UnitType.NONE)
            {
                result.Error = $"Unknown unit type: {unitTypeStr}";
                return result;
            }

            if (_sendUpgradeMethod == null)
            {
                result.Error = "Upgrade command not available";
                return result;
            }

            try
            {
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                _sendUpgradeMethod.Invoke(clientManager, new object[] { unit, unitType, buyGoods });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"Upgrade failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteSpreadReligion(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;
            int cityId = cityIdResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            if (_sendSpreadReligionMethod == null)
            {
                result.Error = "SpreadReligion command not available";
                return result;
            }

            try
            {
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                _sendSpreadReligionMethod.Invoke(clientManager, new object[] { unit, cityId });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"SpreadReligion failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        #endregion

        #region Phase 2: Worker Commands

        private static CommandResult ExecuteBuildImprovement(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "improvementType", out var improvementTypeResult))
            {
                result.Error = GetParamError("improvementType", improvementTypeResult, "string");
                return result;
            }

            if (!TryGetIntParam(cmd, "tileId", out var tileIdResult))
            {
                result.Error = GetParamError("tileId", tileIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;
            string improvementTypeStr = improvementTypeResult.Value;
            int tileId = tileIdResult.Value;
            bool buyGoods = GetBoolParam(cmd, "buyGoods", false);
            bool queue = GetBoolParam(cmd, "queue", false);

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            ImprovementType improvementType = ResolveImprovementType(game, improvementTypeStr);
            if (improvementType == ImprovementType.NONE)
            {
                result.Error = $"Unknown improvement type: {improvementTypeStr}";
                return result;
            }

            if (_sendBuildImprovementMethod == null)
            {
                result.Error = "BuildImprovement command not available";
                return result;
            }

            try
            {
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }

                Tile tile = game.tile(tileId);
                if (tile == null)
                {
                    result.Error = $"Tile not found: {tileId}";
                    return result;
                }

                _sendBuildImprovementMethod.Invoke(clientManager, new object[] { unit, improvementType, buyGoods, queue, tile });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"BuildImprovement failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteUpgradeImprovement(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;
            bool buyGoods = GetBoolParam(cmd, "buyGoods", false);

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            if (_sendUpgradeImprovementMethod == null)
            {
                result.Error = "UpgradeImprovement command not available";
                return result;
            }

            try
            {
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                _sendUpgradeImprovementMethod.Invoke(clientManager, new object[] { unit, buyGoods });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"UpgradeImprovement failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteAddRoad(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "tileId", out var tileIdResult))
            {
                result.Error = GetParamError("tileId", tileIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;
            int tileId = tileIdResult.Value;
            bool buyGoods = GetBoolParam(cmd, "buyGoods", false);
            bool queue = GetBoolParam(cmd, "queue", false);

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            if (_sendAddRoadMethod == null)
            {
                result.Error = "AddRoad command not available";
                return result;
            }

            try
            {
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }

                Tile tile = game.tile(tileId);
                if (tile == null)
                {
                    result.Error = $"Tile not found: {tileId}";
                    return result;
                }

                _sendAddRoadMethod.Invoke(clientManager, new object[] { unit, buyGoods, queue, tile });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"AddRoad failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        #endregion

        #region Phase 3: City Foundation Commands

        private static CommandResult ExecuteFoundCity(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "familyType", out var familyTypeResult))
            {
                result.Error = GetParamError("familyType", familyTypeResult, "string");
                return result;
            }

            int unitId = unitIdResult.Value;
            string familyTypeStr = familyTypeResult.Value;
            string nationTypeStr = GetStringParam(cmd, "nationType", null);

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            FamilyType familyType = ResolveFamilyType(game, familyTypeStr);
            if (familyType == FamilyType.NONE)
            {
                result.Error = $"Unknown family type: {familyTypeStr}";
                return result;
            }

            NationType nationType = NationType.NONE;
            if (!string.IsNullOrEmpty(nationTypeStr))
            {
                nationType = ResolveNationType(game, nationTypeStr);
                if (nationType == NationType.NONE)
                {
                    result.Error = $"Unknown nation type: {nationTypeStr}";
                    return result;
                }
            }

            if (_sendFoundCityMethod == null)
            {
                result.Error = "FoundCity command not available";
                return result;
            }

            try
            {
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                _sendFoundCityMethod.Invoke(clientManager, new object[] { unit, familyType, nationType });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"FoundCity failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteJoinCity(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            if (_sendJoinCityMethod == null)
            {
                result.Error = "JoinCity command not available";
                return result;
            }

            try
            {
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                _sendJoinCityMethod.Invoke(clientManager, new object[] { unit });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"JoinCity failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        #endregion

        #region Phase 4: City Production Commands

        private static CommandResult ExecuteBuildQueue(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "oldSlot", out var oldSlotResult))
            {
                result.Error = GetParamError("oldSlot", oldSlotResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "newSlot", out var newSlotResult))
            {
                result.Error = GetParamError("newSlot", newSlotResult, "integer");
                return result;
            }

            int cityId = cityIdResult.Value;
            int oldSlot = oldSlotResult.Value;
            int newSlot = newSlotResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            City city = game.city(cityId);
            if (city == null)
            {
                result.Error = $"City not found: {cityId}";
                return result;
            }

            if (_sendBuildQueueMethod == null)
            {
                result.Error = "BuildQueue command not available";
                return result;
            }

            try
            {
                _sendBuildQueueMethod.Invoke(clientManager, new object[] { city, oldSlot, newSlot });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"BuildQueue failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        #endregion

        #region Phase 5: Research & Decisions Commands

        private static CommandResult ExecuteRedrawTech(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (_sendRedrawTechMethod == null)
            {
                result.Error = "RedrawTech command not available";
                return result;
            }

            try
            {
                _sendRedrawTechMethod.Invoke(clientManager, null);
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"RedrawTech failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteTargetTech(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetStringParam(cmd, "techType", out var techTypeResult))
            {
                result.Error = GetParamError("techType", techTypeResult, "string");
                return result;
            }

            string techTypeStr = techTypeResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            TechType techType = ResolveTechType(game, techTypeStr);
            if (techType == TechType.NONE)
            {
                result.Error = $"Unknown tech type: {techTypeStr}";
                return result;
            }

            if (_sendTargetTechMethod == null)
            {
                result.Error = "TargetTech command not available";
                return result;
            }

            try
            {
                _sendTargetTechMethod.Invoke(clientManager, new object[] { techType });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"TargetTech failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteMakeDecision(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "decisionId", out var decisionIdResult))
            {
                result.Error = GetParamError("decisionId", decisionIdResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "choiceIndex", out var choiceIndexResult))
            {
                result.Error = GetParamError("choiceIndex", choiceIndexResult, "integer");
                return result;
            }

            int decisionId = decisionIdResult.Value;
            int choiceIndex = choiceIndexResult.Value;
            int data = GetIntParam(cmd, "data", 0);

            if (_sendMakeDecisionMethod == null)
            {
                result.Error = "MakeDecision command not available";
                return result;
            }

            try
            {
                _sendMakeDecisionMethod.Invoke(clientManager, new object[] { decisionId, choiceIndex, data });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"MakeDecision failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteRemoveDecision(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "decisionId", out var decisionIdResult))
            {
                result.Error = GetParamError("decisionId", decisionIdResult, "integer");
                return result;
            }

            int decisionId = decisionIdResult.Value;

            if (_sendRemoveDecisionMethod == null)
            {
                result.Error = "RemoveDecision command not available";
                return result;
            }

            try
            {
                _sendRemoveDecisionMethod.Invoke(clientManager, new object[] { decisionId });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"RemoveDecision failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        #endregion

        #region Phase 6: Diplomacy Commands

        private static CommandResult ExecuteDeclareWar(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "targetPlayer", out var targetPlayerResult))
            {
                result.Error = GetParamError("targetPlayer", targetPlayerResult, "integer");
                return result;
            }

            int targetPlayer = targetPlayerResult.Value;

            if (_sendDiplomacyPlayerMethod == null || _getActivePlayerMethod == null)
            {
                result.Error = "DeclareWar command not available";
                return result;
            }

            try
            {
                var activePlayer = _getActivePlayerMethod.Invoke(clientManager, null);
                // ActionType.DIPLOMACY_HOSTILE = 0 based on game patterns
                _sendDiplomacyPlayerMethod.Invoke(clientManager, new object[] { activePlayer, (PlayerType)targetPlayer, ActionType.DIPLOMACY_HOSTILE });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"DeclareWar failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteMakePeace(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "targetPlayer", out var targetPlayerResult))
            {
                result.Error = GetParamError("targetPlayer", targetPlayerResult, "integer");
                return result;
            }

            int targetPlayer = targetPlayerResult.Value;

            if (_sendDiplomacyPlayerMethod == null || _getActivePlayerMethod == null)
            {
                result.Error = "MakePeace command not available";
                return result;
            }

            try
            {
                var activePlayer = _getActivePlayerMethod.Invoke(clientManager, null);
                _sendDiplomacyPlayerMethod.Invoke(clientManager, new object[] { activePlayer, (PlayerType)targetPlayer, ActionType.DIPLOMACY_PEACE });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"MakePeace failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteDeclareTruce(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "targetPlayer", out var targetPlayerResult))
            {
                result.Error = GetParamError("targetPlayer", targetPlayerResult, "integer");
                return result;
            }

            int targetPlayer = targetPlayerResult.Value;

            if (_sendDiplomacyPlayerMethod == null || _getActivePlayerMethod == null)
            {
                result.Error = "DeclareTruce command not available";
                return result;
            }

            try
            {
                var activePlayer = _getActivePlayerMethod.Invoke(clientManager, null);
                _sendDiplomacyPlayerMethod.Invoke(clientManager, new object[] { activePlayer, (PlayerType)targetPlayer, ActionType.DIPLOMACY_TRUCE });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"DeclareTruce failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteDeclareWarTribe(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetStringParam(cmd, "tribeType", out var tribeTypeResult))
            {
                result.Error = GetParamError("tribeType", tribeTypeResult, "string");
                return result;
            }

            string tribeTypeStr = tribeTypeResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            TribeType tribeType = ResolveTribeType(game, tribeTypeStr);
            if (tribeType == TribeType.NONE)
            {
                result.Error = $"Unknown tribe type: {tribeTypeStr}";
                return result;
            }

            if (_sendDiplomacyTribeMethod == null || _getActivePlayerMethod == null)
            {
                result.Error = "DeclareWarTribe command not available";
                return result;
            }

            try
            {
                var activePlayer = _getActivePlayerMethod.Invoke(clientManager, null);
                _sendDiplomacyTribeMethod.Invoke(clientManager, new object[] { tribeType, activePlayer, ActionType.DIPLOMACY_HOSTILE_TRIBE });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"DeclareWarTribe failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteMakePeaceTribe(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetStringParam(cmd, "tribeType", out var tribeTypeResult))
            {
                result.Error = GetParamError("tribeType", tribeTypeResult, "string");
                return result;
            }

            string tribeTypeStr = tribeTypeResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            TribeType tribeType = ResolveTribeType(game, tribeTypeStr);
            if (tribeType == TribeType.NONE)
            {
                result.Error = $"Unknown tribe type: {tribeTypeStr}";
                return result;
            }

            if (_sendDiplomacyTribeMethod == null || _getActivePlayerMethod == null)
            {
                result.Error = "MakePeaceTribe command not available";
                return result;
            }

            try
            {
                var activePlayer = _getActivePlayerMethod.Invoke(clientManager, null);
                _sendDiplomacyTribeMethod.Invoke(clientManager, new object[] { tribeType, activePlayer, ActionType.DIPLOMACY_PEACE_TRIBE });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"MakePeaceTribe failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteDeclareTruceTribe(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetStringParam(cmd, "tribeType", out var tribeTypeResult))
            {
                result.Error = GetParamError("tribeType", tribeTypeResult, "string");
                return result;
            }

            string tribeTypeStr = tribeTypeResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            TribeType tribeType = ResolveTribeType(game, tribeTypeStr);
            if (tribeType == TribeType.NONE)
            {
                result.Error = $"Unknown tribe type: {tribeTypeStr}";
                return result;
            }

            if (_sendDiplomacyTribeMethod == null || _getActivePlayerMethod == null)
            {
                result.Error = "DeclareTruceTribe command not available";
                return result;
            }

            try
            {
                var activePlayer = _getActivePlayerMethod.Invoke(clientManager, null);
                _sendDiplomacyTribeMethod.Invoke(clientManager, new object[] { tribeType, activePlayer, ActionType.DIPLOMACY_TRUCE_TRIBE });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"DeclareTruceTribe failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteGiftCity(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "targetPlayer", out var targetPlayerResult))
            {
                result.Error = GetParamError("targetPlayer", targetPlayerResult, "integer");
                return result;
            }

            int cityId = cityIdResult.Value;
            int targetPlayer = targetPlayerResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            City city = game.city(cityId);
            if (city == null)
            {
                result.Error = $"City not found: {cityId}";
                return result;
            }

            if (_sendGiftCityMethod == null)
            {
                result.Error = "GiftCity command not available";
                return result;
            }

            try
            {
                _sendGiftCityMethod.Invoke(clientManager, new object[] { city, (PlayerType)targetPlayer });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"GiftCity failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteGiftYield(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetStringParam(cmd, "yieldType", out var yieldTypeResult))
            {
                result.Error = GetParamError("yieldType", yieldTypeResult, "string");
                return result;
            }

            if (!TryGetIntParam(cmd, "targetPlayer", out var targetPlayerResult))
            {
                result.Error = GetParamError("targetPlayer", targetPlayerResult, "integer");
                return result;
            }

            string yieldTypeStr = yieldTypeResult.Value;
            int targetPlayer = targetPlayerResult.Value;
            bool reverse = GetBoolParam(cmd, "reverse", false);

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            YieldType yieldType = ResolveYieldType(game, yieldTypeStr);
            if (yieldType == YieldType.NONE)
            {
                result.Error = $"Unknown yield type: {yieldTypeStr}";
                return result;
            }

            if (_sendGiftYieldMethod == null)
            {
                result.Error = "GiftYield command not available";
                return result;
            }

            try
            {
                _sendGiftYieldMethod.Invoke(clientManager, new object[] { yieldType, (PlayerType)targetPlayer, reverse });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"GiftYield failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteAllyTribe(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetStringParam(cmd, "tribeType", out var tribeTypeResult))
            {
                result.Error = GetParamError("tribeType", tribeTypeResult, "string");
                return result;
            }

            string tribeTypeStr = tribeTypeResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            TribeType tribeType = ResolveTribeType(game, tribeTypeStr);
            if (tribeType == TribeType.NONE)
            {
                result.Error = $"Unknown tribe type: {tribeTypeStr}";
                return result;
            }

            if (_sendAllyTribeMethod == null || _getActivePlayerMethod == null)
            {
                result.Error = "AllyTribe command not available";
                return result;
            }

            try
            {
                var activePlayer = _getActivePlayerMethod.Invoke(clientManager, null);
                _sendAllyTribeMethod.Invoke(clientManager, new object[] { tribeType, activePlayer });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"AllyTribe failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        #endregion

        #region Phase 7: Character Management Commands

        private static CommandResult ExecuteAssignGovernor(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "characterId", out var characterIdResult))
            {
                result.Error = GetParamError("characterId", characterIdResult, "integer");
                return result;
            }

            int cityId = cityIdResult.Value;
            int characterId = characterIdResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            City city = game.city(cityId);
            if (city == null)
            {
                result.Error = $"City not found: {cityId}";
                return result;
            }

            Character character = game.character(characterId);
            if (character == null)
            {
                result.Error = $"Character not found: {characterId}";
                return result;
            }

            if (_sendMakeGovernorMethod == null)
            {
                result.Error = "AssignGovernor command not available";
                return result;
            }

            try
            {
                _sendMakeGovernorMethod.Invoke(clientManager, new object[] { city, character });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"AssignGovernor failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteReleaseGovernor(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            int cityId = cityIdResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            City city = game.city(cityId);
            if (city == null)
            {
                result.Error = $"City not found: {cityId}";
                return result;
            }

            if (_sendReleaseGovernorMethod == null)
            {
                result.Error = "ReleaseGovernor command not available";
                return result;
            }

            try
            {
                _sendReleaseGovernorMethod.Invoke(clientManager, new object[] { city });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"ReleaseGovernor failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteAssignGeneral(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "characterId", out var characterIdResult))
            {
                result.Error = GetParamError("characterId", characterIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;
            int characterId = characterIdResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            Unit unit = game.unit(unitId);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitId}";
                return result;
            }

            Character character = game.character(characterId);
            if (character == null)
            {
                result.Error = $"Character not found: {characterId}";
                return result;
            }

            if (_sendMakeUnitCharacterMethod == null)
            {
                result.Error = "AssignGeneral command not available";
                return result;
            }

            try
            {
                // sendMakeUnitCharacter(Unit, Character, bool bGeneral) - true for general
                _sendMakeUnitCharacterMethod.Invoke(clientManager, new object[] { unit, character, true });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"AssignGeneral failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteReleaseGeneral(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            Unit unit = game.unit(unitId);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitId}";
                return result;
            }

            if (_sendReleaseUnitCharacterMethod == null)
            {
                result.Error = "ReleaseGeneral command not available";
                return result;
            }

            try
            {
                _sendReleaseUnitCharacterMethod.Invoke(clientManager, new object[] { unit });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"ReleaseGeneral failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteAssignAgent(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "characterId", out var characterIdResult))
            {
                result.Error = GetParamError("characterId", characterIdResult, "integer");
                return result;
            }

            int cityId = cityIdResult.Value;
            int characterId = characterIdResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            City city = game.city(cityId);
            if (city == null)
            {
                result.Error = $"City not found: {cityId}";
                return result;
            }

            Character character = game.character(characterId);
            if (character == null)
            {
                result.Error = $"Character not found: {characterId}";
                return result;
            }

            if (_sendMakeAgentMethod == null)
            {
                result.Error = "AssignAgent command not available";
                return result;
            }

            try
            {
                _sendMakeAgentMethod.Invoke(clientManager, new object[] { city, character });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"AssignAgent failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteReleaseAgent(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            int cityId = cityIdResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            City city = game.city(cityId);
            if (city == null)
            {
                result.Error = $"City not found: {cityId}";
                return result;
            }

            if (_sendReleaseAgentMethod == null)
            {
                result.Error = "ReleaseAgent command not available";
                return result;
            }

            try
            {
                _sendReleaseAgentMethod.Invoke(clientManager, new object[] { city });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"ReleaseAgent failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteStartMission(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetStringParam(cmd, "missionType", out var missionTypeResult))
            {
                result.Error = GetParamError("missionType", missionTypeResult, "string");
                return result;
            }

            if (!TryGetIntParam(cmd, "characterId", out var characterIdResult))
            {
                result.Error = GetParamError("characterId", characterIdResult, "integer");
                return result;
            }

            string missionTypeStr = missionTypeResult.Value;
            int characterId = characterIdResult.Value;
            string target = GetStringParam(cmd, "target", "");
            bool cancel = GetBoolParam(cmd, "cancel", false);

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            MissionType missionType = ResolveMissionType(game, missionTypeStr);
            if (missionType == MissionType.NONE)
            {
                result.Error = $"Unknown mission type: {missionTypeStr}";
                return result;
            }

            if (_sendStartMissionMethod == null)
            {
                result.Error = "StartMission command not available";
                return result;
            }

            try
            {
                _sendStartMissionMethod.Invoke(clientManager, new object[] { missionType, characterId, target, cancel });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"StartMission failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        #endregion

        private static CommandResult ExecuteBuildUnit(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "unitType", out var unitTypeResult))
            {
                result.Error = GetParamError("unitType", unitTypeResult, "string");
                return result;
            }

            int cityId = cityIdResult.Value;
            string unitTypeStr = unitTypeResult.Value;
            bool buyGoods = GetBoolParam(cmd, "buyGoods", false);
            bool first = GetBoolParam(cmd, "first", false);

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            // Resolve unit type
            UnitType unitType = ResolveUnitType(game, unitTypeStr);
            if (unitType == UnitType.NONE)
            {
                result.Error = $"Unknown unit type: {unitTypeStr}";
                return result;
            }

            // Get the city object
            City city = game.city(cityId);
            if (city == null)
            {
                result.Error = $"City not found: {cityId}";
                return result;
            }

            if (_sendCityBuildUnitMethod == null)
            {
                result.Error = "BuildUnit command not available";
                return result;
            }

            try
            {
                // sendBuildUnit(City, UnitType, Boolean buyGoods, Tile rallyTile, Boolean first)
                _sendCityBuildUnitMethod.Invoke(clientManager, new object[] { city, unitType, buyGoods, city.tile(), first });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Building unit {unitTypeStr} in city {cityId}");
            }
            catch (Exception ex)
            {
                result.Error = $"BuildUnit failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteBuildProject(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "projectType", out var projectTypeResult))
            {
                result.Error = GetParamError("projectType", projectTypeResult, "string");
                return result;
            }

            int cityId = cityIdResult.Value;
            string projectTypeStr = projectTypeResult.Value;
            bool buyGoods = GetBoolParam(cmd, "buyGoods", false);
            bool first = GetBoolParam(cmd, "first", false);
            bool repeat = GetBoolParam(cmd, "repeat", false);

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            // Resolve project type
            ProjectType projectType = ResolveProjectType(game, projectTypeStr);
            if (projectType == ProjectType.NONE)
            {
                result.Error = $"Unknown project type: {projectTypeStr}";
                return result;
            }

            // Get the city object
            City city = game.city(cityId);
            if (city == null)
            {
                result.Error = $"City not found: {cityId}";
                return result;
            }

            if (_sendCityBuildProjectMethod == null)
            {
                result.Error = "BuildProject command not available";
                return result;
            }

            try
            {
                // sendBuildProject(City, ProjectType, Boolean buyGoods, Boolean first, Boolean repeat)
                _sendCityBuildProjectMethod.Invoke(clientManager, new object[] { city, projectType, buyGoods, first, repeat });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Building project {projectTypeStr} in city {cityId}");
            }
            catch (Exception ex)
            {
                result.Error = $"BuildProject failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteHurryCivics(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            int cityId = cityIdResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            City city = game.city(cityId);
            if (city == null)
            {
                result.Error = $"City not found: {cityId}";
                return result;
            }

            if (_sendHurryCivicsMethod == null)
            {
                result.Error = "HurryCivics command not available";
                return result;
            }

            try
            {
                // sendHurryCivics(City)
                _sendHurryCivicsMethod.Invoke(clientManager, new object[] { city });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Hurried production with civics in city {cityId}");
            }
            catch (Exception ex)
            {
                result.Error = $"HurryCivics failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteHurryTraining(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            int cityId = cityIdResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            City city = game.city(cityId);
            if (city == null)
            {
                result.Error = $"City not found: {cityId}";
                return result;
            }

            if (_sendHurryTrainingMethod == null)
            {
                result.Error = "HurryTraining command not available";
                return result;
            }

            try
            {
                // sendHurryTraining(City)
                _sendHurryTrainingMethod.Invoke(clientManager, new object[] { city });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Hurried production with training in city {cityId}");
            }
            catch (Exception ex)
            {
                result.Error = $"HurryTraining failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteHurryMoney(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            int cityId = cityIdResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            City city = game.city(cityId);
            if (city == null)
            {
                result.Error = $"City not found: {cityId}";
                return result;
            }

            if (_sendHurryMoneyMethod == null)
            {
                result.Error = "HurryMoney command not available";
                return result;
            }

            try
            {
                // sendHurryMoney(City)
                _sendHurryMoneyMethod.Invoke(clientManager, new object[] { city });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Hurried production with money in city {cityId}");
            }
            catch (Exception ex)
            {
                result.Error = $"HurryMoney failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteHurryPopulation(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            int cityId = cityIdResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            City city = game.city(cityId);
            if (city == null)
            {
                result.Error = $"City not found: {cityId}";
                return result;
            }

            if (_sendHurryPopulationMethod == null)
            {
                result.Error = "HurryPopulation command not available";
                return result;
            }

            try
            {
                // sendHurryPopulation(City)
                _sendHurryPopulationMethod.Invoke(clientManager, new object[] { city });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Hurried production with population in city {cityId}");
            }
            catch (Exception ex)
            {
                result.Error = $"HurryPopulation failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteHurryOrders(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            int cityId = cityIdResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            City city = game.city(cityId);
            if (city == null)
            {
                result.Error = $"City not found: {cityId}";
                return result;
            }

            if (_sendHurryOrdersMethod == null)
            {
                result.Error = "HurryOrders command not available";
                return result;
            }

            try
            {
                // sendHurryOrders(City)
                _sendHurryOrdersMethod.Invoke(clientManager, new object[] { city });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Hurried production with orders in city {cityId}");
            }
            catch (Exception ex)
            {
                result.Error = $"HurryOrders failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteResearch(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetStringParam(cmd, "tech", out var techResult))
            {
                result.Error = GetParamError("tech", techResult, "string");
                return result;
            }

            string techStr = techResult.Value;

            TechType techType = ResolveTechType(game, techStr);
            if (techType == TechType.NONE)
            {
                result.Error = $"Unknown tech type: {techStr}";
                return result;
            }

            if (_sendResearchMethod == null)
            {
                result.Error = "Research command not available";
                return result;
            }

            try
            {
                _sendResearchMethod.Invoke(clientManager, new object[] { techType });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"Research failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteEndTurn(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (_sendEndTurnMethod == null)
            {
                result.Error = "EndTurn command not available";
                return result;
            }

            try
            {
                // sendEndTurn(int iTurn, bool bForce) - iTurn is Game.getTurn(), not player index!
                int turn = game != null ? game.getTurn() : 0;
                bool force = GetBoolParam(cmd, "force", true); // default to true to actually end turn
                _sendEndTurnMethod.Invoke(clientManager, new object[] { turn, force });
                result.Success = true;
                Debug.Log($"[APIEndpoint] End turn {turn} (force={force})");
            }
            catch (Exception ex)
            {
                result.Error = $"EndTurn failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        #region Batch A: Laws & Economy

        private static CommandResult ExecuteChooseLaw(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetStringParam(cmd, "lawType", out var lawTypeResult))
            {
                result.Error = GetParamError("lawType", lawTypeResult, "string");
                return result;
            }

            LawType lawType = ResolveLawType(game, lawTypeResult.Value);
            if (lawType == LawType.NONE)
            {
                result.Error = $"Unknown law type: {lawTypeResult.Value}";
                return result;
            }

            if (_sendChooseLawMethod == null)
            {
                result.Error = "ChooseLaw command not available";
                return result;
            }

            try
            {
                _sendChooseLawMethod.Invoke(clientManager, new object[] { lawType });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Chose law: {lawTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"ChooseLaw failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteCancelLaw(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetStringParam(cmd, "lawType", out var lawTypeResult))
            {
                result.Error = GetParamError("lawType", lawTypeResult, "string");
                return result;
            }

            LawType lawType = ResolveLawType(game, lawTypeResult.Value);
            if (lawType == LawType.NONE)
            {
                result.Error = $"Unknown law type: {lawTypeResult.Value}";
                return result;
            }

            if (_sendCancelLawMethod == null)
            {
                result.Error = "CancelLaw command not available";
                return result;
            }

            try
            {
                _sendCancelLawMethod.Invoke(clientManager, new object[] { lawType });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Cancelled law: {lawTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"CancelLaw failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteBuyYield(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetStringParam(cmd, "yieldType", out var yieldTypeResult))
            {
                result.Error = GetParamError("yieldType", yieldTypeResult, "string");
                return result;
            }

            if (!TryGetIntParam(cmd, "amount", out var amountResult))
            {
                result.Error = GetParamError("amount", amountResult, "integer");
                return result;
            }

            YieldType yieldType = ResolveYieldType(game, yieldTypeResult.Value);
            if (yieldType == YieldType.NONE)
            {
                result.Error = $"Unknown yield type: {yieldTypeResult.Value}";
                return result;
            }

            if (_sendBuyYieldMethod == null)
            {
                result.Error = "BuyYield command not available";
                return result;
            }

            try
            {
                _sendBuyYieldMethod.Invoke(clientManager, new object[] { yieldType, amountResult.Value });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Bought {amountResult.Value} of {yieldTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"BuyYield failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteSellYield(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetStringParam(cmd, "yieldType", out var yieldTypeResult))
            {
                result.Error = GetParamError("yieldType", yieldTypeResult, "string");
                return result;
            }

            if (!TryGetIntParam(cmd, "amount", out var amountResult))
            {
                result.Error = GetParamError("amount", amountResult, "integer");
                return result;
            }

            YieldType yieldType = ResolveYieldType(game, yieldTypeResult.Value);
            if (yieldType == YieldType.NONE)
            {
                result.Error = $"Unknown yield type: {yieldTypeResult.Value}";
                return result;
            }

            if (_sendSellYieldMethod == null)
            {
                result.Error = "SellYield command not available";
                return result;
            }

            try
            {
                _sendSellYieldMethod.Invoke(clientManager, new object[] { yieldType, amountResult.Value });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Sold {amountResult.Value} of {yieldTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"SellYield failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteConvertOrders(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (_sendConvertOrdersMethod == null)
            {
                result.Error = "ConvertOrders command not available";
                return result;
            }

            try
            {
                _sendConvertOrdersMethod.Invoke(clientManager, null);
                result.Success = true;
                Debug.Log($"[APIEndpoint] Converted orders to civics");
            }
            catch (Exception ex)
            {
                result.Error = $"ConvertOrders failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteConvertLegitimacy(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (_sendConvertLegitimacyMethod == null)
            {
                result.Error = "ConvertLegitimacy command not available";
                return result;
            }

            try
            {
                _sendConvertLegitimacyMethod.Invoke(clientManager, null);
                result.Success = true;
                Debug.Log($"[APIEndpoint] Converted legitimacy");
            }
            catch (Exception ex)
            {
                result.Error = $"ConvertLegitimacy failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteConvertOrdersToScience(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (_sendConvertOrdersToScienceMethod == null)
            {
                result.Error = "ConvertOrdersToScience command not available";
                return result;
            }

            try
            {
                _sendConvertOrdersToScienceMethod.Invoke(clientManager, null);
                result.Success = true;
                Debug.Log($"[APIEndpoint] Converted orders to science");
            }
            catch (Exception ex)
            {
                result.Error = $"ConvertOrdersToScience failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        #endregion

        #region Batch B: Luxury Trading

        private static CommandResult ExecuteTradeCityLuxury(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "resourceType", out var resourceTypeResult))
            {
                result.Error = GetParamError("resourceType", resourceTypeResult, "string");
                return result;
            }

            bool enable = GetBoolParam(cmd, "enable", true);

            ResourceType resourceType = ResolveResourceType(game, resourceTypeResult.Value);
            if (resourceType == ResourceType.NONE)
            {
                result.Error = $"Unknown resource type: {resourceTypeResult.Value}";
                return result;
            }

            City city = game.city(cityIdResult.Value);
            if (city == null)
            {
                result.Error = $"City not found: {cityIdResult.Value}";
                return result;
            }

            if (_sendTradeCityLuxuryMethod == null)
            {
                result.Error = "TradeCityLuxury command not available";
                return result;
            }

            try
            {
                _sendTradeCityLuxuryMethod.Invoke(clientManager, new object[] { city, resourceType, enable });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Trade city luxury: city={cityIdResult.Value}, resource={resourceTypeResult.Value}, enable={enable}");
            }
            catch (Exception ex)
            {
                result.Error = $"TradeCityLuxury failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteTradeFamilyLuxury(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetStringParam(cmd, "familyType", out var familyTypeResult))
            {
                result.Error = GetParamError("familyType", familyTypeResult, "string");
                return result;
            }

            if (!TryGetStringParam(cmd, "resourceType", out var resourceTypeResult))
            {
                result.Error = GetParamError("resourceType", resourceTypeResult, "string");
                return result;
            }

            bool enable = GetBoolParam(cmd, "enable", true);

            FamilyType familyType = ResolveFamilyType(game, familyTypeResult.Value);
            if (familyType == FamilyType.NONE)
            {
                result.Error = $"Unknown family type: {familyTypeResult.Value}";
                return result;
            }

            ResourceType resourceType = ResolveResourceType(game, resourceTypeResult.Value);
            if (resourceType == ResourceType.NONE)
            {
                result.Error = $"Unknown resource type: {resourceTypeResult.Value}";
                return result;
            }

            if (_sendTradeFamilyLuxuryMethod == null)
            {
                result.Error = "TradeFamilyLuxury command not available";
                return result;
            }

            try
            {
                _sendTradeFamilyLuxuryMethod.Invoke(clientManager, new object[] { familyType, resourceType, enable });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Trade family luxury: family={familyTypeResult.Value}, resource={resourceTypeResult.Value}, enable={enable}");
            }
            catch (Exception ex)
            {
                result.Error = $"TradeFamilyLuxury failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteTradeTribeLuxury(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetStringParam(cmd, "tribeType", out var tribeTypeResult))
            {
                result.Error = GetParamError("tribeType", tribeTypeResult, "string");
                return result;
            }

            if (!TryGetStringParam(cmd, "resourceType", out var resourceTypeResult))
            {
                result.Error = GetParamError("resourceType", resourceTypeResult, "string");
                return result;
            }

            bool enable = GetBoolParam(cmd, "enable", true);

            TribeType tribeType = ResolveTribeType(game, tribeTypeResult.Value);
            if (tribeType == TribeType.NONE)
            {
                result.Error = $"Unknown tribe type: {tribeTypeResult.Value}";
                return result;
            }

            ResourceType resourceType = ResolveResourceType(game, resourceTypeResult.Value);
            if (resourceType == ResourceType.NONE)
            {
                result.Error = $"Unknown resource type: {resourceTypeResult.Value}";
                return result;
            }

            if (_sendTradeTribeLuxuryMethod == null)
            {
                result.Error = "TradeTribeLuxury command not available";
                return result;
            }

            try
            {
                _sendTradeTribeLuxuryMethod.Invoke(clientManager, new object[] { tribeType, resourceType, enable });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Trade tribe luxury: tribe={tribeTypeResult.Value}, resource={resourceTypeResult.Value}, enable={enable}");
            }
            catch (Exception ex)
            {
                result.Error = $"TradeTribeLuxury failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteTradePlayerLuxury(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "targetPlayer", out var targetPlayerResult))
            {
                result.Error = GetParamError("targetPlayer", targetPlayerResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "resourceType", out var resourceTypeResult))
            {
                result.Error = GetParamError("resourceType", resourceTypeResult, "string");
                return result;
            }

            bool enable = GetBoolParam(cmd, "enable", true);

            ResourceType resourceType = ResolveResourceType(game, resourceTypeResult.Value);
            if (resourceType == ResourceType.NONE)
            {
                result.Error = $"Unknown resource type: {resourceTypeResult.Value}";
                return result;
            }

            if (_sendTradePlayerLuxuryMethod == null)
            {
                result.Error = "TradePlayerLuxury command not available";
                return result;
            }

            try
            {
                PlayerType targetPlayer = (PlayerType)targetPlayerResult.Value;
                _sendTradePlayerLuxuryMethod.Invoke(clientManager, new object[] { targetPlayer, resourceType, enable });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Trade player luxury: player={targetPlayerResult.Value}, resource={resourceTypeResult.Value}, enable={enable}");
            }
            catch (Exception ex)
            {
                result.Error = $"TradePlayerLuxury failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteTribute(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetStringParam(cmd, "yieldType", out var yieldTypeResult))
            {
                result.Error = GetParamError("yieldType", yieldTypeResult, "string");
                return result;
            }

            if (!TryGetIntParam(cmd, "amount", out var amountResult))
            {
                result.Error = GetParamError("amount", amountResult, "integer");
                return result;
            }

            // At least one of toPlayer or toTribe must be specified
            int toPlayerIndex = GetIntParam(cmd, "toPlayer", -1);
            string toTribeStr = GetStringParam(cmd, "toTribe", null);
            int fromPlayerIndex = GetIntParam(cmd, "fromPlayer", -1);

            YieldType yieldType = ResolveYieldType(game, yieldTypeResult.Value);
            if (yieldType == YieldType.NONE)
            {
                result.Error = $"Unknown yield type: {yieldTypeResult.Value}";
                return result;
            }

            TribeType toTribe = TribeType.NONE;
            if (!string.IsNullOrEmpty(toTribeStr))
            {
                toTribe = ResolveTribeType(game, toTribeStr);
                if (toTribe == TribeType.NONE)
                {
                    result.Error = $"Unknown tribe type: {toTribeStr}";
                    return result;
                }
            }

            if (_sendTributeMethod == null)
            {
                result.Error = "Tribute command not available";
                return result;
            }

            try
            {
                PlayerType toPlayer = toPlayerIndex >= 0 ? (PlayerType)toPlayerIndex : PlayerType.NONE;
                PlayerType fromPlayer = fromPlayerIndex >= 0 ? (PlayerType)fromPlayerIndex : PlayerType.NONE;
                _sendTributeMethod.Invoke(clientManager, new object[] { toPlayer, toTribe, yieldType, amountResult.Value, fromPlayer });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Tribute: {amountResult.Value} {yieldTypeResult.Value} to player={toPlayerIndex}/tribe={toTribeStr}");
            }
            catch (Exception ex)
            {
                result.Error = $"Tribute failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        // Batch C: Unit Special Actions

        private static CommandResult ExecuteSwap(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "targetTileId", out var targetTileIdResult))
            {
                result.Error = GetParamError("targetTileId", targetTileIdResult, "integer");
                return result;
            }

            bool forceMarch = GetBoolParam(cmd, "forceMarch", false);

            Unit unit = game.unit(unitIdResult.Value);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitIdResult.Value}";
                return result;
            }

            Tile targetTile = game.tile(targetTileIdResult.Value);
            if (targetTile == null)
            {
                result.Error = $"Tile not found: {targetTileIdResult.Value}";
                return result;
            }

            if (_sendSwapMethod == null)
            {
                result.Error = "Swap command not available";
                return result;
            }

            try
            {
                _sendSwapMethod.Invoke(clientManager, new object[] { unit, targetTile, forceMarch });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Swap unit {unitIdResult.Value} to tile {targetTileIdResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"Swap failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteDoUnitQueue(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            Unit unit = game.unit(unitIdResult.Value);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitIdResult.Value}";
                return result;
            }

            if (_sendDoUnitQueueMethod == null)
            {
                result.Error = "DoUnitQueue command not available";
                return result;
            }

            try
            {
                _sendDoUnitQueueMethod.Invoke(clientManager, new object[] { unit });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Do unit queue for unit {unitIdResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"DoUnitQueue failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteCancelUnitQueue(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            bool clearAll = GetBoolParam(cmd, "clearAll", false);

            Unit unit = game.unit(unitIdResult.Value);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitIdResult.Value}";
                return result;
            }

            if (_sendCancelUnitQueueMethod == null)
            {
                result.Error = "CancelUnitQueue command not available";
                return result;
            }

            try
            {
                _sendCancelUnitQueueMethod.Invoke(clientManager, new object[] { unit, !clearAll });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Cancel unit queue for unit {unitIdResult.Value}, clearAll={clearAll}");
            }
            catch (Exception ex)
            {
                result.Error = $"CancelUnitQueue failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteFormation(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "effectUnitType", out var effectTypeResult))
            {
                result.Error = GetParamError("effectUnitType", effectTypeResult, "string");
                return result;
            }

            Unit unit = game.unit(unitIdResult.Value);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitIdResult.Value}";
                return result;
            }

            EffectUnitType effectType = ResolveEffectUnitType(game, effectTypeResult.Value);
            if (effectType == EffectUnitType.NONE)
            {
                result.Error = $"Unknown effect unit type: {effectTypeResult.Value}";
                return result;
            }

            if (_sendFormationMethod == null)
            {
                result.Error = "Formation command not available";
                return result;
            }

            try
            {
                _sendFormationMethod.Invoke(clientManager, new object[] { unit, effectType });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Set formation for unit {unitIdResult.Value} to {effectTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"Formation failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteUnlimber(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            Unit unit = game.unit(unitIdResult.Value);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitIdResult.Value}";
                return result;
            }

            if (_sendUnlimberMethod == null)
            {
                result.Error = "Unlimber command not available";
                return result;
            }

            try
            {
                _sendUnlimberMethod.Invoke(clientManager, new object[] { unit });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Unlimber unit {unitIdResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"Unlimber failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteAnchor(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            Unit unit = game.unit(unitIdResult.Value);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitIdResult.Value}";
                return result;
            }

            if (_sendAnchorMethod == null)
            {
                result.Error = "Anchor command not available";
                return result;
            }

            try
            {
                _sendAnchorMethod.Invoke(clientManager, new object[] { unit });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Anchor unit {unitIdResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"Anchor failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteRepair(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            bool buyGoods = GetBoolParam(cmd, "buyGoods", false);
            bool queue = GetBoolParam(cmd, "queue", false);
            int tileId = GetIntParam(cmd, "tileId", -1);

            Unit unit = game.unit(unitIdResult.Value);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitIdResult.Value}";
                return result;
            }

            Tile tile = tileId >= 0 ? game.tile(tileId) : null;

            if (_sendRepairMethod == null)
            {
                result.Error = "Repair command not available";
                return result;
            }

            try
            {
                _sendRepairMethod.Invoke(clientManager, new object[] { unit, buyGoods, queue, tile });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Repair unit {unitIdResult.Value}, buyGoods={buyGoods}, queue={queue}");
            }
            catch (Exception ex)
            {
                result.Error = $"Repair failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteCancelImprovement(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            Unit unit = game.unit(unitIdResult.Value);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitIdResult.Value}";
                return result;
            }

            if (_sendCancelImprovementMethod == null)
            {
                result.Error = "CancelImprovement command not available";
                return result;
            }

            try
            {
                _sendCancelImprovementMethod.Invoke(clientManager, new object[] { unit });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Cancel improvement for unit {unitIdResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"CancelImprovement failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteRemoveVegetation(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            Unit unit = game.unit(unitIdResult.Value);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitIdResult.Value}";
                return result;
            }

            if (_sendRemoveVegetationMethod == null)
            {
                result.Error = "RemoveVegetation command not available";
                return result;
            }

            try
            {
                _sendRemoveVegetationMethod.Invoke(clientManager, new object[] { unit });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Remove vegetation for unit {unitIdResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"RemoveVegetation failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteHarvestResource(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            bool autoHarvest = GetBoolParam(cmd, "autoHarvest", false);

            Unit unit = game.unit(unitIdResult.Value);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitIdResult.Value}";
                return result;
            }

            if (_sendHarvestResourceMethod == null)
            {
                result.Error = "HarvestResource command not available";
                return result;
            }

            try
            {
                _sendHarvestResourceMethod.Invoke(clientManager, new object[] { unit, autoHarvest });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Harvest resource for unit {unitIdResult.Value}, auto={autoHarvest}");
            }
            catch (Exception ex)
            {
                result.Error = $"HarvestResource failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteUnitAutomate(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            Unit unit = game.unit(unitIdResult.Value);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitIdResult.Value}";
                return result;
            }

            if (_sendUnitAutomateMethod == null)
            {
                result.Error = "UnitAutomate command not available";
                return result;
            }

            try
            {
                _sendUnitAutomateMethod.Invoke(clientManager, new object[] { unit });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Automate unit {unitIdResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"UnitAutomate failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteAddUrban(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            bool buyGoods = GetBoolParam(cmd, "buyGoods", false);

            Unit unit = game.unit(unitIdResult.Value);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitIdResult.Value}";
                return result;
            }

            if (_sendAddUrbanMethod == null)
            {
                result.Error = "AddUrban command not available";
                return result;
            }

            try
            {
                _sendAddUrbanMethod.Invoke(clientManager, new object[] { unit, buyGoods });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Add urban for unit {unitIdResult.Value}, buyGoods={buyGoods}");
            }
            catch (Exception ex)
            {
                result.Error = $"AddUrban failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteRoadTo(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            bool buyGoods = GetBoolParam(cmd, "buyGoods", false);

            Unit unit = game.unit(unitIdResult.Value);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitIdResult.Value}";
                return result;
            }

            // Get tileIds array
            int[] tileIds = null;
            if (cmd.Params != null && cmd.Params.TryGetValue("tileIds", out var tileIdsObj))
            {
                if (tileIdsObj is Newtonsoft.Json.Linq.JArray jArray)
                {
                    tileIds = jArray.ToObject<int[]>();
                }
                else if (tileIdsObj is int[] arr)
                {
                    tileIds = arr;
                }
            }

            if (tileIds == null || tileIds.Length == 0)
            {
                result.Error = "Missing or invalid parameter: tileIds (expected array of integers)";
                return result;
            }

            if (_sendRoadToMethod == null)
            {
                result.Error = "RoadTo command not available";
                return result;
            }

            try
            {
                _sendRoadToMethod.Invoke(clientManager, new object[] { unit, buyGoods, tileIds });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Road to for unit {unitIdResult.Value}, tiles={string.Join(",", tileIds)}");
            }
            catch (Exception ex)
            {
                result.Error = $"RoadTo failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteBuyTile(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "yieldType", out var yieldTypeResult))
            {
                result.Error = GetParamError("yieldType", yieldTypeResult, "string");
                return result;
            }

            Unit unit = game.unit(unitIdResult.Value);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitIdResult.Value}";
                return result;
            }

            YieldType yieldType = ResolveYieldType(game, yieldTypeResult.Value);
            if (yieldType == YieldType.NONE)
            {
                result.Error = $"Unknown yield type: {yieldTypeResult.Value}";
                return result;
            }

            if (_sendBuyTileMethod == null)
            {
                result.Error = "BuyTile command not available";
                return result;
            }

            try
            {
                _sendBuyTileMethod.Invoke(clientManager, new object[] { unit, cityIdResult.Value, yieldType });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Buy tile for unit {unitIdResult.Value}, city={cityIdResult.Value}, yield={yieldTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"BuyTile failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteRecruitMercenary(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            Unit unit = game.unit(unitIdResult.Value);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitIdResult.Value}";
                return result;
            }

            if (_sendRecruitMercenaryMethod == null)
            {
                result.Error = "RecruitMercenary command not available";
                return result;
            }

            try
            {
                _sendRecruitMercenaryMethod.Invoke(clientManager, new object[] { unit });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Recruit mercenary unit {unitIdResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"RecruitMercenary failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteHireMercenary(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            Unit unit = game.unit(unitIdResult.Value);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitIdResult.Value}";
                return result;
            }

            if (_sendHireMercenaryMethod == null)
            {
                result.Error = "HireMercenary command not available";
                return result;
            }

            try
            {
                _sendHireMercenaryMethod.Invoke(clientManager, new object[] { unit });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Hire mercenary unit {unitIdResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"HireMercenary failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteGiftUnit(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "targetPlayer", out var targetPlayerResult))
            {
                result.Error = GetParamError("targetPlayer", targetPlayerResult, "integer");
                return result;
            }

            Unit unit = game.unit(unitIdResult.Value);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitIdResult.Value}";
                return result;
            }

            if (_sendGiftUnitMethod == null)
            {
                result.Error = "GiftUnit command not available";
                return result;
            }

            try
            {
                PlayerType targetPlayer = (PlayerType)targetPlayerResult.Value;
                _sendGiftUnitMethod.Invoke(clientManager, new object[] { unit, targetPlayer });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Gift unit {unitIdResult.Value} to player {targetPlayerResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"GiftUnit failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteLaunchOffensive(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            Unit unit = game.unit(unitIdResult.Value);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitIdResult.Value}";
                return result;
            }

            if (_sendLaunchOffensiveMethod == null)
            {
                result.Error = "LaunchOffensive command not available";
                return result;
            }

            try
            {
                _sendLaunchOffensiveMethod.Invoke(clientManager, new object[] { unit });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Launch offensive for unit {unitIdResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"LaunchOffensive failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteApplyEffectUnit(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "effectUnitType", out var effectTypeResult))
            {
                result.Error = GetParamError("effectUnitType", effectTypeResult, "string");
                return result;
            }

            Unit unit = game.unit(unitIdResult.Value);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitIdResult.Value}";
                return result;
            }

            EffectUnitType effectType = ResolveEffectUnitType(game, effectTypeResult.Value);
            if (effectType == EffectUnitType.NONE)
            {
                result.Error = $"Unknown effect unit type: {effectTypeResult.Value}";
                return result;
            }

            if (_sendApplyEffectUnitMethod == null)
            {
                result.Error = "ApplyEffectUnit command not available";
                return result;
            }

            try
            {
                _sendApplyEffectUnitMethod.Invoke(clientManager, new object[] { unit, effectType });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Apply effect {effectTypeResult.Value} to unit {unitIdResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"ApplyEffectUnit failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteSelectUnit(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            Unit unit = game.unit(unitIdResult.Value);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitIdResult.Value}";
                return result;
            }

            if (_sendSelectUnitMethod == null)
            {
                result.Error = "SelectUnit command not available";
                return result;
            }

            try
            {
                _sendSelectUnitMethod.Invoke(clientManager, new object[] { unit });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Select unit {unitIdResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"SelectUnit failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        // Batch D: Agent & Caravan Units

        private static CommandResult ExecuteCreateAgentNetwork(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            Unit unit = game.unit(unitIdResult.Value);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitIdResult.Value}";
                return result;
            }

            if (_sendCreateAgentNetworkMethod == null)
            {
                result.Error = "CreateAgentNetwork command not available";
                return result;
            }

            try
            {
                _sendCreateAgentNetworkMethod.Invoke(clientManager, new object[] { unit, cityIdResult.Value });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Create agent network: unit={unitIdResult.Value}, city={cityIdResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"CreateAgentNetwork failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteCreateTradeOutpost(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "tileId", out var tileIdResult))
            {
                result.Error = GetParamError("tileId", tileIdResult, "integer");
                return result;
            }

            Unit unit = game.unit(unitIdResult.Value);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitIdResult.Value}";
                return result;
            }

            if (_sendCreateTradeOutpostMethod == null)
            {
                result.Error = "CreateTradeOutpost command not available";
                return result;
            }

            try
            {
                _sendCreateTradeOutpostMethod.Invoke(clientManager, new object[] { unit, tileIdResult.Value });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Create trade outpost: unit={unitIdResult.Value}, tile={tileIdResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"CreateTradeOutpost failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteCaravanMissionStart(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "targetPlayer", out var targetPlayerResult))
            {
                result.Error = GetParamError("targetPlayer", targetPlayerResult, "integer");
                return result;
            }

            Unit unit = game.unit(unitIdResult.Value);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitIdResult.Value}";
                return result;
            }

            if (_sendCaravanMissionStartMethod == null)
            {
                result.Error = "CaravanMissionStart command not available";
                return result;
            }

            try
            {
                PlayerType targetPlayer = (PlayerType)targetPlayerResult.Value;
                _sendCaravanMissionStartMethod.Invoke(clientManager, new object[] { unit, targetPlayer });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Caravan mission start: unit={unitIdResult.Value}, target={targetPlayerResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"CaravanMissionStart failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteCaravanMissionCancel(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            Unit unit = game.unit(unitIdResult.Value);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitIdResult.Value}";
                return result;
            }

            if (_sendCaravanMissionCancelMethod == null)
            {
                result.Error = "CaravanMissionCancel command not available";
                return result;
            }

            try
            {
                _sendCaravanMissionCancelMethod.Invoke(clientManager, new object[] { unit });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Caravan mission cancel: unit={unitIdResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"CaravanMissionCancel failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        // Batch E: Religious Units

        private static CommandResult ExecutePurgeReligion(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "religionType", out var religionTypeResult))
            {
                result.Error = GetParamError("religionType", religionTypeResult, "string");
                return result;
            }

            Unit unit = game.unit(unitIdResult.Value);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitIdResult.Value}";
                return result;
            }

            ReligionType religionType = ResolveReligionType(game, religionTypeResult.Value);
            if (religionType == ReligionType.NONE)
            {
                result.Error = $"Unknown religion type: {religionTypeResult.Value}";
                return result;
            }

            if (_sendPurgeReligionMethod == null)
            {
                result.Error = "PurgeReligion command not available";
                return result;
            }

            try
            {
                _sendPurgeReligionMethod.Invoke(clientManager, new object[] { unit, religionType });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Purge religion: unit={unitIdResult.Value}, religion={religionTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"PurgeReligion failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteSpreadReligionTribe(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "tribeType", out var tribeTypeResult))
            {
                result.Error = GetParamError("tribeType", tribeTypeResult, "string");
                return result;
            }

            Unit unit = game.unit(unitIdResult.Value);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitIdResult.Value}";
                return result;
            }

            TribeType tribeType = ResolveTribeType(game, tribeTypeResult.Value);
            if (tribeType == TribeType.NONE)
            {
                result.Error = $"Unknown tribe type: {tribeTypeResult.Value}";
                return result;
            }

            if (_sendSpreadReligionTribeMethod == null)
            {
                result.Error = "SpreadReligionTribe command not available";
                return result;
            }

            try
            {
                _sendSpreadReligionTribeMethod.Invoke(clientManager, new object[] { unit, tribeType });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Spread religion to tribe: unit={unitIdResult.Value}, tribe={tribeTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"SpreadReligionTribe failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteEstablishTheology(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "theologyType", out var theologyTypeResult))
            {
                result.Error = GetParamError("theologyType", theologyTypeResult, "string");
                return result;
            }

            Unit unit = game.unit(unitIdResult.Value);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitIdResult.Value}";
                return result;
            }

            TheologyType theologyType = ResolveTheologyType(game, theologyTypeResult.Value);
            if (theologyType == TheologyType.NONE)
            {
                result.Error = $"Unknown theology type: {theologyTypeResult.Value}";
                return result;
            }

            if (_sendEstablishTheologyMethod == null)
            {
                result.Error = "EstablishTheology command not available";
                return result;
            }

            try
            {
                _sendEstablishTheologyMethod.Invoke(clientManager, new object[] { unit, theologyType });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Establish theology: unit={unitIdResult.Value}, theology={theologyTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"EstablishTheology failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        // Batch F: Character Management

        private static CommandResult ExecuteCharacterName(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "characterId", out var charIdResult))
            {
                result.Error = GetParamError("characterId", charIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "name", out var nameResult))
            {
                result.Error = GetParamError("name", nameResult, "string");
                return result;
            }

            if (_sendCharacterNameMethod == null)
            {
                result.Error = "CharacterName command not available";
                return result;
            }

            try
            {
                _sendCharacterNameMethod.Invoke(clientManager, new object[] { charIdResult.Value, nameResult.Value });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Set character name: id={charIdResult.Value}, name={nameResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"CharacterName failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteAddCharacterTrait(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "characterId", out var charIdResult))
            {
                result.Error = GetParamError("characterId", charIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "traitType", out var traitTypeResult))
            {
                result.Error = GetParamError("traitType", traitTypeResult, "string");
                return result;
            }

            bool remove = GetBoolParam(cmd, "remove", false);

            TraitType traitType = ResolveTraitType(game, traitTypeResult.Value);
            if (traitType == TraitType.NONE)
            {
                result.Error = $"Unknown trait type: {traitTypeResult.Value}";
                return result;
            }

            if (_sendAddCharacterTraitMethod == null)
            {
                result.Error = "AddCharacterTrait command not available";
                return result;
            }

            try
            {
                _sendAddCharacterTraitMethod.Invoke(clientManager, new object[] { charIdResult.Value, traitType, remove });
                result.Success = true;
                Debug.Log($"[APIEndpoint] {(remove ? "Remove" : "Add")} character trait: id={charIdResult.Value}, trait={traitTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"AddCharacterTrait failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteSetCharacterRating(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "characterId", out var charIdResult))
            {
                result.Error = GetParamError("characterId", charIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "ratingType", out var ratingTypeResult))
            {
                result.Error = GetParamError("ratingType", ratingTypeResult, "string");
                return result;
            }

            if (!TryGetIntParam(cmd, "value", out var valueResult))
            {
                result.Error = GetParamError("value", valueResult, "integer");
                return result;
            }

            RatingType ratingType = ResolveRatingType(game, ratingTypeResult.Value);
            if (ratingType == RatingType.NONE)
            {
                result.Error = $"Unknown rating type: {ratingTypeResult.Value}";
                return result;
            }

            if (_sendSetCharacterRatingMethod == null)
            {
                result.Error = "SetCharacterRating command not available";
                return result;
            }

            try
            {
                _sendSetCharacterRatingMethod.Invoke(clientManager, new object[] { charIdResult.Value, ratingType, valueResult.Value });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Set character rating: id={charIdResult.Value}, rating={ratingTypeResult.Value}, value={valueResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"SetCharacterRating failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteSetCharacterExperience(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "characterId", out var charIdResult))
            {
                result.Error = GetParamError("characterId", charIdResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "xp", out var xpResult))
            {
                result.Error = GetParamError("xp", xpResult, "integer");
                return result;
            }

            if (_sendSetCharacterExperienceMethod == null)
            {
                result.Error = "SetCharacterExperience command not available";
                return result;
            }

            try
            {
                _sendSetCharacterExperienceMethod.Invoke(clientManager, new object[] { charIdResult.Value, xpResult.Value });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Set character experience: id={charIdResult.Value}, xp={xpResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"SetCharacterExperience failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteSetCharacterCognomen(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "characterId", out var charIdResult))
            {
                result.Error = GetParamError("characterId", charIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "cognomenType", out var cognomenTypeResult))
            {
                result.Error = GetParamError("cognomenType", cognomenTypeResult, "string");
                return result;
            }

            CognomenType cognomenType = ResolveCognomenType(game, cognomenTypeResult.Value);
            if (cognomenType == CognomenType.NONE)
            {
                result.Error = $"Unknown cognomen type: {cognomenTypeResult.Value}";
                return result;
            }

            if (_sendSetCharacterCognomenMethod == null)
            {
                result.Error = "SetCharacterCognomen command not available";
                return result;
            }

            try
            {
                _sendSetCharacterCognomenMethod.Invoke(clientManager, new object[] { charIdResult.Value, cognomenType });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Set character cognomen: id={charIdResult.Value}, cognomen={cognomenTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"SetCharacterCognomen failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteSetCharacterNation(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "characterId", out var charIdResult))
            {
                result.Error = GetParamError("characterId", charIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "nationType", out var nationTypeResult))
            {
                result.Error = GetParamError("nationType", nationTypeResult, "string");
                return result;
            }

            NationType nationType = ResolveNationType(game, nationTypeResult.Value);
            if (nationType == NationType.NONE)
            {
                result.Error = $"Unknown nation type: {nationTypeResult.Value}";
                return result;
            }

            if (_sendSetCharacterNationMethod == null)
            {
                result.Error = "SetCharacterNation command not available";
                return result;
            }

            try
            {
                _sendSetCharacterNationMethod.Invoke(clientManager, new object[] { charIdResult.Value, nationType });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Set character nation: id={charIdResult.Value}, nation={nationTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"SetCharacterNation failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteSetCharacterFamily(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "characterId", out var charIdResult))
            {
                result.Error = GetParamError("characterId", charIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "familyType", out var familyTypeResult))
            {
                result.Error = GetParamError("familyType", familyTypeResult, "string");
                return result;
            }

            FamilyType familyType = ResolveFamilyType(game, familyTypeResult.Value);
            if (familyType == FamilyType.NONE)
            {
                result.Error = $"Unknown family type: {familyTypeResult.Value}";
                return result;
            }

            if (_sendSetCharacterFamilyMethod == null)
            {
                result.Error = "SetCharacterFamily command not available";
                return result;
            }

            try
            {
                _sendSetCharacterFamilyMethod.Invoke(clientManager, new object[] { charIdResult.Value, familyType });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Set character family: id={charIdResult.Value}, family={familyTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"SetCharacterFamily failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteSetCharacterReligion(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "characterId", out var charIdResult))
            {
                result.Error = GetParamError("characterId", charIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "religionType", out var religionTypeResult))
            {
                result.Error = GetParamError("religionType", religionTypeResult, "string");
                return result;
            }

            ReligionType religionType = ResolveReligionType(game, religionTypeResult.Value);
            if (religionType == ReligionType.NONE)
            {
                result.Error = $"Unknown religion type: {religionTypeResult.Value}";
                return result;
            }

            if (_sendSetCharacterReligionMethod == null)
            {
                result.Error = "SetCharacterReligion command not available";
                return result;
            }

            try
            {
                _sendSetCharacterReligionMethod.Invoke(clientManager, new object[] { religionType, charIdResult.Value });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Set character religion: id={charIdResult.Value}, religion={religionTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"SetCharacterReligion failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteSetCharacterCourtier(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "characterId", out var charIdResult))
            {
                result.Error = GetParamError("characterId", charIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "courtierType", out var courtierTypeResult))
            {
                result.Error = GetParamError("courtierType", courtierTypeResult, "string");
                return result;
            }

            CourtierType courtierType = ResolveCourtierType(game, courtierTypeResult.Value);
            if (courtierType == CourtierType.NONE)
            {
                result.Error = $"Unknown courtier type: {courtierTypeResult.Value}";
                return result;
            }

            if (_sendSetCharacterCourtierMethod == null)
            {
                result.Error = "SetCharacterCourtier command not available";
                return result;
            }

            try
            {
                _sendSetCharacterCourtierMethod.Invoke(clientManager, new object[] { charIdResult.Value, courtierType });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Set character courtier: id={charIdResult.Value}, courtier={courtierTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"SetCharacterCourtier failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteSetCharacterCouncil(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "characterId", out var charIdResult))
            {
                result.Error = GetParamError("characterId", charIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "councilType", out var councilTypeResult))
            {
                result.Error = GetParamError("councilType", councilTypeResult, "string");
                return result;
            }

            CouncilType councilType = ResolveCouncilType(game, councilTypeResult.Value);
            if (councilType == CouncilType.NONE)
            {
                result.Error = $"Unknown council type: {councilTypeResult.Value}";
                return result;
            }

            if (_sendSetCharacterCouncilMethod == null)
            {
                result.Error = "SetCharacterCouncil command not available";
                return result;
            }

            try
            {
                _sendSetCharacterCouncilMethod.Invoke(clientManager, new object[] { charIdResult.Value, councilType });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Set character council: id={charIdResult.Value}, council={councilTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"SetCharacterCouncil failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecutePlayerLeader(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "playerType", out var playerTypeResult))
            {
                result.Error = GetParamError("playerType", playerTypeResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "characterId", out var charIdResult))
            {
                result.Error = GetParamError("characterId", charIdResult, "integer");
                return result;
            }

            if (_sendPlayerLeaderMethod == null)
            {
                result.Error = "PlayerLeader command not available";
                return result;
            }

            try
            {
                PlayerType playerType = (PlayerType)playerTypeResult.Value;
                _sendPlayerLeaderMethod.Invoke(clientManager, new object[] { playerType, charIdResult.Value });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Set player leader: player={playerTypeResult.Value}, character={charIdResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"PlayerLeader failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteFamilyHead(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "playerType", out var playerTypeResult))
            {
                result.Error = GetParamError("playerType", playerTypeResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "familyType", out var familyTypeResult))
            {
                result.Error = GetParamError("familyType", familyTypeResult, "string");
                return result;
            }

            if (!TryGetIntParam(cmd, "characterId", out var charIdResult))
            {
                result.Error = GetParamError("characterId", charIdResult, "integer");
                return result;
            }

            FamilyType familyType = ResolveFamilyType(game, familyTypeResult.Value);
            if (familyType == FamilyType.NONE)
            {
                result.Error = $"Unknown family type: {familyTypeResult.Value}";
                return result;
            }

            if (_sendFamilyHeadMethod == null)
            {
                result.Error = "FamilyHead command not available";
                return result;
            }

            try
            {
                PlayerType playerType = (PlayerType)playerTypeResult.Value;
                _sendFamilyHeadMethod.Invoke(clientManager, new object[] { playerType, familyType, charIdResult.Value });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Set family head: player={playerTypeResult.Value}, family={familyTypeResult.Value}, character={charIdResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"FamilyHead failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecutePinCharacter(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "characterId", out var charIdResult))
            {
                result.Error = GetParamError("characterId", charIdResult, "integer");
                return result;
            }

            if (_sendPinCharacterMethod == null)
            {
                result.Error = "PinCharacter command not available";
                return result;
            }

            try
            {
                _sendPinCharacterMethod.Invoke(clientManager, new object[] { charIdResult.Value });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Pin character: id={charIdResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"PinCharacter failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        // Batch G: City Management

        private static CommandResult ExecuteCityRename(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "name", out var nameResult))
            {
                result.Error = GetParamError("name", nameResult, "string");
                return result;
            }

            if (_sendCityRenameMethod == null)
            {
                result.Error = "CityRename command not available";
                return result;
            }

            try
            {
                _sendCityRenameMethod.Invoke(clientManager, new object[] { cityIdResult.Value, nameResult.Value });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Rename city: id={cityIdResult.Value}, name={nameResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"CityRename failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteCityAutomate(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            bool enable = GetBoolParam(cmd, "enable", true);

            City city = game.city(cityIdResult.Value);
            if (city == null)
            {
                result.Error = $"City not found: {cityIdResult.Value}";
                return result;
            }

            if (_sendCityAutomateMethod == null)
            {
                result.Error = "CityAutomate command not available";
                return result;
            }

            try
            {
                _sendCityAutomateMethod.Invoke(clientManager, new object[] { city, enable });
                result.Success = true;
                Debug.Log($"[APIEndpoint] City automate: id={cityIdResult.Value}, enable={enable}");
            }
            catch (Exception ex)
            {
                result.Error = $"CityAutomate failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteBuildSpecialist(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "tileId", out var tileIdResult))
            {
                result.Error = GetParamError("tileId", tileIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "specialistType", out var specialistTypeResult))
            {
                result.Error = GetParamError("specialistType", specialistTypeResult, "string");
                return result;
            }

            bool buyGoods = GetBoolParam(cmd, "buyGoods", false);
            bool addFirst = GetBoolParam(cmd, "addFirst", false);

            Tile tile = game.tile(tileIdResult.Value);
            if (tile == null)
            {
                result.Error = $"Tile not found: {tileIdResult.Value}";
                return result;
            }

            SpecialistType specialistType = ResolveSpecialistType(game, specialistTypeResult.Value);
            if (specialistType == SpecialistType.NONE)
            {
                result.Error = $"Unknown specialist type: {specialistTypeResult.Value}";
                return result;
            }

            if (_sendBuildSpecialistMethod == null)
            {
                result.Error = "BuildSpecialist command not available";
                return result;
            }

            try
            {
                _sendBuildSpecialistMethod.Invoke(clientManager, new object[] { tile, specialistType, buyGoods, addFirst });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Build specialist: tile={tileIdResult.Value}, specialist={specialistTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"BuildSpecialist failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteSetSpecialist(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "tileId", out var tileIdResult))
            {
                result.Error = GetParamError("tileId", tileIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "specialistType", out var specialistTypeResult))
            {
                result.Error = GetParamError("specialistType", specialistTypeResult, "string");
                return result;
            }

            Tile tile = game.tile(tileIdResult.Value);
            if (tile == null)
            {
                result.Error = $"Tile not found: {tileIdResult.Value}";
                return result;
            }

            SpecialistType specialistType = ResolveSpecialistType(game, specialistTypeResult.Value);
            if (specialistType == SpecialistType.NONE)
            {
                result.Error = $"Unknown specialist type: {specialistTypeResult.Value}";
                return result;
            }

            if (_sendSetSpecialistMethod == null)
            {
                result.Error = "SetSpecialist command not available";
                return result;
            }

            try
            {
                _sendSetSpecialistMethod.Invoke(clientManager, new object[] { tile, specialistType });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Set specialist: tile={tileIdResult.Value}, specialist={specialistTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"SetSpecialist failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteChangeCitizens(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "delta", out var deltaResult))
            {
                result.Error = GetParamError("delta", deltaResult, "integer");
                return result;
            }

            City city = game.city(cityIdResult.Value);
            if (city == null)
            {
                result.Error = $"City not found: {cityIdResult.Value}";
                return result;
            }

            if (_sendChangeCitizensMethod == null)
            {
                result.Error = "ChangeCitizens command not available";
                return result;
            }

            try
            {
                _sendChangeCitizensMethod.Invoke(clientManager, new object[] { city, deltaResult.Value });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Change citizens: city={cityIdResult.Value}, delta={deltaResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"ChangeCitizens failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteChangeReligion(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "religionType", out var religionTypeResult))
            {
                result.Error = GetParamError("religionType", religionTypeResult, "string");
                return result;
            }

            bool add = GetBoolParam(cmd, "add", true);

            City city = game.city(cityIdResult.Value);
            if (city == null)
            {
                result.Error = $"City not found: {cityIdResult.Value}";
                return result;
            }

            ReligionType religionType = ResolveReligionType(game, religionTypeResult.Value);
            if (religionType == ReligionType.NONE)
            {
                result.Error = $"Unknown religion type: {religionTypeResult.Value}";
                return result;
            }

            if (_sendChangeReligionMethod == null)
            {
                result.Error = "ChangeReligion command not available";
                return result;
            }

            try
            {
                _sendChangeReligionMethod.Invoke(clientManager, new object[] { city, religionType, add });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Change religion: city={cityIdResult.Value}, religion={religionTypeResult.Value}, add={add}");
            }
            catch (Exception ex)
            {
                result.Error = $"ChangeReligion failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteChangeFamily(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "familyType", out var familyTypeResult))
            {
                result.Error = GetParamError("familyType", familyTypeResult, "string");
                return result;
            }

            City city = game.city(cityIdResult.Value);
            if (city == null)
            {
                result.Error = $"City not found: {cityIdResult.Value}";
                return result;
            }

            FamilyType familyType = ResolveFamilyType(game, familyTypeResult.Value);
            if (familyType == FamilyType.NONE)
            {
                result.Error = $"Unknown family type: {familyTypeResult.Value}";
                return result;
            }

            if (_sendChangeFamilyMethod == null)
            {
                result.Error = "ChangeFamily command not available";
                return result;
            }

            try
            {
                _sendChangeFamilyMethod.Invoke(clientManager, new object[] { city, familyType });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Change family: city={cityIdResult.Value}, family={familyTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"ChangeFamily failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteChangeFamilySeat(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "familyType", out var familyTypeResult))
            {
                result.Error = GetParamError("familyType", familyTypeResult, "string");
                return result;
            }

            City city = game.city(cityIdResult.Value);
            if (city == null)
            {
                result.Error = $"City not found: {cityIdResult.Value}";
                return result;
            }

            FamilyType familyType = ResolveFamilyType(game, familyTypeResult.Value);
            if (familyType == FamilyType.NONE)
            {
                result.Error = $"Unknown family type: {familyTypeResult.Value}";
                return result;
            }

            if (_sendChangeFamilySeatMethod == null)
            {
                result.Error = "ChangeFamilySeat command not available";
                return result;
            }

            try
            {
                _sendChangeFamilySeatMethod.Invoke(clientManager, new object[] { city, familyType });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Change family seat: city={cityIdResult.Value}, family={familyTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"ChangeFamilySeat failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        // Batch H: Goals & Communication

        private static CommandResult ExecuteAbandonAmbition(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "goalId", out var goalIdResult))
            {
                result.Error = GetParamError("goalId", goalIdResult, "integer");
                return result;
            }

            if (_sendAbandonAmbitionMethod == null)
            {
                result.Error = "AbandonAmbition command not available";
                return result;
            }

            try
            {
                _sendAbandonAmbitionMethod.Invoke(clientManager, new object[] { goalIdResult.Value });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Abandon ambition: id={goalIdResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"AbandonAmbition failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteAddPlayerGoal(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "playerType", out var playerTypeResult))
            {
                result.Error = GetParamError("playerType", playerTypeResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "goalType", out var goalTypeResult))
            {
                result.Error = GetParamError("goalType", goalTypeResult, "string");
                return result;
            }

            GoalType goalType = ResolveGoalType(game, goalTypeResult.Value);
            if (goalType == GoalType.NONE)
            {
                result.Error = $"Unknown goal type: {goalTypeResult.Value}";
                return result;
            }

            if (_sendAddPlayerGoalMethod == null)
            {
                result.Error = "AddPlayerGoal command not available";
                return result;
            }

            try
            {
                PlayerType playerType = (PlayerType)playerTypeResult.Value;
                _sendAddPlayerGoalMethod.Invoke(clientManager, new object[] { playerType, goalType });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Add player goal: player={playerTypeResult.Value}, goal={goalTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"AddPlayerGoal failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteRemovePlayerGoal(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "playerType", out var playerTypeResult))
            {
                result.Error = GetParamError("playerType", playerTypeResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "goalId", out var goalIdResult))
            {
                result.Error = GetParamError("goalId", goalIdResult, "integer");
                return result;
            }

            if (_sendRemovePlayerGoalMethod == null)
            {
                result.Error = "RemovePlayerGoal command not available";
                return result;
            }

            try
            {
                PlayerType playerType = (PlayerType)playerTypeResult.Value;
                _sendRemovePlayerGoalMethod.Invoke(clientManager, new object[] { playerType, goalIdResult.Value });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Remove player goal: player={playerTypeResult.Value}, goalId={goalIdResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"RemovePlayerGoal failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteEventStory(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "playerType", out var playerTypeResult))
            {
                result.Error = GetParamError("playerType", playerTypeResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "eventStoryType", out var eventStoryTypeResult))
            {
                result.Error = GetParamError("eventStoryType", eventStoryTypeResult, "string");
                return result;
            }

            EventStoryType eventStoryType = ResolveEventStoryType(game, eventStoryTypeResult.Value);
            if (eventStoryType == EventStoryType.NONE)
            {
                result.Error = $"Unknown event story type: {eventStoryTypeResult.Value}";
                return result;
            }

            if (_sendEventStoryMethod == null)
            {
                result.Error = "EventStory command not available";
                return result;
            }

            try
            {
                PlayerType playerType = (PlayerType)playerTypeResult.Value;
                _sendEventStoryMethod.Invoke(clientManager, new object[] { playerType, eventStoryType });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Event story: player={playerTypeResult.Value}, event={eventStoryTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"EventStory failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteFinishGoal(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetStringParam(cmd, "goalType", out var goalTypeResult))
            {
                result.Error = GetParamError("goalType", goalTypeResult, "string");
                return result;
            }

            bool fail = GetBoolParam(cmd, "fail", false);

            GoalType goalType = ResolveGoalType(game, goalTypeResult.Value);
            if (goalType == GoalType.NONE)
            {
                result.Error = $"Unknown goal type: {goalTypeResult.Value}";
                return result;
            }

            if (_sendFinishGoalMethod == null)
            {
                result.Error = "FinishGoal command not available";
                return result;
            }

            try
            {
                _sendFinishGoalMethod.Invoke(clientManager, new object[] { goalType, fail });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Finish goal: goal={goalTypeResult.Value}, fail={fail}");
            }
            catch (Exception ex)
            {
                result.Error = $"FinishGoal failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteChat(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetStringParam(cmd, "chatType", out var chatTypeResult))
            {
                result.Error = GetParamError("chatType", chatTypeResult, "string");
                return result;
            }

            if (!TryGetStringParam(cmd, "message", out var messageResult))
            {
                result.Error = GetParamError("message", messageResult, "string");
                return result;
            }

            int targetPlayerIndex = GetIntParam(cmd, "targetPlayer", -1);

            if (!Enum.TryParse<ChatType>(chatTypeResult.Value, true, out var chatType))
            {
                result.Error = $"Unknown chat type: {chatTypeResult.Value}";
                return result;
            }

            if (_sendChatMethod == null)
            {
                result.Error = "Chat command not available";
                return result;
            }

            try
            {
                PlayerType targetPlayer = targetPlayerIndex >= 0 ? (PlayerType)targetPlayerIndex : PlayerType.NONE;
                _sendChatMethod.Invoke(clientManager, new object[] { chatType, targetPlayer, messageResult.Value });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Chat: type={chatTypeResult.Value}, target={targetPlayerIndex}, message={messageResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"Chat failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecutePing(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "tileId", out var tileIdResult))
            {
                result.Error = GetParamError("tileId", tileIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "pingType", out var pingTypeResult))
            {
                result.Error = GetParamError("pingType", pingTypeResult, "string");
                return result;
            }

            string message = GetStringParam(cmd, "message", "");
            int reminderTurn = GetIntParam(cmd, "reminderTurn", -1);

            PingType pingType = ResolvePingType(game, pingTypeResult.Value);
            if (pingType == PingType.NONE)
            {
                result.Error = $"Unknown ping type: {pingTypeResult.Value}";
                return result;
            }

            if (_sendPingMethod == null)
            {
                result.Error = "Ping command not available";
                return result;
            }

            try
            {
                _sendPingMethod.Invoke(clientManager, new object[] { tileIdResult.Value, pingType, message, reminderTurn, ImprovementType.NONE });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Ping: tile={tileIdResult.Value}, type={pingTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"Ping failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteCustomReminder(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetStringParam(cmd, "message", out var messageResult))
            {
                result.Error = GetParamError("message", messageResult, "string");
                return result;
            }

            if (_sendCustomReminderMethod == null)
            {
                result.Error = "CustomReminder command not available";
                return result;
            }

            try
            {
                _sendCustomReminderMethod.Invoke(clientManager, new object[] { messageResult.Value });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Custom reminder: {messageResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"CustomReminder failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteClearChat(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (_sendClearChatMethod == null)
            {
                result.Error = "ClearChat command not available";
                return result;
            }

            try
            {
                _sendClearChatMethod.Invoke(clientManager, null);
                result.Success = true;
                Debug.Log("[APIEndpoint] Clear chat");
            }
            catch (Exception ex)
            {
                result.Error = $"ClearChat failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        // Batch I: Game State & Turn

        private static CommandResult ExecuteExtendTime(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (_sendExtendTimeMethod == null)
            {
                result.Error = "ExtendTime command not available";
                return result;
            }

            try
            {
                _sendExtendTimeMethod.Invoke(clientManager, null);
                result.Success = true;
                Debug.Log("[APIEndpoint] Extend time");
            }
            catch (Exception ex)
            {
                result.Error = $"ExtendTime failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecutePause(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (_sendPauseMethod == null)
            {
                result.Error = "Pause command not available";
                return result;
            }

            try
            {
                _sendPauseMethod.Invoke(clientManager, null);
                result.Success = true;
                Debug.Log("[APIEndpoint] Pause");
            }
            catch (Exception ex)
            {
                result.Error = $"Pause failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteUndo(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            bool turnUndo = GetBoolParam(cmd, "turnUndo", false);

            if (_sendUndoMethod == null)
            {
                result.Error = "Undo command not available";
                return result;
            }

            try
            {
                _sendUndoMethod.Invoke(clientManager, new object[] { turnUndo });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Undo: turnUndo={turnUndo}");
            }
            catch (Exception ex)
            {
                result.Error = $"Undo failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteRedo(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (_sendRedoMethod == null)
            {
                result.Error = "Redo command not available";
                return result;
            }

            try
            {
                _sendRedoMethod.Invoke(clientManager, null);
                result.Success = true;
                Debug.Log("[APIEndpoint] Redo");
            }
            catch (Exception ex)
            {
                result.Error = $"Redo failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteReplayTurn(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            int numTurns = GetIntParam(cmd, "numTurns", 1);
            bool step = GetBoolParam(cmd, "step", false);

            if (_sendReplayTurnMethod == null)
            {
                result.Error = "ReplayTurn command not available";
                return result;
            }

            try
            {
                _sendReplayTurnMethod.Invoke(clientManager, new object[] { numTurns, step });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Replay turn: numTurns={numTurns}, step={step}");
            }
            catch (Exception ex)
            {
                result.Error = $"ReplayTurn failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteAIFinishTurn(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            int numTurns = GetIntParam(cmd, "numTurns", 1);

            if (_sendAIFinishTurnMethod == null)
            {
                result.Error = "AIFinishTurn command not available";
                return result;
            }

            try
            {
                _sendAIFinishTurnMethod.Invoke(clientManager, new object[] { numTurns });
                result.Success = true;
                Debug.Log($"[APIEndpoint] AI finish turn: numTurns={numTurns}");
            }
            catch (Exception ex)
            {
                result.Error = $"AIFinishTurn failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteToggleNoReplay(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (_sendToggleNoReplayMethod == null)
            {
                result.Error = "ToggleNoReplay command not available";
                return result;
            }

            try
            {
                _sendToggleNoReplayMethod.Invoke(clientManager, null);
                result.Success = true;
                Debug.Log("[APIEndpoint] Toggle no replay");
            }
            catch (Exception ex)
            {
                result.Error = $"ToggleNoReplay failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        // Batch J: Diplomacy Extended

        private static CommandResult ExecuteTeamAlliance(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "player1", out var player1Result))
            {
                result.Error = GetParamError("player1", player1Result, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "player2", out var player2Result))
            {
                result.Error = GetParamError("player2", player2Result, "integer");
                return result;
            }

            if (_sendTeamAllianceMethod == null)
            {
                result.Error = "TeamAlliance command not available";
                return result;
            }

            try
            {
                PlayerType player1 = (PlayerType)player1Result.Value;
                PlayerType player2 = (PlayerType)player2Result.Value;
                _sendTeamAllianceMethod.Invoke(clientManager, new object[] { player1, player2 });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Team alliance: player1={player1Result.Value}, player2={player2Result.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"TeamAlliance failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteTribeInvasion(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetStringParam(cmd, "tribeType", out var tribeTypeResult))
            {
                result.Error = GetParamError("tribeType", tribeTypeResult, "string");
                return result;
            }

            if (!TryGetIntParam(cmd, "targetPlayer", out var targetPlayerResult))
            {
                result.Error = GetParamError("targetPlayer", targetPlayerResult, "integer");
                return result;
            }

            TribeType tribeType = ResolveTribeType(game, tribeTypeResult.Value);
            if (tribeType == TribeType.NONE)
            {
                result.Error = $"Unknown tribe type: {tribeTypeResult.Value}";
                return result;
            }

            if (_sendTribeInvasionMethod == null)
            {
                result.Error = "TribeInvasion command not available";
                return result;
            }

            try
            {
                PlayerType targetPlayer = (PlayerType)targetPlayerResult.Value;
                _sendTribeInvasionMethod.Invoke(clientManager, new object[] { tribeType, targetPlayer });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Tribe invasion: tribe={tribeTypeResult.Value}, target={targetPlayerResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"TribeInvasion failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteVictoryTeam(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetStringParam(cmd, "victoryType", out var victoryTypeResult))
            {
                result.Error = GetParamError("victoryType", victoryTypeResult, "string");
                return result;
            }

            if (!TryGetIntParam(cmd, "teamType", out var teamTypeResult))
            {
                result.Error = GetParamError("teamType", teamTypeResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "actionType", out var actionTypeResult))
            {
                result.Error = GetParamError("actionType", actionTypeResult, "string");
                return result;
            }

            VictoryType victoryType = ResolveVictoryType(game, victoryTypeResult.Value);
            if (victoryType == VictoryType.NONE)
            {
                result.Error = $"Unknown victory type: {victoryTypeResult.Value}";
                return result;
            }

            if (!Enum.TryParse<ActionType>(actionTypeResult.Value, true, out var actionType))
            {
                result.Error = $"Unknown action type: {actionTypeResult.Value}";
                return result;
            }

            if (_sendVictoryTeamMethod == null)
            {
                result.Error = "VictoryTeam command not available";
                return result;
            }

            try
            {
                TeamType teamType = (TeamType)teamTypeResult.Value;
                _sendVictoryTeamMethod.Invoke(clientManager, new object[] { victoryType, teamType, actionType });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Victory team: victory={victoryTypeResult.Value}, team={teamTypeResult.Value}, action={actionTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"VictoryTeam failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        // ===== Batch K: Editor/Debug Commands =====

        private static CommandResult ExecuteCreateUnit(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "tileId", out var tileIdResult))
            {
                result.Error = GetParamError("tileId", tileIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "unitType", out var unitTypeResult))
            {
                result.Error = GetParamError("unitType", unitTypeResult, "string");
                return result;
            }

            UnitType unitType = ResolveUnitType(game, unitTypeResult.Value);
            if (unitType == UnitType.NONE)
            {
                result.Error = $"Unknown unit type: {unitTypeResult.Value}";
                return result;
            }

            Tile tile = game?.tile(tileIdResult.Value);
            if (tile == null)
            {
                result.Error = $"Tile not found: {tileIdResult.Value}";
                return result;
            }

            // Optional: playerType (defaults to -1 which uses current player)
            int playerTypeInt = GetIntParam(cmd, "playerType", -1);
            PlayerType playerType = playerTypeInt >= 0 ? (PlayerType)playerTypeInt : PlayerType.NONE;

            // Optional: tribeType
            string tribeTypeStr = GetStringParam(cmd, "tribeType", null);
            TribeType tribeType = !string.IsNullOrEmpty(tribeTypeStr) ? ResolveTribeType(game, tribeTypeStr) : TribeType.NONE;

            if (_sendCreateUnitMethod == null)
            {
                result.Error = "CreateUnit command not available";
                return result;
            }

            try
            {
                _sendCreateUnitMethod.Invoke(clientManager, new object[] { tile, unitType, playerType, tribeType });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Create unit: tile={tileIdResult.Value}, type={unitTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"CreateUnit failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteUnitName(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "name", out var nameResult))
            {
                result.Error = GetParamError("name", nameResult, "string");
                return result;
            }

            Unit unit = game?.unit(unitIdResult.Value);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitIdResult.Value}";
                return result;
            }

            if (_sendUnitNameMethod == null)
            {
                result.Error = "UnitName command not available";
                return result;
            }

            try
            {
                _sendUnitNameMethod.Invoke(clientManager, new object[] { unit, nameResult.Value });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Unit name: unitId={unitIdResult.Value}, name={nameResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"UnitName failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteSetUnitFamily(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "familyType", out var familyTypeResult))
            {
                result.Error = GetParamError("familyType", familyTypeResult, "string");
                return result;
            }

            Unit unit = game?.unit(unitIdResult.Value);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitIdResult.Value}";
                return result;
            }

            FamilyType familyType = ResolveFamilyType(game, familyTypeResult.Value);
            if (familyType == FamilyType.NONE)
            {
                result.Error = $"Unknown family type: {familyTypeResult.Value}";
                return result;
            }

            if (_sendSetUnitFamilyMethod == null)
            {
                result.Error = "SetUnitFamily command not available";
                return result;
            }

            try
            {
                _sendSetUnitFamilyMethod.Invoke(clientManager, new object[] { unit, familyType });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Set unit family: unitId={unitIdResult.Value}, family={familyTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"SetUnitFamily failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteChangeUnitOwner(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            Unit unit = game?.unit(unitIdResult.Value);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitIdResult.Value}";
                return result;
            }

            // Either playerType or tribeType should be provided
            int playerTypeInt = GetIntParam(cmd, "playerType", -1);
            PlayerType playerType = playerTypeInt >= 0 ? (PlayerType)playerTypeInt : PlayerType.NONE;

            string tribeTypeStr = GetStringParam(cmd, "tribeType", null);
            TribeType tribeType = !string.IsNullOrEmpty(tribeTypeStr) ? ResolveTribeType(game, tribeTypeStr) : TribeType.NONE;

            if (_sendChangeUnitOwnerMethod == null)
            {
                result.Error = "ChangeUnitOwner command not available";
                return result;
            }

            try
            {
                _sendChangeUnitOwnerMethod.Invoke(clientManager, new object[] { unit, playerType, tribeType });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Change unit owner: unitId={unitIdResult.Value}, player={playerTypeInt}, tribe={tribeTypeStr}");
            }
            catch (Exception ex)
            {
                result.Error = $"ChangeUnitOwner failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteChangeCooldown(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "delta", out var deltaResult))
            {
                result.Error = GetParamError("delta", deltaResult, "integer");
                return result;
            }

            Unit unit = game?.unit(unitIdResult.Value);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitIdResult.Value}";
                return result;
            }

            if (_sendChangeCooldownMethod == null)
            {
                result.Error = "ChangeCooldown command not available";
                return result;
            }

            try
            {
                _sendChangeCooldownMethod.Invoke(clientManager, new object[] { unit, deltaResult.Value });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Change cooldown: unitId={unitIdResult.Value}, delta={deltaResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"ChangeCooldown failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteChangeDamage(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "delta", out var deltaResult))
            {
                result.Error = GetParamError("delta", deltaResult, "integer");
                return result;
            }

            Unit unit = game?.unit(unitIdResult.Value);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitIdResult.Value}";
                return result;
            }

            if (_sendChangeDamageMethod == null)
            {
                result.Error = "ChangeDamage command not available";
                return result;
            }

            try
            {
                _sendChangeDamageMethod.Invoke(clientManager, new object[] { unit, deltaResult.Value });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Change damage: unitId={unitIdResult.Value}, delta={deltaResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"ChangeDamage failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteUnitIncrementLevel(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            Unit unit = game?.unit(unitIdResult.Value);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitIdResult.Value}";
                return result;
            }

            if (_sendUnitIncrementLevelMethod == null)
            {
                result.Error = "UnitIncrementLevel command not available";
                return result;
            }

            try
            {
                _sendUnitIncrementLevelMethod.Invoke(clientManager, new object[] { unit });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Unit increment level: unitId={unitIdResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"UnitIncrementLevel failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteUnitChangePromotion(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "promotionType", out var promotionTypeResult))
            {
                result.Error = GetParamError("promotionType", promotionTypeResult, "string");
                return result;
            }

            Unit unit = game?.unit(unitIdResult.Value);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitIdResult.Value}";
                return result;
            }

            PromotionType promotionType = ResolvePromotionType(game, promotionTypeResult.Value);
            if (promotionType == PromotionType.NONE)
            {
                result.Error = $"Unknown promotion type: {promotionTypeResult.Value}";
                return result;
            }

            int delta = GetIntParam(cmd, "delta", 1);

            if (_sendUnitChangePromotionMethod == null)
            {
                result.Error = "UnitChangePromotion command not available";
                return result;
            }

            try
            {
                _sendUnitChangePromotionMethod.Invoke(clientManager, new object[] { unit, promotionType, delta });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Unit change promotion: unitId={unitIdResult.Value}, promotion={promotionTypeResult.Value}, delta={delta}");
            }
            catch (Exception ex)
            {
                result.Error = $"UnitChangePromotion failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteCreateCity(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "playerType", out var playerTypeResult))
            {
                result.Error = GetParamError("playerType", playerTypeResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "tileId", out var tileIdResult))
            {
                result.Error = GetParamError("tileId", tileIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "familyType", out var familyTypeResult))
            {
                result.Error = GetParamError("familyType", familyTypeResult, "string");
                return result;
            }

            Tile tile = game?.tile(tileIdResult.Value);
            if (tile == null)
            {
                result.Error = $"Tile not found: {tileIdResult.Value}";
                return result;
            }

            FamilyType familyType = ResolveFamilyType(game, familyTypeResult.Value);
            if (familyType == FamilyType.NONE)
            {
                result.Error = $"Unknown family type: {familyTypeResult.Value}";
                return result;
            }

            int turn = GetIntParam(cmd, "turn", -1);

            if (_sendCreateCityMethod == null)
            {
                result.Error = "CreateCity command not available";
                return result;
            }

            try
            {
                PlayerType playerType = (PlayerType)playerTypeResult.Value;
                _sendCreateCityMethod.Invoke(clientManager, new object[] { playerType, tile, familyType, turn });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Create city: player={playerTypeResult.Value}, tile={tileIdResult.Value}, family={familyTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"CreateCity failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteRemoveCity(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            City city = game?.city(cityIdResult.Value);
            if (city == null)
            {
                result.Error = $"City not found: {cityIdResult.Value}";
                return result;
            }

            if (_sendRemoveCityMethod == null)
            {
                result.Error = "RemoveCity command not available";
                return result;
            }

            try
            {
                _sendRemoveCityMethod.Invoke(clientManager, new object[] { city });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Remove city: cityId={cityIdResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"RemoveCity failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteCityOwner(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "playerType", out var playerTypeResult))
            {
                result.Error = GetParamError("playerType", playerTypeResult, "integer");
                return result;
            }

            City city = game?.city(cityIdResult.Value);
            if (city == null)
            {
                result.Error = $"City not found: {cityIdResult.Value}";
                return result;
            }

            if (_sendCityOwnerMethod == null)
            {
                result.Error = "CityOwner command not available";
                return result;
            }

            try
            {
                PlayerType playerType = (PlayerType)playerTypeResult.Value;
                _sendCityOwnerMethod.Invoke(clientManager, new object[] { city, playerType });
                result.Success = true;
                Debug.Log($"[APIEndpoint] City owner: cityId={cityIdResult.Value}, player={playerTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"CityOwner failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteChangeCityDamage(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "delta", out var deltaResult))
            {
                result.Error = GetParamError("delta", deltaResult, "integer");
                return result;
            }

            City city = game?.city(cityIdResult.Value);
            if (city == null)
            {
                result.Error = $"City not found: {cityIdResult.Value}";
                return result;
            }

            if (_sendChangeCityDamageMethod == null)
            {
                result.Error = "ChangeCityDamage command not available";
                return result;
            }

            try
            {
                _sendChangeCityDamageMethod.Invoke(clientManager, new object[] { city, deltaResult.Value });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Change city damage: cityId={cityIdResult.Value}, delta={deltaResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"ChangeCityDamage failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteChangeCulture(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            bool grow = GetBoolParam(cmd, "grow", true);

            City city = game?.city(cityIdResult.Value);
            if (city == null)
            {
                result.Error = $"City not found: {cityIdResult.Value}";
                return result;
            }

            if (_sendChangeCultureMethod == null)
            {
                result.Error = "ChangeCulture command not available";
                return result;
            }

            try
            {
                _sendChangeCultureMethod.Invoke(clientManager, new object[] { city, grow });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Change culture: cityId={cityIdResult.Value}, grow={grow}");
            }
            catch (Exception ex)
            {
                result.Error = $"ChangeCulture failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteChangeCityBuildTurns(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "delta", out var deltaResult))
            {
                result.Error = GetParamError("delta", deltaResult, "integer");
                return result;
            }

            City city = game?.city(cityIdResult.Value);
            if (city == null)
            {
                result.Error = $"City not found: {cityIdResult.Value}";
                return result;
            }

            if (_sendChangeCityBuildTurnsMethod == null)
            {
                result.Error = "ChangeCityBuildTurns command not available";
                return result;
            }

            try
            {
                _sendChangeCityBuildTurnsMethod.Invoke(clientManager, new object[] { city, deltaResult.Value });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Change city build turns: cityId={cityIdResult.Value}, delta={deltaResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"ChangeCityBuildTurns failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteChangeCityDiscontentLevel(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "delta", out var deltaResult))
            {
                result.Error = GetParamError("delta", deltaResult, "integer");
                return result;
            }

            City city = game?.city(cityIdResult.Value);
            if (city == null)
            {
                result.Error = $"City not found: {cityIdResult.Value}";
                return result;
            }

            if (_sendChangeCityDiscontentLevelMethod == null)
            {
                result.Error = "ChangeCityDiscontentLevel command not available";
                return result;
            }

            try
            {
                _sendChangeCityDiscontentLevelMethod.Invoke(clientManager, new object[] { city, deltaResult.Value });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Change city discontent level: cityId={cityIdResult.Value}, delta={deltaResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"ChangeCityDiscontentLevel failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteChangeProject(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "projectType", out var projectTypeResult))
            {
                result.Error = GetParamError("projectType", projectTypeResult, "string");
                return result;
            }

            City city = game?.city(cityIdResult.Value);
            if (city == null)
            {
                result.Error = $"City not found: {cityIdResult.Value}";
                return result;
            }

            ProjectType projectType = ResolveProjectType(game, projectTypeResult.Value);
            if (projectType == ProjectType.NONE)
            {
                result.Error = $"Unknown project type: {projectTypeResult.Value}";
                return result;
            }

            int delta = GetIntParam(cmd, "delta", 1);

            if (_sendChangeProjectMethod == null)
            {
                result.Error = "ChangeProject command not available";
                return result;
            }

            try
            {
                _sendChangeProjectMethod.Invoke(clientManager, new object[] { city, projectType, delta });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Change project: cityId={cityIdResult.Value}, project={projectTypeResult.Value}, delta={delta}");
            }
            catch (Exception ex)
            {
                result.Error = $"ChangeProject failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteSetTerrain(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "tileId", out var tileIdResult))
            {
                result.Error = GetParamError("tileId", tileIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "terrainType", out var terrainTypeResult))
            {
                result.Error = GetParamError("terrainType", terrainTypeResult, "string");
                return result;
            }

            Tile tile = game?.tile(tileIdResult.Value);
            if (tile == null)
            {
                result.Error = $"Tile not found: {tileIdResult.Value}";
                return result;
            }

            TerrainType terrainType = ResolveTerrainType(game, terrainTypeResult.Value);
            if (terrainType == TerrainType.NONE)
            {
                result.Error = $"Unknown terrain type: {terrainTypeResult.Value}";
                return result;
            }

            if (_sendTerrainMethod == null)
            {
                result.Error = "SetTerrain command not available";
                return result;
            }

            try
            {
                _sendTerrainMethod.Invoke(clientManager, new object[] { tile, terrainType });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Set terrain: tileId={tileIdResult.Value}, terrain={terrainTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"SetTerrain failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteSetTerrainHeight(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "tileId", out var tileIdResult))
            {
                result.Error = GetParamError("tileId", tileIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "heightType", out var heightTypeResult))
            {
                result.Error = GetParamError("heightType", heightTypeResult, "string");
                return result;
            }

            Tile tile = game?.tile(tileIdResult.Value);
            if (tile == null)
            {
                result.Error = $"Tile not found: {tileIdResult.Value}";
                return result;
            }

            HeightType heightType = ResolveHeightType(game, heightTypeResult.Value);
            if (heightType == HeightType.NONE)
            {
                result.Error = $"Unknown height type: {heightTypeResult.Value}";
                return result;
            }

            if (_sendTerrainHeightMethod == null)
            {
                result.Error = "SetTerrainHeight command not available";
                return result;
            }

            try
            {
                _sendTerrainHeightMethod.Invoke(clientManager, new object[] { tile, heightType });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Set terrain height: tileId={tileIdResult.Value}, height={heightTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"SetTerrainHeight failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteSetVegetation(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "tileId", out var tileIdResult))
            {
                result.Error = GetParamError("tileId", tileIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "vegetationType", out var vegetationTypeResult))
            {
                result.Error = GetParamError("vegetationType", vegetationTypeResult, "string");
                return result;
            }

            Tile tile = game?.tile(tileIdResult.Value);
            if (tile == null)
            {
                result.Error = $"Tile not found: {tileIdResult.Value}";
                return result;
            }

            VegetationType vegetationType = ResolveVegetationType(game, vegetationTypeResult.Value);
            if (vegetationType == VegetationType.NONE)
            {
                result.Error = $"Unknown vegetation type: {vegetationTypeResult.Value}";
                return result;
            }

            if (_sendVegetationMethod == null)
            {
                result.Error = "SetVegetation command not available";
                return result;
            }

            try
            {
                _sendVegetationMethod.Invoke(clientManager, new object[] { tile, vegetationType });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Set vegetation: tileId={tileIdResult.Value}, vegetation={vegetationTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"SetVegetation failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteSetResource(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "tileId", out var tileIdResult))
            {
                result.Error = GetParamError("tileId", tileIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "resourceType", out var resourceTypeResult))
            {
                result.Error = GetParamError("resourceType", resourceTypeResult, "string");
                return result;
            }

            Tile tile = game?.tile(tileIdResult.Value);
            if (tile == null)
            {
                result.Error = $"Tile not found: {tileIdResult.Value}";
                return result;
            }

            ResourceType resourceType = ResolveResourceType(game, resourceTypeResult.Value);
            if (resourceType == ResourceType.NONE)
            {
                result.Error = $"Unknown resource type: {resourceTypeResult.Value}";
                return result;
            }

            if (_sendResourceMethod == null)
            {
                result.Error = "SetResource command not available";
                return result;
            }

            try
            {
                _sendResourceMethod.Invoke(clientManager, new object[] { tile, resourceType });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Set resource: tileId={tileIdResult.Value}, resource={resourceTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"SetResource failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteSetRoad(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "tileId", out var tileIdResult))
            {
                result.Error = GetParamError("tileId", tileIdResult, "integer");
                return result;
            }

            bool hasRoad = GetBoolParam(cmd, "hasRoad", true);

            Tile tile = game?.tile(tileIdResult.Value);
            if (tile == null)
            {
                result.Error = $"Tile not found: {tileIdResult.Value}";
                return result;
            }

            if (_sendRoadMethod == null)
            {
                result.Error = "SetRoad command not available";
                return result;
            }

            try
            {
                _sendRoadMethod.Invoke(clientManager, new object[] { tile, hasRoad });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Set road: tileId={tileIdResult.Value}, hasRoad={hasRoad}");
            }
            catch (Exception ex)
            {
                result.Error = $"SetRoad failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteSetImprovement(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "tileId", out var tileIdResult))
            {
                result.Error = GetParamError("tileId", tileIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "improvementType", out var improvementTypeResult))
            {
                result.Error = GetParamError("improvementType", improvementTypeResult, "string");
                return result;
            }

            Tile tile = game?.tile(tileIdResult.Value);
            if (tile == null)
            {
                result.Error = $"Tile not found: {tileIdResult.Value}";
                return result;
            }

            ImprovementType improvementType = ResolveImprovementType(game, improvementTypeResult.Value);
            if (improvementType == ImprovementType.NONE)
            {
                result.Error = $"Unknown improvement type: {improvementTypeResult.Value}";
                return result;
            }

            if (_sendSetImprovementMethod == null)
            {
                result.Error = "SetImprovement command not available";
                return result;
            }

            try
            {
                _sendSetImprovementMethod.Invoke(clientManager, new object[] { tile, improvementType });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Set improvement: tileId={tileIdResult.Value}, improvement={improvementTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"SetImprovement failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteSetTileOwner(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "tileId", out var tileIdResult))
            {
                result.Error = GetParamError("tileId", tileIdResult, "integer");
                return result;
            }

            Tile tile = game?.tile(tileIdResult.Value);
            if (tile == null)
            {
                result.Error = $"Tile not found: {tileIdResult.Value}";
                return result;
            }

            // Either playerType or tribeType can be provided
            int playerTypeInt = GetIntParam(cmd, "playerType", -1);
            PlayerType playerType = playerTypeInt >= 0 ? (PlayerType)playerTypeInt : PlayerType.NONE;

            string tribeTypeStr = GetStringParam(cmd, "tribeType", null);
            TribeType tribeType = !string.IsNullOrEmpty(tribeTypeStr) ? ResolveTribeType(game, tribeTypeStr) : TribeType.NONE;

            if (_sendTileOwnerMethod == null)
            {
                result.Error = "SetTileOwner command not available";
                return result;
            }

            try
            {
                _sendTileOwnerMethod.Invoke(clientManager, new object[] { tile, playerType, tribeType });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Set tile owner: tileId={tileIdResult.Value}, player={playerTypeInt}, tribe={tribeTypeStr}");
            }
            catch (Exception ex)
            {
                result.Error = $"SetTileOwner failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteSetCitySite(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "tileId", out var tileIdResult))
            {
                result.Error = GetParamError("tileId", tileIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "citySiteType", out var citySiteTypeResult))
            {
                result.Error = GetParamError("citySiteType", citySiteTypeResult, "string");
                return result;
            }

            Tile tile = game?.tile(tileIdResult.Value);
            if (tile == null)
            {
                result.Error = $"Tile not found: {tileIdResult.Value}";
                return result;
            }

            CitySiteType citySiteType = ResolveCitySiteType(game, citySiteTypeResult.Value);
            if (citySiteType == CitySiteType.NONE)
            {
                result.Error = $"Unknown city site type: {citySiteTypeResult.Value}";
                return result;
            }

            if (_sendSetCitySiteMethod == null)
            {
                result.Error = "SetCitySite command not available";
                return result;
            }

            try
            {
                _sendSetCitySiteMethod.Invoke(clientManager, new object[] { tile, citySiteType });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Set city site: tileId={tileIdResult.Value}, citySite={citySiteTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"SetCitySite failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteImprovementBuildTurns(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "tileId", out var tileIdResult))
            {
                result.Error = GetParamError("tileId", tileIdResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "turns", out var turnsResult))
            {
                result.Error = GetParamError("turns", turnsResult, "integer");
                return result;
            }

            Tile tile = game?.tile(tileIdResult.Value);
            if (tile == null)
            {
                result.Error = $"Tile not found: {tileIdResult.Value}";
                return result;
            }

            if (_sendImprovementBuildTurnsMethod == null)
            {
                result.Error = "ImprovementBuildTurns command not available";
                return result;
            }

            try
            {
                _sendImprovementBuildTurnsMethod.Invoke(clientManager, new object[] { tile, turnsResult.Value });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Improvement build turns: tileId={tileIdResult.Value}, turns={turnsResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"ImprovementBuildTurns failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteMapReveal(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            int teamTypeInt = GetIntParam(cmd, "teamType", -1);
            TeamType teamType = teamTypeInt >= 0 ? (TeamType)teamTypeInt : TeamType.NONE;

            if (_sendMapRevealMethod == null)
            {
                result.Error = "MapReveal command not available";
                return result;
            }

            try
            {
                _sendMapRevealMethod.Invoke(clientManager, new object[] { teamType });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Map reveal: team={teamTypeInt}");
            }
            catch (Exception ex)
            {
                result.Error = $"MapReveal failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteMapUnreveal(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            int teamTypeInt = GetIntParam(cmd, "teamType", -1);
            TeamType teamType = teamTypeInt >= 0 ? (TeamType)teamTypeInt : TeamType.NONE;

            if (_sendMapUnrevealMethod == null)
            {
                result.Error = "MapUnreveal command not available";
                return result;
            }

            try
            {
                _sendMapUnrevealMethod.Invoke(clientManager, new object[] { teamType });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Map unreveal: team={teamTypeInt}");
            }
            catch (Exception ex)
            {
                result.Error = $"MapUnreveal failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteAddTech(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "playerType", out var playerTypeResult))
            {
                result.Error = GetParamError("playerType", playerTypeResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "techType", out var techTypeResult))
            {
                result.Error = GetParamError("techType", techTypeResult, "string");
                return result;
            }

            TechType techType = ResolveTechType(game, techTypeResult.Value);
            if (techType == TechType.NONE)
            {
                result.Error = $"Unknown tech type: {techTypeResult.Value}";
                return result;
            }

            if (_sendAddTechMethod == null)
            {
                result.Error = "AddTech command not available";
                return result;
            }

            try
            {
                PlayerType playerType = (PlayerType)playerTypeResult.Value;
                _sendAddTechMethod.Invoke(clientManager, new object[] { playerType, techType });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Add tech: player={playerTypeResult.Value}, tech={techTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"AddTech failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteAddYield(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "playerType", out var playerTypeResult))
            {
                result.Error = GetParamError("playerType", playerTypeResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "yieldType", out var yieldTypeResult))
            {
                result.Error = GetParamError("yieldType", yieldTypeResult, "string");
                return result;
            }

            if (!TryGetIntParam(cmd, "amount", out var amountResult))
            {
                result.Error = GetParamError("amount", amountResult, "integer");
                return result;
            }

            YieldType yieldType = ResolveYieldType(game, yieldTypeResult.Value);
            if (yieldType == YieldType.NONE)
            {
                result.Error = $"Unknown yield type: {yieldTypeResult.Value}";
                return result;
            }

            if (_sendAddYieldMethod == null)
            {
                result.Error = "AddYield command not available";
                return result;
            }

            try
            {
                PlayerType playerType = (PlayerType)playerTypeResult.Value;
                _sendAddYieldMethod.Invoke(clientManager, new object[] { playerType, yieldType, amountResult.Value });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Add yield: player={playerTypeResult.Value}, yield={yieldTypeResult.Value}, amount={amountResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"AddYield failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteAddMoney(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "playerType", out var playerTypeResult))
            {
                result.Error = GetParamError("playerType", playerTypeResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "amount", out var amountResult))
            {
                result.Error = GetParamError("amount", amountResult, "integer");
                return result;
            }

            if (_sendAddMoneyMethod == null)
            {
                result.Error = "AddMoney command not available";
                return result;
            }

            try
            {
                PlayerType playerType = (PlayerType)playerTypeResult.Value;
                _sendAddMoneyMethod.Invoke(clientManager, new object[] { playerType, amountResult.Value });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Add money: player={playerTypeResult.Value}, amount={amountResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"AddMoney failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteCheat(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetStringParam(cmd, "hotkeyType", out var hotkeyTypeResult))
            {
                result.Error = GetParamError("hotkeyType", hotkeyTypeResult, "string");
                return result;
            }

            HotkeyType hotkeyType = ResolveHotkeyType(game, hotkeyTypeResult.Value);
            if (hotkeyType == HotkeyType.NONE)
            {
                result.Error = $"Unknown hotkey type: {hotkeyTypeResult.Value}";
                return result;
            }

            if (_sendCheatMethod == null)
            {
                result.Error = "Cheat command not available";
                return result;
            }

            try
            {
                _sendCheatMethod.Invoke(clientManager, new object[] { hotkeyType });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Cheat: hotkey={hotkeyTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"Cheat failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteMakeCharacterDead(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "characterId", out var characterIdResult))
            {
                result.Error = GetParamError("characterId", characterIdResult, "integer");
                return result;
            }

            Character character = game?.character(characterIdResult.Value);
            if (character == null)
            {
                result.Error = $"Character not found: {characterIdResult.Value}";
                return result;
            }

            if (_sendMakeCharacterDeadMethod == null)
            {
                result.Error = "MakeCharacterDead command not available";
                return result;
            }

            try
            {
                _sendMakeCharacterDeadMethod.Invoke(clientManager, new object[] { character });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Make character dead: characterId={characterIdResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"MakeCharacterDead failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteMakeCharacterSafe(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "characterId", out var characterIdResult))
            {
                result.Error = GetParamError("characterId", characterIdResult, "integer");
                return result;
            }

            Character character = game?.character(characterIdResult.Value);
            if (character == null)
            {
                result.Error = $"Character not found: {characterIdResult.Value}";
                return result;
            }

            int numTurns = GetIntParam(cmd, "numTurns", 10);

            if (_sendMakeCharacterSafeMethod == null)
            {
                result.Error = "MakeCharacterSafe command not available";
                return result;
            }

            try
            {
                _sendMakeCharacterSafeMethod.Invoke(clientManager, new object[] { character, numTurns });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Make character safe: characterId={characterIdResult.Value}, numTurns={numTurns}");
            }
            catch (Exception ex)
            {
                result.Error = $"MakeCharacterSafe failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteNewCharacter(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "playerType", out var playerTypeResult))
            {
                result.Error = GetParamError("playerType", playerTypeResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "familyType", out var familyTypeResult))
            {
                result.Error = GetParamError("familyType", familyTypeResult, "string");
                return result;
            }

            FamilyType familyType = ResolveFamilyType(game, familyTypeResult.Value);
            if (familyType == FamilyType.NONE)
            {
                result.Error = $"Unknown family type: {familyTypeResult.Value}";
                return result;
            }

            int age = GetIntParam(cmd, "age", 18);
            int fillValue = GetIntParam(cmd, "fillValue", 0);

            if (_sendNewCharacterMethod == null)
            {
                result.Error = "NewCharacter command not available";
                return result;
            }

            try
            {
                PlayerType playerType = (PlayerType)playerTypeResult.Value;
                _sendNewCharacterMethod.Invoke(clientManager, new object[] { playerType, familyType, age, fillValue });
                result.Success = true;
                Debug.Log($"[APIEndpoint] New character: player={playerTypeResult.Value}, family={familyTypeResult.Value}, age={age}");
            }
            catch (Exception ex)
            {
                result.Error = $"NewCharacter failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteAddCharacter(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetStringParam(cmd, "characterType", out var characterTypeResult))
            {
                result.Error = GetParamError("characterType", characterTypeResult, "string");
                return result;
            }

            if (!TryGetIntParam(cmd, "playerType", out var playerTypeResult))
            {
                result.Error = GetParamError("playerType", playerTypeResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "familyType", out var familyTypeResult))
            {
                result.Error = GetParamError("familyType", familyTypeResult, "string");
                return result;
            }

            CharacterType characterType = ResolveCharacterType(game, characterTypeResult.Value);
            if (characterType == CharacterType.NONE)
            {
                result.Error = $"Unknown character type: {characterTypeResult.Value}";
                return result;
            }

            FamilyType familyType = ResolveFamilyType(game, familyTypeResult.Value);
            if (familyType == FamilyType.NONE)
            {
                result.Error = $"Unknown family type: {familyTypeResult.Value}";
                return result;
            }

            if (_sendAddCharacterMethod == null)
            {
                result.Error = "AddCharacter command not available";
                return result;
            }

            try
            {
                PlayerType playerType = (PlayerType)playerTypeResult.Value;
                _sendAddCharacterMethod.Invoke(clientManager, new object[] { characterType, playerType, familyType });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Add character: type={characterTypeResult.Value}, player={playerTypeResult.Value}, family={familyTypeResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"AddCharacter failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteTribeLeader(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetStringParam(cmd, "tribeType", out var tribeTypeResult))
            {
                result.Error = GetParamError("tribeType", tribeTypeResult, "string");
                return result;
            }

            if (!TryGetIntParam(cmd, "characterId", out var characterIdResult))
            {
                result.Error = GetParamError("characterId", characterIdResult, "integer");
                return result;
            }

            TribeType tribeType = ResolveTribeType(game, tribeTypeResult.Value);
            if (tribeType == TribeType.NONE)
            {
                result.Error = $"Unknown tribe type: {tribeTypeResult.Value}";
                return result;
            }

            Character character = game?.character(characterIdResult.Value);
            if (character == null)
            {
                result.Error = $"Character not found: {characterIdResult.Value}";
                return result;
            }

            if (_sendTribeLeaderMethod == null)
            {
                result.Error = "TribeLeader command not available";
                return result;
            }

            try
            {
                _sendTribeLeaderMethod.Invoke(clientManager, new object[] { tribeType, character });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Tribe leader: tribe={tribeTypeResult.Value}, character={characterIdResult.Value}");
            }
            catch (Exception ex)
            {
                result.Error = $"TribeLeader failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        #endregion

        #endregion
    }
}
