using System;
using System.Collections.Generic;

namespace TabGroups
{
    internal static class Extensions
    {
        public static DocumentGroup FindByName(this List<DocumentGroup> groups, string name) =>
            groups?.Find(g => g.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));

        public static DocumentGroup FindBySlot(this List<DocumentGroup> groups, int index) =>
            groups?.Find(g => g.Slot == index);
    }
}