using UnityEngine;

namespace FlyJam.EditorTools
{
    /// Stores the shared Google Sheets link. Committed to git so everyone pulls from the same sheet.
    public class BalanceSyncSettings : ScriptableObject
    {
        public string sheetUrl = "";
    }
}
