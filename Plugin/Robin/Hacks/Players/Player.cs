using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Plugins.Hacks.Players
{
    [Obfuscation(Feature = "Virtualization", Exclude = false)]
    public class Player
    {
        public bool HasC4 => this.entity.basicInfo.Current.HasC4;
        public int WeaponLevel => this.entity.currentWeapon.Weapon;
        public string Weapon => this.entity.currentWeapon.WeaponInfo.StringName;
        public long TeamId => this.entity.GetTeam();
        public bool IsDead => this.entity.IsDead();
        public float Hp => entity.basicInfo.Current.Hp;
        public float HpMax => entity.basicInfo.Current.MaxHp;
        public float HpRatio => this.HpMax != 0f ? this.Hp / this.HpMax : 0f;
        public bool IsValid => this.model.IsValid && this.entity.hasBasicInfo;

        public PlayerEntity PlayerEntity => this.entity;

        public string CleanName => name.Replace("[", "").Replace("]", "");

        private PlayerEntity GetEntity(string name)
        {
            foreach (PlayerEntity entity in Contexts.sharedInstance.player.GetEntities().Cast<PlayerEntity>())
            {
                if (entity != null)
                {
                    if (entity.basicInfo.PlayerName == name)
                    {
                        return entity;
                    }
                }
            }
            return null;
        }

        public Player(Transform root)
        {
            this.entity = this.GetEntity(root.name);
            this.name = root.name;
            this.model = new PlayerModel(root);
            this.model.CacheBones();
        }

        public PlayerEntity entity;
        public PlayerModel model;
        public string name;
    }
}