using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Drawing;
using DirectShowLib;
using Emgu.CV;
using System.IO.Ports;
using Emgu.CV.Structure;
using CascadeClassifier = Emgu.CV.CascadeClassifier;
using System.Windows.Threading;

namespace CheckpointHSEApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static Emgu.CV.VideoCapture capture = null;
        private static DsDevice[] webCams = null;
        private int selectedCameraId = 0;
        private string sadSmilePath = Cut(Environment.CurrentDirectory, 0, Environment.CurrentDirectory.LastIndexOf("CheckpointHSEApp") + "CheckpointHSEApp".Length + 1) + @"\SadSmile.png";
        private static CascadeClassifier classifier = new CascadeClassifier("haarcascade_frontalface_alt_tree.xml");
        private System.Windows.Forms.PictureBox CameraPictureBox = new System.Windows.Forms.PictureBox();
        private System.Windows.Forms.PictureBox PersonPictureBox = new System.Windows.Forms.PictureBox();
        private double fps;
        private DispatcherTimer timer = null;




        public async void ChangePerson(object sender, EventArgs e)
        {
            string info = await Task.Run(() => /*Сюда вставить нужную функцию - передается изображение, принимается строка*/(PersonPictureBox.Image));
            if (info != "Нет информации")
            {
                //Функция на открытие двери
            }
            this.PersonInfoLabel.Content = info;
        }





        private void InitializeTimer()
        {
            timer = new DispatcherTimer();
            timer.Tick += new EventHandler(ChangePerson);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 1000);
            timer.Start();
        }


        public static string Cut(string s, int beg, int end)
        {
            string sNew = "";
            for (int i = beg; i < end; i++)
            {
                sNew += s[i];
            }
            return sNew;
        }




        public Image<Bgr, byte> DetectFace(Image<Bgr, byte> image)
        {
            Bitmap bitmap = image.ToBitmap();
            Image<Bgr, byte> grayImage = bitmap.ToImage<Bgr, byte>();
            System.Drawing.Rectangle[] faces = classifier.DetectMultiScale(grayImage, 1.4, 0);
            if (faces.Length > 0)
            {
                System.Drawing.Rectangle newFace = faces[0];
                newFace.Height = Convert.ToInt32(newFace.Width * 1.33);
                newFace.Y = newFace.Y - Convert.ToInt32(newFace.Height/4.7);

                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    using (System.Drawing.Pen pen = new System.Drawing.Pen(System.Drawing.Color.MintCream, 3))
                    {
                        graphics.DrawRectangle(pen, newFace);
                    }
                }
                try
                {
                    PersonPictureBox.Image = bitmap.Clone(newFace, bitmap.PixelFormat);
                }
                catch { }
            }
            else
            {
                try
                {
                    PersonPictureBox.Image = System.Drawing.Image.FromFile(sadSmilePath);
                }
                catch { }
            }
            return bitmap.ToImage<Bgr, byte>();
        }


        public MainWindow()
        {
            InitializeComponent();
            CameraHost.Child = CameraPictureBox;
            PersonHost.Child = PersonPictureBox;
            PersonPictureBox.Image = System.Drawing.Image.FromFile(sadSmilePath);

            CameraPictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
            PersonPictureBox.SizeMode = PictureBoxSizeMode.StretchImage;


            webCams = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);

            for (int i = 0; i < webCams.Length; i++)
            {
                CameraIDCombobox.Items.Add(webCams[i].Name);
            }

        }
        

        private void StartCameraButton_Click(object sender, RoutedEventArgs e)
        {
            InitializeTimer();
            try
            {
                if (webCams.Length == 0)
                {
                    throw new Exception("Нет доступных камер.");
                }
                else if (capture != null)
                {
                    capture.Start();
                }
                else
                {
                    capture = new VideoCapture(selectedCameraId);
                    capture.ImageGrabbed += Capture_ImageGrabbed;                    
                    capture.Start();
                }
            }
            catch { }
        }


        private async void Capture_ImageGrabbed(object sender, EventArgs e)
        {            
            try
            {
                Mat m = new Mat();
                capture.Retrieve(m);
                CameraPictureBox.Image = DetectFace(m.ToImage<Bgr, byte>().Flip(Emgu.CV.CvEnum.FlipType.Horizontal)).ToBitmap();
                fps = capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.Fps);
                await Task.Delay(1000/Convert.ToInt16(fps));
            }
            catch { }
        }


        private void StopCameraButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (capture != null)
                {
                    capture.Pause();
                }
            }
            catch { }
        }


        private void GateOpenButton_Click(object sender, RoutedEventArgs e)
        {
            new OpenGateWindow().ShowDialog();
        }


        private void AddInfoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                new AdditionalInfoWindow(PersonPictureBox.Image, PersonInfoLabel.Content.ToString()).ShowDialog();
            }
            catch
            {
                new AdditionalInfoWindow().ShowDialog();
            }
        }


        private void CameraIDCombobox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            selectedCameraId = CameraIDCombobox.SelectedIndex;
        }


        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Forms.Application.MessageLoop)
            {
                System.Windows.Forms.Application.Exit();
            }
            else
            {
                System.Environment.Exit(1);
            }
        }
    }
}
