using System.Collections.Generic;
using System.Reflection;
using Assets.Sources.Framework;
using Assets.Sources.Framework.System;
using Assets.Sources.Modules.SceneObject.SceneWeapon;
using Plugins.ReplaceMethod;
using Plugins.Unity;
using Plugins.Unity.Extension;

namespace Plugins.Hacks.Functions
{
    [Obfuscation(Feature = "Virtualization", Exclude = false)]
    public class PlayBackSystem : ModuleBase
    {
        public override void Update()
        {
            Set_c4_Icon_playbackSystem();
        }

        private void Set_c4_Icon_playbackSystem()
        {
            PlaybackSystem playbackSystem = GameModuleFeature.Instance.GetFieldValue<PlaybackSystem>("_playbackSystem");
            List<IPlaybackSystem> systems = playbackSystem.GetFieldValue<List<IPlaybackSystem>>("_systems");
            int systemIndex = systems.FindIndex(s => s.GetType().Name == typeof(SceneWeaponSmallMapImageSystem).Name);
            if (systemIndex < 0)
            {
                systemIndex = systems.FindIndex(s => s.GetType().Name == typeof(FakeSceneWeaponSmallMapImageSystem).Name);
            }
            if (systemIndex >= 0)
            {
                systems[systemIndex] = Menu.c4Position
                    ? new FakeSceneWeaponSmallMapImageSystem(Contexts.sharedInstance)
                    : (IPlaybackSystem)new SceneWeaponSmallMapImageSystem(Contexts.sharedInstance);
            }
        }
    }
}