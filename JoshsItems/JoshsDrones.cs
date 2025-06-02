using IL.RoR2.CharacterAI;
using R2API;
using RoR2;
using RoR2.CharacterAI;
using RoR2.UI;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace JoshsItems
{
    internal static class JoshsDrones
    {
        private static float droneMaxDistance;
        public static void Init()
        {
            // Hi
            OverrideDroneBehavior();

            // Set variables
            droneMaxDistance = 100000;
        }

        private static void OverrideDroneBehavior()
        {
            // Call necessary hooks for JoshsDrones
            Hooks();
        }

        private static void Hooks()
        {

            // ----------------------------------
            // ----------------------------------
            // ----------------------------------
            // ----------------------------------
            // CREATE CUSTOM AI SKILL DRIVER AND SET THE CURRENT TARGET FOR EACH DRONE WHEN AN ENEMY IS PINGED, THAT WAY WE CAN SET MAX DISTANCE FOR JUST ONE SKILL DRIVER
            // ----------------------------------
            // ----------------------------------
            // ----------------------------------
            // ----------------------------------


            On.RoR2.CharacterAI.BaseAI.ManagedFixedUpdate += (orig, self, deltaTime) =>
            {
                // Run original function
                orig(self, deltaTime);

                // If this is a drone on the player's team
                if (self.body && self.body.gameObject && self.body.bodyFlags.HasFlag(CharacterBody.BodyFlags.Mechanical) && self.body.teamComponent.teamIndex == TeamIndex.Player)
                {
                    foreach (RoR2.CharacterAI.AISkillDriver driver in self.skillDrivers)
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

        public static void DebugSpawnDrone(Vector3 position, GameObject summonerBody)
        {
            GameObject droneMasterPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/characterMasters/Drone1Master");

            if (droneMasterPrefab)
            {
                MasterSummon summon = new MasterSummon
                {
                    masterPrefab = droneMasterPrefab,
                    position = position,
                    rotation = Quaternion.identity,
                    summonerBodyObject = summonerBody,
                    teamIndexOverride = TeamIndex.Player,
                    ignoreTeamMemberLimit = true
                };

                CharacterMaster droneMaster = summon.Perform();
                if (droneMaster)
                {
                    Log.Debug("Drone spawned.");
                }
            }
            else
            {
                Log.Debug("Failed to load gunner drone prefab.");
            }
        }

        public static void Debug(string option)
        {
            // This if statement checks if the player has currently pressed F5.
            if (option == "F5")
            {
                Log.Debug($"Player pressed F5.");
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;

                DebugSpawnDrone(transform.position, PlayerCharacterMasterController.instances[0].master.gameObject);
            }
        }
    }
}
