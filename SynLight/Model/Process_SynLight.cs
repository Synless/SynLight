using System;
using System.Collections.Generic;
using System.Windows;
using System.Drawing;
using System.Threading;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

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
        private int GCCounter = 0;
        private void CheckMethodProcess()
        {
            Stopwatch watch;

            while (PlayPause)
            {
                watch = Stopwatch.StartNew();

                Tick();

                Thread.Sleep(sleepDelayMs);

                if (Mix == 100)
                    Thread.Sleep(500);

                if(GCCounter++ >= 100)
                {
                    GC.Collect();
                    GCCounter = 0;
                }

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

        private Queue<float> rrHistory = new Queue<float>(8);
        private Queue<float> ggHistory = new Queue<float>(8);
        private Queue<float> bbHistory = new Queue<float>(8);

        private float CalculateAverage(Queue<float> history, float newValue)
        {
            if (history.Count >= 3)
            {
                history.Dequeue(); // Remove the oldest value if we already have 3 values
            }
            history.Enqueue(newValue); // Add the new value

            return history.Average(); // Return the average of the current values
        }
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

            if (Lantern)
            {
                try
                {
                    List<byte> lbts = new List<byte>();

                    Bitmap lanternSizeBitmap = CaptureAndResizeScreenshot();

                    // Extract the RGB values from the resized bitmap (3x1)
                    float rr = (lanternSizeBitmap.GetPixel(0, 0).R);
                    float gg = (lanternSizeBitmap.GetPixel(0, 0).G);
                    float bb = (lanternSizeBitmap.GetPixel(0, 0).B);

                    // Apply the low-pass filter (average of the last 3 values)
                    rr = CalculateAverage(rrHistory, rr);
                    gg = CalculateAverage(ggHistory, gg);
                    bb = CalculateAverage(bbHistory, bb);

                    // Add the filtered values to the list
                    lbts.Add((byte)rr);
                    lbts.Add((byte)gg);
                    lbts.Add((byte)bb);

                    // Send the payload
                    arduinoUDPManual.Send(System.Net.IPAddress.Parse("192.168.8.138"), PayloadType.fixedColor, lbts);
                }
                catch
                {
                    // Handle any exceptions
                }
            }
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
        private Bitmap CaptureAndResizeScreenshot()
        {
            // Define the size you want to resize the screenshot to
            System.Drawing.Size newSize = new System.Drawing.Size(1, 1);

            // Capture the screenshot of the entire screen or a specific area
            Rectangle screenRect = new Rectangle(0, 0, (int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight);
            Bitmap screenshot = new Bitmap(screenRect.Width, screenRect.Height, PixelFormat.Format32bppRgb);
            using (Graphics g = Graphics.FromImage(screenshot))
            {
                g.CopyFromScreen(screenRect.Left, screenRect.Top, 0, 0, screenRect.Size);
            }

            // Resize the captured screenshot
            return RescaleImage(screenshot, newSize);
        }

        private Bitmap reusableLeftBmp;
        private Bitmap reusableRightBmp;
        private Bitmap reusableTopBmp;
        private Bitmap reusableBotBmp;

        private void InitReusableEdgeBitmaps()
        {
            int edgeW = hX / _Width;
            int edgeH = hY / _Height;

            reusableLeftBmp?.Dispose();
            reusableRightBmp?.Dispose();
            reusableTopBmp?.Dispose();
            reusableBotBmp?.Dispose();

            reusableLeftBmp = new Bitmap(edgeW, hY, PixelFormat.Format32bppRgb);
            reusableRightBmp = new Bitmap(edgeW, hY, PixelFormat.Format32bppRgb);
            reusableTopBmp = new Bitmap(hX, edgeH, PixelFormat.Format32bppRgb);
            reusableBotBmp = new Bitmap(hX, edgeH, PixelFormat.Format32bppRgb);
        }


        private readonly byte BrightnessForKeyboard = 15;
        private int frameCounter = 0;

        private void GetScreenShotedges()
        {
            try
            {
                frameCounter++;

                startX = scannedArea.X;
                startY = scannedArea.Y;
                endX = scannedArea.Width;
                endY = scannedArea.Height;
                hX = endX - startX;
                hY = endY - startY;

                startY += ((_Shifting * hY) / _Height) / 2;
                endY -= ((_Shifting * hY) / _Height) / 2;

                int edgeW = hX / _Width;
                int edgeH = hY / _Height;

                // Check if we need to recreate reusable bitmaps
                if (reusableLeftBmp == null || reusableLeftBmp.Width != edgeW || reusableLeftBmp.Height != hY)
                {
                    reusableLeftBmp?.Dispose();
                    reusableRightBmp?.Dispose();
                    reusableTopBmp?.Dispose();
                    reusableBotBmp?.Dispose();

                    reusableLeftBmp = new Bitmap(edgeW, hY, PixelFormat.Format32bppRgb);
                    reusableRightBmp = new Bitmap(edgeW, hY, PixelFormat.Format32bppRgb);
                    reusableTopBmp = new Bitmap(hX, edgeH, PixelFormat.Format32bppRgb);
                    reusableBotBmp = new Bitmap(hX, edgeH, PixelFormat.Format32bppRgb);
                }

                // Only left/right OR top/bottom depending on frame parity
                List<Task> tasks = new List<Task>();

                if (frameCounter % 2 == 0)
                {
                    // LEFT
                    tasks.Add(Task.Run(() =>
                    {
                        Rectangle rectLeft = new Rectangle(startX, startY, edgeW, endY - startY);
                        using (Graphics gfx = Graphics.FromImage(reusableLeftBmp))
                        {
                            gfx.CopyFromScreen(rectLeft.Left, rectLeft.Top, 0, 0, reusableLeftBmp.Size);
                        }
                        scalededgeLeft = RescaleImage(reusableLeftBmp, new System.Drawing.Size(1, _Height));
                    }));

                    // RIGHT
                    tasks.Add(Task.Run(() =>
                    {
                        Rectangle rectRight = new Rectangle(endX - edgeW, startY, edgeW, endY - startY);
                        using (Graphics gfx = Graphics.FromImage(reusableRightBmp))
                        {
                            gfx.CopyFromScreen(rectRight.Left, rectRight.Top, 0, 0, reusableRightBmp.Size);
                        }
                        scalededgeRight = RescaleImage(reusableRightBmp, new System.Drawing.Size(1, _Height));
                    }));
                }
                else
                {
                    // TOP
                    tasks.Add(Task.Run(() =>
                    {
                        Rectangle rectTop = new Rectangle(startX, startY, hX, edgeH);
                        using (Graphics gfx = Graphics.FromImage(reusableTopBmp))
                        {
                            gfx.CopyFromScreen(rectTop.Left, rectTop.Top, 0, 0, reusableTopBmp.Size);
                        }
                        scalededgeTop = RescaleImage(reusableTopBmp, new System.Drawing.Size(_Width, 1));
                    }));

                    // BOTTOM
                    tasks.Add(Task.Run(() =>
                    {
                        Rectangle rectBot = new Rectangle(startX, endY - edgeH, hX, edgeH);
                        using (Graphics gfx = Graphics.FromImage(reusableBotBmp))
                        {
                            gfx.CopyFromScreen(rectBot.Left, rectBot.Top, 0, 0, reusableBotBmp.Size);
                        }
                        scalededgeBot = RescaleImage(reusableBotBmp, new System.Drawing.Size(_Width, 1));
                    }));
                    //frameCounter = 0;
                }

                Task.WaitAll(tasks.ToArray());

                // Compose full frame
                ComposeFullBitmap();

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
                    catch { }
                }
            }
            catch { }
        }
        private void ComposeFullBitmap()
        {
            scaledBmpScreenshot?.Dispose();
            scaledBmpScreenshot = new Bitmap(_Width, _Height, PixelFormat.Format32bppArgb);

            BitmapData bmpData = scaledBmpScreenshot.LockBits(
                new Rectangle(0, 0, _Width, _Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            int stride = bmpData.Stride;

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;

                void SetPixel(int x, int y, Color color)
                {
                    byte* pixel = ptr + y * stride + x * 4;
                    pixel[0] = color.B;
                    pixel[1] = color.G;
                    pixel[2] = color.R;
                    pixel[3] = 255;
                }

                for (int y = 0; y < _Height; y++)
                {
                    if (scalededgeLeft != null)
                        SetPixel(0, y, scalededgeLeft.GetPixel(0, y));

                    if (scalededgeRight != null)
                        SetPixel(_Width - 1, y, scalededgeRight.GetPixel(0, y));
                }

                for (int x = 1; x < _Width - 1; x++)
                {
                    if (scalededgeTop != null)
                        SetPixel(x, 0, scalededgeTop.GetPixel(x, 0));

                    if (scalededgeBot != null)
                    {
                        Color bot = scalededgeBot.GetPixel(x, 0);
                        if (KeyboardLight)
                        {
                            SetPixel(x, _Height - 1, Color.FromArgb(255,
                                Math.Min(bot.R + BrightnessForKeyboard, 255),
                                Math.Min(bot.G + BrightnessForKeyboard, 255),
                                Math.Min(bot.B + BrightnessForKeyboard, 255)));
                        }
                        else
                        {
                            SetPixel(x, _Height - 1, bot);
                        }
                    }
                }
            }

            scaledBmpScreenshot.UnlockBits(bmpData);
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

        private void Send()
        {
            newByteToSend = new List<byte>(0);            

            if (LPF) //Low-pass filtering
            {
                while (lastByteToSend.Count < byteToSend.Count) { lastByteToSend.Add(0); }

                int odd; //To correct the -1 error rounding
                for (int n = 0; n < byteToSend.Count; n++)
                {
                    odd = (2*byteToSend[n]) + lastByteToSend[n];
                    if (odd % 2 != 0) { odd++; }
                    odd /= 3;
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

            if(NeighborFilter)
            {
                List<byte> veryNewByteToSend = new List<byte>(newByteToSend);
                List<byte> veryOldByteToSend = new List<byte>(newByteToSend);

                double mainRatio = 0.65;
                double neighborRatio = (1 - mainRatio) / 4;

                int j = veryNewByteToSend.Count;
                for (int i = 0; i <= j - 3; i += 3)
                {
                    double r = newByteToSend[i] * mainRatio;
                    double g = newByteToSend[i + 1] * mainRatio;
                    double b = newByteToSend[i + 2] * mainRatio;

                    // Add previous 2 neighbors if they exist
                    if (i - 3 >= 0)
                    {
                        r += newByteToSend[i - 3] * neighborRatio;
                        g += newByteToSend[i - 2] * neighborRatio;
                        b += newByteToSend[i - 1] * neighborRatio;
                    }
                    if (i - 6 >= 0)
                    {
                        r += newByteToSend[i - 6] * neighborRatio;
                        g += newByteToSend[i - 5] * neighborRatio;
                        b += newByteToSend[i - 4] * neighborRatio;
                    }

                    // Add next 2 neighbors if they exist
                    if (i + 3 < j)
                    {
                        r += newByteToSend[i + 3] * neighborRatio;
                        g += newByteToSend[i + 4] * neighborRatio;
                        b += newByteToSend[i + 5] * neighborRatio;
                    }
                    if (i + 6 < j)
                    {
                        r += newByteToSend[i + 6] * neighborRatio;
                        g += newByteToSend[i + 7] * neighborRatio;
                        b += newByteToSend[i + 8] * neighborRatio;
                    }

                    veryNewByteToSend[i] = (byte)Math.Min(255,Math.Max(0,Math.Round(r)));
                    veryNewByteToSend[i + 1] = (byte)Math.Min(255, Math.Max(0, Math.Round(g)));
                    veryNewByteToSend[i + 2] = (byte)Math.Min(255, Math.Max(0, Math.Round(b)));
                }

                newByteToSend = new List<byte>(veryNewByteToSend);


                int sumVeryOldByteToSend = veryOldByteToSend.Sum(b => b);
                int sumVeryNewByteToSend = newByteToSend.Sum(b => b);

            }

            CalculateSleepTime();

            //SendPayload(newByteToSend);
            //return;

            //Send standard packets if needed, then send the terminal payload
            int packetSize = 489;
            for (int n = 0; n + packetSize <= byteToSend.Count; n += packetSize)
            {
                SendPayload(PayloadType.multiplePayload, newByteToSend.GetRange(n, packetSize));
            }

            int index = newByteToSend.Count - (newByteToSend.Count % packetSize);
            SendPayload(PayloadType.terminalPayload, newByteToSend.GetRange(index, newByteToSend.Count % packetSize));

        }

        private int sleepDelayMs = 0;
        private const int minDifference = 100;
        private const int maxDifference = 10000;

        private int idleStretchAdditionalMs = 0;
        private const int idleStretchCapMs = 400;
        private const int idleStretchIncrementMs = 10;
        private const int stabilityThreshold = 18;

        private int stabilityConsecutiveCount = 0;
        private const int stabilityRequiredCount = 30;
        private bool stabilityRequiredCountTriggered = false;
        private int lastSleepDelayMs = 0;

        private void CalculateSleepTime()
        {
            if (lastByteToSend.Count != byteToSend.Count)
            {
                idleStretchAdditionalMs = 0;
                sleepDelayMs = maxDifference;
                return;
            }

            int totalChange = 0;
            for (int i = 0; i < byteToSend.Count; i++)
                totalChange += Math.Abs(byteToSend[i] - lastByteToSend[i]);

            int mapped = Math.Min(totalChange, maxDifference);
            mapped = Math.Max(mapped, minDifference);
            mapped -= minDifference;
            mapped = (int)Math.Sqrt(mapped);

            if (usePerformanceCounter)
                mapped += (int)Math.Round(cpuCounter.NextValue());

            if (Math.Sqrt(totalChange) <= stabilityThreshold)
            {
                stabilityConsecutiveCount++;
                if (stabilityConsecutiveCount >= stabilityRequiredCount || stabilityRequiredCountTriggered)
                {
                    stabilityRequiredCountTriggered = true;
                    idleStretchAdditionalMs = Math.Min(idleStretchAdditionalMs + idleStretchIncrementMs, idleStretchCapMs);
                }
            }
            else
            {
                stabilityRequiredCountTriggered = false;
                stabilityConsecutiveCount = 0;
                idleStretchAdditionalMs = 0;
            }

            int combined = Math.Min(mapped + idleStretchAdditionalMs, maxDifference);

            if (Turbo)
            {
                sleepDelayMs = 0;
            }

            sleepDelayMs = combined;

            if (sleepDelayMs > lastSleepDelayMs)
            {
                sleepDelayMs = (lastSleepDelayMs + sleepDelayMs) / 2;
            }

            lastSleepDelayMs = sleepDelayMs;
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