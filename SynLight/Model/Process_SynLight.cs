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
            processFindArduino = new Thread(FindArduinoProcess);
            processMainLoop = new Thread(CheckMethodProcess);
            processFindArduino.Start();
        }
        ~Process_SynLight()
        {
            //Immediately turns of the LEDS after pressing the Stop button
            for (int i = 0; i < newByteToSend.Count; i++)
                newByteToSend[i] = 0;

            SendPayload(PayloadType.terminalPayload, newByteToSend);
        }

        const int numberOfTries = 2;
        private void FindArduinoProcess()
        {
            while (!StaticConnected)
            {
                Tittle = "SynLight - " + (useComPort ? "[COM]" : "[WIFI]") + " Trying to connect ...";
                
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
                    //MessageBox.Show("Could not find any Arduino on any " + (useComPort ? "COM port" : "IP address"));
                    useComPort = !useComPort;
                }
                
                Thread.Sleep(2000);
            }

            CanPlayPause = true;
            PlayPause = true;
            processMainLoop.Start();
        }

        #region Privates methodes
        private void CheckMethodProcess()
        {
            Stopwatch watch;

            while (PlayPause)
            {
                watch = Stopwatch.StartNew();

                Tick();

                Thread.Sleep(difference);

                if (Mix == 100)
                    Thread.Sleep(500);

                GC.Collect();

                watch.Stop();

                int Hz = (int)(1000.0 / watch.ElapsedMilliseconds);

                try
                {
                    if (useComPort)
                    {
                        if (StaticConnected)
                        {
                            Tittle = "Synlight - " + arduinoSerial.PortName + " - " + Hz.ToString() + "Hz";
                        }
                    }
                    else
                    {
                        if (arduinoUDP.EndPoint != null)
                        {
                            Tittle = "Synlight - " + arduinoUDP.IPAddress.ToString() + " - " + Hz.ToString() + "Hz";
                        }
                    }
                }
                catch
                {
                }
            }

            //Immediately turns of the LEDS after pressing the Stop button
            for (int i = 0; i < newByteToSend.Count; i++)
                newByteToSend[i] = 0;
            
            SendPayload(PayloadType.terminalPayload, newByteToSend);


            processMainLoop = new Thread(CheckMethodProcess);
            Tittle = "Synlight - " + (useComPort ? arduinoSerial.PortName : arduinoUDP.IPAddress.ToString()) + " - Paused";
        }

        private int _Height;
        private int _Width;
        private int _Corner;
        private int _Shifting;
        private void Tick()
        {
            //Freezing the values for this loop
            _Height = Height;
            _Width = Width;
            _Corner = Corner;
            _Shifting = Shifting;

            GetScreenShotedges();

            if (Contrast > 0) { scaledBmpScreenshot = AdjustContrast(scaledBmpScreenshot, Contrast); }

            ProcessScreenShot();

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

            Send();
        }

        private static Bitmap AdjustContrast(Bitmap Image, float Value) //Copy/Paste from stackoverflow
        {
            Value = (100.0f + Value) / 100.0f;
            Value *= Value;
            Bitmap NewBitmap = (Bitmap)Image.Clone();
            BitmapData data = NewBitmap.LockBits(new Rectangle(0, 0, NewBitmap.Width, NewBitmap.Height), ImageLockMode.ReadWrite, NewBitmap.PixelFormat);
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

        private readonly byte BrightnessForKeyboard = 15;
        private void GetScreenShotedges()
        {
            try
            {
                //MULTIPLE MONITORS
                startX = scannedArea.X;
                startY = scannedArea.Y;
                endX = scannedArea.Width;
                endY = scannedArea.Height;
                hX = endX - startX;
                hY = endY - startY;

                startY += ((_Shifting * hY) / _Height) / 2;
                endY -= ((_Shifting * hY) / _Height) / 2;

                rect = new Rectangle(startX, startY, startX + (hX / _Width), endY);
                bmp = new Bitmap(hX / _Width, hY, PixelFormat.Format32bppRgb);
                Graphics gfxScreenshot = Graphics.FromImage(bmp);
                gfxScreenshot.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size);
                scalededgeLeft = RescaleImage(bmp, new System.Drawing.Size(1, _Height));
                gfxScreenshot.Clear(Color.Empty);

                rect = new Rectangle(endX - (hX / _Width), startY, startX + (hX / _Width), endY);
                bmp = new Bitmap(hX / _Width, hY, PixelFormat.Format32bppRgb);
                Graphics gfxScreenshot2 = Graphics.FromImage(bmp);
                gfxScreenshot2.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size);
                scalededgeRight = RescaleImage(bmp, new System.Drawing.Size(1, _Height));
                gfxScreenshot2.Clear(Color.Empty);

                rect = new Rectangle(startX, startY, endX, startY + (hY / _Height));
                bmp = new Bitmap(hX, hY / _Height, PixelFormat.Format32bppRgb);
                Graphics gfxScreenshot3 = Graphics.FromImage(bmp);
                gfxScreenshot3.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size);
                scalededgeTop = RescaleImage(bmp, new System.Drawing.Size(_Width, 1));
                gfxScreenshot3.Clear(Color.Empty);

                rect = new Rectangle(startX, endY - (hY / _Height), endX, endY);
                bmp = new Bitmap(hX, hY / _Height, PixelFormat.Format32bppRgb);
                Graphics gfxScreenshot4 = Graphics.FromImage(bmp);
                gfxScreenshot4.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size);
                scalededgeBot = RescaleImage(bmp, new System.Drawing.Size(_Width, 1));
                gfxScreenshot4.Clear(Color.Empty);

                scaledBmpScreenshot = new Bitmap(_Width, _Height);

                for (int n = 0; n < scalededgeLeft.Height; n++)
                {
                    scaledBmpScreenshot.SetPixel(0, n, scalededgeLeft.GetPixel(0, n));
                    scaledBmpScreenshot.SetPixel(_Width - 1, n, scalededgeRight.GetPixel(0, n));
                }
                for (int n = 1; n < scalededgeTop.Width - 1; n++)
                {
                    if (KeyboardLight)
                        scaledBmpScreenshot.SetPixel(n, _Height - 1, Color.FromArgb(255,
                                                                                    Math.Min(scalededgeBot.GetPixel(n, 0).R + BrightnessForKeyboard, byte.MaxValue),
                                                                                    Math.Min(scalededgeBot.GetPixel(n, 0).G + BrightnessForKeyboard, byte.MaxValue),
                                                                                    Math.Min(scalededgeBot.GetPixel(n, 0).B + BrightnessForKeyboard, byte.MaxValue)));
                    else
                    {
                        scaledBmpScreenshot.SetPixel(Math.Max(0, Math.Min(n, scaledBmpScreenshot.Width - 1)), _Height - 1, scalededgeBot.GetPixel(n, 0));
                    }

                    scaledBmpScreenshot.SetPixel(Math.Max(0, Math.Min(n, scaledBmpScreenshot.Width - 1)), 0, scalededgeTop.GetPixel(n, 0));
                }

                //Capturing the very first frame for debugging purpose
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
                    catch
                    {
                    }
                }
            }
            catch { }
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

            if (TopLeft)
            {
                for (int y = subCorner; y < scaledBmpScreenshot.Height - 1 - subCorner; y++)
                {
                    processedHeight = true;
                    byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                    byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                    byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                }
                for (int x = _Corner; x <= scaledBmpScreenshot.Width - 1 - _Corner; x++)
                {
                    byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).R));
                    byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).G));
                    byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).B));
                }

                if (!processedHeight)
                    return;

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
                for (int x = scaledBmpScreenshot.Width - 1 - _Corner; x >= _Corner; x--)
                {
                    byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).R));
                    byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).G));
                    byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).B));
                }
                for (int y = subCorner; y < scaledBmpScreenshot.Height - 1 - subCorner; y++)
                {
                    processedHeight = true;
                    byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                    byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                    byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                }

                if (!processedHeight)
                    return;

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
                    processedHeight = true;
                    byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                    byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                    byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                }
                for (int x = scaledBmpScreenshot.Width - 1 - _Corner; x >= _Corner; x--)
                {
                    byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).R));
                    byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).G));
                    byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).B));
                }

                if (!processedHeight)
                    return;

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
                for (int x = _Corner; x <= scaledBmpScreenshot.Width - 1 - _Corner; x++)
                {
                    byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).R));
                    byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).G));
                    byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).B));
                }
                for (int y = scaledBmpScreenshot.Height - 1 - subCorner; y > subCorner; y--)
                {
                    processedHeight = true;
                    byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                    byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                    byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                }

                if (!processedHeight)
                    return;

                for (int x = scaledBmpScreenshot.Width - 1 - _Corner; x >= _Corner; x--)
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

            if (Clockwise) //Revert the array by chunks of 3 for clockwise setup
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
            newByteToSend = new List<byte>(0);            

            if (LPF) //Low-pass filtering
            {
                while (lastByteToSend.Count < byteToSend.Count) { lastByteToSend.Add(0); }

                int odd; //To correct the -1 error rounding
                for (int n = 0; n < byteToSend.Count; n++)
                {
                    odd = byteToSend[n] + lastByteToSend[n];
                    if (odd % 2 != 0) { odd++; }
                    odd /= 2;
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
                for (int n = 0; n < newByteToSend.Count; n++) { rotatedByteToSend[n] = newByteToSend[(n + UpDown * 3) % (byteToSend.Count)]; }
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

            CalculateSleepTime();

            //Send standard packets if needed, then send the terminal payload
            for (int n = 0; n + packetSize <= byteToSend.Count; n += packetSize)
                SendPayload(PayloadType.multiplePayload, newByteToSend.GetRange(n, packetSize));

            int index = newByteToSend.Count - (newByteToSend.Count % packetSize);
            SendPayload(PayloadType.terminalPayload, newByteToSend.GetRange(index, newByteToSend.Count % packetSize));

        }

        //Difference
        private int difference = 0;
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
                    difference += Math.Abs(byteToSend[n] - lastByteToSend[n]);
            }

            difference = Math.Min(difference, maxDif);
            difference = Math.Max(difference, minDif);
            difference -= minDif;
            difference = (int)Math.Round(Map(difference, 0, maxDif - minDif, minDif, 0));
            if (usePerformanceCounter)
                difference += (int)Math.Round(cpuCounter.NextValue());

            if (Turbo)
                difference /= 5;
        }
        private double Map(double s, double a1, double a2, double b1, double b2)
        {
            return b1 + (s - a1) * (b2 - b1) / (a2 - a1);
        }
        #endregion
        protected static void SendPayload(PayloadType plt, List<byte> payload)
        {
            if (useComPort)
            {
                arduinoSerial.Send(plt, payload);
            }
            else
            {
                arduinoUDP.Send(plt, payload);
            }
        }
    }
}