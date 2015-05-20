using System;
using System.Windows;
using System.Windows.Controls;

namespace SaveAllTheTabs
{
    /// <summary>
    /// Interaction logic for SaveTabGroupWindow.xaml
    /// </summary>
    public partial class SaveTabsWindow : Window
    {
        public string TabsName { get; private set; }

        public SaveTabsWindow(string defaultName = null)
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
                TabsName = group;
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
