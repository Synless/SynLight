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
        Thread process;

        public Process_SynLight()
        {
            bmpScreenshot = new Bitmap((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight);
            process = new Thread(CheckMethod);
            process.Start();
        }

        public void PlayPausePushed()
        {
#pragma warning disable CS0618
            PlayPause = !PlayPause;
            if (PlayPause)
            {
                //CREATION OF A SECOND WORKER NOT TO BLOCK THE PROGRAM
                process.Priority = ThreadPriority.Lowest;
                process.Resume();
            }
            else
            {
                Close();
                process.Suspend();
            }
#pragma warning disable CS0618
        }
        #region Privates methodes
        private void CheckMethod()
        {
            /*long counter = 0;
            long ticks = 0;
            long means = 0;*/
            while (PlayPause)
            {
                System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
                Tick();
                Thread.Sleep(currentSleepTime/2); // DEVIDED BY TWO THANKS TO THE edge METHOD
                GC.Collect(); //COUPLES OF MB WON //1
                watch.Stop();
                long elapsedMs = watch.ElapsedMilliseconds;
                //ticks += elapsedMs;
                ///counter++;
                //means = ticks / counter;
                int Hz = (int)(1000.0 / elapsedMs);
                Tittle = Hz.ToString();
            }
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
            ProcessScreenShot();
            Send();
                
        }
        private void GetScreenShot()
        {
            try
            {
                Graphics gfxScreenshot = Graphics.FromImage(bmpScreenshot); //1
                gfxScreenshot.CopyFromScreen(0, 0, 0, 0, Screen);
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
                int xScreen = (int)(SystemParameters.PrimaryScreenWidth);
                int yScreen = (int)(SystemParameters.PrimaryScreenHeight);

                Rectangle rect = new Rectangle(0, 0, xScreen/Width, yScreen);
                Bitmap bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
                Graphics gfxScreenshot = Graphics.FromImage(bmp);
                gfxScreenshot.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size);
                scalededgeLeft = new Bitmap(bmp, 1, Height);
                gfxScreenshot.Clear(Color.Empty);

                rect = new Rectangle(xScreen-(xScreen/Width)-1, 0, xScreen / Width, yScreen);
                bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
                Graphics gfxScreenshot2 = Graphics.FromImage(bmp);
                gfxScreenshot2.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size);
                scalededgeRight = new Bitmap(bmp, 1, Height);
                gfxScreenshot2.Clear(Color.Empty);

                rect = new Rectangle(0, 0, xScreen, yScreen/Height);
                bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
                Graphics gfxScreenshot3 = Graphics.FromImage(bmp);
                gfxScreenshot3.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size);
                scalededgeTop = new Bitmap(bmp, Width, 1);
                gfxScreenshot3.Clear(Color.Empty);

                rect = new Rectangle(0, yScreen-(yScreen/Height)-1, xScreen, yScreen / Height);
                bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
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
                    scaledBmpScreenshot.SetPixel(n, Height-1, scalededgeBot.GetPixel(n, 0));
                }
                /*try
                {
                    ResizeSizes(scalededgeLeft).Save("1Left.bmp");
                    ResizeSizes(scalededgeRight).Save("3Right.bmp");
                    ResizeTops(scalededgeTop).Save("2Top.bmp");
                    ResizeTops(scalededgeBot).Save("4Bot.bmp");
                    Resize(scaledBmpScreenshot).Save("5full.bmp");
                }
                catch
                {

                }*/
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
            Bitmap newImage = new Bitmap((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight/Height);
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
                if (Corner==0)
                {
                    if (Clockwise)
                    {
                        if (TopLeft)
                        {
                            for (int x = 0; x < scaledBmpScreenshot.Width - 1; x++)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).B));
                            }
                            for (int y = 0; y < scaledBmpScreenshot.Height - 1; y++)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).B));
                            }
                            for (int x = scaledBmpScreenshot.Width - 1; x > 0; x--)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).B));
                            }
                            for (int y = scaledBmpScreenshot.Height - 1; y > 0; y--)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).B));
                            }
                        }
                        if (TopRight)
                        {
                            for (int y = 0; y < scaledBmpScreenshot.Height - 1; y++)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).B));
                            }
                            for (int x = scaledBmpScreenshot.Width - 1; x > 0; x--)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).B));
                            }
                            for (int y = scaledBmpScreenshot.Height - 1; y > 0; y--)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).B));
                            }
                            for (int x = 0; x < scaledBmpScreenshot.Width - 1; x++)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).B));
                            }
                        }
                        if (BotRight)
                        {
                            for (int x = scaledBmpScreenshot.Width - 1; x > 0; x--)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).B));
                            }
                            for (int y = scaledBmpScreenshot.Height - 1; y > 0; y--)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).B));
                            }
                            for (int x = 0; x < scaledBmpScreenshot.Width - 1; x++)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).B));
                            }
                            for (int y = 0; y < scaledBmpScreenshot.Height - 1; y++)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).B));
                            }
                        }
                        if (BotLeft)
                        {
                            for (int y = scaledBmpScreenshot.Height - 1; y > 0; y--)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).B));
                            }
                            for (int x = 0; x < scaledBmpScreenshot.Width - 1; x++)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).B));
                            }
                            for (int y = 0; y < scaledBmpScreenshot.Height - 1; y++)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).B));
                            }
                            for (int x = scaledBmpScreenshot.Width - 1; x > 0; x--)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).B));
                            }
                        }
                    }
                    else
                    {
                        if (TopLeft)
                        {
                            for (int y = 0; y < scaledBmpScreenshot.Height - 1; y++)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).B));
                            }
                            for (int x = 0; x < scaledBmpScreenshot.Width - 1; x++)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).B));
                            }
                            for (int y = scaledBmpScreenshot.Height - 1; y > 0; y--)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).B));
                            }
                            for (int x = scaledBmpScreenshot.Width - 1; x > 0; x--)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).B));
                            }
                        }
                        if (TopRight)
                        {
                            for (int x = scaledBmpScreenshot.Width - 1; x > 0; x--)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).B));
                            }
                            for (int y = 0; y < scaledBmpScreenshot.Height - 1; y++)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).B));
                            }
                            for (int x = 0; x < scaledBmpScreenshot.Width - 1; x++)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).B));
                            }
                            for (int y = scaledBmpScreenshot.Height - 1; y > 0; y--)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).B));
                            }
                        }
                        if (BotRight)
                        {
                            for (int y = scaledBmpScreenshot.Height - 1; y > 0; y--)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).B));
                            }
                            for (int x = scaledBmpScreenshot.Width - 1; x > 0; x--)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).B));
                            }
                            for (int y = 0; y < scaledBmpScreenshot.Height - 1; y++)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).B));
                            }
                            for (int x = 0; x < scaledBmpScreenshot.Width - 1; x++)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).B));
                            }                            
                        }
                        if (BotLeft)
                        {
                            for (int x = 0; x < scaledBmpScreenshot.Width - 1; x++)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).B));
                            }
                            for (int y = scaledBmpScreenshot.Height - 1; y > 0; y--)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).B));
                            }
                            for (int x = scaledBmpScreenshot.Width - 1; x > 0; x--)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).B));
                            }
                            for (int y = 0; y < scaledBmpScreenshot.Height - 1; y++)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).B));
                            }                            
                        }
                    }
                }
                else
                {
                    if (Clockwise)
                    {
                        if (TopLeft)
                        {
                            for (int x = Corner; x < scaledBmpScreenshot.Width - 1 - Corner; x++)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).B));
                            }
                            for (int y = Corner; y < scaledBmpScreenshot.Height - 1 - Corner; y++)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).B));
                            }
                            for (int x = scaledBmpScreenshot.Width - 1 - Corner; x > Corner; x--)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).B));
                            }
                            for (int y = scaledBmpScreenshot.Height - 1 - Corner; y > Corner; y--)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).B));
                            }
                        }
                        if (TopRight)
                        {                            
                            for (int y = Corner; y < scaledBmpScreenshot.Height - 1 - Corner; y++)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).B));
                            }
                            for (int x = scaledBmpScreenshot.Width - 1 - Corner; x > Corner; x--)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).B));
                            }
                            for (int y = scaledBmpScreenshot.Height - 1 - Corner; y > Corner; y--)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).B));
                            }
                            for (int x = Corner; x < scaledBmpScreenshot.Width - 1 - Corner; x++)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).B));
                            }
                        }
                        if (BotRight)
                        {                            
                            for (int x = scaledBmpScreenshot.Width - 1 - Corner; x > Corner; x--)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).B));
                            }
                            for (int y = scaledBmpScreenshot.Height - 1 - Corner; y > Corner; y--)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).B));
                            }
                            for (int x = Corner; x < scaledBmpScreenshot.Width - 1 - Corner; x++)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).B));
                            }
                            for (int y = Corner; y < scaledBmpScreenshot.Height - 1 - Corner; y++)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).B));
                            }
                        }
                        if (BotLeft)
                        {
                            for (int y = scaledBmpScreenshot.Height - 1 - Corner; y > Corner; y--)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).B));
                            }
                            for (int x = Corner; x < scaledBmpScreenshot.Width - 1 - Corner; x++)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).B));
                            }
                            for (int y = Corner; y < scaledBmpScreenshot.Height - 1 - Corner; y++)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).B));
                            }
                            for (int x = scaledBmpScreenshot.Width - 1 - Corner; x > Corner; x--)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).B));
                            }
                        }
                    }
                    else
                    {
                        if (TopLeft)
                        {
                            for (int y = Corner; y < scaledBmpScreenshot.Height - 1 - Corner; y++)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).B));
                            }
                            for (int x = Corner; x < scaledBmpScreenshot.Width - 1 - Corner; x++)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).B));
                            }
                            for (int y = scaledBmpScreenshot.Height - 1 - Corner; y > Corner; y--)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).B));
                            }
                            for (int x = scaledBmpScreenshot.Width - 1 - Corner; x > Corner; x--)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).B));
                            }
                        }
                        if (TopRight)
                        {
                            for (int x = scaledBmpScreenshot.Width - 1 - Corner; x > Corner; x--)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).B));
                            }
                            for (int y = Corner; y < scaledBmpScreenshot.Height - 1 - Corner; y++)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).B));
                            }
                            for (int x = Corner; x < scaledBmpScreenshot.Width - 1 - Corner; x++)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).B));
                            }
                            for (int y = scaledBmpScreenshot.Height - 1 - Corner; y > Corner; y--)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).B));
                            }
                        }
                        if (BotRight)
                        {
                            for (int y = scaledBmpScreenshot.Height - 1 - Corner; y > Corner; y--)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).B));
                            }
                            for (int x = scaledBmpScreenshot.Width - 1 - Corner; x > Corner; x--)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).B));
                            }
                            for (int y = Corner; y < scaledBmpScreenshot.Height - 1 - Corner; y++)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).B));
                            }
                            for (int x = Corner; x < scaledBmpScreenshot.Width - 1 - Corner; x++)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).B));
                            }
                        }
                        if (BotLeft)
                        {
                            for (int x = Corner; x < scaledBmpScreenshot.Width - 1 - Corner; x++)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1 - Shifting).B));
                            }
                            for (int y = scaledBmpScreenshot.Height - 1 - Corner; y > Corner; y--)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).B));
                            }
                            for (int x = scaledBmpScreenshot.Width - 1 - Corner; x > Corner; x--)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(x, Shifting).B));
                            }
                            for (int y = Corner; y < scaledBmpScreenshot.Height - 1 - Corner; y++)
                            {
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).R));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).G));
                                byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(Shifting, Math.Min(y, scaledBmpScreenshot.Height - 1 - Shifting))).B));
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }
        private void Send()
        {
            /*if (ScaledBlank())
            {
                if (blankCounter++ > maxBlankCounter)
                {
                    try
                    {
                        sock.SendTo(new byte[1] { 1 }, endPoint);
                        if (moreTime < 1000)
                            moreTime += 100;
                        Thread.Sleep(moreTime);
                    }
                    catch { }
                    return;
                }
            }
            else
            {
                blankCounter = 0;
                moreTime = 0;
            }*/
            NewToSend();
            newByteToSend = new List<byte>(0);
            if (LPF)
            {
                for (int n = byteToSend.Count; n < LastByteToSend.Count && n < byteToSend.Count + 6; n++) { LastByteToSend.Add(0); }
                for (int n = 0; n < byteToSend.Count; n++) { newByteToSend.Add((byte)(byteToSend[n] >> 1)); }
                for (int n = 0; n < byteToSend.Count && n < LastByteToSend.Count; n++) { newByteToSend[n] += (byte)(LastByteToSend[n] >> 1); }
                LastByteToSend = new List<byte>(byteToSend);
            }
            else
            {
                LastByteToSend = newByteToSend = byteToSend;
            }
            if (BGF)
            {
                for (int n = 0; n < newByteToSend.Count - 2; n += 3)
                {  //CONTRAST AND BRIGHNESS TWEAKING, RELEVENT FOR LOW BRIGNESS COUNTENT
                    newByteToSend[n] = (byte)((newByteToSend[n] * 2 + sRed) / 3);
                    newByteToSend[n + 1] = (byte)((newByteToSend[n + 1] * 2 + sGreen) / 3);
                    newByteToSend[n + 2] = (byte)((newByteToSend[n + 2] * 2 + sBlue) / 3);
                }
            }
            if(UpDown!=0)
            {
                rotateArray();
            }
            sock.SendTo(newByteToSend.ToArray(), endPoint);

            //IDLE TIME TO REDUCE CPU USAGE WHEN THE FRAMES AREN'T CHANGING MUCH AND WHEN CPU USAGE IS HIGH
            currentSleepTime = ((currentSleepTime + Math.Max(Properties.Settings.Default.minTime, Math.Min(Properties.Settings.Default.maxTime, Math.Max(0, Properties.Settings.Default.maxTime - difference)))) / 4) + (int)(cpuCounter.NextValue()*2);
        }
        private void rotateArray()
        {
            List<byte> byteToSend2 = new List<byte>(newByteToSend);
            
            for (int n = 0; n < newByteToSend.Count; n++)
            {
                byteToSend2[n] = newByteToSend[(n + UpDown * 3)%(byteToSend.Count - 1)];
            }

            newByteToSend = new List<byte>(byteToSend2);
        }
        private bool ScaledBlank()
        {
            for (int x = 0; x < scaledBmpScreenshot.Width; x++)
            {
                for (int y = 0; y < scaledBmpScreenshot.Height; y++)
                {
                    if (scaledBmpScreenshot.GetPixel(x, y).R + scaledBmpScreenshot.GetPixel(x, y).G + scaledBmpScreenshot.GetPixel(x, y).B > 0)
                        return false;
                }
            }
            return true;
        }
        private void NewToSend()
        {
            if (LastByteToSend.Count != byteToSend.Count)
            {
                difference = Properties.Settings.Default.maxTime;
                return;
            }
            else
            {
                difference = 0;
            }
            for (int n = 0; n < byteToSend.Count; n++)
            {
                difference += Math.Abs((int)(byteToSend[n]) - (int)(LastByteToSend[n]));
            }
        }
        #endregion
    }
}
