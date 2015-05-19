using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.PlatformUI;
//using WPF.JoshSmith.ServiceProviders.UI;

namespace TabGroups
{
    /// <summary>
    /// Interaction logic for GroupsToolWindowControl.
    /// </summary>
    public partial class GroupsToolWindowControl : UserControl
    {
        private TabGroupsPackage Package { get; }
        public ObservableCollection<DocumentGroup> Groups { get; private set; }

        //private ListViewDragDropManager<DocumentGroup> _listViewDragDropManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="GroupsToolWindowControl"/> class.
        /// </summary>
        public GroupsToolWindowControl(TabGroupsPackage package)
        {
            Package = package;

            Loaded += (sender, args) => RefreshBindingSources(Package.DocumentManager);
            Package.DocumentManager.GroupsReset += (sender, args) => RefreshBindingSources(sender as DocumentManager, true);

            Groups = package.DocumentManager?.Groups;

            InitializeComponent();

            //_listViewDragDropManager = new ListViewDragDropManager<DocumentGroup>(TabGroupsList);
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
                if (reset)
                {
                    cvs.Source = Groups;
                }
                cvs.View?.Refresh();
            }
        }

        private void OnTabGroupsListItemMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var group = (sender as ListViewItem)?.Content as DocumentGroup;
            if (group == null || group.IsEditing)
            {
                return;
            }

            Package.DocumentManager.ApplyGroup(group);
        }

        private void OnTabGroupsListItemPreviewKeyDown(object sender, KeyEventArgs e)
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
                    Package.DocumentManager.ApplyGroup(group);
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

                    if (group.IsStash)
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
                    delay.ContinueWith(t =>
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

                    if (group.IsStash)
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
                    delay.ContinueWith(t =>
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

        private void OnTabGroupsListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
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

            if (e.RemovedItems.Count == 0)
            {
                Package.UpdateCommandsUI();
            }

            var group = e.AddedItems[0] as DocumentGroup;
            if (group == null)
            {
                return;
            }

            group.IsSelected = true;

            if (group.IsEditing || group.IsStash)
            {
                return;
            }

            var item = list.GetListViewItem(group);
            if (item == null)
            {
                // Why is this null on the first selection?!?
                return;
            }

            item.Tag = (Observable.FromEventPattern<RoutedEventArgs>(item, "PreviewMouseLeftButtonDown")
                                  .Where(re => ((re.Sender as ListViewItem)?.Content as DocumentGroup)?.IsEditing == false)
                                  .Buffer(TimeSpan.FromMilliseconds(600))
                                  .Where(re => re.Count == 1)
                                  .ObserveOnDispatcher()
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
                                             }));
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
}