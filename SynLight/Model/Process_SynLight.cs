using System;
using System.Collections.Generic;
using System.Windows;
using System.Drawing;
using System.Threading;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Diagnostics;

namespace SynLight.Model
{
    public class Process_SynLight : Param_SynLight
    {
        public Process_SynLight()
        {
            bmpScreenshot = new Bitmap((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight);
            processFindESP = new Thread(FindESP);
            processMainLoop = new Thread(CheckMethod);
            processFindESP.Start();
        }

        private void FindESP()
        {
            while (!staticConnected)
            {
                //If not connected, try to reconnect
                Tittle = "SynLight - Trying to connect ...";
                FindNodeMCU();
                if(staticConnected) { break; }
                Thread.Sleep(2000);
            }
            CanPlayPause = true;
            PlayPause = true;
            processMainLoop.Start();
        }

        #region Privates methodes
        Stopwatch watch;
        private void CheckMethod()
        {            
            while (PlayPause)
            {
                //Start measuring how much time it takes to complete the task from here ... [1]
                watch = Stopwatch.StartNew();

                //Soon to be changed for a version where a static color can be added on top of the screen capture with an alpha channel
                if (Mix < 100)
                {
                    Tick();
                    Thread.Sleep(difference);
                }
                else if (Mix == 100)
                {
                    //Only send the static color payload once in a while, or if the selected color has changed
                    for(int n=0;((n< 100) && (!staticColorChanged));n++)
                    {
                        Thread.Sleep(10);
                    }
                    SingleColor();
                }

                GC.Collect(); //Saves a couple MB

                //[1] ... to here
                watch.Stop();
                int Hz = (int)(1000.0 / watch.ElapsedMilliseconds);
                Tittle = "Synlight - " + Hz.ToString() + "Hz";
            }
            //Immediately turns of the LEDS after pressing the Stop button
            SendPayload(PayloadType.fixedColor, 0);
            processMainLoop = new Thread(CheckMethod);

            Tittle = "Synlight - Paused";
        }

        private int _Height;
        private int _Wigth;
        private int _Corner;
        private int _Shifting;
        private bool tryGetAndProcessScreenPart = true;
        private void Tick()
        {
            //Freezing the values for this loop
            _Height = Height;
            _Wigth = Width;
            _Corner = Corner;
            _Shifting = Shifting;
            
            //GetScreenShot(); //Old method
            GetScreenShotedges();

            //Test for standalone beacon
            if (tryGetAndProcessScreenPart)
            {
                Rectangle rect = new Rectangle(10, 10, 500, 500); //Paramters : X,Y,Width,Height - 0,0 is the top-left corner
                GetAndProcessScreenPart(rect);
                tryGetAndProcessScreenPart = false; //To execute only once, remove for continuous operation
            }
            
            if (Contrast > 0)
            {
                scaledBmpScreenshot = AdjustContrast(scaledBmpScreenshot, (float)Contrast);
            }
            ProcessScreenShot();
            if(Mix > 0)
            {
                int bl = Mix;
                List<byte> bts = new List<byte>(byteToSend);
                for (int n = 0; n < byteToSend.Count - 2; n += 3)
                {
                    bts[n] = (byte)(byteToSend[n] * ((100.0 - bl)/100.0) + Red * (bl/100.0));
                    bts[n+1] = (byte)(byteToSend[n+1] * ((100.0 - bl) / 100.0) + Green * (bl / 100.0));
                    bts[n+2] = (byte)(byteToSend[n+2] * ((100.0 - bl) / 100.0) + Blue * (bl / 100.0));
                }
                byteToSend = new List<byte>(bts);
            }
            Send();
        }
        #region Legacy
        private static Bitmap AdjustContrast(Bitmap Image, float Value) //Copy/Pasted from stackoverflow
        {
            Value = (100.0f + Value) / 100.0f;
            Value *= Value;
            Bitmap NewBitmap = (Bitmap)Image.Clone();
            BitmapData data = NewBitmap.LockBits(new Rectangle(0, 0, NewBitmap.Width, NewBitmap.Height),ImageLockMode.ReadWrite,NewBitmap.PixelFormat);
            int Height = NewBitmap.Height;
            int Width = NewBitmap.Width;

            unsafe
            {
                for (int y = 0; y < Height; ++y)
                {
                    byte* row = (byte*)data.Scan0 + (y * data.Stride);
                    int columnOffset = 0;
                    for (int x = 0; x < Width; ++x)
                    {
                        byte B = row[columnOffset];
                        byte G = row[columnOffset + 1];
                        byte R = row[columnOffset + 2];

                        float Red = R / 255.0f;
                        float Green = G / 255.0f;
                        float Blue = B / 255.0f;
                        Red = (((Red - 0.5f) * Value) + 0.5f) * 255.0f;
                        Green = (((Green - 0.5f) * Value) + 0.5f) * 255.0f;
                        Blue = (((Blue - 0.5f) * Value) + 0.5f) * 255.0f;

                        int iR = (int)Red;
                        iR = iR > 255 ? 255 : iR;
                        iR = iR < 0 ? 0 : iR;
                        int iG = (int)Green;
                        iG = iG > 255 ? 255 : iG;
                        iG = iG < 0 ? 0 : iG;
                        int iB = (int)Blue;
                        iB = iB > 255 ? 255 : iB;
                        iB = iB < 0 ? 0 : iB;

                        row[columnOffset] = (byte)iB;
                        row[columnOffset + 1] = (byte)iG;
                        row[columnOffset + 2] = (byte)iR;

                        columnOffset += 4;
                    }
                }
            }

            NewBitmap.UnlockBits(data);

            return NewBitmap;
        }

        private void GetScreenShot()
        {
            //This method is much slower since it captures the whole screen
            //It has been replaced with a faster method, that only captures the edges
            try
            {
                Graphics gfxScreenshot = Graphics.FromImage(bmpScreenshot);
                gfxScreenshot.CopyFromScreen(0, 0, 0, 0, currentScreen);
                scaledBmpScreenshot = new Bitmap(bmpScreenshot, Width, Height);
                gfxScreenshot.Clear(Color.Empty);
                //Resize(scaledBmpScreenshot).Save("6regular.bmp");
            }
            catch
            {
                scaledBmpScreenshot = new Bitmap(1, 1);
                scaledBmpScreenshot.SetPixel(0, 0, Color.Black);
            }
        }

        private bool saveBmpBeacon = true;
        private void GetAndProcessScreenPart(Rectangle _rect)
        {
            try
            {
                //Sanitizing
                Rect rect = new Rect
                {
                    X = Math.Min(screensSize.Width, Math.Max(_rect.X, 0)),
                    Y = Math.Min(screensSize.Height, Math.Max(_rect.Y, 0)),
                    Width = Math.Min(screensSize.Width, Math.Max(_rect.Width, 1)),
                    Height = Math.Min(screensSize.Height, Math.Max(_rect.Height, 1))
                };

                //Copy from GetScreenShotedges
                Bitmap bmpBeacon = new Bitmap((int)rect.Width, (int)rect.Height, PixelFormat.Format32bppRgb);
                Graphics gfxScreenshot = Graphics.FromImage(bmpBeacon);
                //Should always be Left/Top/0/0
                gfxScreenshot.CopyFromScreen((int)rect.Left, (int)rect.Top, 0, 0, bmpBeacon.Size);

                //Rescaling depending on what needs to be sent
                //1 color or one LED -> 1,1
                bmpBeacon = new Bitmap(bmpBeacon, 1, 1);

                //Scanning through the bitmap, creating to payload
                byte r = bmpBeacon.GetPixel(0, 0).R;
                byte g = bmpBeacon.GetPixel(0, 0).G;
                byte b = bmpBeacon.GetPixel(0, 0).B;
                List<byte> screenPartToSend = new List<byte>();
                screenPartToSend.Add(r);
                screenPartToSend.Add(g);
                screenPartToSend.Add(b);

                //Network parameters
                System.Net.IPAddress ip = System.Net.IPAddress.Parse("192.168.1.123");
                int port = 8787;
                System.Net.IPEndPoint edp = new System.Net.IPEndPoint(ip,port);

                //Sending with custom Endpoint
                SendPayload(PayloadType.fixedColor, screenPartToSend, edp);

                //Capturing the very first frame
                if (saveBmpBeacon)
                {
                    try
                    {
                        saveBmpBeacon = false;
                        Resize(bmpBeacon).Save("5full.bmp");
                    }
                    catch
                    {
                    }
                }

                gfxScreenshot.Clear(Color.Empty);
            }
            catch
            {
            }
        }
        private void GetScreenShotedges()
        {
            try
            {
                //MULTIPLE MONITORS FIESTA
                startX = scannedArea.X;
                startY = scannedArea.Y;
                endX = scannedArea.Width;
                endY = scannedArea.Height;
                hX = endX - startX;
                hY = endY - startY;

                startY += ((_Shifting * hY) / _Height) / 2;
                endY -= ((_Shifting * hY) / _Height) / 2;

                rect = new Rectangle(startX, startY, startX + (hX / _Wigth), endY);
                bmp = new Bitmap(hX / _Wigth, hY, PixelFormat.Format32bppRgb);
                Graphics gfxScreenshot = Graphics.FromImage(bmp);
                gfxScreenshot.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size);
                scalededgeLeft = new Bitmap(bmp, 1, _Height);
                gfxScreenshot.Clear(Color.Empty);

                rect = new Rectangle(endX - (hX / _Wigth), startY, startX + (hX / _Wigth), endY);
                bmp = new Bitmap(hX / _Wigth, hY, PixelFormat.Format32bppRgb);
                Graphics gfxScreenshot2 = Graphics.FromImage(bmp);
                gfxScreenshot2.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size);
                scalededgeRight = new Bitmap(bmp, 1, _Height);
                gfxScreenshot2.Clear(Color.Empty);

                rect = new Rectangle(startX, startY, endX, startY + (hY / _Height));
                bmp = new Bitmap(hX, hY / _Height, PixelFormat.Format32bppRgb);
                Graphics gfxScreenshot3 = Graphics.FromImage(bmp);
                gfxScreenshot3.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size);
                scalededgeTop = new Bitmap(bmp, _Wigth, 1);
                gfxScreenshot3.Clear(Color.Empty);

                rect = new Rectangle(startX, endY - (hY / _Height), endX, endY);
                bmp = new Bitmap(hX, hY / _Height, PixelFormat.Format32bppRgb);
                Graphics gfxScreenshot4 = Graphics.FromImage(bmp);
                gfxScreenshot4.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size);
                scalededgeBot = new Bitmap(bmp, _Wigth, 1);
                gfxScreenshot4.Clear(Color.Empty);

                scaledBmpScreenshot = new Bitmap(_Wigth, _Height);

                for (int n = 0; n < scalededgeLeft.Height; n++)
                {
                    scaledBmpScreenshot.SetPixel(0, n, scalededgeLeft.GetPixel(0, n));
                    scaledBmpScreenshot.SetPixel(_Wigth - 1, n, scalededgeRight.GetPixel(0, n));
                }
                for (int n = 1; n < scalededgeTop.Width - 1; n++)
                {
                    scaledBmpScreenshot.SetPixel(n, 0, scalededgeTop.GetPixel(n, 0));
                    scaledBmpScreenshot.SetPixel(n, _Height - 1, scalededgeBot.GetPixel(n, 0));
                }

                //Capturing the very first frame in various formats
                if (debug)
                {
                    try
                    {
                        debug = false;
                        ResizeSizes(scalededgeLeft).Save("1Left.bmp");
                        ResizeSizes(scalededgeRight).Save("3Right.bmp");
                        ResizeTops(scalededgeTop).Save("2Top.bmp");
                        ResizeTops(scalededgeBot).Save("4Bot.bmp");
                        Resize(scaledBmpScreenshot).Save("5full.bmp");
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
                scalededgeLeft = new Bitmap(1, 1);
                scalededgeLeft.SetPixel(0, 0, Color.Black);
            }
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
        private Bitmap ResizeSizes(Bitmap srcImage)
        {
            Bitmap newImage = new Bitmap((int)SystemParameters.PrimaryScreenWidth / Width, (int)SystemParameters.PrimaryScreenHeight);
            using (Graphics gr = Graphics.FromImage(newImage))
            {
                gr.SmoothingMode = SmoothingMode.HighQuality;
                gr.InterpolationMode = InterpolationMode.NearestNeighbor;
                gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                gr.DrawImage(srcImage, new Rectangle(0, 0, (int)SystemParameters.PrimaryScreenWidth / Width, (int)SystemParameters.PrimaryScreenHeight));
            }
            return newImage;
        }
        private Bitmap ResizeTops(Bitmap srcImage)
        {
            Bitmap newImage = new Bitmap((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight / Height);
            using (Graphics gr = Graphics.FromImage(newImage))
            {
                gr.SmoothingMode = SmoothingMode.HighQuality;
                gr.InterpolationMode = InterpolationMode.NearestNeighbor;
                gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                gr.DrawImage(srcImage, new Rectangle(0, 0, (int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight / Height));
            }
            return newImage;
        }
        private void ProcessScreenShot()
        {
            byteToSend = new List<byte>();
            int subCorner = Math.Max(0, _Corner - 1);

            if (Clockwise)
            {
                if (TopLeft)
                {
                    for (int x = _Corner; x < scaledBmpScreenshot.Width - 1 - _Corner; x++)
                    {
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).R));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).G));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).B));
                    }
                    for (int y = subCorner; y < scaledBmpScreenshot.Height - 1 - subCorner; y++)
                    {
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                    }
                    for (int x = scaledBmpScreenshot.Width - 1 - _Corner; x > _Corner; x--)
                    {
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).R));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).G));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).B));
                    }
                    for (int y = scaledBmpScreenshot.Height - 1 - subCorner; y > subCorner; y--)
                    {
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                    }
                }
                if (TopRight)
                {
                    for (int y = subCorner; y < scaledBmpScreenshot.Height - 1 - subCorner; y++)
                    {
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                    }
                    for (int x = scaledBmpScreenshot.Width - 1 - _Corner; x > _Corner; x--)
                    {
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).R));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).G));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).B));
                    }
                    for (int y = scaledBmpScreenshot.Height - 1 - subCorner; y > subCorner; y--)
                    {
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                    }
                    for (int x = _Corner; x < scaledBmpScreenshot.Width - 1 - _Corner; x++)
                    {
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).R));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).G));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).B));
                    }
                }
                if (BotRight)
                {
                    for (int x = scaledBmpScreenshot.Width - 1 - _Corner; x > _Corner; x--)
                    {
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).R));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).G));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).B));
                    }
                    for (int y = scaledBmpScreenshot.Height - 1 - subCorner; y > subCorner; y--)
                    {
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                    }
                    for (int x = _Corner; x < scaledBmpScreenshot.Width - 1 - _Corner; x++)
                    {
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).R));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).G));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).B));
                    }
                    for (int y = subCorner; y < scaledBmpScreenshot.Height - 1 - subCorner; y++)
                    {
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                    }
                }
                if (BotLeft)
                {
                    for (int y = scaledBmpScreenshot.Height - 1 - subCorner; y > subCorner; y--)
                    {
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                    }
                    for (int x = _Corner; x < scaledBmpScreenshot.Width - 1 - _Corner; x++)
                    {
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).R));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).G));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).B));
                    }
                    for (int y = subCorner; y < scaledBmpScreenshot.Height - 1 - subCorner; y++)
                    {
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                    }
                    for (int x = scaledBmpScreenshot.Width - 1 - _Corner; x > _Corner; x--)
                    {
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).R));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).G));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).B));
                    }
                }
            }
            else
            {
                if (TopLeft)
                {
                    for (int y = subCorner; y < scaledBmpScreenshot.Height - 1 - subCorner; y++)
                    {
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                    }
                    for (int x = _Corner; x < scaledBmpScreenshot.Width - 1 - _Corner; x++)
                    {
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).R));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).G));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).B));
                    }
                    for (int y = scaledBmpScreenshot.Height - 1 - subCorner; y > subCorner; y--)
                    {
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                    }
                    for (int x = scaledBmpScreenshot.Width - 1 - _Corner; x > _Corner; x--)
                    {
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).R));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).G));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).B));
                    }
                }
                if (TopRight)
                {
                    for (int x = scaledBmpScreenshot.Width - 1 - _Corner; x > _Corner; x--)
                    {
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).R));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).G));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).B));
                    }
                    for (int y = subCorner; y < scaledBmpScreenshot.Height - 1 - subCorner; y++)
                    {
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                    }
                    for (int x = _Corner; x < scaledBmpScreenshot.Width - 1 - _Corner; x++)
                    {
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).R));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).G));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).B));
                    }
                    for (int y = scaledBmpScreenshot.Height - 1 - subCorner; y > subCorner; y--)
                    {
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                    }
                }
                if (BotRight)
                {
                    for (int y = scaledBmpScreenshot.Height - 1 - subCorner; y > subCorner; y--)
                    {
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                    }
                    for (int x = scaledBmpScreenshot.Width - 1 - _Corner; x > _Corner; x--)
                    {
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).R));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).G));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).B));
                    }
                    for (int y = subCorner; y < scaledBmpScreenshot.Height - 1 - subCorner; y++)
                    {
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                    }
                    for (int x = _Corner; x < scaledBmpScreenshot.Width - 1 - _Corner; x++)
                    {
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).R));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).G));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).B));
                    }
                }
                if (BotLeft)
                {
                    for (int x = _Corner; x < scaledBmpScreenshot.Width - 1 - _Corner; x++)
                    {
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).R));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).G));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).B));
                    }
                    for (int y = scaledBmpScreenshot.Height - 1 - subCorner; y > subCorner; y--)
                    {
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                    }
                    for (int x = scaledBmpScreenshot.Width - 1 - _Corner; x > _Corner; x--)
                    {
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).R));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).G));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).B));
                    }
                    for (int y = subCorner; y < scaledBmpScreenshot.Height - 1 - subCorner; y++)
                    {
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                        byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                    }
                }
            }
        }
        private void SingleColor()
        {
            byteToSend = new List<byte>() { Red, Green, Blue };
            staticColorChanged = false;
            SendPayload(PayloadType.fixedColor, byteToSend);
        }
        #endregion

        //Send
        private ushort justBlack = 0;
        private bool black = false;
        private void Send()
        {
            Screen2Visible = true;
            Screen3Visible = true;

            //Old stuff. Latests W10 builds seem to support in-game screenshot ...
            #region If the screen is black ...
            black = true;
            foreach (byte b in byteToSend)
            {
                if (b != 0)
                {
                    black = false;
                    break;
                }
            }
            if (black)
            {
                if (justBlack++ > 5)
                {
                    justBlack = 0;
                    sock.SendTo(new byte[] { (byte)('A'), (byte)PayloadType.fixedColor, 5 }, endPoint);
                }
            }
            #endregion   
            else
            {
                newByteToSend = new List<byte>(0);

                if (LPF) //Low-pass filtering
                {
                    while (lastByteToSend.Count < byteToSend.Count) { lastByteToSend.Add(0); } //In case X/Y increased

                    int odd; //Rounding errorlastByteToSend.Add(0);
                    for (int n = 0; n < byteToSend.Count; n++)
                    {
                        odd = byteToSend[n] + lastByteToSend[n];

                        //To correct the -1 error rounding
                        if (odd % 2 != 0)
                        {
                            odd++;
                        }
                        odd /= 2;

                        newByteToSend.Add((byte)odd); //newByteToSend[n] = rounded sum
                    }
                    //lastByteToSend = new List<byte>(byteToSend);
                    lastByteToSend = new List<byte>(newByteToSend);
                }
                else
                {
                    lastByteToSend = newByteToSend = byteToSend;
                }

                if (UpDown != 0)
                {
                    RotateArray();
                }

                if (UsingFlux) //Reducing the temperature of the colors depending on the time of the day
                {
                    Flux();
                }

                if (staticColorChanged) //When changing tab, one frame could still be processing, so it is discarded here
                {
                    //return;??
                }

                CalculateSleepTime();
                //Console.Write("difference :" + difference + "  -  ");

                //Send standard packets if needed, then send the terminal payload
                for (int n = 0; n + packetSize <= byteToSend.Count; n += packetSize)
                {
                    SendPayload(PayloadType.multiplePayload, newByteToSend.GetRange(n, packetSize));
                }
                int index = newByteToSend.Count - (newByteToSend.Count % packetSize);
                SendPayload(PayloadType.terminalPayload, newByteToSend.GetRange(index, newByteToSend.Count % packetSize));
            }
        }

        //Difference
        private int difference = 0;
        private int lastDifference = -1; //Used for low-pass filtering
        private const int minDif = 100;
        private const int maxDif = 3600;
        private void CalculateSleepTime()
        {
            if (lastByteToSend.Count != byteToSend.Count)
            {
                difference = maxDif;
            }
            else
            {
                difference = 0;
                for (int n = 0; n < byteToSend.Count; n++)
                {
                    difference += Math.Abs(byteToSend[n] - lastByteToSend[n]);
                }
            }

            difference = Math.Min(difference, maxDif);
            difference = Math.Max(difference, minDif);
            difference -= minDif;
            difference = (int)Math.Round(Map(difference, 0, maxDif-minDif, minDif, 0));
            difference += (int)Math.Round(cpuCounter.NextValue());

            if(lastDifference != -1) //Skips if first time
            {
                difference = (difference + lastDifference) >> 1;
            }

            lastDifference = difference;
        }
        private double Map(double s, double a1, double a2, double b1, double b2)
        {
            return b1 + (s - a1) * (b2 - b1) / (a2 - a1);
        }

        //Rotate
        private void RotateArray()
        {
            List<byte> byteToSend2 = new List<byte>(newByteToSend);

            for (int n = 0; n < newByteToSend.Count; n++)
            {
                byteToSend2[n] = newByteToSend[(n + UpDown * 3) % (byteToSend.Count - 1)];
            }

            newByteToSend = new List<byte>(byteToSend2);
        }

        //Flux
        private string s;
        private byte b;
        private int hours = 0, minutes = 0, totalMinutes = 0;
        private DateTime now;
        private double fluxRatio;
        private void Flux()
        {
            now = DateTime.Now;
            hours = (24 - now.Hour) - 1;
            minutes = (60 - now.Minute) - 1;
            totalMinutes = (minutes) + (hours * 60);

            fluxRatio = (Math.Max(Math.Min(totalMinutes, 250.0), 0.0)) / 250.0;

            for (int n = 0; n < byteToSend.Count - 2; n += 3)
            {
                s = (newByteToSend[n] * ((1.5 + fluxRatio) / 2.5)).ToString().Replace('.',',').Split(',')[0];
                b = byte.Parse(s);
                newByteToSend[n] = b;

                s = (newByteToSend[n+1] * ((0.6+fluxRatio)/1.6)).ToString().Replace('.', ',').Split(',')[0];
                b = byte.Parse(s);
                newByteToSend[n+1] = b;

                s = (newByteToSend[n+2] * (fluxRatio/1.2)).ToString().Replace('.', ',').Split(',')[0];
                b = byte.Parse(s);
                newByteToSend[n+2] = b;
            }
        }
        #endregion
    }
}
