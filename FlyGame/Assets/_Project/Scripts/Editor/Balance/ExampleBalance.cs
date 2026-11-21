using UnityEngine;

namespace FlyJam.EditorTools
{
    /// Self-test table for the Balance Sync tool (tabs "Example" and "ExampleRows").
    /// Editor-only and not used by the game. Safe to delete once real tables exist.
    public class ExampleBalance : BalanceTable
    {
        public override string SheetName => "Example";

        [Tooltip("float test")] [SerializeField] float speed = 1.5f;
        [Tooltip("int test")] [SerializeField] int count = 3;
        [Tooltip("bool test: 1 or 0")] [SerializeField] bool flag = true;
        [Tooltip("Vector2 test: two rows, .x and .y")] [SerializeField] Vector2 range = new Vector2(1f, 2f);
        [Tooltip("text test")] [SerializeField] string label = "hello";

        [System.Serializable]
        public struct Row
        {
            public int level;
            public float value;
        }

        [SheetRows("ExampleRows")]
        [SerializeField] Row[] rows =
        {
            new Row { level = 1, value = 10f },
            new Row { level = 2, value = 25f },
        };
    }
}
