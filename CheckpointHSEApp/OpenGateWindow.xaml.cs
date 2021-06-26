using System.Windows;

namespace CheckpointHSEApp
{
    /// <summary>
    /// Вызывается, чтобы оповестить об открытии прохода
    /// 
    /// Взаимодействует с MainWindow
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
