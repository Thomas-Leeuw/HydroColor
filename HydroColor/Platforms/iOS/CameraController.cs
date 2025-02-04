using AVFoundation;
using CoreAnimation;
using CoreGraphics;
using CoreImage;
using CoreVideo;
using Foundation;
using HydroColor.Models;

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

        public AVCaptureSession mCaptureSession { get; private set; }

        public CameraController()
        {
            mPhotoOutput = new AVCapturePhotoOutput();
            mCaptureDelegate = new PhotoCaptureDelegate
            {
                WillCapturePhotoAction = (captureOutput, resolvedSettings) =>
                {
                    ImageISOSpeed = mCameraDevice.ISO;
                },
                DidCapturePhotoAction = (captureOutput, resolvedSettings) =>
                {
                    mPreviewLayer.Session.StopRunning();
                },
                DidFinishProcessingPhotoAction = (captureOutput, photo, error) =>
                {
                    OnImageAvailable(photo);
                }
            };
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

        public void TakeRAWImage()
        {

            // Create the setting used for image capture, they can only be used once
            AVCapturePhotoSettings capturePhotoSettings = CreateCapturePhotoSettings();

            // Trigger image capture
            mPhotoOutput.CapturePhoto(settings: capturePhotoSettings, mCaptureDelegate);
            
        }

        public void CloseCamera()
        {
            if (mCaptureSession != null)
            {
                mCaptureSession.Dispose();
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

                ColorChannelData<double> BlackLevel = new();
                try
                {
                    // Black level might be a single number
                    NSNumber bl = (NSNumber)DNGMetadata[ImageIO.CGImageProperties.DNGBlackLevel];
                    BlackLevel.Red = bl.UInt16Value;
                    BlackLevel.Green = bl.UInt16Value;
                    BlackLevel.Blue = bl.UInt16Value;
                }
                catch
                {
                    // OR an array of numbers
                    NSArray blackLevelStringArray = (NSArray)DNGMetadata[ImageIO.CGImageProperties.DNGBlackLevel];
                    if (blackLevelStringArray.Count >= 3)
                    {
                        // order of color channels is not known....
                        BlackLevel.Red = blackLevelStringArray.GetItem<NSNumber>(0).UInt16Value;
                        BlackLevel.Green = blackLevelStringArray.GetItem<NSNumber>(1).UInt16Value;
                        BlackLevel.Blue = blackLevelStringArray.GetItem<NSNumber>(2).UInt16Value;
                    }
                    else
                    {
                        BlackLevel.Red = blackLevelStringArray.GetItem<NSNumber>(0).UInt16Value;
                        BlackLevel.Green = blackLevelStringArray.GetItem<NSNumber>(0).UInt16Value;
                        BlackLevel.Blue = blackLevelStringArray.GetItem<NSNumber>(0).UInt16Value;
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

                mImageData.RawImagePixelData = imageData;
                mImageData.ImageHeight = (int)photo.Properties.Exif.PixelYDimension;
                mImageData.ImageWidth = (int)photo.Properties.Exif.PixelXDimension;
                mImageData.ExposureTime = (float)photo.Properties.Exif.ExposureTime;
                mImageData.SensorSensitivity = sensorSensitivity;
                mImageData.BayerFilterPattern = bayerFilter;
                mImageData.RowStride = (int)bytesPerRow;
                mImageData.BlackLevel = BlackLevel;
                

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
