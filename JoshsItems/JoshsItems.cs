using BepInEx;
using R2API;
using RoR2;
using UnityEngine;

namespace JoshsItems
{
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    public class JoshsItems : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "romdotzip";
        public const string PluginName = "JoshsItems";
        public const string PluginVersion = "1.0.0";

        public void Awake()
        {
            // Init our logging class so that we can properly log for debugging
            Log.Init(Logger);

            //JoshsEquipment.Init();
            JoshsDrones.Init();

            Logger.LogMessage("All items initialized");
        }

#if DEBUG
        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.F2))
            {
                JoshsEquipment.Debug("F2");
            }
            if (Input.GetKeyDown(KeyCode.F3))
            {
                JoshsEquipment.Debug("F3");
            }
            if (Input.GetKeyDown(KeyCode.F4))
            {
                JoshsEquipment.Debug("F4");
            }
            if (Input.GetKeyDown(KeyCode.F5))
            {
                JoshsDrones.Debug("F5");
            }
        }
#endif
    }
}
