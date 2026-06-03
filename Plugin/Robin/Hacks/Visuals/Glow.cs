using System.Collections.Generic;
using System.Reflection;
using Assets.Sources.Constant;
using cakeslice;
using Entitas;
using Plugins.Unity;
using UnityEngine;

namespace Plugins.Hacks.Visuals
{
    [Obfuscation(Feature = "Virtualization", Exclude = false)]
    public class Glow : ModuleBase
    {
        public override void Update()
        {
            if (Menu.Glow && Menu.Esp)
            {
                Camera main = Camera.main;
                OutlineEffect outlineEffect = main.gameObject.GetComponent<OutlineEffect>() ?? main.gameObject.AddComponent<OutlineEffect>();
                outlineEffect.lineColor0 = Aimbot.targetPlayer != null ? Color.red : Color.green;
                outlineEffect.lineColor1 = Color.white;
                outlineEffect.lineColor2 = Color.white;
                outlineEffect.fillAmount = Menu.FillAmount;
                outlineEffect.additiveRendering = Menu.GlowIndex == 0;
                outlineEffect.lineThickness = Menu.Ness;

                using IGroup<PlayerEntity>.Enumator enumerator = Glow.Group.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    PlayerEntity playerEntity = enumerator.Current;
                    if (playerEntity.GetTeam() != Contexts.sharedInstance.player.cameraOwnerEntity.GetTeam())
                    {
                        if (!playerEntity.IsDead())
                        {
                            IEnumerable<SkinnedMeshRenderer> enumerable = (RuleUtilty.EnableAvater() ? playerEntity.thirdPersonUnityObjects.ThirdTran.BodyTransform.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>() : playerEntity.thirdPersonUnityObjects.CareerSkins.ToArray());
                            foreach (SkinnedMeshRenderer skinnedMeshRenderer in enumerable)
                            {
                                if (skinnedMeshRenderer.GetComponent<Outline>() == null)
                                {
                                    skinnedMeshRenderer.gameObject.AddComponent<Outline>();
                                }
                            }
                        }
                        else
                        {
                            foreach (PlayerEntity playerEntity2 in Glow.Group)
                            {
                                if (playerEntity2.IsDead())
                                {
                                    SkinnedMeshRenderer[] array = (RuleUtilty.EnableAvater() ? playerEntity2.thirdPersonUnityObjects.ThirdTran.BodyTransform.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>() : playerEntity2.thirdPersonUnityObjects.CareerSkins.ToArray());
                                    foreach (SkinnedMeshRenderer skinnedMeshRenderer2 in array)
                                    {
                                        Outline component = skinnedMeshRenderer2.GetComponent<Outline>();
                                        if (component != null)
                                        {
                                            Object.Destroy(component);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return;
            }

            foreach (PlayerEntity playerEntity2 in Glow.Group)
            {
                SkinnedMeshRenderer[] array = (RuleUtilty.EnableAvater() ? playerEntity2.thirdPersonUnityObjects.ThirdTran.BodyTransform.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>() : playerEntity2.thirdPersonUnityObjects.CareerSkins.ToArray());
                foreach (SkinnedMeshRenderer skinnedMeshRenderer2 in array)
                {
                    Outline component = skinnedMeshRenderer2.GetComponent<Outline>();
                    if (component != null)
                    {
                        Object.Destroy(component);
                    }
                }
            }
        }

        private static readonly IGroup<PlayerEntity> Group = Contexts.sharedInstance.player.GetGroup(PlayerMatcher.AllOf(new IMatcher<PlayerEntity>[] { PlayerMatcher.ThirdPersonUnityObjects }));
    }
}