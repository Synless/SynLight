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
            process_findESP = new Thread(findESP);
            process_mainLoop = new Thread(CheckMethod);
            process_findESP.Start();
        }

        private void findESP()
        {
            while (!staticConnected)
            {
                //IF NOT CONNECTED, TRY TO RECONNECT
                Tittle = "SynLight - Trying to connect ...";
                FindNodeMCU();
                Thread.Sleep(2000);
            }
            CanPlayPause = true;
            PlayPause = true;
            process_mainLoop.Start();
        }
        #region Privates methodes
        private void CheckMethod()
        {
            while (PlayPause)
            {
                Stopwatch watch = Stopwatch.StartNew();
                if (Index == 0)
                {
                    Tick();
                }
                else if (Index == 1)
                {
                    SingleColor();
                }

                Thread.Sleep(currentSleepTime);
                GC.Collect(); //COUPLES OF MB WON
                watch.Stop();

                int Hz = (int)(1000.0 / watch.ElapsedMilliseconds);
                Tittle = "Synlight - " + Hz.ToString() + "Hz";
            }
            SendPayload(PayloadType.fixedColor, 0);
            process_mainLoop = new Thread(CheckMethod);

            Tittle = "Synlight - Paused";
        }

        private void Tick()
        {
            if (!edges)
            {
                GetScreenShot();
            }
            else
            {
                GetScreenShotedges();
            }
            if(ContrastEnable)
                scaledBmpScreenshot = AdjustContrast(scaledBmpScreenshot, (float)Contrast);
            ProcessScreenShot();
            Send();
        }

        public static Bitmap AdjustContrast(Bitmap Image, float Value)
        {
            Value = (100.0f + Value) / 100.0f;
            Value *= Value;
            Bitmap NewBitmap = (Bitmap)Image.Clone();
            BitmapData data = NewBitmap.LockBits(
                new Rectangle(0, 0, NewBitmap.Width, NewBitmap.Height),
                ImageLockMode.ReadWrite,
                NewBitmap.PixelFormat);
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
            try
            {
                Graphics gfxScreenshot = Graphics.FromImage(bmpScreenshot); //1
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
        private void GetScreenShotedges()
        {
            try
            {
                #region old single screen
                /*
                Rectangle screenToCapture;
                int xScreen = (int)(SystemParameters.PrimaryScreenWidth);
                int yScreen = (int)(SystemParameters.PrimaryScreenHeight);

                Rectangle rect = new Rectangle(0, 0, xScreen / Width, yScreen);
                Bitmap bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
                Graphics gfxScreenshot = Graphics.FromImage(bmp);
                gfxScreenshot.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size);
                scalededgeLeft = new Bitmap(bmp, 1, Height);
                gfxScreenshot.Clear(Color.Empty);

                rect = new Rectangle(xScreen - (xScreen / Width) - 1, 0, xScreen / Width, yScreen);
                bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
                Graphics gfxScreenshot2 = Graphics.FromImage(bmp);
                gfxScreenshot2.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size);
                scalededgeRight = new Bitmap(bmp, 1, Height);
                gfxScreenshot2.Clear(Color.Empty);

                rect = new Rectangle(0, 0, xScreen, yScreen / Height);
                bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
                Graphics gfxScreenshot3 = Graphics.FromImage(bmp);
                gfxScreenshot3.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size);
                scalededgeTop = new Bitmap(bmp, Width, 1);
                gfxScreenshot3.Clear(Color.Empty);

                rect = new Rectangle(0, yScreen - (yScreen / Height) - 1, xScreen, yScreen / Height);
                bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
                Graphics gfxScreenshot4 = Graphics.FromImage(bmp);
                gfxScreenshot4.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size);
                scalededgeBot = new Bitmap(bmp, Width, 1);
                gfxScreenshot4.Clear(Color.Empty);
                */
                /*t xScreen = (scannedArea.Width-scannedArea.X);
                int yScreen = (scannedArea.Height-scannedArea.Y);*/
                #endregion

                //MULTIPLE MONITORS FIESTA
                startX = scannedArea.X;
                startY = scannedArea.Y;
                endX = scannedArea.Width;
                endY = scannedArea.Height;
                hX = endX - startX;
                hY = endY - startY;

                startY += ((Shifting * hY) / Height)/2;
                endY -= ((Shifting * hY) / Height) / 2;

                rect = new Rectangle(startX, startY, startX+(hX/Width), endY);
                bmp = new Bitmap(hX/Width, hY, PixelFormat.Format32bppRgb);
                Graphics gfxScreenshot = Graphics.FromImage(bmp);
                gfxScreenshot.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size);
                scalededgeLeft = new Bitmap(bmp, 1, Height);
                gfxScreenshot.Clear(Color.Empty);

                rect = new Rectangle(endX-(hX/Width), startY, startX + (hX / Width), endY);
                bmp = new Bitmap(hX/Width, hY, PixelFormat.Format32bppRgb);
                Graphics gfxScreenshot2 = Graphics.FromImage(bmp);
                gfxScreenshot2.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size);
                scalededgeRight = new Bitmap(bmp, 1, Height);
                gfxScreenshot2.Clear(Color.Empty);

                rect = new Rectangle(startX, startY, endX, startY+(hY/Height));
                bmp = new Bitmap(hX, hY/Height, PixelFormat.Format32bppRgb);
                Graphics gfxScreenshot3 = Graphics.FromImage(bmp);
                gfxScreenshot3.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size);
                scalededgeTop = new Bitmap(bmp, Width, 1);
                gfxScreenshot3.Clear(Color.Empty);

                rect = new Rectangle(startX, endY- (hY / Height), endX, endY);
                bmp = new Bitmap(hX, hY/ Height, PixelFormat.Format32bppRgb);
                Graphics gfxScreenshot4 = Graphics.FromImage(bmp);
                gfxScreenshot4.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size);
                scalededgeBot = new Bitmap(bmp, Width, 1);
                gfxScreenshot4.Clear(Color.Empty);

                scaledBmpScreenshot = new Bitmap(Width, Height);

                for (int n = 0; n < scalededgeLeft.Height; n++)
                {
                    scaledBmpScreenshot.SetPixel(0, n, scalededgeLeft.GetPixel(0, n));
                    scaledBmpScreenshot.SetPixel(Width - 1, n, scalededgeRight.GetPixel(0, n));
                }
                for (int n = 1; n < scalededgeTop.Width - 1; n++)
                {
                    scaledBmpScreenshot.SetPixel(n, 0, scalededgeTop.GetPixel(n, 0));
                    scaledBmpScreenshot.SetPixel(n, Height - 1, scalededgeBot.GetPixel(n, 0));
                }
                try
                {
                    if (debug)
                    {
                        debug = false;
                        ResizeSizes(scalededgeLeft).Save("1Left.bmp");
                        ResizeSizes(scalededgeRight).Save("3Right.bmp");
                        ResizeTops(scalededgeTop).Save("2Top.bmp");
                        ResizeTops(scalededgeBot).Save("4Bot.bmp");
                        Resize(scaledBmpScreenshot).Save("5full.bmp");
                    }
                }
                catch
                {
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
            try
            {
                byteToSend = new List<byte>();
                int subCorner = Math.Max(0, Corner - 1);

                /*BitmapData data = scaledBmpScreenshot.LockBits(new Rectangle(0, 0, scaledBmpScreenshot.Width, scaledBmpScreenshot.Height), ImageLockMode.ReadWrite, scaledBmpScreenshot.PixelFormat);
                int Height = scaledBmpScreenshot.Height;
                int Width = scaledBmpScreenshot.Width;*/

                //unsafe
                //{

                    if (Clockwise)
                    {
                        if (TopLeft)
                        {
                            for (int x = Corner; x < scaledBmpScreenshot.Width - 1 - Corner; x++)
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
                            for (int x = scaledBmpScreenshot.Width - 1 - Corner; x > Corner; x--)
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
                            for (int x = scaledBmpScreenshot.Width - 1 - Corner; x > Corner; x--)
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
                            for (int x = Corner; x < scaledBmpScreenshot.Width - 1 - Corner; x++)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).B));
                            }
                        }
                        if (BotRight)
                        {
                            for (int x = scaledBmpScreenshot.Width - 1 - Corner; x > Corner; x--)
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
                            for (int x = Corner; x < scaledBmpScreenshot.Width - 1 - Corner; x++)
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
                            for (int x = Corner; x < scaledBmpScreenshot.Width - 1 - Corner; x++)
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
                            for (int x = scaledBmpScreenshot.Width - 1 - Corner; x > Corner; x--)
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
                            for (int x = Corner; x < scaledBmpScreenshot.Width - 1 - Corner; x++)
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
                            for (int x = scaledBmpScreenshot.Width - 1 - Corner; x > Corner; x--)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).B));
                            }
                        }
                        if (TopRight)
                        {
                            for (int x = scaledBmpScreenshot.Width - 1 - Corner; x > Corner; x--)
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
                            for (int x = Corner; x < scaledBmpScreenshot.Width - 1 - Corner; x++)
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
                            for (int x = scaledBmpScreenshot.Width - 1 - Corner; x > Corner; x--)
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
                            for (int x = Corner; x < scaledBmpScreenshot.Width - 1 - Corner; x++)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).B));
                            }
                        }
                        if (BotLeft)
                        {
                            //BotLeft
                            /*Stopwatch stopwatch1 = Stopwatch.StartNew();
                            byte* row = (byte*)data.Scan0 + ((Height-1-Shifting) * data.Stride);
                            int columnOffset = Corner*4;
                            for (int x = Corner; x < Width - 1 - Corner; x++)
                            {
                                byteToSend.Add(row[columnOffset + 2]);
                                byteToSend.Add(row[columnOffset + 1]);
                                byteToSend.Add(row[columnOffset]);
                                columnOffset += 4;
                            }

                            columnOffset = data.Stride - 4;
                            for (int y = Height - 1 - subCorner; y > subCorner; y--)
                            {
                                row = (byte*)data.Scan0 + (y * data.Stride);
                                byteToSend.Add(row[columnOffset + 2]);
                                byteToSend.Add(row[columnOffset + 1]);
                                byteToSend.Add(row[columnOffset]);
                            }

                            row = (byte*)data.Scan0 + ((Shifting) * data.Stride);
                            columnOffset = data.Stride - 4 - (Corner*4);
                            for (int x = Width - 1 - Corner; x > Corner; x--)
                            {
                                byteToSend.Add(row[columnOffset + 2]);
                                byteToSend.Add(row[columnOffset + 1]);
                                byteToSend.Add(row[columnOffset]);
                                columnOffset -= 4;
                            }

                            columnOffset = 0;
                            for (int y = subCorner; y < Height - 1 - subCorner; y++)
                            {
                                row = (byte*)data.Scan0 + ((y-Shifting) * data.Stride);
                                byteToSend.Add(row[columnOffset + 2]);
                                byteToSend.Add(row[columnOffset + 1]);
                                byteToSend.Add(row[columnOffset]);
                            }

                            string tmp1 = "";
                            for(int n = 0; n < byteToSend.Count-2; n++)
                            {
                                byte b = byteToSend[n];
                                tmp1 += "R:" + b.ToString() + ':';
                                b = byteToSend[n + 1];
                                tmp1 += "G:" + b.ToString() + ':';
                                b = byteToSend[n + 2];
                                tmp1 += "B:" + b.ToString() + Environment.NewLine;
                            }
                            stopwatch1.Stop();
                            string stp1 = stopwatch1.ElapsedTicks.ToString();
                            byteToSend = new List<byte>();

                            scaledBmpScreenshot.UnlockBits(data);
                            Stopwatch stopwatch2 = Stopwatch.StartNew();
                            */
                            for (int x = Corner; x < scaledBmpScreenshot.Width - 1 - Corner; x++)
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
                            for (int x = scaledBmpScreenshot.Width - 1 - Corner; x > Corner; x--)
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
                            /*
                            stopwatch2.Stop();
                            string stp2 = stopwatch2.ElapsedTicks.ToString();

                            string tmp2 = "";
                            for (int n = 0; n < byteToSend.Count - 2; n++)
                            {
                                byte b = byteToSend[n];
                                tmp2 += "R:" + b.ToString() + ':';
                                b = byteToSend[n + 1];
                                tmp2 += "G:" + b.ToString() + ':';
                                b = byteToSend[n + 2];
                                tmp2 += "B:" + b.ToString() + Environment.NewLine;
                            }
                            if(tmp1==tmp2)
                            {
                                int g = 0;
                                if(g++==10)
                                {
                                    MessageBox.Show("");
                                }
                            }
                            */
                        }
                    }
                //}
            }
            catch
            {
            }
        }
        private void SingleColor()
        {
            byteToSend = new List<byte>() { Red, Green, Blue };
            if (staticColorChanged)
            {
                staticColorChanged = false;
                SendPayload(PayloadType.fixedColor, byteToSend);
                staticColorCurrentTime = 0;
                currentSleepTime = (((currentSleepTime + Math.Max(Properties.Settings.Default.minTime, Properties.Settings.Default.maxTime - difference)) / 4) + (int)(cpuCounter.NextValue() * 2))/2;
            }
            else
            {
                staticColorCurrentTime++;
                if (staticColorCurrentTime > staticColorMaxTime)
                {
                    SendPayload(PayloadType.fixedColor, byteToSend);
                    staticColorCurrentTime = 0;
                }
                currentSleepTime = 100;
            }
        }
        private void Send()
        {
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
                if (justBlack++>5)
                {
                    justBlack = 0;
                    sock.SendTo(new byte[] { (byte)('A'), (byte)PayloadType.fixedColor, 5 }, endPoint);
                }
            } 
            #endregion

            else
            {
                NewToSend();
                newByteToSend = new List<byte>(0);
                if (LPF)
                {
                    while (LastByteToSend.Count < byteToSend.Count) { LastByteToSend.Add(0); } //In case X/Y increased

                    int odd = 0; //rounding errorLastByteToSend.Add(0);
                    for (int n = 0; n < byteToSend.Count; n++)
                    {
                        odd = byteToSend[n] + LastByteToSend[n];
                        if(odd % 2 != 0) //To correct the -1 error rounding
                        {
                            odd++;
                        }
                        odd = odd / 2;
                        newByteToSend.Add((byte)odd); //newByteToSend[n] = rounded-up sum
                    }
                    //LastByteToSend = new List<byte>(byteToSend);
                    LastByteToSend = new List<byte>(newByteToSend);
                }
                else
                {
                    LastByteToSend = newByteToSend = byteToSend;
                }
                
                if (UpDown != 0){ RotateArray(); }
                if (UsingFlux)  { Flux(); }

                if(staticColorChanged) //When changing tab, one frame could still be processing, so it is discarded here
                {
                    return;
                }

                for (int n = 0; n+packetSize <= byteToSend.Count; n += packetSize)
                {
                    SendPayload(PayloadType.multiplePayload, newByteToSend.GetRange(n, packetSize));
                }
                int index = newByteToSend.Count - (newByteToSend.Count % packetSize);
                SendPayload(PayloadType.terminalPayload, newByteToSend.GetRange(index, newByteToSend.Count%packetSize));
            }

            //IDLE TIME TO REDUCE CPU USAGE WHEN THE FRAMES AREN'T CHANGING MUCH AND WHEN CPU USAGE IS HIGH
            currentSleepTime = (((currentSleepTime + Math.Max(Properties.Settings.Default.minTime, Properties.Settings.Default.maxTime - difference)) / 4) + (int)(cpuCounter.NextValue() * 2))/3;
            if(countDifference>120)
            {
                currentSleepTime = (countDifference-120)+(((currentSleepTime + Math.Max(Properties.Settings.Default.minTime, Properties.Settings.Default.maxTime - difference)) / 3) + (int)(cpuCounter.NextValue() * 2)) / 3;
            }
            else
            {
                currentSleepTime = (((currentSleepTime + Math.Max(Properties.Settings.Default.minTime, Properties.Settings.Default.maxTime - difference)) / 4) + (int)(cpuCounter.NextValue() * 2)) / 3;
            }

        }
        private void RotateArray()
        {
            List<byte> byteToSend2 = new List<byte>(newByteToSend);

            for (int n = 0; n < newByteToSend.Count; n++)
            {
                byteToSend2[n] = newByteToSend[(n + UpDown * 3) % (byteToSend.Count - 1)];
            }

            newByteToSend = new List<byte>(byteToSend2);
        }
        int prevDifference = 0;
        int countDifference = 0;
        private void NewToSend()
        {
            if (LastByteToSend.Count != byteToSend.Count)
            {
                difference = Properties.Settings.Default.maxTime;
            }
            else
            {
                difference = 0;
                for (int n = 0; n < byteToSend.Count; n++)
                {
                    difference += Math.Abs((int)(byteToSend[n]) - (int)(LastByteToSend[n]));
                }
            }
            if(difference == prevDifference)
            {
                if(countDifference < 2000)
                {
                    countDifference++;
                }
            }
            else
            {
                prevDifference = difference;
                countDifference = 0;
            }
        }
        private void Flux()
        {
            for (int n = 2; n < byteToSend.Count - 2; n += 3)
            {
                string s;
                byte b;

                
                s = (newByteToSend[n] * fluxRatio).ToString().Replace('.',',').Split(',')[0];
                b = byte.Parse(s);
                newByteToSend[n] = b;

                s = (newByteToSend[n-1] * ((1+fluxRatio)/2)).ToString().Replace('.', ',').Split(',')[0];
                b = byte.Parse(s);
                newByteToSend[n-1] = b;

                s = (newByteToSend[n - 2] * (1 + (1-fluxRatio)/3)).ToString().Replace('.', ',').Split(',')[0];
                short i = short.Parse(s);
                if(i>255)
                {
                    i = 255;
                }
                b = Byte.Parse(i.ToString());
                newByteToSend[n - 2] = b;
            }
        }
        #endregion
    }
}
