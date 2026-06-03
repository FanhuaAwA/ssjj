using System.Reflection;

namespace Plugins.Utils
{
    [Obfuscation(Feature = "Virtualization", Exclude = false)]
    public class GameManager
    {
        public static bool IsPlayerInInvincibleState(PlayerEntity player)
        {
            return player.HasState(1);
        }

        public static bool IsGameActive => GameController.Instance.InitFinish;
    }
}