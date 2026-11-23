using AVFoundation;
using CoreAnimation;
using CoreGraphics;
using CoreImage;
using CoreMedia;
using CoreVideo;
using Foundation;
using HydroColor.Models;
using HydroColor.Services;

namespace HydroColor.Platforms.iOS
{
    public class CameraController
    {
        public event EventHandler<ImageCaptureEventArgs> ImageCaptureCompletedEvent;

        public CALayer mCALayer;

        HydroColorRawImageData mImageData = new();

        AVCapturePhotoOutput mPhotoOutput;
        PhotoCaptureDelegate mCaptureDelegate;
        AVCaptureVideoPreviewLayer mPreviewLayer;
        AVCaptureDevice mCameraDevice;

        double ImageISOSpeed;

        // The camera's own exposure time just before the capture (seconds). Used for the calculation;
        // zero if it could not be read.
        double ImageExposureDuration;

        public AVCaptureSession mCaptureSession { get; private set; }

        // true from OpenCamera until CloseCamera
        public bool IsOpen => mCaptureSession != null;

        // When true the preview keeps running after a capture, so several captures can be taken in a row
        // (sensitivity test). False for a normal measurement, where the page closes after one capture.
        public bool KeepRunningAfterCapture { get; set; }

        public CameraController()
        {
            mPhotoOutput = new AVCapturePhotoOutput();
            mCaptureDelegate = new PhotoCaptureDelegate
            {
                WillCapturePhotoAction = (captureOutput, resolvedSettings) =>
                {
                    ImageISOSpeed = mCameraDevice.ISO;

                    // The camera's own exposure time, read just before the capture like the ISO.
                    // Zero means it could not be read, and the EXIF value is used instead.
                    ImageExposureDuration = 0;
                    try
                    {
                        ImageExposureDuration = mCameraDevice.ExposureDuration.Seconds;
                    }
                    catch
                    {
                    }
                },
                DidCapturePhotoAction = (captureOutput, resolvedSettings) =>
                {
                    if (!KeepRunningAfterCapture)
                    {
                        mPreviewLayer.Session.StopRunning();
                    }
                },
                DidFinishProcessingPhotoAction = (captureOutput, photo, error) =>
                {
                    if (error != null || photo == null)
                    {
                        ReportCaptureFailed(error?.LocalizedDescription ?? "The camera did not return an image.");
                        return;
                    }

                    try
                    {
                        OnImageAvailable(photo);
                    }
                    catch (Exception ex)
                    {
                        ReportCaptureFailed(ex.Message);
                    }
                }
            };
        }

        // Tells the page that the capture has failed (ImageData is null).
        // This can be raised for both the raw and the processed photo; the page acts on the first one only.
        void ReportCaptureFailed(string message)
        {
            ImageCaptureCompletedEvent?.Invoke(this, new ImageCaptureEventArgs()
            {
                ImageData = null,
                ErrorMessage = message
            });
        }

        public void SetCALayer(CALayer layer)
        {
            mCALayer = layer;
        }

        public CALayer GetCALayer()
        {
            return mCALayer;
        }

        public bool OpenCamera()
        {

            // Get rear facing camera
            mCameraDevice = AVCaptureDevice.GetDefaultDevice(AVCaptureDeviceType.BuiltInWideAngleCamera, AVMediaTypes.Video, AVCaptureDevicePosition.Back);
            if (mCameraDevice == null)
            {
                throw new System.NotSupportedException("No camera found on the device");
            }

            // Create capture session
            mCaptureSession = new AVCaptureSession()
            {
                SessionPreset = AVCaptureSession.PresetPhoto,
            };

            NSError error;
            var input = new AVCaptureDeviceInput(mCameraDevice, out error);

            mCaptureSession.AddInput(input);

            if (mCaptureSession.CanAddOutput(mPhotoOutput))
            {
                mCaptureSession.AddOutput(mPhotoOutput);
            }
            else
            {
                throw new System.InvalidOperationException("A camera capture session could not be opened.");
            }
            mCaptureSession.CommitConfiguration();

            mPreviewLayer = new AVCaptureVideoPreviewLayer(mCaptureSession)
            {
                Frame = mCALayer.Bounds,
                VideoGravity = AVLayerVideoGravity.ResizeAspect,
            };
  
            mCALayer.AddSublayer(mPreviewLayer);
            mCaptureSession.StartRunning();

            return true;
        }

        AVCapturePhotoSettings CreateCapturePhotoSettings()
        {
            // Check for supported raw camera sensor data
            NSNumber[] rawFormats = mPhotoOutput.AvailableRawPhotoPixelFormatTypes;
            CVPixelFormatType availableRawPixelFormat = new();
            bool rawFormatSupported = false;
            foreach (NSNumber format in rawFormats)
            {
                CVPixelFormatType pixelFormat = (CVPixelFormatType)format.UInt32Value;
                if (pixelFormat == CVPixelFormatType.CV14BayerBggr ||
                    pixelFormat == CVPixelFormatType.CV14BayerGbrg ||
                    pixelFormat == CVPixelFormatType.CV14BayerGrbg ||
                    pixelFormat == CVPixelFormatType.CV14BayerRggb)
                {
                    availableRawPixelFormat = pixelFormat;
                    rawFormatSupported = true;
                }
            }

            if (!rawFormatSupported)
            {
                throw new System.NotSupportedException("Raw camera sensor data is not supported on your device");
            }

            // Use Jpeg for the processed photo type
            int jpegIndex = Array.FindIndex(mPhotoOutput.AvailablePhotoCodecTypes, x => x.ToLower().Contains("jpeg"));
            if (jpegIndex < 0)
            {
                throw new System.NotSupportedException("jpeg images are not supported on your device");
            }
            var ProcessedFormat = new NSDictionary<NSString, NSObject>(AVVideo.CodecKey, new NSString(mPhotoOutput.AvailablePhotoCodecTypes[jpegIndex]));

            // Create capture settings useing the raw and processed format
            AVCapturePhotoSettings capturePhotoSettings = AVCapturePhotoSettings.FromRawPixelFormatType(rawPixelFormatType: (uint)availableRawPixelFormat, processedFormat: ProcessedFormat);
            capturePhotoSettings.FlashMode = AVCaptureFlashMode.Off;

            // Add thumbnail image
            var previewPixelType = capturePhotoSettings.AvailablePreviewPhotoPixelFormatTypes.First();

            var keys = new[]
            {
                new NSString(CVPixelBuffer.PixelFormatTypeKey),
                new NSString(CVPixelBuffer.WidthKey),
                new NSString(CVPixelBuffer.HeightKey),
            };

            var objects = new NSObject[]
            {
                previewPixelType,
                new NSNumber(512),
                new NSNumber(512)
            };
            
            var dictionary = new NSDictionary<NSString, NSObject>(keys, objects);
            capturePhotoSettings.PreviewPhotoFormat = dictionary;

            return capturePhotoSettings;
        }

        // The ISO and exposure time range for captures with settings chosen by the app.
        // Call after OpenCamera.
        public ManualExposureLimits GetManualExposureLimits()
        {
            ManualExposureLimits limits = new();

            limits.Supported = mCameraDevice.IsExposureModeSupported(AVCaptureExposureMode.Custom);
            if (!limits.Supported)
            {
                return limits;
            }

            limits.MinISO = mCameraDevice.ActiveFormat.MinISO;
            limits.MaxISO = mCameraDevice.ActiveFormat.MaxISO;
            limits.MinExposureSeconds = mCameraDevice.ActiveFormat.MinExposureDuration.Seconds;
            limits.MaxExposureSeconds = mCameraDevice.ActiveFormat.MaxExposureDuration.Seconds;
            return limits;
        }

        // Takes a raw image with the ISO and exposure time given, instead of automatic exposure (sensitivity test).
        // The image is taken once the camera reports that the settings are in effect.
        public void TakeRAWImage(double iso, double exposureSeconds)
        {
            if (!mCameraDevice.LockForConfiguration(out NSError error))
            {
                throw new InvalidOperationException(error?.LocalizedDescription ?? "The camera settings could not be changed.");
            }

            try
            {
                mCameraDevice.LockExposure(CMTime.FromSeconds(exposureSeconds, 1000000000), (float)iso, (syncTime) =>
                {
                    try
                    {
                        TakeRAWImage();
                    }
                    catch (Exception ex)
                    {
                        ReportCaptureFailed(ex.Message);
                    }
                });
            }
            finally
            {
                mCameraDevice.UnlockForConfiguration();
            }
        }

        // Returns the camera to automatic exposure after captures with manual settings
        public void RestoreAutoExposure()
        {
            if (mCameraDevice == null || !mCameraDevice.LockForConfiguration(out NSError error))
            {
                return;
            }

            try
            {
                if (mCameraDevice.IsExposureModeSupported(AVCaptureExposureMode.ContinuousAutoExposure))
                {
                    mCameraDevice.ExposureMode = AVCaptureExposureMode.ContinuousAutoExposure;
                }
            }
            finally
            {
                mCameraDevice.UnlockForConfiguration();
            }
        }

        public void TakeRAWImage()
        {
            // Start each capture with empty image data, so a result is never completed with parts of an earlier capture
            mImageData = new HydroColorRawImageData();

            // Create the setting used for image capture, they can only be used once
            AVCapturePhotoSettings capturePhotoSettings = CreateCapturePhotoSettings();

            // Trigger image capture
            mPhotoOutput.CapturePhoto(settings: capturePhotoSettings, mCaptureDelegate);
            
        }

        public void CloseCamera()
        {
            if (mCaptureSession != null)
            {
                if (mCaptureSession.Running)
                {
                    mCaptureSession.StopRunning();
                }
                mPreviewLayer?.RemoveFromSuperLayer();
                mCaptureSession.Dispose();
                mCaptureSession = null;
            }
        }

        void OnImageAvailable(AVCapturePhoto photo)
        {
            
            if (photo.RawPhoto)
            {

                CVPixelBuffer pixelBuffer = photo.PixelBuffer;

                pixelBuffer.Lock(CVPixelBufferLock.ReadOnly);
                nuint bytesPerRow = (nuint)pixelBuffer.BytesPerRow;
                byte[] imageData = new byte[pixelBuffer.DataSize];
                System.Runtime.InteropServices.Marshal.Copy(pixelBuffer.BaseAddress, imageData, 0, Convert.ToInt32(pixelBuffer.DataSize));
                pixelBuffer.Unlock(CVPixelBufferLock.ReadOnly);

                /* Extract black level from Meta Data
                /  NOTE: Black Level & White Level extracted from 'photo.FileDataRepresentation' (DNG file) 
                /  was incorrect. White Level listed as 4095 (10 bit) & black level listed as 528.
                /  Actual white level is 16383 (14 bit) & black level 2112 (on iPhone SE). Values come out correct
                /  when accesing them via 'photo.WeakMetadata'
                */
                NSDictionary ImageMetadata = photo.WeakMetadata;
                NSDictionary DNGMetadata = ((NSDictionary)ImageMetadata[ImageIO.CGImageProperties.DNGDictionary]);

                // The black level is either a single number or a list of numbers.
                // It is converted to one value per color channel below, once the Bayer pattern is known.
                List<double> blackLevelValues = new();
                NSObject blackLevelMetadata = DNGMetadata[ImageIO.CGImageProperties.DNGBlackLevel];
                if (blackLevelMetadata is NSNumber singleBlackLevel)
                {
                    blackLevelValues.Add(singleBlackLevel.DoubleValue);
                }
                else if (blackLevelMetadata is NSArray blackLevelArray)
                {
                    for (nuint i = 0; i < blackLevelArray.Count; i++)
                    {
                        blackLevelValues.Add(blackLevelArray.GetItem<NSNumber>(i).DoubleValue);
                    }
                }

                CVPixelFormatType pixelFormat = pixelBuffer.PixelFormatType;
                BayerFilterType bayerFilter;
                switch (pixelFormat)
                {
                    case CVPixelFormatType.CV14BayerBggr: /* Bayer 14-bit Little-Endian, packed in 16-bits, ordered B G B G... alternating with G R G R... */
                        bayerFilter = BayerFilterType.BGGR;
                        break;
                    case CVPixelFormatType.CV14BayerGbrg: /* Bayer 14-bit Little-Endian, packed in 16-bits, ordered G B G B... alternating with R G R G... */
                        bayerFilter = BayerFilterType.GBRG;
                        break;
                    case CVPixelFormatType.CV14BayerGrbg: /* Bayer 14-bit Little-Endian, packed in 16-bits, ordered G R G R... alternating with B G B G... */
                        bayerFilter = BayerFilterType.GRBG;
                        break;
                    case CVPixelFormatType.CV14BayerRggb: /* Bayer 14-bit Little-Endian, packed in 16-bits, ordered R G R G... alternating with G B G B... */
                        bayerFilter = BayerFilterType.RGGB;
                        break;
                    default:
                        bayerFilter = BayerFilterType.Unknown;
                        break;
                }

                // A list of black levels is ordered by position in the Bayer pattern, not by color,
                // so the Bayer pattern is needed to assign them to red, green and blue.
                ColorChannelData<double> BlackLevel = BayerPatternDemosaic.GetChannelBlackLevels(blackLevelValues, bayerFilter);

                // The ISO value saved in the EXIF data is rounded to the nearest standard ISO value.
                // The rounded value is not accurate enough. According to Apple tech support, the EXIF ISO
                // value is the only one availible in the image metadata. Therefore, I'm recording the exact
                // ISO value from the camera device in the WillCapturePhoto callback, which is called
                // immediately before the image is captured.

                double ISOSpeed = ImageISOSpeed;

                // ISO speed can usually be interpreted as analog gain applied to the signal coming out of the
                // CCD before the signal is recorded. This is not true with Apple devices. Based on my testing, hardward gain is only applied
                // to the CCD signal up to certain ISO speed (200 on iPhone SE). Above that value, the ISO reported has
                // no effect on the raw pixel data. For higher ISO speeds the gain is apparently applied in post processing
                // according the ExposureBaseline value in the DNG meta data. Apple tech support could not provide
                // me with a method for determining the hardware gain applied to the raw pixel data. Based on my own experiments,
                // the relative hardward gain applied to the raw pixel data is:
                //
                // gain = ISO / 2^BaselineExposure
                //
                // I'm still unsure if this is true for all devices/lighting conditions. However, I have yet to see any data to the contrary.

                NSNumber baselineExposure = (NSNumber)DNGMetadata[ImageIO.CGImageProperties.DNGBaselineExposure];
                double sensorSensitivity = ISOSpeed / Math.Pow(2, (double)baselineExposure);

                mImageData.CameraId = mCameraDevice.UniqueID;
                mImageData.RawImagePixelData = imageData;
                // The size is taken from the raw pixel buffer itself, as on Android, so it always matches the pixel data.
                mImageData.ImageHeight = (int)pixelBuffer.Height;
                mImageData.ImageWidth = (int)pixelBuffer.Width;
                // The EXIF exposure time is rounded to 1 over a whole number, which is several percent off at long
                // exposures (measured with the sensitivity test). The camera's own value is used instead,
                // with the EXIF value kept as a fallback.
                mImageData.ExposureTime = ImageExposureDuration > 0 ? ImageExposureDuration : (float)photo.Properties.Exif.ExposureTime;
                mImageData.SensorSensitivity = sensorSensitivity;
                mImageData.BayerFilterPattern = bayerFilter;
                mImageData.RowStride = (int)bytesPerRow;
                mImageData.BlackLevel = BlackLevel;

                // Diagnostics are gathered only in diagnostics mode, after the measurement values are complete,
                // and can never fail the capture. None of these values is used in a calculation.
                // Each value is read on its own; one that the phone does not provide is shown as "not reported".
                if (CameraDiagnostics.Enabled)
                {
                    try
                    {
                        CaptureDiagnostics d = new();
                        d.Add("Camera used", () => mCameraDevice.UniqueID);
                        d.Add("Camera type", () => mCameraDevice.DeviceType.ToString());
                        d.Add("Lenses behind camera", () => OperatingSystem.IsIOSVersionAtLeast(13) ? mCameraDevice.ConstituentDevices?.Length ?? 0 : null);
                        d.Add("Camera format", () => mCameraDevice.ActiveFormat.ToString());
                        d.Add("Lens model", () => photo.Properties.Exif.Dictionary[ImageIO.CGImageProperties.ExifLensModel]?.ToString());
                        d.Add("Bayer pattern", () => bayerFilter.ToString());
                        d.Add("Raw size", () => $"{pixelBuffer.Width} x {pixelBuffer.Height}");
                        d.Add("Row length (bytes)", () => bytesPerRow);
                        d.Add("Active area", () => DNGMetadata[ImageIO.CGImageProperties.DNGActiveArea]?.ToString());
                        d.Add("Default crop size", () => DNGMetadata[ImageIO.CGImageProperties.DNGDefaultCropSize]?.ToString());
                        d.Add("Black level, reported", () => blackLevelValues);
                        d.Add("White level", () => DNGMetadata[ImageIO.CGImageProperties.DNGWhiteLevel]?.ToString());
                        d.Add("USED baseline exposure",() => baselineExposure.DoubleValue);
                        d.Add("USED camera ISO",() => ISOSpeed);
                        d.Add("ISO, EXIF", () => photo.Properties.Exif.ISOSpeedRatings);
                        d.Add("ISO range of format", () => $"{mCameraDevice.ActiveFormat.MinISO} to {mCameraDevice.ActiveFormat.MaxISO}");
                        d.Add("Exposure time, EXIF (s)", () => photo.Properties.Exif.ExposureTime);
                        d.Add("Exposure time, camera (s)", () => ImageExposureDuration);
                        d.Add("Exposure mode", () => mCameraDevice.ExposureMode.ToString());
                        d.Add("Exposure offset", () => mCameraDevice.ExposureTargetOffset);
                        d.Add("Exposure bias", () => mCameraDevice.ExposureTargetBias);
                        d.Add("Aperture, camera (f-number)", () => mCameraDevice.LensAperture);
                        d.Add("Aperture, EXIF (f-number)", () => photo.Properties.Exif.Dictionary[ImageIO.CGImageProperties.ExifFNumber]?.ToString());
                        d.Add("Focal length (mm)", () => photo.Properties.Exif.Dictionary[ImageIO.CGImageProperties.ExifFocalLength]?.ToString());
                        d.Add("Lens position (0 to 1)", () => mCameraDevice.LensPosition);
                        d.Add("Zoom factor", () => mCameraDevice.VideoZoomFactor);
                        d.Add("Noise profile", () => DNGMetadata[ImageIO.CGImageProperties.DNGNoiseProfile]?.ToString());
                        d.Add("Baseline noise", () => DNGMetadata[ImageIO.CGImageProperties.DNGBaselineNoise]?.ToString());

                        try
                        {
                            d.WhiteLevel = ((NSNumber)DNGMetadata[ImageIO.CGImageProperties.DNGWhiteLevel]).DoubleValue;
                        }
                        catch
                        {
                        }

                        mImageData.Diagnostics = d;
                    }
                    catch
                    {
                    }
                }


                pixelBuffer.Dispose();
                photo.Dispose();


            }
            else
            {

                // convert preview image to byte array with metadata
                CIImage ciiamge = new CIImage(photo.PreviewPixelBuffer);
                ciiamge = ciiamge.CreateBySettingProperties(photo.Properties.Dictionary); // add EXIF data
                NSData imageData = CIContext_ImageRepresentation.GetJpegRepresentation(new CIContext(), ciiamge, CGColorSpace.CreateDeviceRGB(), new CIImageRepresentationOptions());

                mImageData.JpegImage = new byte[imageData.Length];
                System.Runtime.InteropServices.Marshal.Copy(imageData.Bytes, mImageData.JpegImage, 0, Convert.ToInt32(imageData.Length));

                imageData.Dispose();
                photo.Dispose();

               
            }

            if (mImageData.RawImagePixelData != null && mImageData.JpegImage != null)
            {
                
                ImageCaptureEventArgs capArgs = new()
                {
                    ImageData = mImageData
                };
                ImageCaptureCompletedEvent?.Invoke(this, capArgs);
            }
        }

    }
}
