using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using RemoteControl.Core.Models;
using RemoteControl.Services.Interfaces;

namespace RemoteControl.Services.Implementations
{
    public class WindowsScreenCaptureService : IScreenCaptureService
    {
        private readonly ILogger<WindowsScreenCaptureService> _logger;
        private CancellationTokenSource? _captureCancellationSource;
        private Task? _captureTask;

        public bool IsCapturing { get; private set; }

        public WindowsScreenCaptureService(ILogger<WindowsScreenCaptureService> logger)
        {
            _logger = logger;
        }

        public async Task<ScreenFrame> CaptureScreenAsync(int quality = 75, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                var screen = Screen.PrimaryScreen;
                var bounds = screen.Bounds;
                
                using var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
                using var graphics = Graphics.FromImage(bitmap);
                
                graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
                
                var imageData = BitmapToByteArray(bitmap, quality);
                
                return new ScreenFrame
                {
                    ImageData = imageData,
                    Width = bounds.Width,
                    Height = bounds.Height,
                    Quality = quality,
                    Timestamp = DateTime.UtcNow,
                    IsKeyFrame = true,
                    Format = "JPEG"
                };
            }, cancellationToken);
        }

        public async Task<ScreenFrame> CaptureRegionAsync(int x, int y, int width, int height, int quality = 75, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                using var graphics = Graphics.FromImage(bitmap);
                
                graphics.CopyFromScreen(x, y, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
                
                var imageData = BitmapToByteArray(bitmap, quality);
                
                return new ScreenFrame
                {
                    ImageData = imageData,
                    Width = width,
                    Height = height,
                    Quality = quality,
                    Timestamp = DateTime.UtcNow,
                    IsKeyFrame = true,
                    Format = "JPEG"
                };
            }, cancellationToken);
        }

        public (int Width, int Height) GetScreenDimensions()
        {
            var screen = Screen.PrimaryScreen;
            return (screen.Bounds.Width, screen.Bounds.Height);
        }

        public async Task StartContinuousCaptureAsync(Action<ScreenFrame> onFrameCaptured, int frameRate = 30, CancellationToken cancellationToken = default)
        {
            if (IsCapturing)
            {
                await StopContinuousCaptureAsync();
            }

            _captureCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            IsCapturing = true;

            _captureTask = Task.Run(async () =>
            {
                var frameDelay = TimeSpan.FromMilliseconds(1000.0 / frameRate);
                long frameNumber = 0;

                _logger.LogInformation("Started continuous screen capture at {FrameRate} FPS", frameRate);

                try
                {
                    while (!_captureCancellationSource.Token.IsCancellationRequested)
                    {
                        var frameStartTime = DateTime.UtcNow;
                        
                        try
                        {
                            var frame = await CaptureScreenAsync(75, _captureCancellationSource.Token);
                            frame.FrameNumber = frameNumber++;
                            frame.IsKeyFrame = frameNumber % 30 == 0; // Key frame every 30 frames
                            
                            onFrameCaptured(frame);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error capturing screen frame");
                        }

                        var processingTime = DateTime.UtcNow - frameStartTime;
                        var remainingDelay = frameDelay - processingTime;
                        
                        if (remainingDelay > TimeSpan.Zero)
                        {
                            await Task.Delay(remainingDelay, _captureCancellationSource.Token);
                        }
                    }
                }
                finally
                {
                    IsCapturing = false;
                    _logger.LogInformation("Stopped continuous screen capture");
                }
            }, _captureCancellationSource.Token);

            await Task.CompletedTask;
        }

        public async Task StopContinuousCaptureAsync()
        {
            if (_captureCancellationSource != null)
            {
                _captureCancellationSource.Cancel();
                
                if (_captureTask != null)
                {
                    await _captureTask;
                }
                
                _captureCancellationSource.Dispose();
                _captureCancellationSource = null;
                _captureTask = null;
            }
            
            IsCapturing = false;
        }

        private static byte[] BitmapToByteArray(Bitmap bitmap, int quality)
        {
            var encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            
            var jpegCodec = GetEncoderInfo("image/jpeg");
            
            using var memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, jpegCodec, encoderParams);
            return memoryStream.ToArray();
        }

        private static ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            var codecs = ImageCodecInfo.GetImageEncoders();
            foreach (var codec in codecs)
            {
                if (codec.MimeType == mimeType)
                    return codec;
            }
            throw new NotSupportedException($"Codec for {mimeType} not found");
        }

        public void Dispose()
        {
            StopContinuousCaptureAsync().Wait();
        }
    }
}
