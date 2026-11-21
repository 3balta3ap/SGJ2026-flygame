using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    [RequireComponent(typeof(MeshFilter))]
    public class SurfaceDisc : MonoBehaviour
    {
        [SerializeField] Planet planet;
        [SerializeField] float radius = 2.5f;
        [SerializeField] float lift = 0.02f;
        [SerializeField] int segments = 40;
        [SerializeField] int rings = 8;

        void Start() => Build();

        [ContextMenu("Build Mesh")]
        public void Build()
        {
            if (planet == null && GameContext.Current != null) planet = GameContext.Current.Planet;
            if (planet == null) return;
            float R = planet.Radius;
            Vector3 center = new Vector3(0f, -R, 0f);   // Planet center in local space

            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            AddVertex(Vector3.zero, center, R, verts, norms, uvs);
            for (int r = 1; r <= rings; r++)
            {
                float rr = radius * r / rings;
                for (int s = 0; s < segments; s++)
                {
                    float a = Mathf.PI * 2f * s / segments;
                    AddVertex(new Vector3(Mathf.Cos(a) * rr, 0f, Mathf.Sin(a) * rr), center, R, verts, norms, uvs);
                }
            }

            // Center triangle fan
            for (int s = 0; s < segments; s++)
            {
                int cur = 1 + s, next = 1 + (s + 1) % segments;
                tris.Add(0); tris.Add(next); tris.Add(cur);
            }
            // Quads between rings
            for (int r = 1; r < rings; r++)
            {
                int inner = 1 + (r - 1) * segments, outer = 1 + r * segments;
                for (int s = 0; s < segments; s++)
                {
                    int a = inner + s, b = inner + (s + 1) % segments;
                    int c = outer + s, d = outer + (s + 1) % segments;
                    tris.Add(a); tris.Add(b); tris.Add(c);
                    tris.Add(b); tris.Add(d); tris.Add(c);
                }
            }

            var mesh = new Mesh { name = "SurfaceDisc" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        void AddVertex(Vector3 q, Vector3 center, float R, List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs)
        {
            Vector3 dir = (q - center).normalized;
            verts.Add(center + dir * (R + lift));
            norms.Add(dir);
            uvs.Add(new Vector2(0.5f + q.x / (2f * radius), 0.5f + q.z / (2f * radius)));
        }
    }
}
