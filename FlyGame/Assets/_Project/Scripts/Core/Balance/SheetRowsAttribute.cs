using System;

namespace FlyJam
{
    /// Marks an array or List<T> field of a BalanceTable as a row table filled from the given sheet tab.
    /// T must be [Serializable] with a parameterless constructor (a struct is easiest).
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SheetRowsAttribute : Attribute
    {
        public string Tab { get; }
        public SheetRowsAttribute(string tab) { Tab = tab; }
    }
}
