using System.Windows;
using System.Windows.Forms;

namespace CheckpointHSEApp
{
    /// <summary>
    /// Диалоговое окно для вывода дополнительной информации (при наличии)
    /// или вывода информации в большем окне, чем показана на главном
    /// 
    /// Взаимодействует с MainWindow
    /// </summary>


    public partial class AdditionalInfoWindow : Window
    {
        //Создание нового PictureBox
        private PictureBox PersonPictureBox = new PictureBox();


        public AdditionalInfoWindow(System.Drawing.Image image, string info)
        {
            InitializeComponent();

            //Добавление созданных PictureBox на форму
            //Видео будет вписано в рамку таким образом, чтобы не менялся масштаб
            PersonHost.Child = PersonPictureBox;
            PersonPictureBox.SizeMode = PictureBoxSizeMode.Zoom;

            //Добавление информации и изображения на форму
            PersonPictureBox.Image = image;
            InfoLabel.Content = info;
        }


        //Вызывает функцию открытия прохода из MainWindow
        private void GateOpenButton_Click(object sender, RoutedEventArgs e)
        {
            var myObject = this.Owner as MainWindow;
            myObject.Opening();
        }
    }
}