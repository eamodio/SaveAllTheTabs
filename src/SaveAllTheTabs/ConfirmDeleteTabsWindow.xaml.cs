using System;
using System.Windows;

namespace SaveAllTheTabs
{
    /// <summary>
    /// Interaction logic for ConfirmDeleteTabsWindow.xaml
    /// </summary>
    public partial class ConfirmDeleteTabsWindow : Window
    {
        public ConfirmDeleteTabsWindow(string name)
        {
            InitializeComponent();

            ConfirmMessageTextBlock.Text = $"Are you sure you want to delete saved tabs '{name}'?";
        }

        private void OnConfirm(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
