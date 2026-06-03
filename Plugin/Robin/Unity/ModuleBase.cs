using System.Reflection;
using Plugins.Init;
using UnityEngine;

namespace Plugins.Unity
{
    [Obfuscation(Feature = "Virtualization", Exclude = false)]
    public class ModuleBase : MonoBehaviour
    {
        public virtual void Awake()
        {
        }

        public virtual void Start()
        {
        }

        public virtual void Update()
        {
        }

        public virtual void FixedUpdate()
        {
        }

        public virtual void LateUpdate()
        {
        }

        public virtual void OnGUI()
        {
        }

        public virtual void OnDestroy()
        {
        }

        public static T GetPlugin<T>() where T : ModuleBase
        {
            return Loop.GetPlugin<T>();
        }
    }
}