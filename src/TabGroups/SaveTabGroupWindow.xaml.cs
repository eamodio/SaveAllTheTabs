using System;
using System.Windows;
using System.Windows.Controls;

namespace TabGroups
{
    /// <summary>
    /// Interaction logic for SaveTabGroupWindow.xaml
    /// </summary>
    public partial class SaveTabGroupWindow : Window
    {
        private TabGroupsPackage Package { get; }

        public SaveTabGroupWindow(TabGroupsPackage package)
        {
            Package = package;
            InitializeComponent();

            TextBox.Text = "";
            TextBox.SelectedText = $"New Tab Group {(Package.DocumentManager?.GroupCount ?? 0) + 1}";
            TextBox.Focus();
        }

        private void OnSave(object sender, RoutedEventArgs e)
        {
            var group = TextBox.Text;
            if (!string.IsNullOrWhiteSpace(group))
            {
                Package.DocumentManager.SaveGroup(group);
                DialogResult = true;
            }
            Close();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            var group = TextBox.Text;
            if (OkButton != null)
            {
                OkButton.IsEnabled = !string.IsNullOrWhiteSpace(group);
            }
        }
    }
}
