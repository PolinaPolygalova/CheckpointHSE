using System.IO.Ports;
using System.Windows;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace CheckpointHSEApp
{
    /// <summary>
    /// Логика взаимодействия для AdditionalInfoWindow.xaml
    /// </summary>
    public partial class AdditionalInfoWindow : Window
    {
        private System.Windows.Forms.PictureBox PersonPictureBox = new System.Windows.Forms.PictureBox();
        private static SerialPort mySearialPort = new SerialPort();
        private static System.Windows.Controls.ComboBox PortsComboBox = new System.Windows.Controls.ComboBox();

        public AdditionalInfoWindow(System.Drawing.Image image, string info, SerialPort port, System.Windows.Controls.ComboBox ports)
        {
            InitializeComponent();
            PersonHost.Child = PersonPictureBox;
            PersonPictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
            PersonPictureBox.Image = image;
            InfoLabel.Content = info;
            mySearialPort = port;
            PortsComboBox = ports;
        }

        private void GateOpenButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Open(mySearialPort, PortsComboBox);
            new OpenGateWindow().ShowDialog();
        }
    }
}