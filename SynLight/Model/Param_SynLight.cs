using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace SynLight.Model
{
    public class Param_SynLight : AutoNodeMCU
    {
        #region variables
        public static readonly string paramTxt = "param.txt";
        public static readonly string paramXml = "param.xml";

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
                OnPropertyChanged("Tittle");
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
            if (screenFull) { scannedArea = new Rectangle(0, 0, screensSize.Width, screensSize.Height);
            }
            if (screen1)
            {
                scannedArea = new Rectangle(0, 0, screen1Size.Width, screen1Size.Height);
            }
            if (screen2)
            {
                scannedArea = new Rectangle(screen1Size.Width, 0, screen1Size.Width + screen2Size.Width, screen2Size.Height);
            }
            if (screen3)
            {
                scannedArea = new Rectangle(screen1Size.Width + screen2Size.Width, 0, screen1Size.Width + screen2Size.Width + screen3Size.Width, screen3Size.Height);
            }
            OnPropertyChanged("Screen1");
            OnPropertyChanged("Screen2");
            OnPropertyChanged("Screen3");
            OnPropertyChanged("ScreenFull");
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
                OnPropertyChanged("Screen2Visible");
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
                OnPropertyChanged("Screen3Visible");
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
                OnPropertyChanged("Width");
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
                OnPropertyChanged("Height");
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
                if ((value >= 0) && (value < 200) && (value < height / 2))
                {
                    corner = value;
                }
                OnPropertyChanged("Corner");
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
                {
                    shifting = value;
                }
                OnPropertyChanged("Shifting");
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
                OnPropertyChanged("UpDown");
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
                OnPropertyChanged("Ratio");
            }
        }

        private bool clockwise = false;
        public bool Clockwise
        {
            get { return clockwise; }
            set
            {
                clockwise = value;
                OnPropertyChanged("Clockwise");
            }
        }

        private bool topLeft = false;
        public bool TopLeft
        {
            get { return topLeft; }
            set
            {
                topLeft = value;
                OnPropertyChanged("TopLeft");
            }
        }

        private bool topRight = false;
        public bool TopRight
        {
            get { return topRight; }
            set
            {
                topRight = value;
                OnPropertyChanged("TopRight");
            }
        }

        private bool botRight = false;
        public bool BotRight
        {
            get { return botRight; }
            set
            {
                botRight = value;
                OnPropertyChanged("BotRight");
            }
        }

        private bool botLeft = true;
        public bool BotLeft
        {
            get { return botLeft; }
            set
            {
                botLeft = value;
                OnPropertyChanged("BotLeft");
            }
        }

        private bool playPause = false;
        public bool PlayPause
        {
            get { return playPause; }
            set
            {
                playPause = value;
                System.Threading.Thread.Sleep(10);
                if (playPause && !processMainLoop.IsAlive && !processFindESP.IsAlive)
                {
                    processMainLoop.Start();
                }
                OnPropertyChanged("PlayPause");
            }
        }
        private bool canPlayPause = false;
        public bool CanPlayPause
        {
            get { return canPlayPause; }
            set
            {
                canPlayPause = value;
                OnPropertyChanged("CanPlayPause");
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
                OnPropertyChanged("LPF");
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
                OnPropertyChanged("BGF");
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
                OnPropertyChanged("Red");
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
                OnPropertyChanged("Green");
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
                OnPropertyChanged("Blue");
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
                OnPropertyChanged("Contrast");
            }
        }
        private bool usingFlux = true;
        public bool UsingFlux
        {
            get { return usingFlux; }
            set
            {
                usingFlux = value;
                OnPropertyChanged("UsingFlux");
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
                OnPropertyChanged("Mix");
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
        protected Bitmap bmpScreenshot;
        protected Bitmap scaledBmpScreenshot;
        protected Bitmap secondScaledBmpScreenshot;
        protected Bitmap scalededgeLeft;
        protected Bitmap scalededgeRight;
        protected Bitmap scalededgeTop;
        protected Bitmap scalededgeBot;
        protected bool screenConfigured = false;
        protected bool debug = false;
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
        protected System.Threading.Thread processFindESP;
        protected System.Threading.Thread processMainLoop;

        #endregion

        public Param_SynLight()
        {
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            try
            {
                if(File.Exists(paramXml))
                {
                    XmlTextReader reader = new XmlTextReader(paramXml);                    
                    while (reader.Read())
                    {
                        if(reader.NodeType == XmlNodeType.Element)
                        {
                            string r = reader.Name.ToUpper();
                            reader.Read();
                            string v = reader.Value;

                            switch (r)
                            {
                                case "MAINSCREEN":
                                    if (v == "1")       { Screen1 = true; }
                                    else if (v == "2")  { Screen2 = true; }
                                    else if (v == "3")  { Screen3 = true; }
                                    else                { ScreenFull = true; }
                                    //screen1Size.Width = int.Parse(v.Split('x')[0]);
                                    //screen1Size.Height = int.Parse(v.Split('x')[1]);
                                    break;
                                case "SCREEN1":
                                    screen1Size.Width = Math.Min(30720, Math.Max(800, int.Parse(v.Split('x')[0])));
                                    screen1Size.Height = Math.Min(17280, Math.Max(600, int.Parse(v.Split('x')[1])));
                                    break;
                                case "SCREEN2":
                                    screen2Size.Width = Math.Min(30720, Math.Max(800, int.Parse(v.Split('x')[0])));
                                    screen2Size.Height = Math.Min(17280, Math.Max(600, int.Parse(v.Split('x')[1])));
                                    break;
                                case "SCREEN3":
                                    screen3Size.Width = Math.Min(30720, Math.Max(800, int.Parse(v.Split('x')[0])));
                                    screen3Size.Height = Math.Min(17280, Math.Max(600, int.Parse(v.Split('x')[1])));
                                    break;
                                case "IP":
                                    try
                                    {
                                        nodeMCU = IPAddress.Parse(v);
                                        endPoint = new IPEndPoint(nodeMCU, UDPPort);
                                        Tittle = "Synlight - " + v;
                                        staticConnected = true;
                                    }
                                    catch { }
                                    break;
                                case "UDPPORT":
                                case "PORT":
                                    try
                                    {
                                        UDPPort = int.Parse(v);
                                        endPoint = new IPEndPoint(nodeMCU, UDPPort);
                                    }
                                    catch { }
                                    break;
                                case "X": Width = int.Parse(v); break;
                                case "Y": Height = int.Parse(v); break;
                                case "CORNER": Corner = int.Parse(v); break;
                                case "SHIFTING": Shifting = int.Parse(v); break;
                                case "CONTRAST": Contrast = int.Parse(v); break;
                                case "UPDOWN": UpDown = int.Parse(v); break;
                                case "START":
                                case "STARTLED":
                                    if (v == "TL")      { TopLeft  = true; }
                                    else if (v == "BL") { BotLeft  = true; }
                                    else if (v == "BR") { BotRight = true; }
                                    else if (v == "TR") { TopRight = true; }
                                    break;
                                case "DIRECTION":
                                    if (v == "CW") { Clockwise = true; }
                                    else if (v == "CCW") { Clockwise = false; }
                                    break;
                                case "A": A = Convert.ToDouble(v); break;
                                case "B": B = Convert.ToDouble(v); break;
                                case "BACKGROUNDFILTER": BGF = bool.Parse(v); break;
                                case "LOWPASSFILTER": LPF = bool.Parse(v); break;
                                case "MOBILESHARING": if (bool.Parse(v)) { Startup.MobileHotstop(); } break;
                                case "CLEANFILES": if (bool.Parse(v)) { Startup.CleanFiles(); } break;
                                case "FLUX": UsingFlux = bool.Parse(v); break;
                                default: break;
                            }
                        }
                    }
                    reader.Close();
                }
                else if (File.Exists(paramTxt))
                {
                    using (StreamReader sr = new StreamReader(paramTxt))
                    {
                        string[] lines = sr.ReadToEnd().Split('\n');
                        foreach (string line in lines)
                        {
                            try
                            {
                                string[] subLine = line.ToUpper().Trim('\r').Split('=');
                                if (subLine[0] == "MAINSCREEN")
                                {
                                    if (subLine[1] == "1") { Screen1 = true; }
                                    else if (subLine[1] == "2") { Screen2 = true; }
                                    else if (subLine[1] == "3") { Screen3 = true; }
                                    else { ScreenFull = true; }

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
                                    try
                                    {
                                        nodeMCU = IPAddress.Parse(subLine[1]);
                                        endPoint = new IPEndPoint(nodeMCU, UDPPort);
                                        Tittle = "Synlight - " + subLine[1];
                                        staticConnected = true;
                                    }
                                    catch { }
                                }
                                else if (subLine[0] == "UDPPort") { UDPPort = int.Parse(subLine[1]); endPoint = new IPEndPoint(nodeMCU, UDPPort); }
                                else if (subLine[0] == "X") { Width = int.Parse(subLine[1]); }
                                else if (subLine[0] == "Y") { Height = int.Parse(subLine[1]); }
                                else if (subLine[0] == "S") { Shifting = int.Parse(subLine[1]); }
                                else if (subLine[0] == "CORNERS") { Corner = int.Parse(subLine[1]); }
                                else if (subLine[0] == "UPDOWN") { UpDown = int.Parse(subLine[1]); }
                                else if (subLine[0] == "TL") { TopLeft = true; }
                                else if (subLine[0] == "BL") { BotLeft = true; }
                                else if (subLine[0] == "BR") { BotRight = true; }
                                else if (subLine[0] == "TR") { TopRight = true; }
                                else if (subLine[0] == "CW") { Clockwise = true; }
                                else if (subLine[0] == "CCW") { Clockwise = false; }
                                else if (subLine[0] == "A") { A = Convert.ToDouble(subLine[1].Replace(',', '.')); }
                                else if (subLine[0] == "B") { B = Convert.ToDouble(subLine[1].Replace(',', '.')); }
                                else if (subLine[0] == "LPF") { LPF = true; }
                                else if (subLine[0] == "BGF") { BGF = true; }
                                else if (subLine[0] == "CONTRAST") { Contrast = int.Parse(subLine[1]); }
                                else if (subLine[0] == "MOBILESHARING") { Startup.MobileHotstop(); }
                                else if (subLine[0] == "CLEANFILES") { Startup.CleanFiles(); }
                            }
                            catch (Exception e) { }
                        }
                    }
                }
            }
            catch { }
        }

        private void EdgesComp()
        {
            double ratio = screensSize.Width / (double)screensSize.Height;
            bool multipleScreen = ratio > (21.0 / 9.0);

            if (multipleScreen && !Screen2Visible)
            {
                System.Windows.MessageBox.Show("It appears you are using multiple monitors.\nMake sure to check the config file.");
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

        public void Close()
        {
            if (endPoint != null)
            {
                SendPayload(PayloadType.fixedColor, 0);
            }
        }
    }
}