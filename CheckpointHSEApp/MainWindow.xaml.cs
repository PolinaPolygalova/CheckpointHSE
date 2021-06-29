using System;
using System.Windows;
using System.Windows.Forms;     /*Добавление PictureBox на форму*/
using System.Threading.Tasks;   /*Установка задержки для непрерывности видео*/
using System.Windows.Threading; /*Установка временного промежутка через который будет запускаться функция определения лица*/
using System.Drawing;           /*Выделение лица на изображении*/
using System.IO.Ports;          /*Подключение к портам*/
using DirectShowLib;            /*Подключение к веб-камере*/
using CascadeClassifier = Emgu.CV.CascadeClassifier; /*Определение лица на изображении*/
//Получение изображения с камеры
using Emgu.CV;
using Emgu.CV.Structure;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Drawing.Imaging;


namespace CheckpointHSEApp
{
    /// <summary>
    /// Главное окно 
    /// Подключается камера, из нее изображение передается на экран монитора и в функцию распознавания лиц
    /// Подключаются порты
    /// При распознавании лица в базе данных, соответствующему порту посылается сигнал открыться
    /// 
    /// Взаимодействует с AdditionalWindow, OpenGateWindow
    /// </summary>


    public partial class MainWindow : Window
    {
        //Поле, отвечающее за видео с подключенной камеры
        private static VideoCapture capture = null;
        //Список подключенных веб-камер
        private static DsDevice[] webCams = null;
        //ID выбранной камеры
        private int selectedCameraId = 0;

        //Создание PictureBox для передачи видео на форму
        private PictureBox CameraPictureBox = new PictureBox();
        private PictureBox PersonPictureBox = new PictureBox();

        //Список подключенных портов
        private static string[] ports = null;
        private static SerialPort mySearialPort;

        //Изображение, использующееся при отсутствии обнаруженного лица
        private string sadSmilePath = Cut(Environment.CurrentDirectory, 0, Environment.CurrentDirectory.LastIndexOf("CheckpointHSEApp") + "CheckpointHSEApp".Length + 1) + @"\SadSmile.png";
        //Элемент класса для распознавания лица на изображении
        private static CascadeClassifier classifier = new CascadeClassifier("haarcascade_frontalface_alt_tree.xml");
        //Frames-per-second
        private double fps;
        //Таймер для введения временного промежутка между передачей изображения на функцию распознавания лиц
        private DispatcherTimer timer = null;

        public  async Task<string> GetPersonNameAsync(Image image)
        {
            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(@"http://localhost:5000");
                MultipartFormDataContent form = new MultipartFormDataContent(Guid.NewGuid().ToString());

                using (var stream = new MemoryStream())
                {
                    image.Save(stream, ImageFormat.Jpeg);
                    stream.Seek(0, SeekOrigin.Begin);
                    var content = new StreamContent(stream);
                    content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                    content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                    {
                        Name = "file",
                        FileName = "uploadedFile"
                    };
                    form.Add(content, "file");

                    var response = await client.PostAsync(@"/Employees/RecognizePerson", form);
                    return await response.Content.ReadAsStringAsync();
                }

            }
            catch (Exception)
            {
                return string.Empty;
            }
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
            if ((webCams == null)||(CameraIDCombobox.SelectedIndex == -1))
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


        //Обработка нажатия на кнопку "запустить камеру"
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
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        //Обработка нажатия на кнопку "остановить камеру"
        private void StopCameraButton_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            ChangePerson(sender, e);
            try
            {
                if (capture != null)
                {
                    capture.Pause();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        //Обработка нажатия на кнопку "открыть проход"
        public void GateOpenButton_Click(object sender, RoutedEventArgs e)
        {
            Opening();
        }


        //Обработка нажатия на кнопку "подробнее" - вывод диалогового окна
        private void AddInfoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AdditionalInfoWindow window = new AdditionalInfoWindow(PersonPictureBox.Image, PersonInfoLabel.Content.ToString());
                window.Owner = this;
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        //Обработка нажатия на кнопку "выход"
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Forms.Application.MessageLoop)
            {
                //Выход из формы
                System.Windows.Forms.Application.Exit();
            }
            else
            {
                //Выход из приложения
                System.Environment.Exit(0);
            }
        }



        #region Вспомагательные функции

        //Определение одного лица на входящем изображении
        public Image<Bgr, byte> DetectFace(Image<Bgr, byte> image)
        {
            //Преобразование изображения в формат bitmap для последующй работы с ним
            Bitmap bitmap = image.ToBitmap();

            //Создание списка лиц на изображении - обводятся прямоугольниками
            System.Drawing.Rectangle[] faces = classifier.DetectMultiScale(image, 1.4, 0);

            if (faces.Length > 0) /*если есть хотя бы одно лицо*/
            {
                //Выбор одного прямоугольника из списка
                System.Drawing.Rectangle newFace = faces[0];

                //Изменение соотношения сторок приямоугольника к 3х4 и перемещение прямоугольника на уровень волос
                newFace.Height = Convert.ToInt32(newFace.Width * 1.33);
                newFace.Y = newFace.Y - Convert.ToInt32(newFace.Height/4.7);

                //Прорисовка прямоугольника на изображении
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    using (System.Drawing.Pen pen = new System.Drawing.Pen(System.Drawing.Color.MintCream, 3))
                    {
                        graphics.DrawRectangle(pen, newFace);
                    }
                }

                //Если лицо полностью находится в кадре - отображение его в отдельном окошке
                try
                {
                    PersonPictureBox.Image = bitmap.Clone(newFace, bitmap.PixelFormat);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            //Если не обнаружено ни одного лица - ставится картинка "нет изображения"
            else
            {
                PersonPictureBox.Image = System.Drawing.Image.FromFile(sadSmilePath);
            }

            //Возвращение исходного изображения или изображения с выделенным прямоугольником
            return bitmap.ToImage<Bgr, byte>();
        }


        //Вызов функции распознавания лица на изображении
        public async void ChangePerson(object sender, EventArgs e)
        {
            //Получение информации о человеке на изображении
            string info = await GetPersonNameAsync(PersonPictureBox.Image);
            if (!string.IsNullOrEmpty(info))
            {
                info += "\n\nПроход открыт";
                //Функция на открытие двери
                Open(PortsСomboBox);
            }
            else
            {
                info = "Нет информации";
            }

            //Изменение информации на главном окне
            PersonInfoLabel.Content = info;
        }


        //Инициализация таймера - временного промежутка между передачей изображения на функцию распознавания лиц
        private void InitializeTimer()
        {
            timer = new DispatcherTimer();
            timer.Tick += new EventHandler(ChangePerson);
            //Временной промежуток - 5 секунд
            timer.Interval = new TimeSpan(0, 0, 0, 5, 0);
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
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        //При изменении выбранной камеры - меняется selectedCameraId и обновляется видео
        private void CameraIDCombobox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            selectedCameraId = CameraIDCombobox.SelectedIndex;
            if ((webCams != null) && (CameraIDCombobox.SelectedIndex != -1))
            {
                StartCameraButton.IsEnabled = true;
                StopCameraButton.IsEnabled = true;
            }
        }


        //Открытие выбранного порта или вывод сообщения об ошибке при сбое
        private bool Open(System.Windows.Controls.ComboBox box)
        {
            bool flag = true;
            try
            {
                mySearialPort = new SerialPort(box.Text, 9600);
                mySearialPort.Open();
                mySearialPort.Write("on");
                box.IsEditable = false;
            }
            catch
            {
                flag = false;
                System.Windows.MessageBox.Show("Ошибка подключения турникета!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return flag;
        }


        //Действия выполняемые при попытке открытия прохода
        public void Opening()
        {
            if (PortsСomboBox.SelectedIndex == CameraIDCombobox.SelectedIndex)
            {
                if(Open(PortsСomboBox))
                    new OpenGateWindow().ShowDialog();
            }
            else
            {
                if (System.Windows.MessageBox.Show("Камера и турникет не совпадают, все равно открыть проход?", "Вопрос", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    if (Open(PortsСomboBox))
                        new OpenGateWindow().ShowDialog();
                }
            }
        }

        #endregion
    }
}