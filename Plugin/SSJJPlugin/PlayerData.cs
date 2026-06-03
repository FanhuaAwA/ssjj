using UnityEngine;

namespace UnityEngine.Components
{
    public class EntityInfo
    {
        public object Entity;
        public Transform Root;
        public Transform Head;
        public Transform Spine;
        public Transform Pelvis;
        public string Name;
        public int Team;
        public float HP;
        public float MaxHP;
        public bool IsDead;
        public Vector3 Position;
        public Vector3 HeadPosition;
    }
}
