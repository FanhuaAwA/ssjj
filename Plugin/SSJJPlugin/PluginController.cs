using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Components;

namespace UnityEngine.Components
{
    public class ComponentManager : MonoBehaviour
    {
        private Camera _cam;
        private object _contexts;
        private object _playerCtx;
        private object _ucCtx;
        private object _tpr;
        private CameraController _cc;
        private RenderHelper _rh;
        private DebugSystem _dbg;
        private MenuSystem _mn;
        private KeyBinder _kb;
        private bool _init;
        private List<EntityInfo> _entities = new List<EntityInfo>();

        private Vector2 _fovC;
        private float _fovR;
        private bool _ht;
        private Vector2 _ts;
        private string _tn;
        private float _td;

        // Cached reflection helpers
        private static readonly Dictionary<string, Type> _tc = new Dictionary<string, Type>();
        private static readonly Dictionary<string, FieldInfo> _fc = new Dictionary<string, FieldInfo>();
        private static readonly Dictionary<string, PropertyInfo> _pc = new Dictionary<string, PropertyInfo>();
        private static readonly Dictionary<string, MethodInfo> _mc = new Dictionary<string, MethodInfo>();

        private void Start()
        {
            _cam = Camera.main;
            _contexts = GS(FT("Contexts"), "sharedInstance");
            _playerCtx = GI(_contexts, "player");
            _ucCtx = GI(_contexts, "userCommand");

            _cc = new CameraController();
            _rh = new RenderHelper();
            _dbg = new DebugSystem();
            _kb = new KeyBinder();
            _mn = new MenuSystem(_rh, _cc, _dbg, _kb);
            _init = true;
        }

        private void Update()
        {
            if (!_init) return;
            try
            {
                _cam = Camera.main;
                _kb.CheckKeyBinding();

                if (Input.GetKeyDown(_kb.MenuKey))
                    _mn.Visible = !_mn.Visible;

                RefreshEntities();

                if (_cc.Enabled)
                {
                    _cc.Compute(_entities, _cam, _kb.AimKey,
                        _rh.Speed, _rh.Fov, _rh.BodyIndex,
                        _rh.RecoilControl, _rh.ShowFov, _rh.ShowAimLine,
                        out _ht, out _ts, out _tn, out _td,
                        out _fovC, out _fovR);
                }
                else { _ht = false; }
            }
            catch (Exception e) { DebugSystem.LogError("U", e); }
        }

        private void OnGUI()
        {
            if (!_init) return;
            try
            {
                if (_cc.Enabled && _rh.ShowFov && _cam != null)
                    DrawingHelper.DrawCircle(_fovC, _fovR, new Color(1, 0, 0, 0.3f));

                if (_ht && _rh.ShowAimLine)
                    DrawingHelper.DrawLine(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), _ts, Color.magenta);

                if (_cam != null) _rh.Draw(_entities, _cam);
                _mn.Draw();
                GUI.color = Color.white;
            }
            catch (Exception e) { DebugSystem.LogError("G", e); }
        }

        private void RefreshEntities()
        {
            _entities.Clear();
            if (_playerCtx == null || _cam == null) return;
            if (_tpr == null) _tpr = GameObject.Find("thirdPersonResources");

            var my = GI(_playerCtx, "myPlayerEntity");
            if (my == null) return;

            int myTeam = GetTeam(my);
            var method = GM(_playerCtx.GetType(), "GetEntities");
            if (method == null) return;
            var entities = method.Invoke(_playerCtx, null) as IEnumerable;
            if (entities == null) return;

            foreach (var entity in entities)
            {
                try
                {
                    if (IsSelf(entity, my)) continue;
                    int team = GetTeam(entity);
                    if (team == myTeam && team != -1) continue;
                    string name = GetName(entity);
                    if (IsDead(entity)) continue;

                        Transform root = FTr(name);
                        Transform head = root != null ? FB(root, "Bip01_Head") : null;
                        Transform spine = root != null ? FB(root, "Bip01_Spine") : null;
                        Transform pelvis = root != null ? FB(root, "Bip01_Pelvis") : null;

                    Vector3 pos = root != null ? root.position : Vector3.zero;
                    Vector3 headPos = head != null ? head.position :
                        spine != null ? spine.position + new Vector3(0, 0.5f, 0) :
                        pos + new Vector3(0, 1.7f, 0);

                    if (pos == Vector3.zero) continue;

                    _entities.Add(new EntityInfo
                    {
                        Entity = entity, Root = root, Head = head,
                        Spine = spine, Pelvis = pelvis, Name = name,
                        Team = team, HP = GetHP(entity), MaxHP = GetMaxHP(entity),
                        IsDead = false, Position = pos, HeadPosition = headPos
                    });
                }
                catch { }
            }
        }

        private string GetName(object e)
        {
            try { var bi = GI(e, "basicInfo"); return bi != null ? GI(bi, "PlayerName") as string ?? "?" : "?"; }
            catch { return "?"; }
        }

        private int GetTeam(object e)
        {
            try { var m = GM(e.GetType(), "GetTeam"); return m != null ? (int)m.Invoke(e, null) : -1; }
            catch { return -1; }
        }

        private bool IsDead(object e)
        {
            try { var m = GM(e.GetType(), "IsDead"); return m != null && (bool)m.Invoke(e, null); }
            catch { return false; }
        }

        private bool IsSelf(object e, object my)
        {
            try { var m = GM(e.GetType(), "IsMySelf"); return m != null && (bool)m.Invoke(e, null); }
            catch { return false; }
        }

        private float GetHP(object e)
        {
            try { var bi = GI(e, "basicInfo"); var cur = bi != null ? GI(bi, "Current") : null; return cur != null ? GF(cur, "_hp") : 0; }
            catch { return 0; }
        }

        private float GetMaxHP(object e)
        {
            try { var bi = GI(e, "basicInfo"); var cur = bi != null ? GI(bi, "Current") : null; return cur != null ? GF(cur, "_maxHp") : 0; }
            catch { return 0; }
        }

        private Transform FTr(string name)
        {
            if (_tpr == null) return null;
            var root = (_tpr as GameObject)?.transform;
            if (root == null) return null;
            for (int i = 0; i < root.childCount; i++)
                if (root.GetChild(i).name == name) return root.GetChild(i);
            return null;
        }

        private Transform FB(Transform root, string name)
        {
            if (root == null) return null;
            var t = root.Find(name); if (t != null) return t;
            return FBR(root, name);
        }

        private Transform FBR(Transform p, string name)
        {
            for (int i = 0; i < p.childCount; i++)
            {
                var c = p.GetChild(i);
                if (c.name == name) return c;
                var f = FBR(c, name);
                if (f != null) return f;
            }
            return null;
        }

        // ---- Reflection helpers ----
        public static Type FT(string name)
        {
            if (_tc.TryGetValue(name, out var v)) return v;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var t = a.GetType(name); if (t != null) { _tc[name] = t; return t; }
                    foreach (var t2 in a.GetTypes())
                        if (t2.Name == name || t2.FullName == name) { _tc[name] = t2; return t2; }
                }
                catch { }
            }
            return null;
        }

        public static object GS(Type t, string n)
        {
            if (t == null) return null;
            var f = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var fi = t.GetField(n, f); if (fi != null) return fi.GetValue(null);
            return t.GetProperty(n, f)?.GetValue(null);
        }

        public static object GI(object o, string n)
        {
            if (o == null) return null;
            var k = o.GetType().FullName + "." + n;
            FieldInfo fi; if (!_fc.TryGetValue(k, out fi)) { fi = o.GetType().GetField(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); _fc[k] = fi; }
            if (fi != null) return fi.GetValue(o);
            PropertyInfo pi; if (!_pc.TryGetValue(k, out pi)) { pi = o.GetType().GetProperty(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); _pc[k] = pi; }
            return pi?.GetValue(o);
        }

        public static MethodInfo GM(Type t, string n)
        {
            if (t == null) return null;
            var k = t.FullName + "." + n;
            MethodInfo mi; if (!_mc.TryGetValue(k, out mi)) { mi = t.GetMethod(n, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic); _mc[k] = mi; }
            return mi;
        }

        public static float GF(object o, string n)
        {
            try { var v = GI(o, n); return v != null ? Convert.ToSingle(v) : 0f; }
            catch { return 0f; }
        }

        public static void SF(object o, string n, object v)
        {
            if (o == null) return;
            var k = o.GetType().FullName + "." + n;
            FieldInfo fi; if (!_fc.TryGetValue(k, out fi)) { fi = o.GetType().GetField(n, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic); _fc[k] = fi; }
            if (fi != null) { fi.SetValue(o, v); return; }
            PropertyInfo pi; if (!_pc.TryGetValue(k, out pi)) { pi = o.GetType().GetProperty(n, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic); _pc[k] = pi; }
            if (pi != null && pi.CanWrite) pi.SetValue(o, v);
        }
    }
}
