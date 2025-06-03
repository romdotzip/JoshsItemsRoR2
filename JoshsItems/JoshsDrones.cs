using R2API;
using RoR2;
using RoR2.CharacterAI;
using RoR2.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace JoshsItems
{
    internal static class JoshsDrones
    {
        private static float droneMaxDistance;
        private static AISkillDriver attackDriver;

        public static void Init()
        {
            // Hi
            OverrideDroneBehavior();

            // Set variables
            droneMaxDistance = 300;
        }

        private static void OverrideDroneBehavior()
        {
            // Call necessary hooks
            Hooks();
        }

        private static void Hooks()
        {
            // Taken from BetterDrones teehee
            On.RoR2.CharacterAI.BaseAI.UpdateTargets += (orig, self) =>
            {
                orig(self);

                if (NetworkServer.active)
                {
                    if (self.master && self.master.minionOwnership && self.body.bodyFlags.HasFlag(CharacterBody.BodyFlags.Mechanical))
                    {
                        if (self.master.minionOwnership.ownerMaster)
                        {
                            CharacterMaster owner = self.master.minionOwnership.ownerMaster;
                            if (owner.playerCharacterMasterController && owner.playerCharacterMasterController.pingerController)
                            {
                                PingerController controller = owner.playerCharacterMasterController.pingerController;
                                if (controller.currentPing.active && controller.currentPing.targetGameObject)
                                {
                                    if (controller.currentPing.targetGameObject.GetComponent<CharacterBody>() && controller.currentPing.targetGameObject.GetComponent<CharacterBody>().teamComponent.teamIndex != TeamIndex.Player)
                                    {
                                        self.currentEnemy.gameObject = controller.currentPing.targetGameObject;
                                    }
                                }
                            }
                        }
                    }
                }
            };

            On.RoR2.CharacterAI.BaseAI.ManagedFixedUpdate += (orig, self, deltaTime) =>
            {
                // Run original function
                orig(self, deltaTime);

                // If this is a drone on the player's team
                if (self.body && self.body.gameObject && self.body.bodyFlags.HasFlag(CharacterBody.BodyFlags.Mechanical) && self.body.teamComponent.teamIndex == TeamIndex.Player)
                {
                    foreach (AISkillDriver driver in self.skillDrivers)
                    {
                        switch (driver.customName)
                        {
                            case "HardLeashToLeader":
                                driver.minDistance = droneMaxDistance;
                                break;
                            case "SoftLeashAttack":
                                driver.minDistance = droneMaxDistance;
                                break;
                            case "SoftLeashToLeader":
                                driver.minDistance = droneMaxDistance;
                                break;
                            case "StrafeNearbyEnemies":
                                driver.maxDistance = droneMaxDistance;
                                break;
                            case "ChaseFarEnemies":
                                driver.maxDistance = droneMaxDistance;
                                break;
                            default:
                                break;
                        }
                    }
                }
            };
        }


        // Currently not used
        private static void Hooks1()
        {
            // Whenever a drone is spawned, add the driver to it
            
            On.RoR2.CharacterMaster.OnBodyStart += (orig, self, body) =>
            {
                orig(self, body);

                // No null reference errors today
                if (!self || !self.gameObject || !body)
                    return;

                if (self.teamIndex == TeamIndex.Player && body.bodyFlags.HasFlag(CharacterBody.BodyFlags.Mechanical))
                {
                    AddAttackDriver(self.gameObject);
                }
            };

            // Taken from BetterDrones teehee
            // Each drone runs this code when an enemy is pinged
            On.RoR2.CharacterAI.BaseAI.UpdateTargets += (orig, self) =>
            {
                orig(self);

                if (NetworkServer.active)
                {
                    if (self.master && self.master.minionOwnership && self.body.bodyFlags.HasFlag(CharacterBody.BodyFlags.Mechanical))
                    {
                        if (self.master.minionOwnership.ownerMaster)
                        {
                            CharacterMaster owner = self.master.minionOwnership.ownerMaster;
                            if (owner.playerCharacterMasterController && owner.playerCharacterMasterController.pingerController)
                            {
                                PingerController controller = owner.playerCharacterMasterController.pingerController;
                                if (controller.currentPing.active && controller.currentPing.targetGameObject)
                                {
                                    if (controller.currentPing.targetGameObject.GetComponent<CharacterBody>() && controller.currentPing.targetGameObject.GetComponent<CharacterBody>().teamComponent.teamIndex != TeamIndex.Player)
                                    {
                                        // Set current enemy
                                        self.currentEnemy.gameObject = controller.currentPing.targetGameObject;
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }
        // Currently not used
        private static void AddAttackDriver(GameObject droneObject)
        {
            attackDriver = droneObject.AddComponent<AISkillDriver>();
            attackDriver = new AISkillDriver();
            attackDriver.customName = "AttackPingedEnemy";
            attackDriver.skillSlot = SkillSlot.None;
            attackDriver.requireSkillReady = false;
            attackDriver.maxDistance = 3000f;
            attackDriver.minDistance = 0f;
            attackDriver.movementType = AISkillDriver.MovementType.ChaseMoveTarget;
            attackDriver.moveTargetType = AISkillDriver.TargetType.CurrentEnemy;
            attackDriver.aimType = AISkillDriver.AimType.AtCurrentEnemy;
            attackDriver.buttonPressType = AISkillDriver.ButtonPressType.Hold;
            attackDriver.ignoreNodeGraph = false;

            Log.Debug("Added attack driver to drone");
        }

        public static void DebugSpawnCharacter(GameObject characterMasterPrefab, Vector3 position, Quaternion rotation, GameObject summonerBody)
        {
            if (characterMasterPrefab == LegacyResourcesAPI.Load<GameObject>("prefabs/characterMasters/Drone1Master"))
            {
                // Have to spawn the drone interactable, cause spawning the drone directly messes up the AI and I dont know how to fix it rn
                // Give player money so they can buy the drone
                PlayerCharacterMasterController.instances[0].master.GiveMoney(1000);

                characterMasterPrefab.GetComponent<SpawnCard>();

                InteractableSpawnCard isc = Object.Instantiate(LegacyResourcesAPI.Load<InteractableSpawnCard>("SpawnCards/InteractableSpawnCard/iscBrokenDrone1"));

                DirectorPlacementRule placementRule = new DirectorPlacementRule();
                placementRule.placementMode = DirectorPlacementRule.PlacementMode.Direct;
                placementRule.position = position;
                placementRule.minDistance = 0f;
                placementRule.maxDistance = 0f;

                DirectorSpawnRequest request = new DirectorSpawnRequest(isc, placementRule, RoR2Application.rng);
                request.ignoreTeamMemberLimit = true;
                request.teamIndexOverride = TeamIndex.Player;

                GameObject spawnedDrone = DirectorCore.instance.TrySpawnObject(request);

                if (spawnedDrone)
                {
                    Log.Debug("Spawned drone interactable");
                }
            }
            else if (characterMasterPrefab == LegacyResourcesAPI.Load<GameObject>("prefabs/characterMasters/GreaterWispMaster"))
            {
                MasterSummon summon = new MasterSummon();
                summon.masterPrefab = characterMasterPrefab;
                summon.position = position;
                summon.rotation = rotation;
                summon.summonerBodyObject = summonerBody;
                summon.ignoreTeamMemberLimit = true;
                summon.useAmbientLevel = true;

                CharacterMaster characterMaster = summon.Perform();

                if (characterMaster)
                {
                    Log.Debug("Wisp spawned");
                }
                
            }
            else
            {
                Log.Debug("Failed to load character master prefab");
            }
        }



        public static void Debug(string option)
        {
            // This if statement checks if the player has currently pressed F5.
            if (option == "F5")
            {
                Log.Debug($"Player pressed F5.");

                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;
                GameObject droneMasterPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/characterMasters/Drone1Master");

                DebugSpawnCharacter(droneMasterPrefab, transform.position + new Vector3(0f, 3f, 0f), transform.rotation, PlayerCharacterMasterController.instances[0].master.gameObject);
            }
            // This if statement checks if the player has currently pressed F6.
            if (option == "F6")
            {
                Log.Debug($"Player pressed F6.");

                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;
                GameObject wispMasterPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/characterMasters/GreaterWispMaster");
                Camera playerCam = CameraRigController.readOnlyInstancesList[0].sceneCam;

                DebugSpawnCharacter(wispMasterPrefab, transform.position + playerCam.transform.forward * 100, transform.rotation, PlayerCharacterMasterController.instances[0].master.gameObject);
            }
        }
    }
}
