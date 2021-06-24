using System.Windows;
using System.Windows.Forms;

namespace CheckpointHSEApp
{
    /// <summary>
    /// Логика взаимодействия для AdditionalInfoWindow.xaml
    /// </summary>
    public partial class AdditionalInfoWindow : Window
    {
        private System.Windows.Forms.PictureBox PersonPictureBox = new System.Windows.Forms.PictureBox();

        public AdditionalInfoWindow(System.Drawing.Image image, string info)
        {
            InitializeComponent();
            PersonHost.Child = PersonPictureBox;
            PersonPictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
            PersonPictureBox.Image = image;
            InfoLabel.Content = info;
        }
        public AdditionalInfoWindow()
        {
            InitializeComponent();
        }

        private void GateOpenButton_Click(object sender, RoutedEventArgs e)
        {
            new OpenGateWindow().ShowDialog();
        }
    }
}