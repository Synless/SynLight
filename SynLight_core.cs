using SynLight.Arduino;
using SynLight.MonitorState;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Net;
using System.Windows;

namespace SynLight
{
    class SynLight_core : ModelBase, IDisposable
    {
        #region VARIABLES
        private const string param = "param.txt";

        private string title = "SynLight - Disconnected";
        public string Title
        {
            get => title;
            set
            {
                title = value;
                OnPropertyChanged(nameof(Title));
            }
        }

        private bool screenFull = true;
        public bool ScreenFull
        {
            get => screenFull;
            set
            {
                screenFull = value;
                screen1 = !value;
                screen2 = !value;
                screen3 = !value;
                ScreenSelectionUpdated();
            }
        }

        private bool screen1 = false;
        public bool Screen1
        {
            get => screen1;
            set
            {
                screenFull = !value;
                screen1 = value;
                screen2 = !value;
                screen3 = !value;
                ScreenSelectionUpdated();
            }
        }

        private bool screen2 = false;
        public bool Screen2
        {
            get => screen2;
            set
            {
                screenFull = !value;
                screen1 = !value;
                screen2 = value;
                screen3 = !value;
                ScreenSelectionUpdated();
            }
        }

        private bool screen3 = false;
        public bool Screen3
        {
            get => screen3;
            set
            {
                screenFull = !value;
                screen1 = !value;
                screen2 = !value;
                screen3 = value;
                ScreenSelectionUpdated();
            }
        }

        private bool screen2Visible;
        public bool Screen2Visible { get => screen2Visible; set { screen2Visible = value; OnPropertyChanged(nameof(Screen2Visible)); } }

        private bool screen3Visible;
        public bool Screen3Visible { get => screen3Visible; set { screen3Visible = value; OnPropertyChanged(nameof(Screen3Visible)); } }

        private void ScreenSelectionUpdated()
        {
            if (screenFull)
            {
                scannedArea = new Rectangle(0, 0, (int)screensSize.Width, (int)screensSize.Height);
            }

            if (screen1)
            {
                scannedArea = new Rectangle(0, 0, (int)screen1Size.Width, (int)screen1Size.Height);
            }

            if (screen2)
            {
                scannedArea = new Rectangle((int)screen1Size.Width, 0, (int)screen1Size.Width + (int)screen2Size.Width, (int)screen2Size.Height);
            }

            if (screen3)
            {
                scannedArea = new Rectangle((int)screen1Size.Width + (int)screen2Size.Width, 0, (int)screen1Size.Width + (int)screen2Size.Width + (int)screen3Size.Width, (int)screen3Size.Height);
            }

            OnPropertyChanged(nameof(Screen1));
            OnPropertyChanged(nameof(Screen2));
            OnPropertyChanged(nameof(Screen3));
            OnPropertyChanged(nameof(ScreenFull));
        }

        private int width = 98;
        public int Width
        {
            get => width;
            set
            {
                if (value > 0 && value < 500)
                {
                    width = value;
                    EdgesComp();
                }
                OnPropertyChanged(nameof(Width));
            }
        }

        private int height = 55;
        private bool fromWidth;
        public int Height
        {
            get => height;
            set
            {
                if (value > 0 && value < 500 && value > shifting * 2)
                {
                    height = value;
                    fromWidth = true;
                    Ratio = ratio;
                    fromWidth = false;
                }
                else if (value > 0 && value < 50 && value <= shifting * 2)
                {
                    height = value;
                    Shifting = Math.Max((value / 2) - 1, 0);
                }

                EdgesComp();
                OnPropertyChanged(nameof(Height));
            }
        }
        private void EdgesComp()
        {
            if (EdgesCompOnce)
            {
                return;
            }

            EdgesCompOnce = true;

            double ratio = screensSize.Width / screensSize.Height;
            bool multipleScreen = ratio > (21.0 / 9.0);

            if (multipleScreen && !Screen2Visible)
            {
                System.Windows.MessageBox.Show("It appears you are using multiple screens.\nMake sure to check the config file.");
            }
            else if (multipleScreen && Screen2Visible)
            {
            }
            else
            {
                scannedArea = new Rectangle(0, 0, (int)System.Windows.SystemParameters.PrimaryScreenWidth, (int)System.Windows.SystemParameters.PrimaryScreenHeight);
                Screen3Visible = false;
                Screen2Visible = false;
            }
        }

        private int corner = 0;
        public int Corner
        {
            get => corner;
            set
            {
                if (value >= 0 && value < 200 && value <= height / 2)
                    corner = value;
                OnPropertyChanged(nameof(Corner));
            }
        }

        private int shifting = 0;
        public int Shifting
        {
            get => shifting;
            set
            {
                if (value >= 0 && value < 200 && value < height / 2)
                    shifting = value;
                OnPropertyChanged(nameof(Shifting));
            }
        }

        private int upDown = 0;
        public int UpDown { get => upDown; set { upDown = value; OnPropertyChanged(nameof(UpDown)); } }

        private bool ratio = true;
        public bool Ratio
        {
            get => ratio;
            set
            {
                if (!fromWidth) ratio = !ratio;

                if (!ratio)
                {
                    double tmp = Height / AShift;
                    double tmpHeight = Height / 2.0;
                    Shifting = (int)(tmpHeight - (tmp / 2) + BShift);
                }
                else
                {
                    Shifting = 0;
                }

                OnPropertyChanged(nameof(Ratio));
            }
        }

        private bool clockwise = true;
        public bool Clockwise { get => clockwise; set { clockwise = value; OnPropertyChanged(nameof(Clockwise)); } }

        private bool topLeft = false;
        public bool TopLeft { get => topLeft; set { topLeft = value; OnPropertyChanged(nameof(TopLeft)); } }

        private bool topRight = false;
        public bool TopRight { get => topRight; set { topRight = value; OnPropertyChanged(nameof(TopRight)); } }

        private bool botRight = false;
        public bool BotRight { get => botRight; set { botRight = value; OnPropertyChanged(nameof(BotRight)); } }

        private bool botLeft = true;
        public bool BotLeft { get => botLeft; set { botLeft = value; OnPropertyChanged(nameof(BotLeft)); } }

        private bool playPause;
        public bool PlayPause
        {
            get => playPause;
            set
            {
                playPause = value;
                Thread.Sleep(1);
                if (playPause && !processMainLoop.IsAlive && !processFindArduino.IsAlive)
                {
                    debug = true;

                    processMainLoop = new Thread(Loop);
                    processMainLoop.Start();
                }
                OnPropertyChanged(nameof(PlayPause));
            }
        }

        private bool canPlayPause;
        public bool CanPlayPause { get => canPlayPause; set { canPlayPause = value; OnPropertyChanged(nameof(CanPlayPause)); } }

        private bool lpf = true;
        public bool LPF { get => lpf; set { lpf = value; OnPropertyChanged(nameof(LPF)); } }

        private bool bgf = false;
        public bool BGF { get => bgf; set { bgf = value; OnPropertyChanged(nameof(BGF)); } }

        private byte red = 0;
        public byte Red { get => red; set { red = value; staticColorChanged = true; OnPropertyChanged(nameof(Red)); } }

        private byte green = 0;
        public byte Green { get => green; set { green = value; staticColorChanged = true; OnPropertyChanged(nameof(Green)); } }

        private byte blue = 0;
        public byte Blue { get => blue; set { blue = value; staticColorChanged = true; OnPropertyChanged(nameof(Blue)); } }

        private int contrast = 4;
        public int Contrast { get => contrast; set { contrast = value; OnPropertyChanged(nameof(Contrast)); } }

        private bool frameCounterEnabled = false;
        public bool FrameCounterEnabled { get => frameCounterEnabled; set { frameCounterEnabled = value; OnPropertyChanged(nameof(FrameCounterEnabled)); } }

        private bool turbo = false;
        public bool Turbo
        {
            get => turbo;
            set
            {
                turbo = value;
                OnPropertyChanged(nameof(Turbo));
            }
        }

        private bool keyboardLight = false;
        public bool KeyboardLight { get => keyboardLight; set { keyboardLight = value; OnPropertyChanged(nameof(KeyboardLight)); } }

        private bool neighborFilter = false;
        public bool NeighborFilter { get => neighborFilter; set { neighborFilter = value; OnPropertyChanged(nameof(NeighborFilter)); } }

        private int mix = 0;
        public int Mix
        {
            get => mix;
            set
            {
                mix = Math.Max(0, Math.Min(100, value));
                staticColorChanged = true;
                OnPropertyChanged(nameof(Mix));
            }
        }

        private bool staticConnected;
        public bool StaticConnected { get => staticConnected; set { staticConnected = value; OnPropertyChanged(nameof(StaticConnected)); } }

        private double AShift = 1.32;
        private double BShift = 1;

        private System.Windows.Size screensSize = new System.Windows.Size((int)SystemParameters.VirtualScreenWidth, (int)SystemParameters.VirtualScreenHeight);
        private System.Windows.Size screen1Size = new System.Windows.Size((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight);
        private System.Windows.Size screen2Size = new System.Windows.Size((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight);
        private System.Windows.Size screen3Size = new System.Windows.Size((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight);
        private System.Windows.Size currentScreen = new System.Windows.Size((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight);

        private Rectangle scannedArea = new Rectangle(0, 0, (int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight);

        private Bitmap bmpScreenshot = new Bitmap((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight);
        private Bitmap scaledBmpScreenshot;
        private Bitmap scalededgeLeft;
        private Bitmap scalededgeRight;
        private Bitmap scalededgeTop;
        private Bitmap scalededgeBot;

        private static bool debug = true;

        private int startX;
        private int startY;
        private int endX;
        private int endY;
        private int hX;
        private int hY;

        private List<byte> lastByteToSend = new List<byte>(0);
        private List<byte> newByteToSend = new List<byte>(0);
        private List<byte> byteToSend;

        private PerformanceCounter cpuCounter;
        private bool usePerformanceCounter = true;

        private Thread processFindArduino;
        private Thread processMainLoop;

        private Arduino_Serial arduinoSerial = new Arduino_Serial();
        private Arduino_UDP arduinoUDP = new Arduino_UDP();
        private static bool useComPort = false;

        private int GCCounter = 0;
        private volatile bool monitorIsOn = true;

        private int _Height;
        private int _Width;
        private int _Corner;
        private int _Shifting;

        private Bitmap reusableLeftBmp;
        private Bitmap reusableRightBmp;
        private Bitmap reusableTopBmp;
        private Bitmap reusableBotBmp;

        private readonly byte BrightnessForKeyboard = 15;
        private int frameCounter = 0;

        private static bool EdgesCompOnce = false;
        private bool staticColorChanged = false;

        private int sleepDelayMs = 0;
        private int lastSleepDelayMs = 0;
        private const int minDifference = 100;
        private const int maxDifference = 10000;

        private int idleStretchAdditionalMs = 0;
        private const int idleStretchCapMs = 200;
        private const int idleStretchIncrementMs = 10;
        private const int stabilityThreshold = 17;

        private int stabilityConsecutiveCount = 0;
        private const int stabilityRequiredCount = 30;
        private bool stabilityRequiredCountTriggered = false;

        private const int numberOfTries = 2;
        #endregion

        public SynLight_core()
        {
            usePerformanceCounter = false;

            try { new Thread(delegate () { cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"); usePerformanceCounter = true; }).Start(); } catch { }

            Services.MonitorState.MonitorStateChanged += OnMonitorStateChanged;

            ReadParam();

            processFindArduino = new Thread(FindArduinoProcess);
            processMainLoop = new Thread(Loop);
            processFindArduino.Start();
        }
        private void OnMonitorStateChanged(bool isOn)
        {
            monitorIsOn = isOn;

            if (!isOn)
            {
                for (int i = 0; i < newByteToSend.Count; i++)
                {
                    newByteToSend[i] = 0;
                }

                SendPayload(newByteToSend);
            }
        }

        private bool _disposed = false;
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                PlayPause = false;
                monitorIsOn = false;

                if (processMainLoop != null && processMainLoop.IsAlive)
                {
                    processMainLoop.Join(500);
                }

                if (processFindArduino != null && processFindArduino.IsAlive)
                {
                    processFindArduino.Join(500);
                }
            }
            catch
            {
            }

            try
            {
                if (newByteToSend != null)
                {
                    for (int i = 0; i < newByteToSend.Count; i++)
                    {
                        newByteToSend[i] = 0;
                    }

                    SendPayload(newByteToSend);
                }
            }
            catch
            {
            }

            try
            {
                cpuCounter?.Dispose();
            }
            catch
            {
            }

            try
            {
                bmpScreenshot?.Dispose();
                scaledBmpScreenshot?.Dispose();

                scalededgeLeft?.Dispose();
                scalededgeRight?.Dispose();
                scalededgeTop?.Dispose();
                scalededgeBot?.Dispose();

                reusableLeftBmp?.Dispose();
                reusableRightBmp?.Dispose();
                reusableTopBmp?.Dispose();
                reusableBotBmp?.Dispose();
            }
            catch
            {
            }

            try
            {
                gfxLeft?.Dispose();
                gfxRight?.Dispose();
                gfxTop?.Dispose();
                gfxBot?.Dispose();
            }
            catch
            {
            }

            try
            {
                arduinoSerial?.Dispose();
            }
            catch
            {
            }

            try
            {
                arduinoUDP?.Dispose();
            }
            catch
            {
            }

            _disposed = true;
        }

        private void FindArduinoProcess()
        {
            while (!StaticConnected)
            {
                Title = "SynLight - " + (useComPort ? "[COM]" : "[WIFI]") + " - Trying to connect ...";

                bool found = false;

                for (int i = 0; i < numberOfTries; i++)
                {
                    found = useComPort ? arduinoSerial.Setup() : arduinoUDP.Setup();

                    if (found)
                    {
                        StaticConnected = true;
                        break;
                    }
                }

                if (found)
                {
                    break;
                }
                else
                {
                    useComPort = !useComPort;
                }

                Thread.Sleep(1000);
            }

            CanPlayPause = true;
            PlayPause = true;

            processMainLoop = new Thread(Loop);
            processMainLoop.Start();
        }
        public void ReadParam()
        {
            try
            {
                if (File.Exists(param))
                {
                    using (StreamReader sr = new StreamReader(param))
                    {
                        string[] lines = sr.ReadToEnd().Split('\n');

                        foreach (string line in lines)
                        {
                            try
                            {
                                string[] subLine = line.ToUpper().Trim('\r').Split('=');

                                if (subLine[0] == "MAINSCREEN")
                                {
                                    if (subLine[1] == "1")
                                    {
                                        Screen1 = true;
                                    }
                                    else if (subLine[1] == "2")
                                    {
                                        Screen2 = true;
                                    }
                                    else if (subLine[1] == "3")
                                    {
                                        Screen3 = true;
                                    }
                                    else
                                    {
                                        ScreenFull = true;
                                    }

                                    screen1Size.Width = int.Parse(subLine[1].Split(',')[0]);
                                    screen1Size.Height = int.Parse(subLine[1].Split(',')[1]);
                                }
                                else if (subLine[0] == "SCREEN1")
                                {
                                    screen1Size.Width = Math.Min(30720, Math.Max(800, int.Parse(subLine[1].Split(',')[0])));
                                    screen1Size.Height = Math.Min(17280, Math.Max(600, int.Parse(subLine[1].Split(',')[1])));
                                }
                                else if (subLine[0] == "SCREEN2")
                                {
                                    screen2Size.Width = Math.Min(30720, Math.Max(800, int.Parse(subLine[1].Split(',')[0])));
                                    screen2Size.Height = Math.Min(17280, Math.Max(600, int.Parse(subLine[1].Split(',')[1])));
                                    Screen2Visible = true;
                                }
                                else if (subLine[0] == "SCREEN3")
                                {
                                    screen3Size.Width = Math.Min(30720, Math.Max(800, int.Parse(subLine[1].Split(',')[0])));
                                    screen3Size.Height = Math.Min(17280, Math.Max(600, int.Parse(subLine[1].Split(',')[1])));
                                    Screen3Visible = true;
                                }
                                else if (subLine[0] == "IP")
                                {
                                    arduinoUDP.SetDeviceAddress(IPAddress.Parse(subLine[1]));
                                    StaticConnected = true;
                                    useComPort = false;
                                }
                                else if (subLine[0] == "UDPPORT")
                                {
                                    arduinoUDP.SetDevicePort(int.Parse(subLine[1]));
                                }
                                else if (subLine[0].StartsWith("COM"))
                                {
                                    if (int.TryParse(subLine[0].Replace("COM", String.Empty), out int tmp))
                                    {
                                        arduinoSerial.SetPortName(subLine[0]);
                                        useComPort = true;
                                        StaticConnected = true;
                                    }
                                }
                                else if (subLine[0] == "X")
                                {
                                    Width = int.Parse(subLine[1]);
                                }
                                else if (subLine[0] == "Y")
                                {
                                    Height = int.Parse(subLine[1]);
                                }
                                else if (subLine[0] == "S")
                                {
                                    Shifting = int.Parse(subLine[1]);
                                }
                                else if (subLine[0] == "MIX")
                                {
                                    Mix = int.Parse(subLine[1]);
                                }
                                else if (subLine[0] == "RED" || subLine[0] == "R")
                                {
                                    Red = byte.Parse(Math.Max(0, Math.Min(255, (int.Parse(subLine[1])))).ToString());
                                }
                                else if (subLine[0] == "GREEN" || subLine[0] == "G")
                                {
                                    Green = byte.Parse(Math.Max(0, Math.Min(255, (int.Parse(subLine[1])))).ToString());
                                }
                                else if (subLine[0] == "BLUE" || subLine[0] == "B")
                                {
                                    Blue = byte.Parse(Math.Max(0, Math.Min(255, (int.Parse(subLine[1])))).ToString());
                                }
                                else if (subLine[0] == "TL")
                                {
                                    TopLeft = true;
                                }
                                else if (subLine[0] == "BL")
                                {
                                    BotLeft = true;
                                }
                                else if (subLine[0] == "BR")
                                {
                                    BotRight = true;
                                }
                                else if (subLine[0] == "TR")
                                {
                                    TopRight = true;
                                }
                                else if (subLine[0] == "CW")
                                {
                                    Clockwise = true;
                                }
                                else if (subLine[0] == "CCW")
                                {
                                    Clockwise = false;
                                }
                                else if (subLine[0] == "CORNERS")
                                {
                                    Corner = int.Parse(subLine[1]);
                                }
                                else if (subLine[0] == "UPDOWN")
                                {
                                    UpDown = int.Parse(subLine[1]);
                                }
                                else if (subLine[0] == "CONTRAST")
                                {
                                    Contrast = int.Parse(subLine[1]);
                                }
                                else if (subLine[0] == "NEIGHBOUR")
                                {
                                    NeighborFilter = true;
                                }
                                else if (subLine[0] == "A")
                                {
                                    AShift = Convert.ToDouble(subLine[1].Replace(",", "."), CultureInfo.InvariantCulture);
                                }
                                else if (subLine[0] == "B")
                                {
                                    BShift = Convert.ToDouble(subLine[1].Replace(",", "."), CultureInfo.InvariantCulture);
                                }
                                else if (subLine[0] == "LPF")
                                {
                                    LPF = true;
                                }
                                else if (subLine[0] == "BGF")
                                {
                                    BGF = true;
                                }
                                else if (subLine[0] == "TURBO")
                                {
                                    Turbo = true;
                                }
                                else if (subLine[0] == "KBLIGHT" || subLine[0] == "KBL")
                                {
                                    KeyboardLight = true;
                                }
                            }
                            catch { }
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("No param file found, using default values.");
                }
            }
            catch { }
        }

        private void Loop()
        {
            Stopwatch watch;

            while (PlayPause)
            {
                if (!monitorIsOn)
                {
                    Thread.Sleep(100);
                    continue;
                }

                watch = Stopwatch.StartNew();

                Tick();

                if (!Turbo)
                {
                    for (int i = 0; i < sleepDelayMs; i++)
                    {
                        if (staticColorChanged)
                        {
                            staticColorChanged = false;
                            break;
                        }
                        Thread.Sleep(1);
                    }
                }

                if (Mix == 100)
                {
                    Thread.Sleep(500);
                }

                if (GCCounter++ >= 100)
                {
                    GC.Collect();
                    GCCounter = 0;
                }

                watch.Stop();

                int Hz = (int)(1000.0 / watch.ElapsedMilliseconds);

                try
                {
                    if (useComPort)
                    {
                        if (StaticConnected)
                        {
                            Title = "Synlight - " + arduinoSerial.PortName + " - " + Hz.ToString() + "Hz";
                        }
                    }
                    else
                    {
                        if (arduinoUDP.DeviceEndPoint != null)
                        {
                            Title = "Synlight - " + arduinoUDP.DeviceAddress?.ToString() + " - " + Hz.ToString() + "Hz";
                        }
                    }
                }
                catch
                {
                }
            }

            for (int i = 0; i < newByteToSend.Count; i++)
            {
                newByteToSend[i] = 0;
            }

            SendPayload(newByteToSend);

            processMainLoop = new Thread(Loop);
            Title = "Synlight - " + (useComPort ? arduinoSerial.PortName : arduinoUDP.DeviceAddress?.ToString()) + " - Paused";
        }

        private void Tick()
        {
            _Height = Height;
            _Width = Width;
            _Corner = Corner;
            _Shifting = Shifting;

            GetScreenShotedges();

            if (Contrast > 0)
            {
                AdjustContrastInPlace(scaledBmpScreenshot, Contrast); // no return value, no clone
            }

            ProcessScreenShot();
            Send();
        }

        private static void AdjustContrastInPlace(Bitmap image, float value)
        {
            value = (100.0f + value) / 100.0f;
            value *= value;

            BitmapData data = image.LockBits(
                new Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadWrite,
                image.PixelFormat);

            int height = image.Height;
            int width = image.Width;

            unsafe
            {
                for (int y = 0; y < height; y++)
                {
                    byte* row = (byte*)data.Scan0 + y * data.Stride;
                    int columnOffset = 0;
                    for (int x = 0; x < width; x++)
                    {
                        float B = row[columnOffset] / 255.0f;
                        float G = row[columnOffset + 1] / 255.0f;
                        float R = row[columnOffset + 2] / 255.0f;

                        int iR = (int)(((R - 0.5f) * value + 0.5f) * 255.0f);
                        int iG = (int)(((G - 0.5f) * value + 0.5f) * 255.0f);
                        int iB = (int)(((B - 0.5f) * value + 0.5f) * 255.0f);

                        row[columnOffset] = (byte)(iB < 0 ? 0 : iB > 255 ? 255 : iB);
                        row[columnOffset + 1] = (byte)(iG < 0 ? 0 : iG > 255 ? 255 : iG);
                        row[columnOffset + 2] = (byte)(iR < 0 ? 0 : iR > 255 ? 255 : iR);

                        columnOffset += 4;
                    }
                }
            }

            image.UnlockBits(data);
        }

        private Graphics gfxLeft, gfxRight, gfxTop, gfxBot;
        private int _lastEdgeW = -1, _lastEdgeH = -1, _lastHX = -1, _lastHY = -1;

        private void GetScreenShotedges()
        {
            try
            {
                frameCounter++;

                startX = scannedArea.X;
                startY = scannedArea.Y;
                endX = scannedArea.Width;
                endY = scannedArea.Height;
                hX = endX - startX;
                hY = endY - startY;

                startY += ((_Shifting * hY) / _Height) / 2;
                endY -= ((_Shifting * hY) / _Height) / 2;

                int edgeW = hX / _Width;
                int edgeH = hY / _Height;

                if (reusableLeftBmp == null || reusableLeftBmp.Width != edgeW || reusableLeftBmp.Height != hY)
                {
                    reusableLeftBmp?.Dispose();
                    reusableRightBmp?.Dispose();
                    reusableTopBmp?.Dispose();
                    reusableBotBmp?.Dispose();

                    gfxLeft?.Dispose();
                    gfxRight?.Dispose();
                    gfxTop?.Dispose();
                    gfxBot?.Dispose();

                    reusableLeftBmp = new Bitmap(edgeW, hY, PixelFormat.Format32bppRgb);
                    reusableRightBmp = new Bitmap(edgeW, hY, PixelFormat.Format32bppRgb);
                    reusableTopBmp = new Bitmap(hX, edgeH, PixelFormat.Format32bppRgb);
                    reusableBotBmp = new Bitmap(hX, edgeH, PixelFormat.Format32bppRgb);

                    gfxLeft = Graphics.FromImage(reusableLeftBmp);
                    gfxRight = Graphics.FromImage(reusableRightBmp);
                    gfxTop = Graphics.FromImage(reusableTopBmp);
                    gfxBot = Graphics.FromImage(reusableBotBmp);
                }

                List<Task> tasks = new List<Task>();

                if (frameCounterEnabled)
                {
                    if (frameCounter % 2 == 0)
                    {
                        tasks.Add(Task.Run(() =>
                        {
                            Rectangle rectLeft = new Rectangle(startX, startY, edgeW, endY - startY);
                            lock (gfxLeft) gfxLeft.CopyFromScreen(rectLeft.Left, rectLeft.Top, 0, 0, reusableLeftBmp.Size);
                            scalededgeLeft = RescaleImage(reusableLeftBmp, new System.Drawing.Size(1, _Height));
                        }));

                        tasks.Add(Task.Run(() =>
                        {
                            Rectangle rectRight = new Rectangle(endX - edgeW, startY, edgeW, endY - startY);
                            lock (gfxRight) gfxRight.CopyFromScreen(rectRight.Left, rectRight.Top, 0, 0, reusableRightBmp.Size);
                            scalededgeRight = RescaleImage(reusableRightBmp, new System.Drawing.Size(1, _Height));
                        }));
                    }
                    else
                    {
                        tasks.Add(Task.Run(() =>
                        {
                            Rectangle rectTop = new Rectangle(startX, startY, hX, edgeH);
                            lock (gfxTop) gfxTop.CopyFromScreen(rectTop.Left, rectTop.Top, 0, 0, reusableTopBmp.Size);
                            scalededgeTop = RescaleImage(reusableTopBmp, new System.Drawing.Size(_Width, 1));
                        }));

                        tasks.Add(Task.Run(() =>
                        {
                            Rectangle rectBot = new Rectangle(startX, endY - edgeH, hX, edgeH);
                            lock (gfxBot) gfxBot.CopyFromScreen(rectBot.Left, rectBot.Top, 0, 0, reusableBotBmp.Size);
                            scalededgeBot = RescaleImage(reusableBotBmp, new System.Drawing.Size(_Width, 1));
                        }));
                    }
                }
                else
                {
                    tasks.Add(Task.Run(() =>
                    {
                        Rectangle rectLeft = new Rectangle(startX, startY, edgeW, endY - startY);
                        lock (gfxLeft) gfxLeft.CopyFromScreen(rectLeft.Left, rectLeft.Top, 0, 0, reusableLeftBmp.Size);
                        scalededgeLeft = RescaleImage(reusableLeftBmp, new System.Drawing.Size(1, _Height));
                    }));

                    tasks.Add(Task.Run(() =>
                    {
                        Rectangle rectRight = new Rectangle(endX - edgeW, startY, edgeW, endY - startY);
                        lock (gfxRight) gfxRight.CopyFromScreen(rectRight.Left, rectRight.Top, 0, 0, reusableRightBmp.Size);
                        scalededgeRight = RescaleImage(reusableRightBmp, new System.Drawing.Size(1, _Height));
                    }));

                    tasks.Add(Task.Run(() =>
                    {
                        Rectangle rectTop = new Rectangle(startX, startY, hX, edgeH);
                        lock (gfxTop) gfxTop.CopyFromScreen(rectTop.Left, rectTop.Top, 0, 0, reusableTopBmp.Size);
                        scalededgeTop = RescaleImage(reusableTopBmp, new System.Drawing.Size(_Width, 1));
                    }));

                    tasks.Add(Task.Run(() =>
                    {
                        Rectangle rectBot = new Rectangle(startX, endY - edgeH, hX, edgeH);
                        lock (gfxBot) gfxBot.CopyFromScreen(rectBot.Left, rectBot.Top, 0, 0, reusableBotBmp.Size);
                        scalededgeBot = RescaleImage(reusableBotBmp, new System.Drawing.Size(_Width, 1));
                    }));
                }

                Task.WaitAll(tasks.ToArray());

                ComposeFullBitmap();

                if (debug)
                {
                    try
                    {
                        debug = false;
                        ResizeLeftRight(scalededgeLeft).Save("1Left.png", ImageFormat.Png);
                        ResizeLeftRight(scalededgeRight).Save("3Right.png", ImageFormat.Png);
                        ResizeTopBot(scalededgeTop).Save("2Top.png", ImageFormat.Png);
                        ResizeTopBot(scalededgeBot).Save("4Bot.png", ImageFormat.Png);
                        Resize(scaledBmpScreenshot).Save("5full.png", ImageFormat.Png);
                    }
                    catch { }
                }
            }
            catch { }
        }
        private void ComposeFullBitmap()
        {
            // Reallocate only if size changed
            if (scaledBmpScreenshot == null || scaledBmpScreenshot.Width != _Width || scaledBmpScreenshot.Height != _Height)
            {
                scaledBmpScreenshot?.Dispose();
                scaledBmpScreenshot = new Bitmap(_Width, _Height, PixelFormat.Format32bppArgb);
            }

            BitmapData bmpData = scaledBmpScreenshot.LockBits(
                new Rectangle(0, 0, _Width, _Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            int stride = bmpData.Stride;

            // Lock all edge bitmaps for reading
            BitmapData leftData = scalededgeLeft?.LockBits(new Rectangle(0, 0, scalededgeLeft.Width, scalededgeLeft.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData rightData = scalededgeRight?.LockBits(new Rectangle(0, 0, scalededgeRight.Width, scalededgeRight.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData topData = scalededgeTop?.LockBits(new Rectangle(0, 0, scalededgeTop.Width, scalededgeTop.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData botData = scalededgeBot?.LockBits(new Rectangle(0, 0, scalededgeBot.Width, scalededgeBot.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            unsafe
            {
                byte* dst = (byte*)bmpData.Scan0;

                byte* srcLeft = leftData != null ? (byte*)leftData.Scan0 : null;
                byte* srcRight = rightData != null ? (byte*)rightData.Scan0 : null;
                byte* srcTop = topData != null ? (byte*)topData.Scan0 : null;
                byte* srcBot = botData != null ? (byte*)botData.Scan0 : null;

                // Left and right columns
                for (int y = 0; y < _Height; y++)
                {
                    if (srcLeft != null)
                    {
                        byte* src = srcLeft + y * leftData.Stride;        // x=0, so no x offset
                        byte* pixel = dst + y * stride;                    // x=0
                        pixel[0] = src[0];
                        pixel[1] = src[1];
                        pixel[2] = src[2];
                        pixel[3] = 255;
                    }

                    if (srcRight != null)
                    {
                        byte* src = srcRight + y * rightData.Stride;
                        byte* pixel = dst + y * stride + (_Width - 1) * 4;
                        pixel[0] = src[0];
                        pixel[1] = src[1];
                        pixel[2] = src[2];
                        pixel[3] = 255;
                    }
                }

                // Top and bottom rows
                for (int x = 1; x < _Width - 1; x++)
                {
                    if (srcTop != null)
                    {
                        byte* src = srcTop + x * 4;                      // y=0, stride offset = 0
                        byte* pixel = dst + x * 4;                         // y=0
                        pixel[0] = src[0];
                        pixel[1] = src[1];
                        pixel[2] = src[2];
                        pixel[3] = 255;
                    }

                    if (srcBot != null)
                    {
                        byte* src = srcBot + x * 4;
                        byte* pixel = dst + (_Height - 1) * stride + x * 4;

                        if (KeyboardLight && monitorIsOn)
                        {
                            pixel[0] = (byte)Math.Min(src[0] + BrightnessForKeyboard, 255);
                            pixel[1] = (byte)Math.Min(src[1] + BrightnessForKeyboard, 255);
                            pixel[2] = (byte)Math.Min(src[2] + BrightnessForKeyboard, 255);
                        }
                        else
                        {
                            pixel[0] = src[0];
                            pixel[1] = src[1];
                            pixel[2] = src[2];
                        }
                        pixel[3] = 255;
                    }
                }
            }

            scaledBmpScreenshot.UnlockBits(bmpData);

            scalededgeLeft?.UnlockBits(leftData);
            scalededgeRight?.UnlockBits(rightData);
            scalededgeTop?.UnlockBits(topData);
            scalededgeBot?.UnlockBits(botData);
        }


        private Bitmap Resize(Bitmap srcImage)
        {
            Bitmap newImage = new Bitmap((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight);
            using (Graphics gr = Graphics.FromImage(newImage))
            {
                gr.SmoothingMode = SmoothingMode.HighQuality;
                gr.InterpolationMode = InterpolationMode.NearestNeighbor;
                gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                gr.DrawImage(srcImage, new Rectangle(0, 0, (int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight));
            }
            return newImage;
        }
        public static Bitmap RescaleImage(Image source, System.Drawing.Size size)
        {
            var bmp = new Bitmap(size.Width, size.Height, source.PixelFormat);
            bmp.SetResolution(source.HorizontalResolution, source.VerticalResolution);
            using (var gr = Graphics.FromImage(bmp))
            {
                gr.Clear(Color.Transparent);
                gr.InterpolationMode = InterpolationMode.Bilinear;
                gr.DrawImage(source, new Rectangle(0, 0, size.Width, size.Height));
            }
            return bmp;
        }
        private Bitmap ResizeLeftRight(Bitmap srcImage)
        {
            Bitmap newImage = new Bitmap((int)SystemParameters.PrimaryScreenWidth / Width, (int)SystemParameters.PrimaryScreenHeight);
            using (Graphics gr = Graphics.FromImage(newImage))
            {
                gr.SmoothingMode = SmoothingMode.HighQuality;
                gr.InterpolationMode = InterpolationMode.Bilinear;
                gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                gr.DrawImage(srcImage, new Rectangle(0, 0, (int)SystemParameters.PrimaryScreenWidth / Width, (int)SystemParameters.PrimaryScreenHeight));
            }
            return newImage;
        }
        private Bitmap ResizeTopBot(Bitmap srcImage)
        {
            Bitmap newImage = new Bitmap((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight / Height);
            using (Graphics gr = Graphics.FromImage(newImage))
            {
                gr.SmoothingMode = SmoothingMode.HighQuality;
                gr.InterpolationMode = InterpolationMode.Bilinear;
                gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                gr.DrawImage(srcImage, new Rectangle(0, 0, (int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight / Height));
            }
            return newImage;
        }
        private void ProcessScreenShot()
        {
            byteToSend = new List<byte>();
            int subCorner = Math.Max(0, _Corner - 1);
            bool processedHeight = false;

            BitmapData bmpData = scaledBmpScreenshot.LockBits(new Rectangle(0, 0, scaledBmpScreenshot.Width, scaledBmpScreenshot.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            int stride = bmpData.Stride;
            int width = scaledBmpScreenshot.Width;
            int height = scaledBmpScreenshot.Height;

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;

                byte GetR(int x, int y) => ptr[y * stride + x * 4 + 2];
                byte GetG(int x, int y) => ptr[y * stride + x * 4 + 1];
                byte GetB(int x, int y) => ptr[y * stride + x * 4 + 0];

                if (TopLeft)
                {
                    for (int y = subCorner; y < height - 1 - subCorner; y++)
                    {
                        processedHeight = true;
                        int clampedY = Math.Max(0, Math.Min(y, height - 1));
                        byteToSend.Add(GetR(0, clampedY));
                        byteToSend.Add(GetG(0, clampedY));
                        byteToSend.Add(GetB(0, clampedY));
                    }
                    for (int x = _Corner; x <= width - 1 - _Corner; x++)
                    {
                        byteToSend.Add(GetR(x, height - 1));
                        byteToSend.Add(GetG(x, height - 1));
                        byteToSend.Add(GetB(x, height - 1));
                    }

                    if (!processedHeight) { scaledBmpScreenshot.UnlockBits(bmpData); return; }

                    for (int y = height - 1 - subCorner; y > subCorner; y--)
                    {
                        int clampedY = Math.Max(0, Math.Min(y, height - 1));
                        byteToSend.Add(GetR(width - 1, clampedY));
                        byteToSend.Add(GetG(width - 1, clampedY));
                        byteToSend.Add(GetB(width - 1, clampedY));
                    }
                    for (int x = width - 1 - _Corner; x > _Corner; x--)
                    {
                        byteToSend.Add(GetR(x, 0));
                        byteToSend.Add(GetG(x, 0));
                        byteToSend.Add(GetB(x, 0));
                    }
                }

                if (TopRight)
                {
                    for (int x = width - 1 - _Corner; x >= _Corner; x--)
                    {
                        byteToSend.Add(GetR(x, 0));
                        byteToSend.Add(GetG(x, 0));
                        byteToSend.Add(GetB(x, 0));
                    }
                    for (int y = subCorner; y < height - 1 - subCorner; y++)
                    {
                        processedHeight = true;
                        int clampedY = Math.Max(0, Math.Min(y, height - 1));
                        byteToSend.Add(GetR(0, clampedY));
                        byteToSend.Add(GetG(0, clampedY));
                        byteToSend.Add(GetB(0, clampedY));
                    }

                    if (!processedHeight) { scaledBmpScreenshot.UnlockBits(bmpData); return; }

                    for (int x = _Corner; x < width - 1 - _Corner; x++)
                    {
                        byteToSend.Add(GetR(x, height - 1));
                        byteToSend.Add(GetG(x, height - 1));
                        byteToSend.Add(GetB(x, height - 1));
                    }
                    for (int y = height - 1 - subCorner; y > subCorner; y--)
                    {
                        int clampedY = Math.Max(0, Math.Min(y, height - 1));
                        byteToSend.Add(GetR(width - 1, clampedY));
                        byteToSend.Add(GetG(width - 1, clampedY));
                        byteToSend.Add(GetB(width - 1, clampedY));
                    }
                }

                if (BotRight)
                {
                    for (int y = height - 1 - subCorner; y > subCorner; y--)
                    {
                        processedHeight = true;
                        int clampedY = Math.Max(0, Math.Min(y, height - 1));
                        byteToSend.Add(GetR(width - 1, clampedY));
                        byteToSend.Add(GetG(width - 1, clampedY));
                        byteToSend.Add(GetB(width - 1, clampedY));
                    }
                    for (int x = width - 1 - _Corner; x >= _Corner; x--)
                    {
                        byteToSend.Add(GetR(x, 0));
                        byteToSend.Add(GetG(x, 0));
                        byteToSend.Add(GetB(x, 0));
                    }

                    if (!processedHeight) { scaledBmpScreenshot.UnlockBits(bmpData); return; }

                    for (int y = subCorner; y < height - 1 - subCorner; y++)
                    {
                        int clampedY = Math.Max(0, Math.Min(y, height - 1));
                        byteToSend.Add(GetR(0, clampedY));
                        byteToSend.Add(GetG(0, clampedY));
                        byteToSend.Add(GetB(0, clampedY));
                    }
                    for (int x = _Corner; x < width - 1 - _Corner; x++)
                    {
                        byteToSend.Add(GetR(x, height - 1));
                        byteToSend.Add(GetG(x, height - 1));
                        byteToSend.Add(GetB(x, height - 1));
                    }
                }

                if (BotLeft)
                {
                    for (int x = _Corner; x <= width - 1 - _Corner; x++)
                    {
                        byteToSend.Add(GetR(x, height - 1));
                        byteToSend.Add(GetG(x, height - 1));
                        byteToSend.Add(GetB(x, height - 1));
                    }
                    for (int y = height - 1 - subCorner; y > subCorner; y--)
                    {
                        processedHeight = true;
                        int clampedY = Math.Max(0, Math.Min(y, height - 1));
                        byteToSend.Add(GetR(width - 1, clampedY));
                        byteToSend.Add(GetG(width - 1, clampedY));
                        byteToSend.Add(GetB(width - 1, clampedY));
                    }

                    if (!processedHeight) { scaledBmpScreenshot.UnlockBits(bmpData); return; }

                    for (int x = width - 1 - _Corner; x >= _Corner; x--)
                    {
                        byteToSend.Add(GetR(x, 0));
                        byteToSend.Add(GetG(x, 0));
                        byteToSend.Add(GetB(x, 0));
                    }
                    for (int y = subCorner; y < height - 1 - subCorner; y++)
                    {
                        int clampedY = Math.Max(0, Math.Min(y, height - 1));
                        byteToSend.Add(GetR(0, clampedY));
                        byteToSend.Add(GetG(0, clampedY));
                        byteToSend.Add(GetB(0, clampedY));
                    }
                }
            }

            scaledBmpScreenshot.UnlockBits(bmpData);

            if (Clockwise)
            {
                int chunkSize = 3;
                int numberOfChunks = byteToSend.Count / chunkSize;

                List<byte> reversedRgbValues = new List<byte>();

                for (int i = numberOfChunks - 1; i >= 0; i--)
                {
                    for (int j = 0; j < chunkSize; j++)
                    {
                        reversedRgbValues.Add(byteToSend[i * chunkSize + j]);
                    }
                }

                byteToSend = new List<byte>(reversedRgbValues);
            }
        }

        private void Send()
        {
            if (Mix > 0)
            {
                int bl = Mix;
                List<byte> bts = new List<byte>(byteToSend);
                for (int n = 0; n < byteToSend.Count - 2; n += 3)
                {
                    bts[n] = (byte)(byteToSend[n] * ((100.0 - bl) / 100.0) + Red * (bl / 100.0));
                    bts[n + 1] = (byte)(byteToSend[n + 1] * ((100.0 - bl) / 100.0) + Green * (bl / 100.0));
                    bts[n + 2] = (byte)(byteToSend[n + 2] * ((100.0 - bl) / 100.0) + Blue * (bl / 100.0));
                }
                byteToSend = new List<byte>(bts);
            }

            newByteToSend = new List<byte>(0);

            if (LPF)
            {
                while (lastByteToSend.Count < byteToSend.Count) { lastByteToSend.Add(0); }

                int odd; //To correct the -1 error rounding
                for (int n = 0; n < byteToSend.Count; n++)
                {
                    odd = (2 * byteToSend[n]) + lastByteToSend[n];
                    if (odd % 2 != 0) { odd++; }
                    odd /= 3;
                    newByteToSend.Add((byte)odd);
                }
                lastByteToSend = new List<byte>(newByteToSend);
            }
            else
            {
                lastByteToSend = newByteToSend = byteToSend;
            }

            if (UpDown != 0)
            {
                List<byte> rotatedByteToSend = new List<byte>(newByteToSend);

                for (int n = 0; n < newByteToSend.Count; n++)
                {
                    rotatedByteToSend[n] = newByteToSend[(n + UpDown * 3) % (byteToSend.Count)];
                }

                newByteToSend = new List<byte>(rotatedByteToSend);
            }

            if (BGF)
            {
                long meanR = 0;
                long meanG = 0;
                long meanB = 0;

                int p = 8;
                int q = p + 1;

                for (int n = 0; n < newByteToSend.Count; n += 3)
                {
                    meanR += newByteToSend[n];
                    meanG += newByteToSend[n + 1];
                    meanB += newByteToSend[n + 2];
                }

                meanR = 3 * meanR / newByteToSend.Count;
                meanG = 3 * meanG / newByteToSend.Count;
                meanB = 3 * meanB / newByteToSend.Count;

                for (int n = 0; n < newByteToSend.Count; n += 3)
                {
                    newByteToSend[n] = (byte)(p * (newByteToSend[n] / q) + (meanR / p));
                    newByteToSend[n + 1] = (byte)(p * (newByteToSend[n + 1] / q) + (meanG / p));
                    newByteToSend[n + 2] = (byte)(p * (newByteToSend[n + 2] / q) + (meanB / p));
                }
            }

            if (NeighborFilter)
            {
                List<byte> veryNewByteToSend = new List<byte>(newByteToSend);
                List<byte> veryOldByteToSend = new List<byte>(newByteToSend);

                double mainRatio = 0.65;
                double neighborRatio = (1 - mainRatio) / 4;

                int j = veryNewByteToSend.Count;
                for (int i = 0; i <= j - 3; i += 3)
                {
                    double r = newByteToSend[i] * mainRatio;
                    double g = newByteToSend[i + 1] * mainRatio;
                    double b = newByteToSend[i + 2] * mainRatio;

                    // Add previous 2 neighbors if they exist
                    if (i - 3 >= 0)
                    {
                        r += newByteToSend[i - 3] * neighborRatio;
                        g += newByteToSend[i - 2] * neighborRatio;
                        b += newByteToSend[i - 1] * neighborRatio;
                    }
                    if (i - 6 >= 0)
                    {
                        r += newByteToSend[i - 6] * neighborRatio;
                        g += newByteToSend[i - 5] * neighborRatio;
                        b += newByteToSend[i - 4] * neighborRatio;
                    }

                    // Add next 2 neighbors if they exist
                    if (i + 3 < j)
                    {
                        r += newByteToSend[i + 3] * neighborRatio;
                        g += newByteToSend[i + 4] * neighborRatio;
                        b += newByteToSend[i + 5] * neighborRatio;
                    }
                    if (i + 6 < j)
                    {
                        r += newByteToSend[i + 6] * neighborRatio;
                        g += newByteToSend[i + 7] * neighborRatio;
                        b += newByteToSend[i + 8] * neighborRatio;
                    }

                    veryNewByteToSend[i] = (byte)Math.Min(255, Math.Max(0, Math.Round(r)));
                    veryNewByteToSend[i + 1] = (byte)Math.Min(255, Math.Max(0, Math.Round(g)));
                    veryNewByteToSend[i + 2] = (byte)Math.Min(255, Math.Max(0, Math.Round(b)));
                }

                newByteToSend = new List<byte>(veryNewByteToSend);


                int sumVeryOldByteToSend = veryOldByteToSend.Sum(b => b);
                int sumVeryNewByteToSend = newByteToSend.Sum(b => b);

            }

            CalculateSleepTime();

            SendPayload(newByteToSend);
        }

        private void CalculateSleepTime()
        {
            if (Turbo)
            {
                idleStretchAdditionalMs = 0;
                sleepDelayMs = 0;
                lastSleepDelayMs = 0;
                return;
            }

            if (lastByteToSend.Count != byteToSend.Count)
            {
                idleStretchAdditionalMs = 0;
                sleepDelayMs = maxDifference;
                return;
            }

            int totalChange = 0;

            for (int i = 0; i < byteToSend.Count; i++)
            {
                totalChange += Math.Abs(byteToSend[i] - lastByteToSend[i]);
            }

            int mapped = Math.Min(totalChange, maxDifference);
            mapped = Math.Max(mapped, minDifference);
            mapped -= minDifference;
            mapped = (int)Math.Sqrt(mapped);

            if (usePerformanceCounter)
            {
                mapped += (int)Math.Round(cpuCounter.NextValue());
            }

            mapped = mapped / 2;

            if (Math.Sqrt(totalChange) <= stabilityThreshold)
            {
                stabilityConsecutiveCount++;

                if (stabilityConsecutiveCount >= stabilityRequiredCount || stabilityRequiredCountTriggered)
                {
                    stabilityRequiredCountTriggered = true;
                    idleStretchAdditionalMs = Math.Min(idleStretchAdditionalMs + idleStretchIncrementMs, idleStretchCapMs);
                }
            }
            else
            {
                stabilityRequiredCountTriggered = false;
                stabilityConsecutiveCount = 0;
                idleStretchAdditionalMs = 0;
            }

            int combined = Math.Min(mapped + idleStretchAdditionalMs, maxDifference);

            sleepDelayMs = combined;

            if (sleepDelayMs > lastSleepDelayMs)
            {
                sleepDelayMs = (lastSleepDelayMs + sleepDelayMs) / 2;
            }

            sleepDelayMs = sleepDelayMs / 2;

            lastSleepDelayMs = sleepDelayMs;
        }

        private void SendPayload(List<byte> payload)
        {
            if (useComPort)
            {
                arduinoSerial.Send(payload);
            }
            else
            {
                arduinoUDP.Send(payload);
            }
        }
    }
}
