using System.Windows;

namespace NoCodeMotion.Views
{
    public partial class InstanceConflictWindow : Window
    {
        public InstanceConflictWindow()
        {
            InitializeComponent();
        }

        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
