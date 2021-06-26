using System;
using System.Windows;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Drawing;
using System.IO.Ports;
using Emgu.CV;
using Emgu.CV.Structure;
using CascadeClassifier = Emgu.CV.CascadeClassifier;
using DirectShowLib;

namespace CheckpointHSEApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Поле, отвечающее за видео с подключенной камеры
        private static Emgu.CV.VideoCapture capture = null;
        //Список подключенных веб-камер
        private static DsDevice[] webCams = null;
        //ID выбранной камеры
        private int selectedCameraId = 0;

        //Создание PictureBox для передачи видео на форму
        private System.Windows.Forms.PictureBox CameraPictureBox = new System.Windows.Forms.PictureBox();
        private System.Windows.Forms.PictureBox PersonPictureBox = new System.Windows.Forms.PictureBox();

        //Список подключенных портов
        private static string[] ports = null;
        private static SerialPort mySearialPort = new SerialPort();

        //Изображение, использующееся при отсутствии обнаруженного лица
        private string sadSmilePath = Cut(Environment.CurrentDirectory, 0, Environment.CurrentDirectory.LastIndexOf("CheckpointHSEApp") + "CheckpointHSEApp".Length + 1) + @"\SadSmile.png";
        //ВЫПИСАТЬ ИЗ ВИДЕО
        private static CascadeClassifier classifier = new CascadeClassifier("haarcascade_frontalface_alt_tree.xml");
        //Frames-per-second
        private double fps;
        //Таймер для введения временного промежутка между передачей изображения на функцию распознавания лиц
        private DispatcherTimer timer = null;




        //Вызов функции распознавания лица на изображении
        public async void ChangePerson(object sender, EventArgs e)
        {
            //Получение информации о человеке на изображении
            //string info = await Task.Run(() => /*Сюда вставить нужную функцию - передается изображение, принимается строка*/(PersonPictureBox.Image));
            string info = "ААААААААА";
            if (info != "Нет информации")
            {
                info += "\n\nПроход открыт";
                //Функция на открытие двери
                Open(mySearialPort, PortsСomboBox);
            }

            //Изменение информации на главном окне
            PersonInfoLabel.Content = info;
        }


        //Инициализация таймера - временного промежутка между передачей изображения на функцию распознавания лиц
        private void InitializeTimer()
        {
            timer = new DispatcherTimer();
            timer.Tick += new EventHandler(ChangePerson);
            //Временной промежуток - 1 секунда
            timer.Interval = new TimeSpan(0, 0, 0, 1, 0);
            timer.Start();
        }


        //Обрезка строки от начального индекса до конечного
        public static string Cut(string s, int beg, int end)
        {
            string sNew = "";
            for (int i = beg; i < end; i++)
            {
                sNew += s[i];
            }
            return sNew;
        }


        //Определение одного лица на входящем изображении
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


        //Главное окно
        public MainWindow()
        {
            InitializeComponent();

            //Добавление созданных PictureBox на форму
            CameraHost.Child = CameraPictureBox;
            PersonHost.Child = PersonPictureBox;
            //Видео будет вписано в рамку таким образом, чтобы не менялся масштаб
            CameraPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            PersonPictureBox.SizeMode = PictureBoxSizeMode.Zoom;

            //Задание начального изображения как "нет изображения"
            PersonPictureBox.Image = System.Drawing.Image.FromFile(sadSmilePath);


            //Получение списка доступных камер и занесение их в ComboBox
            webCams = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            for (int i = 0; i < webCams.Length; i++)
            {
                CameraIDCombobox.Items.Add(webCams[i].Name);
            }

            //Если доступных камер нет - кнопки запустить и остановить камеру заблокированы
            if (webCams == null)
            {
                StartCameraButton.IsEnabled = false;
                StopCameraButton.IsEnabled = false;
            }


            //Получение списка доступных портов и занесение их в ComboBox
            ports = SerialPort.GetPortNames();            
            for (int i = 0; i < ports.Length; i++)
            {
                PortsСomboBox.Items.Add(ports[i]);
            }

            //Если доступных портов нет - кнопка открыть проход заблокирована
            if (PortsСomboBox == null)
            {
                GateOpenButton.IsEnabled = false;
            }
        }


        //Попытка запуска камеры
        private void StartCameraButton_Click(object sender, RoutedEventArgs e)
        {
            //Запуск таймера
            InitializeTimer();
            try
            {
                if (capture != null)
                {
                    //Запуск камеры если уже существует capture
                    capture.Start();
                }
                else
                {
                    //Инициализация capture и запуск камеры
                    capture = new VideoCapture(selectedCameraId);
                    capture.ImageGrabbed += Capture_ImageGrabbed;                    
                    capture.Start();
                }
            }
            catch { }
        }


        //Получение и вывод изображения с камеры
        private async void Capture_ImageGrabbed(object sender, EventArgs e)
        {
            try
            {
                //Получение изображения с подключенной камеры и передача его на форму
                Mat m = new Mat();
                capture.Retrieve(m);
                CameraPictureBox.Image = DetectFace(m.ToImage<Bgr, byte>().Flip(Emgu.CV.CvEnum.FlipType.Horizontal)).ToBitmap();

                //Получение fps и установка задержки для непрерывности видео
                fps = capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.Fps);
                await Task.Delay(1000 / Convert.ToInt16(fps));
            }
            catch { }
        }


        //Остановка камеры
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
            if (PortsСomboBox.SelectedIndex == CameraIDCombobox.SelectedIndex)
            {
                new OpenGateWindow().ShowDialog();
                Open(mySearialPort, PortsСomboBox);
            }
            else
            {
                if (System.Windows.MessageBox.Show("Камера и турникет не совпадают, все равно открыть проход?", "Вопрос", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    new OpenGateWindow().ShowDialog();
                    Open(mySearialPort, PortsСomboBox);
                }
            }
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

        #region Вспомагательные функции
        private void Open(SerialPort port, System.Windows.Controls.ComboBox box)
        {
            try
            {
                port.PortName = PortsСomboBox.Text;
                port.Open();
                port.WriteLine("on");
                box.IsEditable = false;
            }
            catch
            {
                System.Windows.MessageBox.Show("Ошибка подключения турникета!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion
    }
}
