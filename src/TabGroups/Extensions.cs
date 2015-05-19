using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using EnvDTE;
using EnvDTE80;

namespace TabGroups
{
    internal static class Extensions
    {
        public static DocumentGroup FindByName(this IList<DocumentGroup> groups, string name)
        {
            return groups?.SingleOrDefault(g => g.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
        }

        public static DocumentGroup FindBySlot(this IList<DocumentGroup> groups, int index)
        {
            return groups?.SingleOrDefault(g => g.Slot == index);
        }

        public static ListViewItem GetListViewItem(this ListView list, object content)
        {
            return list.ItemContainerGenerator.ContainerFromItem(content) as ListViewItem;
        }

        public static Document GetActiveDocument(this DTE2 environment)
        {
            return environment.ActiveDocument;
        }

        public static IEnumerable<Document> GetDocuments(this DTE2 environment)
        {
            return from w in environment.GetDocumentWindows() where w.Document != null select w.Document;
        }

        public static IEnumerable<Window> GetDocumentWindows(this DTE2 environment)
        {
            return environment.Windows.Cast<Window>().Where(x => x.Linkable == false);
        }

        public static void CloseAll(this IEnumerable<Window> windows)
        {
            foreach (var w in windows.Where(w => w.Document?.Saved == true))
            {
                w.Close();
            }
        }
    }
}