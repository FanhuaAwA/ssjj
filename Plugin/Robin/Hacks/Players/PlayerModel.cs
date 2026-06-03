using System.Collections.Generic;
using System.Reflection;
using Plugins.Unity.Extension;
using Plugins.Utils;
using UnityEngine;

namespace Plugins.Hacks.Players
{
    [Obfuscation(Feature = "Virtualization", Exclude = false)]
    public class PlayerModel
    {
        public bool IsValid => this.IsBonesCached && this.root != null;

        public PlayerModel(Transform root)
        {
            this.root = root;
        }

        private void CacheBones1()
        {
            this.bip01 = this.root.FindChildDeep("Bip01");
            this.spine = this.bip01.FindChildDeep("Bip01_Spine");
            this.neck = this.bip01.FindChildDeep("Bip01_Neck");
            this.d_head = this.bip01.FindChildDeep("Bip01_Head");
            if (this.d_head != null)
            {
                if (this.d_head.childCount == 0)
                {
                    GameObject gameObject = new GameObject("fake_u_head");
                    gameObject.transform.SetParent(this.d_head, false);
                    this.u_head = gameObject.transform;
                    this.u_head.localPosition = new Vector3(-21.7f, 0f, 0f);
                    return;
                }
                this.u_head = this.d_head.GetChild(0);
            }
        }

        private void CacheBones2()
        {
            this.l_clavicle = this.bip01.FindChildDeep("Bip01_L_Clavicle");
            this.r_clavicle = this.bip01.FindChildDeep("Bip01_R_Clavicle");
            this.l_upperarm = this.bip01.FindChildDeep("Bip01_L_UpperArm");
            this.r_upperarm = this.bip01.FindChildDeep("Bip01_R_UpperArm");
            this.l_forearm = this.bip01.FindChildDeep("Bip01_L_Forearm");
            this.r_forearm = this.bip01.FindChildDeep("Bip01_R_Forearm");
            this.l_hand = this.bip01.FindChildDeep("Bip01_L_Hand");
            this.r_hand = this.bip01.FindChildDeep("Bip01_R_Hand");
            this.pelvis = this.bip01.FindChildDeep("Bip01_Pelvis");
            this.l_thigh = this.bip01.FindChildDeep("Bip01_L_Thigh");
            this.r_thigh = this.bip01.FindChildDeep("Bip01_R_Thigh");
            this.l_calf = this.bip01.FindChildDeep("Bip01_L_Calf");
            this.r_calf = this.bip01.FindChildDeep("Bip01_R_Calf");
            this.l_foot = this.bip01.FindChildDeep("Bip01_L_Foot");
            this.r_foot = this.bip01.FindChildDeep("Bip01_R_Foot");
        }

        private void CacheBones3()
        {
            if (this.spine != null && this.r_hand == null)
            {
                this.r_hand = this.r_forearm;
            }
            if (this.spine != null && this.d_head == null && this.u_head == null)
            {
                this.d_head = this.u_head = this.neck;
            }
            if (this.spine == null && this.d_head != null && this.pelvis != null)
            {
                this.neck = this.spine = this.d_head;
                this.l_clavicle = this.r_clavicle = this.l_upperarm = this.r_upperarm = this.l_forearm = this.r_forearm = this.l_hand = this.r_hand = this.l_thigh = this.r_thigh = this.l_calf = this.r_calf = this.l_foot = this.r_foot = this.pelvis;
            }
            if (this.root == null || this.bip01 == null || this.spine == null || this.neck == null || this.d_head == null || this.u_head == null || this.l_clavicle == null || this.r_clavicle == null || this.l_upperarm == null || this.r_upperarm == null || this.l_forearm == null || this.r_forearm == null || this.l_hand == null || this.r_hand == null || this.pelvis == null || this.l_thigh == null || this.r_thigh == null || this.l_calf == null || this.r_calf == null || this.l_foot == null || this.r_foot == null)
            {
                this.CacheBones4();
                this.IsBonesCached = false;
                return;
            }
            this.IsBonesCached = true;
        }

        private void CacheBones4()
        {
            string text = "";
            foreach (Transform transform in this.bip01.GetComponentsInChildren<Transform>())
            {
                text = text + transform.GetHierarchyPath() + "\n";
            }
        }

        public void CacheBones()
        {
            this.CacheBones1();
            this.CacheBones2();
            this.CacheBones3();
        }

        public Rectangle GetRect()
        {
            if (!this.IsValid)
            {
                return default;
            }
            Vector3 uiposition = this.root.GetUIPosition();
            Vector3 uiposition2 = this.u_head.GetUIPosition();
            if (uiposition.z <= 0f || uiposition2.z <= 0f)
            {
                return default;
            }
            float num = (uiposition.x + uiposition2.x) * 0.5f;
            float num2 = (uiposition.y + uiposition2.y) * 0.5f;
            float num3 = uiposition2.y - uiposition.y;
            float num4 = num3 * 0.4f;
            return new Rectangle(num, num2, num4, num3);
        }

        private void AddPoint(List<Circle> lst, Transform t)
        {
            Vector3 vector = (t.GetUIPosition() + this.u_head.GetUIPosition()) * 0.5f;
            float num = (this.u_head.GetUIPosition().x - t.GetUIPosition().x) * 1.2f;
            if (vector.z > 0f)
            {
                lst.Add(new Circle(vector, num));
            }
        }

        public List<Circle> GetPoints()
        {
            List<Circle> list = new List<Circle>();
            if (!this.IsValid)
            {
                return list;
            }
            this.AddPoint(list, this.d_head);
            return list;
        }

        private void AddLine(List<LineSegment> lst, Transform t1, Transform t2)
        {
            Vector3 uiposition = t1.GetUIPosition();
            Vector3 uiposition2 = t2.GetUIPosition();
            if (uiposition.z > 0f && uiposition2.z > 0f)
            {
                lst.Add(new LineSegment(uiposition, uiposition2));
            }
        }

        public List<LineSegment> GetBoneLines()
        {
            List<LineSegment> list = new List<LineSegment>();
            if (!this.IsValid)
            {
                return list;
            }
            this.AddLine(list, this.pelvis, this.spine);
            this.AddLine(list, this.spine, this.neck);
            this.AddLine(list, this.neck, this.l_clavicle);
            this.AddLine(list, this.neck, this.r_clavicle);
            this.AddLine(list, this.l_clavicle, this.l_upperarm);
            this.AddLine(list, this.r_clavicle, this.r_upperarm);
            this.AddLine(list, this.l_upperarm, this.l_forearm);
            this.AddLine(list, this.r_upperarm, this.r_forearm);
            this.AddLine(list, this.l_forearm, this.l_hand);
            this.AddLine(list, this.r_forearm, this.r_hand);
            this.AddLine(list, this.pelvis, this.l_thigh);
            this.AddLine(list, this.pelvis, this.r_thigh);
            this.AddLine(list, this.l_thigh, this.l_calf);
            this.AddLine(list, this.r_thigh, this.r_calf);
            this.AddLine(list, this.l_calf, this.l_foot);
            this.AddLine(list, this.r_calf, this.r_foot);
            return list;
        }

        public Transform root;
        public Transform bip01;
        public Transform spine;
        public Transform neck;
        public Transform u_head;
        public Transform d_head;
        public Transform l_clavicle;
        public Transform r_clavicle;
        public Transform l_upperarm;
        public Transform r_upperarm;
        public Transform l_forearm;
        public Transform r_forearm;
        public Transform l_hand;
        public Transform r_hand;
        public Transform pelvis;
        public Transform l_thigh;
        public Transform r_thigh;
        public Transform l_calf;
        public Transform r_calf;
        public Transform l_foot;
        public Transform r_foot;
        public bool IsBonesCached;
    }
}