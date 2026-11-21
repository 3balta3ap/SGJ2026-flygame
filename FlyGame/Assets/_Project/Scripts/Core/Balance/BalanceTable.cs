using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    /// Base class for planner-editable numbers, filled from Google Sheets by FlyJam > Balance Sync.
    ///
    /// Key/value tab (SheetName): row 1 = "key | value | note", then one row per field.
    ///   key = field name of the subclass (public, or private with [SerializeField]).
    /// Row table tab: mark an array/List field with [SheetRows("TabName")].
    ///   row 1 = column names (= fields of the element type), then one row per element.
    /// Rows whose first cell starts with '#' are comments. Keys missing in the sheet keep their current value.
    ///
    /// Features reference their own BalanceTable asset through a [SerializeField] and read it at runtime.
    public abstract class BalanceTable : ScriptableObject
    {
        /// Google Sheets tab for key/value rows. Return null if this table only uses [SheetRows] tabs.
        public abstract string SheetName { get; }

        /// Optional: read the SheetName tab yourself (e.g., localization with "key | en | ja").
        /// Return true when handled; the default key/value import is then skipped.
        public virtual bool ImportCustom(List<string[]> rows, List<string> log) => false;

        /// Optional: write a custom tab back as TSV for the "Copy (TSV)" button. Return null to use the default.
        public virtual string ExportCustomTsv() => null;

        /// Called after a Pull. Override to clamp values or rebuild caches.
        public virtual void OnPulled() { }
    }
}
