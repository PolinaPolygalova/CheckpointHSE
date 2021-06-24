using System.Windows;

namespace CheckpointHSEApp
{
    /// <summary>
    /// Логика взаимодействия для OpenGateWindow.xaml
    /// </summary>
    public partial class OpenGateWindow : Window
    {
        public OpenGateWindow()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
