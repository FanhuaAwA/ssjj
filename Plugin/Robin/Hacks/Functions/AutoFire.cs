using System.Reflection;
using Assets.Sources.Utils.Weapon;
using Plugins.Hacks.Players;
using Plugins.Unity;
using share;
using UnityEngine;

namespace Plugins.Hacks.Functions
{
    [Obfuscation(Feature = "Virtualization", Exclude = false)]
    public class AutoFire : ModuleBase
    {
        public PlayerCollector collector => ModuleBase.GetPlugin<PlayerCollector>();

        public override void Update()
        {
            if (!Contexts.sharedInstance.player.myPlayerEntity.IsDead() && Menu.AutoFire)
            {
                Vector3 vector = SSJJMath.VectorCoordConverter.UnityToSsjj(Camera.main.transform.forward);
                int entityId = FireUtility.BulletTrace(
    Contexts.sharedInstance.battleRoom.pyEngine.PyEngine,
    Contexts.sharedInstance.player.myPlayerEntity,
    Contexts.sharedInstance.player,
    10000000f,
    new Vector3D(vector.x, vector.y, vector.z),
    new float[3],
    new float[3],
    false
).EntityId;
                if (entityId > 0)
                {
                    foreach (Player player in this.collector.players)
                    {
                        if (player.entity.GetId() == entityId)
                        {
                            if (player.entity.GetTeam() != Contexts.sharedInstance.player.myPlayerEntity.GetTeam() && !player.entity.IsDead())
                            {
                                MouseSimulater.ForceMouse(0, MouseSimulater.InputST.TrueOnce);
                                break;
                            }
                            break;
                        }
                    }
                }
            }
        }
    }
}