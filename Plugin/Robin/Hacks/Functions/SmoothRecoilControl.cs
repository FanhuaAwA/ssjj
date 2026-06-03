using System.Reflection;
using Assets.Sources.Free.Data;
using Plugins.Unity;
using UnityEngine;

namespace Plugins.Hacks.Functions
{
    [Obfuscation(Feature = "Virtualization", Exclude = false)]
    public class SmoothRecoilControl : ModuleBase
    {
        public override void Update()
        {
            if (Menu.RecoilControl)
            {
                float punchPitch = Contexts.sharedInstance.player.myPlayerEntity.GetPunchPitch();
                float punchYaw = Contexts.sharedInstance.player.myPlayerEntity.GetPunchYaw();
                Contexts.sharedInstance.userCommand.input.Pitch -= 2f * (punchPitch - this.vector.x);
                Contexts.sharedInstance.userCommand.input.Yaw -= 2f * (punchYaw - this.vector.y);
                Camera.main.transform.Rotate(-this.vector.x - GameModelLocator.GetInstance().GameModel.ShakeAngleOffect.y, -this.vector.y - GameModelLocator.GetInstance().GameModel.ShakeAngleOffect.x, 0f);
                this.vector.x = punchPitch;
                this.vector.y = punchYaw;
            }
        }

        private Vector2 vector;
    }
}