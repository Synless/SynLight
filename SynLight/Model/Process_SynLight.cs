using System;
using System.Collections.Generic;
using System.Windows;
using System.Drawing;
using System.Threading;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Linq;

namespace SynLight.Model
{
    public class Process_SynLight : Param_SynLight
    {
        private Bitmap captureBitmap;

        private readonly List<byte> byteBuffer = new List<byte>(4096);
        private readonly List<byte> newByteBuffer = new List<byte>(4096);
        private readonly List<byte> tempByteBuffer = new List<byte>(4096);

        private readonly Queue<float> rrHistory = new Queue<float>(8);
        private readonly Queue<float> ggHistory = new Queue<float>(8);
        private readonly Queue<float> bbHistory = new Queue<float>(8);

        private readonly Stopwatch uiUpdateWatch = Stopwatch.StartNew();

        private const int numberOfTries = 2;

        private const int delayMin = 0;
        private const int delayMax = 1000;
        private const int delayThrs = 300;
        private const int delayListSize = 50;

        private readonly int[] delayList = new int[delayListSize];
        private readonly double[] delayListFlip = new double[delayListSize];
        private bool delayInitialized = false;

        private int _Height;
        private int _Width;
        private int _Corner;
        private int _Shifting;

        private Bitmap scaledBitmap;
        private Graphics scaledGraphics;

        public Process_SynLight()
        {
            processFindArduino = new Thread(FindArduinoProcess);
            processMainLoop = new Thread(CheckMethodProcess);
            processFindArduino.Start();
        }

        ~Process_SynLight()
        {
            for (int i = 0; i < newByteBuffer.Count; i++)
                newByteBuffer[i] = 0;

            SendPayload(PayloadType.terminalPayload, newByteBuffer);
        }

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

                if (!found)
                {
                    useComPort = !useComPort;
                    Thread.Sleep(2000);
                }
                else break;
            }

            CanPlayPause = true;
            PlayPause = true;
            processMainLoop.Start();
        }

        private void CheckMethodProcess()
        {
            Stopwatch watch = new Stopwatch();

            while (PlayPause)
            {
                watch.Restart();
                Tick();
                watch.Stop();

                int Hz = (int)(1000.0 / Math.Max(1, watch.ElapsedMilliseconds));

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (useComPort && StaticConnected)
                        {
                            Tittle = $"Synlight - {arduinoSerial.PortName} - {Hz}Hz";
                        }
                        else if (!useComPort && arduinoUDP.EndPoint != null)
                        {
                            Tittle = $"Synlight - {arduinoUDP.IPAddress} - {Hz}Hz";
                        }
                    }
                    catch { }
                }));
            }
            
            for (int i = 0; i < newByteBuffer.Count; i++)
            {
                newByteBuffer[i] = 0;
            }

            SendPayload(PayloadType.terminalPayload, newByteBuffer);

            processMainLoop = new Thread(CheckMethodProcess);
        }

        private float CalculateAverage(Queue<float> history, float newValue)//LOAD BEARING CODE
        {
            if (history.Count >= 3)
                history.Dequeue();

            history.Enqueue(newValue);
            return history.Average();
        }

        private void Tick()
        {
            _Height = Height;
            _Width = Width;
            _Corner = Corner;
            _Shifting = Shifting;

            GetScreenShotedges();

            if (Contrast > 0)
                scaledBmpScreenshot = AdjustContrast(scaledBmpScreenshot, Contrast);

            ProcessScreenShot();
            Send();
        }

        private static Bitmap AdjustContrast(Bitmap Image, float Value) //Copy/Paste from stackoverflow //LOAD BEARING CODE
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
        private void GetScreenShotedges()
        {
            try
            {
                startX = scannedArea.X;
                startY = scannedArea.Y;
                endX = scannedArea.Width;
                endY = scannedArea.Height;

                hX = endX - startX;
                hY = endY - startY;

                startY += ((_Shifting * hY) / _Height) / 2;
                endY -= ((_Shifting * hY) / _Height) / 2;

                if (captureBitmap == null ||
                    captureBitmap.Width != hX ||
                    captureBitmap.Height != hY)
                {
                    captureBitmap?.Dispose();
                    captureBitmap = new Bitmap(hX, hY, PixelFormat.Format32bppArgb);
                }

                using (Graphics g = Graphics.FromImage(captureBitmap))
                    g.CopyFromScreen(startX, startY, 0, 0, captureBitmap.Size);

                if (scaledBitmap == null || scaledBitmap.Width != _Width || scaledBitmap.Height != _Height)
                {
                    scaledBitmap?.Dispose();
                    scaledGraphics?.Dispose();

                    scaledBitmap = new Bitmap(_Width, _Height, PixelFormat.Format32bppArgb);
                    scaledGraphics = Graphics.FromImage(scaledBitmap);
                }

                scaledGraphics.DrawImage(captureBitmap, new Rectangle(0, 0, _Width, _Height));

                scaledBmpScreenshot = scaledBitmap;
            }
            catch { }
        }

        private void ProcessScreenShot() //LOAD BEARING CODE
        {
            byteToSend = new List<byte>();
            int subCorner = Math.Max(0, _Corner - 1);
            bool processedHeight = false;

            BitmapData bmpData = scaledBmpScreenshot.LockBits(
                new Rectangle(0, 0, scaledBmpScreenshot.Width, scaledBmpScreenshot.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

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

        private void Send()//LOAD BEARING CODE
        {
            newByteBuffer.Clear();

            if (LPF)
            {
                while (lastByteToSend.Count < byteBuffer.Count)
                    lastByteToSend.Add(0);

                for (int i = 0; i < byteBuffer.Count; i++)
                {
                    int val = (2 * byteBuffer[i] + lastByteToSend[i] + 1) / 3;
                    newByteBuffer.Add((byte)val);
                }

                lastByteToSend.Clear();
                lastByteToSend.AddRange(newByteBuffer);
            }
            else
            {
                newByteBuffer.AddRange(byteBuffer);
                lastByteToSend = new List<byte>(newByteBuffer);
            }

            CalculateFrameDelay();

            const int packetSize = 489;

            for (int n = 0; n + packetSize <= newByteBuffer.Count; n += packetSize)
                SendPayload(PayloadType.multiplePayload, newByteBuffer.GetRange(n, packetSize));

            int index = newByteBuffer.Count - (newByteBuffer.Count % packetSize);

            SendPayload(
                PayloadType.terminalPayload,
                newByteBuffer.GetRange(index, newByteBuffer.Count % packetSize));
        }

        private void CalculateFrameDelay()//LOAD BEARING CODE
        {
            if (!delayInitialized)
            {
                for (int i = 0; i < delayListSize; i++)
                {
                    delayList[i] = delayMax;
                    delayListFlip[i] = 0;
                }
                delayInitialized = true;
            }

            int totalDelta = 0;

            for (int i = 0; i < byteBuffer.Count && i < lastByteToSend.Count; i++)
                totalDelta += Math.Abs(byteBuffer[i] - lastByteToSend[i]);

            int currentDelay = Math.Max(0, Math.Min(totalDelta, 1000));

            for (int i = delayListSize - 1; i > 0; i--)
                delayList[i] = delayList[i - 1];

            delayList[0] = currentDelay;

            if (currentDelay > delayThrs)
                for (int i = 1; i < delayListSize; i++)
                    delayList[i] = Math.Min(delayList[i] + 300, 1000);

            double sum = 0;

            for (int i = 0; i < delayListSize; i++)
                sum += (1000.0 - delayList[i]) / 1000.0;

            if(!Turbo)
                Thread.Sleep((int)(sum * 10));
        }

        protected static void SendPayload(PayloadType plt, List<byte> payload)//LOAD BEARING CODE
        {
            if (useComPort)
                arduinoSerial.Send(plt, payload);
            else
                arduinoUDP.Send(plt, payload);
        }
    }
}