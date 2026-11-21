using UnityEngine;

namespace FlyJam
{
    [RequireComponent(typeof(MeshFilter))]
    public class PlanetMesh : MonoBehaviour
    {
        [SerializeField] int longitude = 96;
        [SerializeField] int latitude = 64;

        void Awake() => Build();

        [ContextMenu("Build Mesh")]
        public void Build()
        {
            int cols = longitude + 1, rows = latitude + 1;
            var verts = new Vector3[cols * rows];
            var norms = new Vector3[verts.Length];
            var uvs = new Vector2[verts.Length];

            for (int y = 0; y < rows; y++)
            {
                float t = (float)y / latitude;
                float theta = t * Mathf.PI;
                for (int x = 0; x < cols; x++)
                {
                    float s = (float)x / longitude;
                    float phi = s * Mathf.PI * 2f;
                    var d = new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Cos(theta), Mathf.Sin(theta) * Mathf.Sin(phi));
                    int i = y * cols + x;
                    verts[i] = d * 0.5f;
                    norms[i] = d;
                    uvs[i] = new Vector2(s, 1f - t);
                }
            }

            var tris = new int[longitude * latitude * 6];
            int k = 0;
            for (int y = 0; y < latitude; y++)
            {
                for (int x = 0; x < longitude; x++)
                {
                    int a = y * cols + x, b = a + 1, c = a + cols, d = c + 1;
                    tris[k++] = a; tris[k++] = b; tris[k++] = c;
                    tris[k++] = b; tris[k++] = d; tris[k++] = c;
                }
            }

            var mesh = new Mesh { name = "PlanetHiRes" };
            mesh.vertices = verts;
            mesh.normals = norms;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }
    }
}
