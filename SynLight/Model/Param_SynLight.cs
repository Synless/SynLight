using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        protected Size Screen = new Size((int)System.Windows.SystemParameters.PrimaryScreenWidth, (int)System.Windows.SystemParameters.PrimaryScreenHeight);
        protected Rectangle edgeLeft;
        protected Rectangle edgeRight;
        protected Rectangle edgeTop;
        protected Rectangle edgeBot;
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

        protected double sRed = 255;
        protected double sGreen = 255;
        protected double sBlue = 255;
        protected List<byte> LastByteToSend = new List<byte>(0);
        protected List<byte> newByteToSend = new List<byte>(0);
        protected List<byte> byteToSend;

        protected PerformanceCounter cpuCounter;
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
                            if (subLine[0] == "X")
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
                                clockwise = true;
                            }
                            else if (subLine[0] == "CCW")
                            {
                                clockwise = false;
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
            bitmapedgeTop = new Bitmap(1, height);
            bitmapedgeBot = new Bitmap(1, height);
            bitmapedgeLeft = new Bitmap(width, 1);
            bitmapedgeRight = new Bitmap(width, 1);
            edgeLeft = new Rectangle(0, 0, (int)(System.Windows.SystemParameters.PrimaryScreenWidth / Width), (int)System.Windows.SystemParameters.PrimaryScreenHeight);
            edgeRight = new Rectangle((int)(System.Windows.SystemParameters.PrimaryScreenWidth) - (int)(System.Windows.SystemParameters.PrimaryScreenWidth / Width), 0, (int)(System.Windows.SystemParameters.PrimaryScreenWidth / Width), (int)System.Windows.SystemParameters.PrimaryScreenHeight);
            edgeTop = new Rectangle(0, 0, (int)(System.Windows.SystemParameters.PrimaryScreenWidth), (int)(System.Windows.SystemParameters.PrimaryScreenHeight / Height));
            edgeBot = new Rectangle(0, (int)(System.Windows.SystemParameters.PrimaryScreenHeight) - (int)(System.Windows.SystemParameters.PrimaryScreenHeight / Height), (int)(System.Windows.SystemParameters.PrimaryScreenWidth), (int)(System.Windows.SystemParameters.PrimaryScreenHeight / Height));
        }

        public static void Close()
        {
            sock.SendTo(new byte[1] { 2 }, endPoint);
        }
    }
}