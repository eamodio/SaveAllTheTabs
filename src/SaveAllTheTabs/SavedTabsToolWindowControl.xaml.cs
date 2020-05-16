using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.PlatformUI;
using SaveAllTheTabs.Commands;
using SaveAllTheTabs.Polyfills;
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.Shell;

namespace SaveAllTheTabs
{
    /// <summary>
    /// Interaction logic for GroupsToolWindowControl.
    /// </summary>
    public partial class SavedTabsToolWindowControl : UserControl
    {
        private SaveAllTheTabsPackage Package { get; }
        private SavedTabsWindowCommands Commands { get; }

        public ObservableCollection<DocumentGroup> Groups { get; private set; }

        //private ListViewDragDropManager<DocumentGroup> _listViewDragDropManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="SavedTabsToolWindowControl"/> class.
        /// </summary>
        public SavedTabsToolWindowControl(SaveAllTheTabsPackage package, SavedTabsWindowCommands commands)
        {
            Package = package;
            Commands = commands;

            Loaded += (sender, args) => RefreshBindingSources(Package.DocumentManager);
            Package.DocumentManager.GroupsReset += (sender, args) => RefreshBindingSources(sender as DocumentManager, true);

            Groups = package.DocumentManager?.Groups;

            InitializeComponent();

            //_listViewDragDropManager = new ListViewDragDropManager<DocumentGroup>(TabsList);
        }

        private void RefreshBindingSources(IDocumentManager documentManager, bool reset = false)
        {
            if (reset)
            {
                Groups = documentManager?.Groups;
            }

            var cvs = FindResource("Groups") as CollectionViewSource;
            if (cvs != null)
            {
                if (reset || cvs.Source == null)
                {
                    cvs.Source = Groups;
                }
                cvs.View?.Refresh();
            }
        }

        private void OnTabsListItemMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var group = (sender as ListViewItem)?.Content as DocumentGroup;
            if (group == null || group.IsEditing)
            {
                return;
            }

            Package.DocumentManager.RestoreGroup(group);
        }

        private void OnTabsListItemPreviewKeyDown(object sender, KeyEventArgs e)
        {
            var group = (sender as ListViewItem)?.Content as DocumentGroup;
            if (group == null || group.IsEditing)
            {
                return;
            }

            switch (e.Key)
            {
                case Key.Enter:
                {
                    Package.DocumentManager.RestoreGroup(group);
                    break;
                }
                case Key.Delete:
                {
                    var list = ((ListViewItem)sender).FindAncestor<ListView>();

                    Package.DocumentManager.RemoveGroup(group);

                    // Reset the selection to the new selection, otherwise selection get reset to the first item
                    if (list?.SelectedItem != null)
                    {
                        list.GetListViewItem(list.SelectedItem)?.Focus();
                    }
                    break;
                }
                case Key.Up:
                {
                    if (e.KeyboardDevice.Modifiers != ModifierKeys.Control)
                    {
                        break;
                    }

                    e.Handled = true;

                    if (group.IsBuiltIn)
                    {
                        break;
                    }

                    var list = ((ListViewItem)sender).FindAncestor<ListView>();

                    Package.DocumentManager.MoveGroup(group, -1);

                    if (list == null)
                    {
                        break;
                    }

                    // Reset focus to current group, otherwise selection get reset to the first item
                    var delay = Task.Run(async () => await Task.Delay(TimeSpan.FromMilliseconds(1)));
                    _ = delay.ContinueWith(t =>
                                            {
                                                list.SelectedItem = group;
                                                list.GetListViewItem(group)?.Focus();
                                            }, TaskScheduler.FromCurrentSynchronizationContext());
                    break;
                }
                case Key.Down:
                {
                    if (e.KeyboardDevice.Modifiers != ModifierKeys.Control)
                    {
                        break;
                    }

                    e.Handled = true;

                    if (group.IsBuiltIn)
                    {
                        break;
                    }

                    var list = ((ListViewItem)sender).FindAncestor<ListView>();

                    Package.DocumentManager.MoveGroup(group, +1);

                    if (list == null)
                    {
                        break;
                    }

                    // Reset focus to current group, otherwise selection get reset to the first item
                    var delay = Task.Run(async () => await Task.Delay(TimeSpan.FromMilliseconds(1)));
                    _ = delay.ContinueWith(t =>
                                            {
                                                list.SelectedItem = group;
                                                list.GetListViewItem(group)?.Focus();
                                            }, TaskScheduler.FromCurrentSynchronizationContext());
                    break;
                }
                default:
                {
                    if ((e.Key >= Key.D1 && e.Key <= Key.D9) || (e.Key >= Key.NumPad1 && e.Key <= Key.NumPad9))
                    {
                        var number = e.Key > Key.NumPad0
                                         ? e.Key - Key.NumPad0
                                         : e.Key - Key.D0;
                        Package.DocumentManager.SetGroupSlot(group, number);
                    }
                    break;
                }
            }
        }

        private void OnTabsListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var list = (sender as ListView);
            if (list == null)
            {
                return;
            }

            if (e.RemovedItems.Count == 1)
            {
                var previous = e.RemovedItems[0] as DocumentGroup;
                if (previous != null)
                {
                    previous.IsSelected = false;
                    (list.GetListViewItem(previous)?.Tag as IDisposable)?.Dispose();
                }
            }

            if (e.AddedItems.Count == 0)
            {
                Package.UpdateCommandsUI();
                return;
            }

            var commandsUpdated = false;
            if (e.RemovedItems.Count == 0)
            {
                commandsUpdated = true;
                Package.UpdateCommandsUI();
            }

            var group = e.AddedItems[0] as DocumentGroup;
            if (group == null)
            {
                return;
            }

            group.IsSelected = true;

            if (!commandsUpdated)
            {
                Package.UpdateCommandsUI();
            }

            if (group.IsEditing || group.IsBuiltIn)
            {
                return;
            }

            var item = list.GetListViewItem(group);
            if (item == null)
            {
                // Why is this null on the first selection?!?
                return;
            }

            var mouseDowns = Observable.FromEventPattern<RoutedEventArgs>(item, "PreviewMouseLeftButtonDown");
            var mouseUps = Observable.FromEventPattern<RoutedEventArgs>(item, "PreviewMouseLeftButtonUp");

            var query = from md in mouseDowns.Take(1)
                        where (md.EventArgs as MouseButtonEventArgs)?.ClickCount == 1 &&
                              ((md.Sender as ListViewItem)?.Content as DocumentGroup)?.IsEditing == false
                        from mu in mouseUps.Take(1)
                        from bmd in mouseDowns.Buffer(TimeSpan.FromMilliseconds(600)).Take(1)
                        where bmd.Count == 0
                        select mu;

            item.Tag = query.ObserveOnDispatcher()
                            .Repeat()
                            .Subscribe(re =>
                                       {
                                           StartEditing(list, item, group,
                                                        (edit, cancel) =>
                                                        {
                                                            var previous = group.EndEditing();
                                                            if (cancel)
                                                            {
                                                                edit.Text = previous;
                                                            }
                                                        });
                                       });
        }

        private static void StartEditing(ListView list, ListViewItem item, DocumentGroup group, Action<TextBox, bool> endEditingFn)
        {
            var edit = item.FindDescendant<TextBox>();

            var visibility = Observable.FromEventPattern<DependencyPropertyChangedEventArgs>(edit, "IsVisibleChanged");

            var disposables = new CompositeDisposable();

            visibility.Where(re => re.EventArgs.NewValue as bool? == false)
                      .Take(1)
                      .Subscribe(re => disposables.Dispose());

            visibility.Where(ivc => ivc.EventArgs.NewValue as bool? == true)
                      .Take(1)
                      .Subscribe(re =>
                                 {
                                     edit.SelectionStart = 0;
                                     edit.SelectionLength = edit.Text.Length;
                                     edit.Focus();
                                 });

            disposables.Add(Observable.FromEventPattern<RoutedEventArgs>(edit, "LostFocus")
                                      .Take(1)
                                      .Subscribe(re => endEditingFn(edit, false)));

            disposables.Add(Observable.FromEventPattern<KeyEventArgs>(edit, "PreviewKeyUp")
                                      .Where(re => re.EventArgs.Key == Key.Escape || re.EventArgs.Key == Key.Enter)
                                      .Take(1)
                                      .Subscribe(re =>
                                                 {
                                                     re.EventArgs.Handled = true;
                                                     endEditingFn(edit, re.EventArgs.Key == Key.Escape);
                                                 }));

            disposables.Add(Observable.FromEventPattern<MouseEventArgs>(list, "MouseLeftButtonDown")
                                      .Take(1)
                                      .Subscribe(re => endEditingFn(edit, false)));

            group.StartEditing();
        }

        private void OnTabsListItemMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var p = PointToScreen(e.GetPosition(this));
            Commands.ShowContextMenu((int)p.X, (int)p.Y);
        }
    }

    public class BoolVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var visible = (parameter as string == "invert")
                              ? !(value as bool?).GetValueOrDefault()
                              : (value as bool?).GetValueOrDefault();
            return visible
                       ? Visibility.Visible
                       : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class WrapTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToString(value).Replace(", ", "\n");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}