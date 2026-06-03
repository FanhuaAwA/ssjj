using System;
using System.Collections;
using System.Collections.Generic;
using SSJJPlugin.Systems;
using SSJJPlugin.Utils;
using UnityEngine;

namespace SSJJPlugin
{
    public class PluginController : MonoBehaviour
    {
        private Camera _cam;
        private object _contexts;
        private object _playerCtx;
        private object _thirdPersonRoot;
        private EspSystem _esp;
        private AimbotSystem _aim;
        private DebugSystem _debug;
        private MenuSystem _menu;
        private KeyBinder _keyBinder;
        private bool _initialized;
        private List<PlayerData> _players = new List<PlayerData>();

        private Vector2 _aimFovCenter;
        private float _aimFovRadius;
        private bool _aimHasTarget;
        private Vector2 _aimTargetScreen;
        private string _aimTargetName;
        private float _aimTargetDist;

        private void Start()
        {
            _cam = Camera.main;
            var ctxType = ReflectionHelper.FindType("Contexts");
            _contexts = ctxType != null ? ReflectionHelper.GetStatic(ctxType, "sharedInstance") : null;
            _playerCtx = _contexts != null ? ReflectionHelper.GetInstance(_contexts, "player") : null;

            _esp = new EspSystem();
            _aim = new AimbotSystem();
            _debug = new DebugSystem();
            _keyBinder = new KeyBinder();
            _menu = new MenuSystem(_esp, _aim, _debug, _keyBinder);
            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized) return;
            try
            {
                _cam = Camera.main;
                _keyBinder.CheckKeyBinding();

                if (Input.GetKeyDown(_keyBinder.MenuKey))
                    _menu.Visible = !_menu.Visible;

                RefreshPlayers();

                if (_aim.Enabled)
                {
                    _aim.Compute(_players, _cam, _keyBinder.AimKey,
                        _esp.Speed, _esp.Fov, _esp.BodyIndex,
                        _esp.RecoilControl, _esp.ShowFov, _esp.ShowAimLine,
                        out _aimHasTarget, out _aimTargetScreen, out _aimTargetName, out _aimTargetDist,
                        out _aimFovCenter, out _aimFovRadius);
                }
                else
                {
                    _aimHasTarget = false;
                }
            }
            catch (Exception e) { DebugSystem.LogError("Update", e); }
        }

        private void OnGUI()
        {
            if (!_initialized) return;
            try
            {
                if (_aim.Enabled && _esp.ShowFov && _cam != null)
                    DrawingHelper.DrawCircle(_aimFovCenter, _aimFovRadius, new Color(1, 0, 0, 0.3f));

                if (_aimHasTarget && _esp.ShowAimLine)
                {
                    var center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                    DrawingHelper.DrawLine(center, _aimTargetScreen, Color.magenta);
                }

                if (_cam != null)
                    _esp.Draw(_players, _cam);

                _menu.Draw();
                GUI.color = Color.white;
            }
            catch (Exception e) { DebugSystem.LogError("OnGUI", e); }
        }

        private void RefreshPlayers()
        {
            _players.Clear();
            if (_playerCtx == null || _cam == null) return;

            if (_thirdPersonRoot == null)
                _thirdPersonRoot = GameObject.Find("thirdPersonResources");

            var myPlayer = ReflectionHelper.GetInstance(_playerCtx, "myPlayerEntity");
            if (myPlayer == null) return;

            int myTeam = GetTeam(myPlayer);
            var method = ReflectionHelper.GetMethod(_playerCtx.GetType(), "GetEntities");
            if (method == null) return;
            var entities = method.Invoke(_playerCtx, null) as IEnumerable;
            if (entities == null) return;

            foreach (var entity in entities)
            {
                try
                {
                    if (IsMySelf(entity, myPlayer)) continue;
                    int team = GetTeam(entity);
                    if (team == myTeam && team != -1) continue;

                    string name = GetName(entity);
                    if (IsDead(entity)) continue;

                    Transform root = FindTransform(name);
                    Transform head = root != null ? FindBone(root, "Bip01_Head") : null;
                    Transform spine = root != null ? FindBone(root, "Bip01_Spine") : null;
                    Transform pelvis = root != null ? FindBone(root, "Bip01_Pelvis") : null;

                    Vector3 pos = root != null ? root.position : Vector3.zero;
                    Vector3 headPos = pos;
                    if (head != null) headPos = head.position;
                    else if (spine != null) headPos = spine.position + new Vector3(0, 0.5f, 0);
                    else headPos = pos + new Vector3(0, 1.7f, 0);

                    if (pos == Vector3.zero) continue;

                    _players.Add(new PlayerData
                    {
                        Entity = entity,
                        Root = root,
                        Head = head,
                        Spine = spine,
                        Pelvis = pelvis,
                        Name = name,
                        Team = team,
                        HP = GetHP(entity),
                        MaxHP = GetMaxHP(entity),
                        IsDead = false,
                        Position = pos,
                        HeadPosition = headPos
                    });
                }
                catch { }
            }
        }

        private string GetName(object entity)
        {
            try { var bi = ReflectionHelper.GetInstance(entity, "basicInfo"); return bi != null ? ReflectionHelper.GetString(bi, "PlayerName") : "?"; }
            catch { return "?"; }
        }

        private int GetTeam(object entity)
        {
            try { var m = ReflectionHelper.GetMethod(entity.GetType(), "GetTeam"); return m != null ? (int)m.Invoke(entity, null) : -1; }
            catch { return -1; }
        }

        private bool IsDead(object entity)
        {
            try { var m = ReflectionHelper.GetMethod(entity.GetType(), "IsDead"); return m != null && (bool)m.Invoke(entity, null); }
            catch { return false; }
        }

        private bool IsMySelf(object entity, object my)
        {
            try { var m = ReflectionHelper.GetMethod(entity.GetType(), "IsMySelf"); return m != null && (bool)m.Invoke(entity, null); }
            catch { return false; }
        }

        private float GetHP(object entity)
        {
            try
            {
                var bi = ReflectionHelper.GetInstance(entity, "basicInfo");
                var cur = bi != null ? ReflectionHelper.GetInstance(bi, "Current") : null;
                return cur != null ? ReflectionHelper.GetFloat(cur, "_hp") : 0;
            }
            catch { return 0; }
        }

        private float GetMaxHP(object entity)
        {
            try
            {
                var bi = ReflectionHelper.GetInstance(entity, "basicInfo");
                var cur = bi != null ? ReflectionHelper.GetInstance(bi, "Current") : null;
                return cur != null ? ReflectionHelper.GetFloat(cur, "_maxHp") : 0;
            }
            catch { return 0; }
        }

        private Transform FindTransform(string name)
        {
            if (_thirdPersonRoot == null) return null;
            var root = (_thirdPersonRoot as GameObject)?.transform;
            if (root == null) return null;
            for (int i = 0; i < root.childCount; i++)
                if (root.GetChild(i).name == name) return root.GetChild(i);
            return null;
        }

        private Transform FindBone(Transform root, string name)
        {
            if (root == null) return null;
            var t = root.Find(name);
            if (t != null) return t;
            return FindBoneRecursive(root, name);
        }

        private Transform FindBoneRecursive(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == name) return child;
                var found = FindBoneRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private static Vector3 SsjjToUnity(Vector3 ssjj)
        {
            return new Vector3(-ssjj.y, ssjj.z, ssjj.x);
        }
    }
}
