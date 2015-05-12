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
        public string GroupName { get; private set; }

        public SaveTabGroupWindow(string defaultName = null)
        {
            InitializeComponent();

            TextBox.SelectedText = defaultName;
            TextBox.Focus();
        }

        private void OnSave(object sender, RoutedEventArgs e)
        {
            var group = TextBox.Text;
            if (!string.IsNullOrWhiteSpace(group))
            {
                GroupName = group;
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
