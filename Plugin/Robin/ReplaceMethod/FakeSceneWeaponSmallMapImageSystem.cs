using System.Reflection;
using Assets.Sources.Components.Common;
using Assets.Sources.Framework;
using Entitas;
using NetData;
using SSJJAsset.Asset;
using UnityEngine;

namespace Plugins.ReplaceMethod
{
    [Obfuscation(Feature = "Virtualization", Exclude = false)]
    public class FakeSceneWeaponSmallMapImageSystem : IPlaybackSystem
    {
        private static IGroup<SceneObjectEntity> _group; private static GameRuleContext _gameRuleContext;

        public FakeSceneWeaponSmallMapImageSystem(Contexts contexts)
        {
            _group = contexts.sceneObject.GetGroup(SceneObjectMatcher.SceneWeapon); _gameRuleContext = contexts.gameRule;
        }

        public void OnPlayback()
        {
            foreach (SceneObjectEntity sceneObjectEntity in _group)
            {
                Contexts.sharedInstance.player.myPlayerEntity.basicInfo.Current.TeamId = 1; if (!sceneObjectEntity.hasSmallMapImage)
                {
                    sceneObjectEntity.AddComponent(13, new SmallMapImageComponent());
                }
                SceneWeaponEntityData current = sceneObjectEntity.sceneWeapon.Current; sceneObjectEntity.smallMapImage.Position = new Vector3(current.X, current.Y, current.Z); sceneObjectEntity.smallMapImage.ScaleX = 1f; sceneObjectEntity.smallMapImage.ScaleY = 1f; sceneObjectEntity.smallMapImage.Alpha = 1f; AssetInfo assetInfo = default; if (current.WeaponName == "c4")
                {
                    assetInfo = _gameRuleContext.hasC4State && _gameRuleContext.c4State.Active ? _c4iconAsset : _c4dropiconAsset;
                }
                sceneObjectEntity.smallMapImage.SmallMapImage = assetInfo; sceneObjectEntity.smallMapImage.RadarImage = assetInfo;
            }
        }

        private static AssetInfo _c4iconAsset = new AssetInfo("ui/blastmodel", "Assets/Res/Assets/ui/GameGUIRes/blastModel/map/c4icon.png");
        private static AssetInfo _c4dropiconAsset = new AssetInfo("ui/blastmodel", "Assets/Res/Assets/ui/GameGUIRes/blastModel/map/c4dropicon.png");
    }
}