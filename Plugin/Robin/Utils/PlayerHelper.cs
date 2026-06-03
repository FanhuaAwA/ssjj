using System;
using System.Reflection;
using Assets.Sources.Constant;
using Entitas;
using share;
using UnityEngine;
using weapon;
namespace Plugins.Utils
{
    [Obfuscation(Feature = "Virtualization", Exclude = false)]
    public class PlayerHelper
    {
        public static bool ScanPlayer(PlayerEntity player)
        {
            Vector3D vector3D = ShootingDirUtils.CalculateShotingDir(0, (double)(player.GetViewYaw() + 2f * player.GetPunchYaw()), (double)(player.GetViewPitch() + 2f * player.GetPunchPitch()), 0.0, 0f, 0.0);
            Vector3 vector = new Vector3((float)vector3D.x, (float)vector3D.y);
            vector.Normalize();
            double cosAngle = GetCosAngle(player);
            foreach (PlayerEntity playerEntity in Contexts.sharedInstance.player.GetGroup(PlayerMatcher.AllOf(new IMatcher<PlayerEntity>[] { PlayerMatcher.Fpos })))
            {
                if (playerEntity.hasBasicInfo && playerEntity.entityId != player.entityId && playerEntity.GetTeam() != player.GetTeam() && !playerEntity.IsDead() && IsInRage(player.GetCompenstatePos(player.fpos.Change.GetPosIndex()), playerEntity.GetCompenstatePos(playerEntity.fpos.Change.GetPosIndex()), vector, cosAngle) && player.normalTrace.EntityId != -2)
                {
                    return true;
                }
            }
            return false;
        }
        private static bool IsInRage(Vector3 start, Vector3 end, Vector3 shotDir, double cosAngle)
        {
            if (Vector3.Distance(start, end) > 950f)
            {
                return false;
            }
            Vector3 vector = end - start;
            vector.z = 0f;
            vector.Normalize();
            return (double)Vector3.Dot(shotDir, vector) > cosAngle;
        }
        private static double GetCosAngle(PlayerEntity player)
        {
            if (RuleUtilty.IsPve(Contexts.sharedInstance.battleRoom.roomData.Data.RaceType))
            {
                return Math.Cos(0.24434609338641167);
            }
            return Math.Cos(0.12217304669320583);
        }
    }
}
