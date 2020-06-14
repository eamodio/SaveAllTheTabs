using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace SaveAllTheTabs.Polyfills
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

        public static IEnumerable<string> GetDocumentFiles(this DTE2 environment)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return from d in environment.GetDocuments() select GetExactPathName(d.FullName);
        }

        public static IEnumerable<Document> GetDocuments(this DTE2 environment)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return from w in environment.GetDocumentWindows() where w.Document != null select w.Document;
        }

        public static IEnumerable<Window> GetDocumentWindows(this DTE2 environment)
        {
            return environment.Windows
                              .Cast<Window>()
                              .Where(w =>
                                     {
                                         try
                                         {
                                             ThreadHelper.ThrowIfNotOnUIThread();
                                             return !w.Linkable;
                                         }
                                         catch (ObjectDisposedException)
                                         {
                                             return false;
                                         }
                                     });
        }

        public static IEnumerable<Breakpoint> GetBreakpoints(this DTE2 environment)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return environment.Debugger.Breakpoints.Cast<Breakpoint>();
        }

        public static IEnumerable<Breakpoint> GetMatchingBreakpoints(this DTE2 environment, HashSet<string> files)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return environment.Debugger.Breakpoints.Cast<Breakpoint>().Where(bp => {
                ThreadHelper.ThrowIfNotOnUIThread();
                return files.Contains(bp.File);
            });
        }

        public static void CloseAll(this IEnumerable<Window> windows, vsSaveChanges saveChanges = vsSaveChanges.vsSaveChangesPrompt)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (var w in windows)
            {
                w.Close(saveChanges);
            }
        }

        public static void CloseAll(this IEnumerable<Document> documents, vsSaveChanges saveChanges = vsSaveChanges.vsSaveChangesPrompt)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (var d in documents)
            {
                d.Close(saveChanges);
            }
        }

        public static Command GetCommand(this DTE2 environment, OleMenuCommand command)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return environment.Commands.Item(command.CommandID.Guid, command.CommandID.ID);
        }

        public static object[] GetKeyBindings(this DTE2 environment, OleMenuCommand command)
        {
            return environment.GetCommand(command)?.Bindings as object[];
        }

        public static void SetKeyBindings(this DTE2 environment, OleMenuCommand command, params object[] bindings)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dteCommand = environment.GetCommand(command);
            if (dteCommand == null)
            {
                return;
            }

            try
            {
                dteCommand.Bindings = bindings;
            } catch (System.Runtime.InteropServices.COMException ex)
            {
                Debug.Assert(false, nameof(SetKeyBindings), ex.ToString());
            }
        }

        public static void SetKeyBindings(this DTE2 environment, OleMenuCommand command, IEnumerable<object> bindings)
        {
            environment.SetKeyBindings(command, bindings.ToArray());
        }

        private static string GetExactPathName(string pathName)
        {
            if (String.IsNullOrEmpty(pathName) ||
                (!File.Exists(pathName) && !Directory.Exists(pathName)))
            {
                return pathName;
            }

            var di = new DirectoryInfo(pathName);
            return di.Parent != null
                       ? Path.Combine(GetExactPathNameCore(di.Parent),
                                      di.Parent.EnumerateFileSystemInfos(di.Name).First().Name)
                       : di.Name;
        }

        private static string GetExactPathNameCore(DirectoryInfo di)
        {
            return di.Parent != null
                       ? Path.Combine(GetExactPathNameCore(di.Parent),
                                      di.Parent.EnumerateFileSystemInfos(di.Name).First().Name)
                       : di.Name;
        }
    }
}