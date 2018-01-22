using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;

namespace SynLight.Model
{
    public class Param_SynLight : AutoNodeMCU
    {
        #region variables
        public static string param = Properties.Settings.Default.path; //MOVE TO PROPERTIES.SETTINGS.DEFAULT

        #region getset
        private string tittle = "SynLight - ";
        public string Tittle
        {
            get
            {
                return tittle;
            }
            set
            {
                tittle = tittle.Split('-')[0] + "- " + value + "Hz";
                OnPropertyChanged("Tittle");
            }
        }
        private int index = 0;
        public int Index
        {
            get
            {
                return index;
            }
            set
            {
                index = value;
                OnPropertyChanged("Index");
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
                screenSelectionUpdated();
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
                screenSelectionUpdated();
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
                screenSelectionUpdated();
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
                screenSelectionUpdated();
            }
        }

        private void screenSelectionUpdated()
        {
            if (screenFull)
            {
                scannedArea = new Rectangle(0, 0, screensSize.Width, screensSize.Height);
            }
            if (screen1)
            {
                scannedArea = new Rectangle(0, 0, screen1Size.Width, screen1Size.Height);
            }
            if (screen2)
            {
                scannedArea = new Rectangle(screen1Size.Width, 0, screen1Size.Width+screen2Size.Width, screen2Size.Height);
            }
            if (screen3)
            {
                scannedArea = new Rectangle(screen1Size.Width + screen2Size.Width, 0, screen1Size.Width + screen2Size.Width+screen3Size.Width, screen3Size.Height);
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
                    Ratio = ratio;
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
                ratio = value;
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

        private static bool playPause = true;
        public bool PlayPause
        {
            get { return playPause; }
            set
            {
                playPause = value;
                OnPropertyChanged("PlayPause");
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

        protected bool edges = true;
        public bool Edges
        {
            get
            {
                return edges;
            }
            set
            {
                edges = value;
                OnPropertyChanged("Edges");
            }
        }
        private byte red = 10;
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
        private byte green = 10;
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
        private byte blue = 10;
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

        #endregion

        protected double A = 1.32;
        protected double B = 1;
        protected int blankCounter = 0;
        protected int maxBlankCounter = 5;
        protected int sleepTime = 5;
        protected static int currentSleepTime = 5;
        protected int moreTime = 0;
        protected int difference = 0;
        protected bool staticColorChanged = true;
        protected int staticColorCurrentTime = 0;
        protected int staticColorMaxTime = 20;

        protected Size screensSize = new Size((int)System.Windows.SystemParameters.VirtualScreenWidth, (int)System.Windows.SystemParameters.VirtualScreenHeight);
        protected Size screen1Size = new Size((int)System.Windows.SystemParameters.PrimaryScreenWidth, (int)System.Windows.SystemParameters.PrimaryScreenHeight);
        protected Size screen2Size = new Size((int)System.Windows.SystemParameters.PrimaryScreenWidth, (int)System.Windows.SystemParameters.PrimaryScreenHeight);
        protected Size screen3Size = new Size((int)System.Windows.SystemParameters.PrimaryScreenWidth, (int)System.Windows.SystemParameters.PrimaryScreenHeight);
        protected Size currentScreen=new Size((int)System.Windows.SystemParameters.PrimaryScreenWidth, (int)System.Windows.SystemParameters.PrimaryScreenHeight);

        protected Rectangle edgeLeft;
        protected Rectangle edgeRight;
        protected Rectangle edgeTop;
        protected Rectangle edgeBot;
        protected Rectangle scannedArea = new Rectangle(0,0, (int)System.Windows.SystemParameters.PrimaryScreenWidth, (int)System.Windows.SystemParameters.PrimaryScreenHeight);
        protected Bitmap bmpScreenshot;
        protected Bitmap scaledBmpScreenshot;
        protected Bitmap secondScaledBmpScreenshot;

        protected Bitmap bitmapedgeLeft;
        protected Bitmap bitmapedgeRight;
        protected Bitmap bitmapedgeTop;
        protected Bitmap bitmapedgeBot;
        protected Bitmap scalededgeLeft;
        protected Bitmap scalededgeRight;
        protected Bitmap scalededgeTop;
        protected Bitmap scalededgeBot;

        protected double sRed   = 255;
        protected double sGreen = 255;
        protected double sBlue  = 255;
        protected List<byte> LastByteToSend = new List<byte>(0);
        protected List<byte> newByteToSend = new List<byte>(0);
        protected List<byte> byteToSend;

        protected PerformanceCounter cpuCounter;
        protected bool screenConfigured = false;
        //protected int screenIndex = 0;

        #endregion

        public Param_SynLight()
        {
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            try
            {
                using (StreamReader sr = new StreamReader(param))
                {
                    string[] lines = sr.ReadToEnd().Split('\n');
                    foreach (string line in lines)
                    {
                        try
                        {
                            string[] subLine = line.Trim('\r').Split('=');
                            if (subLine[0] == "MAINSCREEN")
                            {
                                if (subLine[1]=="1")
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
                                screen1Size.Height = int.Parse(subLine[1].Split(',')[1]);
                            }
                            else if (subLine[0] == "SCREEN1")
                            {
                                screen1Size.Width   = int.Parse(subLine[1].Split(',')[0]);
                                screen1Size.Height  = int.Parse(subLine[1].Split(',')[1]);                                
                            }
                            else if (subLine[0] == "SCREEN2")
                            {
                                screen2Size.Width = int.Parse(subLine[1].Split(',')[0]);
                                screen2Size.Height = int.Parse(subLine[1].Split(',')[1]);
                                Screen2Visible = true;
                            }
                            else if (subLine[0] == "SCREEN3")
                            {
                                screen3Size.Width = int.Parse(subLine[1].Split(',')[0]);
                                screen3Size.Height = int.Parse(subLine[1].Split(',')[1]);
                                Screen3Visible = true;
                            }
                            else if (subLine[0] == "Y")
                            {
                                Height = int.Parse(subLine[1]);
                            }
                            else if (subLine[0] == "S")
                            {
                                Shifting = int.Parse(subLine[1]);
                            }
                            else if (subLine[0] == "UDP_port")
                            {
                                UDP_Port = int.Parse(subLine[1]);
                            }
                            else if (subLine[0] == "IP")
                            {
                                if (!connected || true)
                                {
                                    arduinoIP = IPAddress.Parse(subLine[1]);
                                    endPoint = new IPEndPoint(arduinoIP, UDP_Port);
                                }
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
                            else if (subLine[0] == "CORNERS")
                            {
                                Corner = int.Parse(subLine[1]);
                            }
                            else if (subLine[0] == "UPDOWN")
                            {
                                UpDown = int.Parse(subLine[1]);
                            }
                            else if (subLine[0] == "SLEEPTIME")
                            {
                                sleepTime = int.Parse(subLine[1]);
                            }
                            /*
                            else if (subLine[0] == "QUERRY")
                            {
                                querry = subLine[1];
                            }
                            else if (subLine[0] == "ANSWER")
                            {
                                answer = subLine[1];
                            }*/
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }
        private void EdgesComp()
        {
            bitmapedgeTop   = new Bitmap(1, height);
            bitmapedgeBot   = new Bitmap(1, height);
            bitmapedgeLeft  = new Bitmap(width, 1);
            bitmapedgeRight = new Bitmap(width, 1);

            //experimeting with multiple screen :
            screenSelectionUpdated();

            double ratio = screensSize.Width / screensSize.Height;
            bool multipleScreen = ratio > (21.0 / 9.0);

            Size tmp = screen1Size;

            if(multipleScreen && !Screen2Visible)
            {
                System.Windows.MessageBox.Show("It appears you are using multiple screens.\nMake sure to check the config file.");
                tmp = screensSize;
            }
            else if(multipleScreen && Screen2Visible)
            {
                tmp = screen2Size;
            }
            else
            {
                tmp = screen1Size;
            }

            edgeLeft    = new Rectangle(0, 0, tmp.Width / Width, tmp.Height);
            edgeRight   = new Rectangle(tmp.Width - (tmp.Width / Width), 0, (tmp.Width / Width), tmp.Height);
            edgeTop     = new Rectangle(0, 0, tmp.Width, (tmp.Height / Height));
            edgeBot     = new Rectangle(0, tmp.Height - (tmp.Height / Height), tmp.Height, tmp.Height / Height);
        }

        public static void Close()
        {
            sock.SendTo(new byte[1] { 2 }, endPoint);
        }
    }
}