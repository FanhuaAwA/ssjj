using System.Collections.Generic;
using System.Reflection;
using Plugins.Unity;
using Plugins.Utils;
using UnityEngine;

namespace Plugins.Hacks.Players
{
    [Obfuscation(Feature = "Virtualization", Exclude = false)]
    public class PlayerCollector : ModuleBase
    {
        public override void Update()
        {
            if (GameManager.IsGameActive)
            {
                this.Collect();
            }
        }

        private void Collect()
        {
            if (!this.rootGo)
            {
                this.rootGo = GameObject.Find("thirdPersonResources");
            }
            if (this.rootGo)
            {
                Transform transform = this.rootGo.transform; for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i); if (child.gameObject.activeInHierarchy && !this.players.Exists((Player player) => player.model.root == child))
                    {
                        this.players.Add(new Player(child));
                    }
                }
                _ = this.players.RemoveAll((Player player) => !player.IsValid);
            }
        }

        public PlayerCollector()
        {
            this.players = new List<Player>();
        }

        private GameObject rootGo;
        public List<Player> players;
    }
}