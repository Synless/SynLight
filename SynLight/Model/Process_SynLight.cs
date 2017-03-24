using System;
using System.Collections.Generic;
using System.Windows;
using System.Drawing;
using System.Threading;

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
            while (PlayPause)
            {
                Tick();
                Thread.Sleep(currentSleepTime);
                GC.Collect(); //COUPLES OF MB WON //1--
            }
        }
        private void Tick()
        {
            GetScreenShot();
            ProcessScreenShot();
            Send();
        }
        private void GetScreenShot()
        {
            try
            {
                Graphics gfxScreenshot = Graphics.FromImage(bmpScreenshot); //1--
                gfxScreenshot.CopyFromScreen(0, 0, 0, 0, Screen);
                scaledBmpScreenshot = new Bitmap(bmpScreenshot, Width, Height);
                gfxScreenshot.Clear(Color.Empty);
            }
            catch
            {
                scaledBmpScreenshot = new Bitmap(1, 1);
                scaledBmpScreenshot.SetPixel(0, 0, Color.Black);
            }
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
            if (ScaledBlank())
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
            }
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
            sock.SendTo(newByteToSend.ToArray(), endPoint);

            //IDLE TIME TO REDUCE CPU USAGE WHEN THE FRAMES AREN'T CHANGING MUCH
            currentSleepTime = (currentSleepTime + Math.Max(Properties.Settings.Default.minTime, Math.Min(Properties.Settings.Default.maxTime, Math.Max(0, Properties.Settings.Default.maxTime - difference)))) / 2;
            int Hz = (int)(1000.0 / (double)currentSleepTime);
            Tittle = Hz.ToString();
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