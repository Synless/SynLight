using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Windows.Forms;

namespace SynLight.Model
{
    public class Param_SynLight : AutoNodeMCU
    {
        #region variables
        public static readonly string param = "param.txt";

        #region getset
        private string tittle = "SynLight - Disconnected";
        public string Tittle
        {
            get
            {
                return tittle;
            }
            set
            {
                tittle = value;
                OnPropertyChanged(nameof(Tittle));
            }
        }

        private bool hotstop = false;
        public bool Hotstop
        {
            get
            {
                return hotstop;
            }
            set
            {
                hotstop = value;
                OnPropertyChanged(nameof(Hotstop));
            }
        }

        private bool screenFull = false;
        public bool ScreenFull
        {
            get
            {
                return screenFull;
            }
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
            get
            {
                return screen1;
            }
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
            get
            {
                return screen2;
            }
            set
            {
                screenFull = !value;
                screen1 = !value;
                screen2 = value;
                screen3 = !value;
                ScreenSelectionUpdated();
            }
        }

        private bool screen3 = true;
        public bool Screen3
        {
            get
            {
                return screen3;
            }
            set
            {
                screenFull = !value;
                screen1 = !value;
                screen2 = !value;
                screen3 = value;
                ScreenSelectionUpdated();
            }
        }
        private void ScreenSelectionUpdated()
        {
            if (screenFull)            
                scannedArea = new Rectangle(0, 0, screensSize.Width, screensSize.Height);
            
            if (screen1)            
                scannedArea = new Rectangle(0, 0, screen1Size.Width, screen1Size.Height);
            
            if (screen2)            
                scannedArea = new Rectangle(screen1Size.Width, 0, screen1Size.Width + screen2Size.Width, screen2Size.Height);
            
            if (screen3)            
                scannedArea = new Rectangle(screen1Size.Width + screen2Size.Width, 0, screen1Size.Width + screen2Size.Width + screen3Size.Width, screen3Size.Height);
            
            OnPropertyChanged(nameof(Screen1));
            OnPropertyChanged(nameof(Screen2));
            OnPropertyChanged(nameof(Screen3));
            OnPropertyChanged(nameof(ScreenFull));
        }

        private bool screen2Visible = false;
        public bool Screen2Visible
        {
            get
            {
                return screen2Visible;
            }
            set
            {
                screen2Visible = value;
                OnPropertyChanged(nameof(Screen2Visible));
            }
        }

        private bool screen3Visible = false;
        public bool Screen3Visible
        {
            get
            {
                return screen3Visible;
            }
            set
            {
                screen3Visible = value;
                OnPropertyChanged(nameof(Screen3Visible));
            }
        }

        private int width = 18;
        public int Width
        {
            get
            {
                return width;
            }
            set
            {
                if ((value > 0) && (value < 500))
                {
                    width = value;
                    EdgesComp();
                }

                OnPropertyChanged(nameof(Width));
            }
        }

        private int height = 12;
        public int Height
        {
            get
            {
                return height;
            }
            set
            {
                if ((value > 0) && (value < 500) && (value > shifting * 2))
                {
                    height = value;
                    fromWidth = true;
                    Ratio = ratio;
                    fromWidth = false;
                }
                else if ((value > 0) && (value < 50) && (value <= shifting * 2))
                {
                    height = value;
                    Shifting = Math.Max((value / 2) - 1, 0);
                }

                EdgesComp();

                OnPropertyChanged(nameof(Height));
            }
        }
        private bool fromWidth = false;

        private int corner = 0;
        public int Corner
        {
            get
            {
                return corner;
            }
            set
            {
                if ((value >= 0) && (value < 200) && (value <= height / 2))
                    corner = value;
                
                OnPropertyChanged(nameof(Corner));
            }
        }

        private int shifting = 0;
        public int Shifting
        {
            get
            {
                return shifting;
            }
            set
            {
                if ((value >= 0) && (value < 200) && (value < height / 2))
                    shifting = value;
                
                OnPropertyChanged(nameof(Shifting));
            }
        }

        private int upDown = 0;
        public int UpDown
        {
            get
            {
                return upDown;
            }
            set
            {
                upDown = value;
                OnPropertyChanged(nameof(UpDown));
            }
        }

        private bool ratio = true;
        public bool Ratio
        {
            get
            {
                return ratio;
            }
            set
            {
                if (!fromWidth)
                {
                    ratio = !ratio;
                }
                if (!ratio)
                {
                    double tmp = Height / A;
                    Shifting = (int)(((double)Height / 2) - (tmp / 2) + B);
                }
                else
                {
                    Shifting = 0;
                }

                OnPropertyChanged(nameof(Ratio));
            }
        }

        private bool clockwise = false;
        public bool Clockwise
        {
            get { return clockwise; }
            set
            {
                clockwise = value;
                OnPropertyChanged(nameof(Clockwise));
            }
        }

        private bool topLeft = false;
        public bool TopLeft
        {
            get { return topLeft; }
            set
            {
                topLeft = value;
                OnPropertyChanged(nameof(TopLeft));
            }
        }

        private bool topRight = false;
        public bool TopRight
        {
            get { return topRight; }
            set
            {
                topRight = value;
                OnPropertyChanged(nameof(TopRight));
            }
        }

        private bool botRight = false;
        public bool BotRight
        {
            get { return botRight; }
            set
            {
                botRight = value;
                OnPropertyChanged(nameof(BotRight));
            }
        }

        private bool botLeft = true;
        public bool BotLeft
        {
            get { return botLeft; }
            set
            {
                botLeft = value;
                OnPropertyChanged(nameof(BotLeft));
            }
        }

        private bool playPause = false;
        public bool PlayPause
        {
            get { return playPause; }
            set
            {
                playPause = value;
                System.Threading.Thread.Sleep(1);
                if (playPause && !processMainLoop.IsAlive && !processFindESP.IsAlive)
                {
                    processMainLoop.Start();
                    debug = true;
                }

                OnPropertyChanged(nameof(PlayPause));
            }
        }
        private bool canPlayPause = false;
        public bool CanPlayPause
        {
            get { return canPlayPause; }
            set
            {
                canPlayPause = value;
                OnPropertyChanged(nameof(CanPlayPause));
            }
        }

        private bool lpf = false;
        public bool LPF
        {
            get
            {
                return lpf;
            }
            set
            {
                lpf = value;
                OnPropertyChanged(nameof(LPF));
            }
        }

        private bool bgf = false;
        public bool BGF
        {
            get
            {
                return bgf;
            }
            set
            {
                bgf = value;
                OnPropertyChanged(nameof(BGF));
            }
        }
        private byte red = 100;
        public byte Red
        {
            get
            {
                return red;
            }
            set
            {
                red = value;
                staticColorChanged = true;
                OnPropertyChanged(nameof(Red));
            }
        }
        private byte green = 60;
        public byte Green
        {
            get
            {
                return green;
            }
            set
            {
                green = value;
                staticColorChanged = true;
                OnPropertyChanged(nameof(Green));
            }
        }
        private byte blue = 0;
        public byte Blue
        {
            get
            {
                return blue;
            }
            set
            {
                blue = value;
                staticColorChanged = true;
                OnPropertyChanged(nameof(Blue));
            }
        }
        private int contrast = 0;
        public int Contrast
        {
            get
            {
                return contrast;
            }
            set
            {
                contrast = value;
                OnPropertyChanged(nameof(Contrast));
            }
        }
        private bool usingFlux = true;
        public bool UsingFlux
        {
            get { return usingFlux; }
            set
            {
                usingFlux = value;
                OnPropertyChanged(nameof(UsingFlux));
            }
        }
        private bool turbo = false;
        public bool Turbo
        {
            get { return turbo; }
            set
            {
                turbo = value;
                OnPropertyChanged(nameof(Turbo));
            }
        }
        private bool keyboardLight = false;
        public bool KeyboardLight
        {
            get { return keyboardLight; }
            set
            {
                keyboardLight = value;
                OnPropertyChanged(nameof(KeyboardLight));
            }
        }
        private int mix = 0;
        public int Mix
        {
            get { return mix; }
            set
            {
                mix = value;
                staticColorChanged = true;
                OnPropertyChanged(nameof(Mix));
            }
        }
        #endregion

        protected double A = 1.32;
        protected double B = 1;
        protected bool staticColorChanged = false;
        protected const int packetSize = 1200;

        protected Size screensSize = new Size((int)System.Windows.SystemParameters.VirtualScreenWidth, (int)System.Windows.SystemParameters.VirtualScreenHeight);
        protected Size screen1Size = new Size((int)System.Windows.SystemParameters.PrimaryScreenWidth, (int)System.Windows.SystemParameters.PrimaryScreenHeight);
        protected Size screen2Size = new Size((int)System.Windows.SystemParameters.PrimaryScreenWidth, (int)System.Windows.SystemParameters.PrimaryScreenHeight);
        protected Size screen3Size = new Size((int)System.Windows.SystemParameters.PrimaryScreenWidth, (int)System.Windows.SystemParameters.PrimaryScreenHeight);
        protected Size currentScreen=new Size((int)System.Windows.SystemParameters.PrimaryScreenWidth, (int)System.Windows.SystemParameters.PrimaryScreenHeight);
        protected Rectangle edgeLeft;
        protected Rectangle edgeRight;
        protected Rectangle edgeTop;
        protected Rectangle edgeBot;
        protected Rectangle scannedArea = new Rectangle(0, 0, (int)System.Windows.SystemParameters.PrimaryScreenWidth, (int)System.Windows.SystemParameters.PrimaryScreenHeight);
        protected Bitmap bmpScreenshot = new Bitmap((int)System.Windows.SystemParameters.PrimaryScreenWidth, (int)System.Windows.SystemParameters.PrimaryScreenHeight);
        protected Bitmap scaledBmpScreenshot;
        protected Bitmap secondScaledBmpScreenshot;
        protected Bitmap scalededgeLeft;
        protected Bitmap scalededgeRight;
        protected Bitmap scalededgeTop;
        protected Bitmap scalededgeBot;
        protected bool screenConfigured = false;
        public static bool debug = true;
        protected int startX;
        protected int startY;
        protected int endX;
        protected int endY;
        protected int hX;
        protected int hY;
        protected Rectangle rect;
        protected Bitmap bmp;

        protected List<byte> lastByteToSend = new List<byte>(0);
        protected List<byte> newByteToSend = new List<byte>(0);
        protected List<byte> byteToSend;

        protected PerformanceCounter cpuCounter;
        protected bool usePerformanceCounter = true;
        protected System.Threading.Thread processFindESP;
        protected System.Threading.Thread processMainLoop;

        #endregion

        public Param_SynLight()
        {
            usePerformanceCounter = false;
            try
            {
                new System.Threading.Thread(delegate ()
                {
                    cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                    usePerformanceCounter = true;
                }).Start();
            }
            catch
            {
            }

            try
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
                                    Screen1 = true;
                                else if (subLine[1] == "2")
                                    Screen2 = true;
                                else if (subLine[1] == "3")
                                    Screen3 = true;
                                else
                                    ScreenFull = true;
                                
                                screen1Size.Width = int.Parse(subLine[1].Split(',')[0]);
                                screen1Size.Height = int.Parse(subLine[1].Split(',')[1]);
                            }
                            else if (subLine[0] == "SCREEN1")
                            {
                                screen1Size.Width = Math.Min(30720, Math.Max(800, int.Parse(subLine[1].Split(',')[0])));
                                //Is Min(24000) correct ?
                                screen1Size.Height = Math.Min(17280, Math.Max(600, int.Parse(subLine[1].Split(',')[1])));
                            }
                            else if (subLine[0] == "SCREEN2")
                            {
                                screen2Size.Width = Math.Min(30720, Math.Max(800, int.Parse(subLine[1].Split(',')[0])));
                                screen2Size.Height = Math.Min(17280, Math.Max(600, int.Parse(subLine[1].Split(',')[0])));
                                Screen2Visible = true;
                            }
                            else if (subLine[0] == "SCREEN3")
                            {
                                screen3Size.Width = Math.Min(30720, Math.Max(800, int.Parse(subLine[1].Split(',')[0])));
                                screen3Size.Height = Math.Min(17280, Math.Max(600, int.Parse(subLine[1].Split(',')[0])));
                                Screen3Visible = true;
                            }
                            else if (subLine[0] == "IP")
                            {
                                nodeMCU = IPAddress.Parse(subLine[1]);
                                endPoint = new IPEndPoint(nodeMCU, UDPPort);
                                Tittle = "Synlight - " + nodeMCU.ToString() + " - " + subLine[1];
                                StaticConnected = true;
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
                            else if (subLine[0] == "UDPPort")
                            {
                                UDPPort = int.Parse(subLine[1]);
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
                            else if (subLine[0] == "A")
                            {
                                A = Convert.ToDouble(subLine[1]);
                            }
                            else if (subLine[0] == "B")
                            {
                                B = Convert.ToDouble(subLine[1]);
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
                            else if (subLine[0] == "MOBILESHARING")
                            {
                                Startup.MobileHotstop();
                                Hotstop = true;
                            }
                            else if (subLine[0] == "CLEANFILES")
                            {
                                Startup.CleanFiles();
                            }
                            else if (subLine[0] == "FLUX")
                            {
                                UsingFlux = bool.Parse(subLine[1]);
                            }
                            else if (subLine[0] == "CONTRAST")
                            {
                                Contrast = Math.Max(0,Math.Min(120,int.Parse(subLine[1])));
                            }
                        }
                        catch{ }
                    }
                }
            }
            catch { }
        }

        private static bool EdgesCompOnce = false;
        private void EdgesComp()
        {
            if (EdgesCompOnce)
                return;

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

        public static void Close()
        {
            if (endPoint != null)
            {
                SendPayload(PayloadType.fixedColor, 0);
            }
        }
    }
}