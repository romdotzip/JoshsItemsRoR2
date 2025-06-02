using BepInEx;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using R2API;
using RoR2;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace JoshsItems
{
    internal static class JoshsEquipment
    {
        private static EquipmentDef joshsWatchRepairKit;
        // Times the equipment is used, needed for equipment function
        private static int timesUsed;

        public static void Init()
        {
            GenerateEquipment();
        }

        private static void GenerateEquipment()
        {
            // First let's define our item
            joshsWatchRepairKit = ScriptableObject.CreateInstance<EquipmentDef>();

            // Language Tokens, explained there https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Localization/
            joshsWatchRepairKit.name = "JoshsWatchRepairKit";
            joshsWatchRepairKit.nameToken = "EQUIPMENT_JOSHSWATCHREPAIRKIT_NAME";
            joshsWatchRepairKit.pickupToken = "EQUIPMENT_JOSHSWATCHREPAIRKIT_PICKUP";
            joshsWatchRepairKit.descriptionToken = "EQUIPMENT_JOSHSWATCHREPAIRKIT_DESC";
            joshsWatchRepairKit.loreToken = "EQUIPMENT_JOSHSWATCHREPAIRKIT_LORE";

            AssetBundle assets = AssetBundle.LoadFromFile(Assembly.GetExecutingAssembly().Location.Replace("JoshsItems.dll", "joshsitems"));

            // Load assets in asset bundle
            GameObject repairKitModel = assets.LoadAsset<GameObject>("watch repair kit2.prefab");
            //repairKitModel.transform.localScale = new Vector3(2, 2, 2);

            joshsWatchRepairKit.pickupIconSprite = assets.LoadAsset<Sprite>("watch repair kit icon2.png");
            //joshsWatchRepairKit.pickupModelPrefab = assets.LoadAsset<GameObject>("watch repair kit2.prefab");
            joshsWatchRepairKit.pickupModelPrefab = repairKitModel;

            joshsWatchRepairKit.equipmentIndex = EquipmentIndex.None;
            joshsWatchRepairKit.requiredExpansion = RoR2.ExpansionManagement.ExpansionCatalog.expansionDefs.FirstOrDefault(def => def.nameToken == "DLC1_NAME");
            joshsWatchRepairKit.unlockableDef = null;
            // This makes it drop-able in game, and adds a logbook entry
            joshsWatchRepairKit.canDrop = true;

            joshsWatchRepairKit.cooldown = 100;

            // ----------------------- TODO ----------------------- https://thunderstore.io/package/KingEnderBrine/ItemDisplayPlacementHelper/
            var displayRules = new ItemDisplayRuleDict(null);

            // Then finally add it to R2API
            ItemAPI.Add(new CustomEquipment(joshsWatchRepairKit, displayRules));

            LanguageAPI.Add(joshsWatchRepairKit.nameToken, "Josh's Watch Repair Kit");
            LanguageAPI.Add(joshsWatchRepairKit.pickupToken, "<style=cIsHealing>Restores</style> 1 broken <style=cIsUtility>Delicate Watch</style> on use. Cooldown <style=cIsDamage>increases</style> after 6 uses.");
            LanguageAPI.Add(joshsWatchRepairKit.descriptionToken, "<style=cIsHealing>Restores</style> 1 broken <style=cIsUtility>Delicate Watch</style> on use. After 6 uses, the equipment cooldown <style=cIsDamage>increases</style> by <style=cStack>30%</style>.");
            LanguageAPI.Add(joshsWatchRepairKit.loreToken, "<style=cMono>Friday-1500. Titanic Plains.</style>\r\n\r\nJ - Is that- a watch printer?\r\n\r\nM - Doooon't dude you're throwing the run just like last time!\r\n\r\nJ - Oh yeahhhh we're goin' all in.\r\n\r\n\r\n<style=cMono>Friday-1600. Siren's Call.</style>\r\n\r\n<style=cIsDamage>*CRACK*</style>.\r\n\r\nM - You just haaaaaad to get them didn't you.\r\n\r\nJ - Bad game bad game bad game bad game bad game bad game.......");

            // Call all necessary hooks
            Hooks();

            // Set some variables needed for equipment function
            SetEquipmentVariables();
        }

        private static void SetEquipmentVariables()
        {
            timesUsed = 0;
        }
        private static void Hooks()
        {
            // Hook onto RoR2 equipment action event (this equipment will now call its own code when the game detects an equipment being used)
            On.RoR2.EquipmentSlot.PerformEquipmentAction += (orig, self, equipmentDef) =>
            {
                if (equipmentDef == joshsWatchRepairKit)
                    return ActivateEquipment(self);
                return orig(self, equipmentDef);
            };
        }

        // What happens when the equipment is used
        private static bool ActivateEquipment(EquipmentSlot self)
        {
            Inventory inventory = self.characterBody?.inventory;
            if (!inventory)
            {
                return false;
            }

            ItemIndex watchIndex = DLC1Content.Items.FragileDamageBonus._itemIndex;
            ItemIndex consumedWatchIndex = DLC1Content.Items.FragileDamageBonusConsumed._itemIndex;
            PickupIndex pickupIndex = PickupCatalog.FindPickupIndex(watchIndex);

            int consumedWatchCount = inventory.GetItemCount(consumedWatchIndex);
            if (consumedWatchCount > 0)
            {
                timesUsed += 1;

                inventory.RemoveItem(consumedWatchIndex, 1);
                inventory.GiveItem(watchIndex, 1);
                GenericPickupController.SendPickupMessage(PlayerCharacterMasterController.instances[0].master, pickupIndex);

                //display delicate watch pickup message

                if (timesUsed % 5 == 0)
                {
                    float mult = 1.3F;
                    joshsWatchRepairKit.cooldown *= mult;
                }
            }
            else
            {
                return false;
            }
            return true;

        }

        public static void Debug(string option)
        {
            // This if statement checks if the player has currently pressed F2.
            if (option == "F2")
            {
                // Get the player body to use a position:
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;

                // And then drop our defined item in front of the player.

                Log.Info($"Player pressed F2. Spawning our custom item at coordinates {transform.position}");
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(JoshsEquipment.joshsWatchRepairKit.equipmentIndex), transform.position, transform.forward * 20f);
            }

            // This if statement checks if the player has currently pressed F3.
            if (option == "F3")
            {
                ItemIndex itemIndex = DLC1Content.Items.FragileDamageBonusConsumed._itemIndex;

                Log.Info($"Player pressed F3. Adding a Delicate Watch to the players inventory.");
                Inventory inventory = PlayerCharacterMasterController.instances[0].master.inventory;
                inventory.GiveItem(itemIndex, 1);
            }

            // This if statement checks if the player has currently pressed F4.
            if (option == "F4")
            {
                // Get the player body to use a position:
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;

                Log.Info($"Player pressed F4.");
                Inventory inventory = PlayerCharacterMasterController.instances[0].master.inventory;
                EquipmentIndex itemIndex = RoR2Content.Equipment.Blackhole._equipmentIndex;
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(itemIndex), transform.position, transform.forward * 20f);
                itemIndex = RoR2Content.Equipment.Cleanse._equipmentIndex;
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(itemIndex), transform.position, transform.forward * 20f);
                itemIndex = RoR2Content.Equipment.DroneBackup._equipmentIndex;
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(itemIndex), transform.position, transform.forward * 20f);
            }
        }
    }
}
