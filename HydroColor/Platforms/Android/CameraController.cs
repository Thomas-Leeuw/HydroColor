using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Media;
using Android.OS;
using Android.Views;
using Image = Android.Media.Image;
using Size = Android.Util.Size;
using Java.Nio;
using Android.Graphics;
using Android.Content;
using HydroColor.Models;
using HydroColor.Platforms.Android.Callbacks;
using Java.Util;
using Java.Lang;

namespace HydroColor.Platforms.Android
{
    public class CameraController
    {
        public event EventHandler<ImageCaptureEventArgs> ImageCaptureCompletedEvent;
        public HydroColorRawImageData mImageDataModel;

        AutoFitTextureView mTextureView;
        Surface mOutputSurface;

        HandlerThread mBackgroundThread;
        Handler mBackgroundHandler;

        readonly object mCameraStateLock = new object();
        CameraDevice mCameraDevice;
        CameraCharacteristics mCharacteristics;

        CameraCaptureSession mCaptureSession;
        CaptureRequest mCaptureRequest;

        CameraCaptureListener mCaptureListener;

        ImageReader mJpegImageReader;
        ImageReader mRawImageReader;

        TotalCaptureResult mTotalCaptureResult;

        public CameraCharacteristics GetRearFacingCameraCharacteristics()
        {
            // Search for a compatible camera 
            CameraManager cameraManager = (CameraManager)Platform.CurrentActivity.GetSystemService(Context.CameraService);
            string cameraID = GetCompatibleCameraID(cameraManager);

            if (string.IsNullOrEmpty(cameraID))
            {
                return null;
            }
            else
            {
                return cameraManager.GetCameraCharacteristics(cameraID);
            }

        }

        public bool OpenCamera()
        {

            // Search for a compatible camera 
            CameraManager cameraManager = (CameraManager)Platform.CurrentActivity.GetSystemService(Context.CameraService);
            string cameraID = GetCompatibleCameraID(cameraManager);
            if (string.IsNullOrEmpty(cameraID))
            {
                throw new System.NotSupportedException("No camera found on the device");
            }

            mCharacteristics = cameraManager.GetCameraCharacteristics(cameraID);

            CameraStateListener StateCallback = new CameraStateListener
            {
                OnOpenedAction = camera =>
                {
                    lock (mCameraStateLock)
                    {
                        mCameraDevice = camera;
                        StartCaptureSessionPreview();
                    }
                },
                OnDisconnectedAction = camera =>
                {
                    CloseCamera();
                },
                OnErrorAction = (camera, error) =>
                {
                    CloseCamera();
                },
                OnClosedAction = camera =>
                {
                }
            };

            // Open the camera
            StartBackgroundThread();
            cameraManager.OpenCamera(cameraID, StateCallback, mBackgroundHandler);

            return true;
        }

        public string GetCompatibleCameraID(CameraManager cameraManager)
        {
            foreach (string cameraId in cameraManager.GetCameraIdList())
            {
                CameraCharacteristics characteristics = cameraManager.GetCameraCharacteristics(cameraId);

                // Verify the camera is the back facing camera
                var facing = (int)characteristics.Get(CameraCharacteristics.LensFacing);
                if (facing != (int)LensFacing.Back)
                {
                    continue;
                }

                // Check if camera supports RAW images
                if (!characteristics.Get(
                        CameraCharacteristics.RequestAvailableCapabilities).ToArray<int>().Contains(
                        (int)RequestAvailableCapabilities.Raw))
                {
                    continue;
                }

                return cameraId;
            }

            return null;
        }

        
        void StartCaptureSessionPreview()
        {

            // This is the output Surface we need to start preview.
            mOutputSurface = new Surface(mTextureView.SurfaceTexture);

            OnImageAvailableListener mOnImageAvailableListener = new OnImageAvailableListener
            {
                OnImageAvailableAction = reader => OnImageAvailable(reader)
            };

            mRawImageReader = CreateImageReader(mOnImageAvailableListener, ImageFormatType.RawSensor);
            mJpegImageReader = CreateImageReader(mOnImageAvailableListener, ImageFormatType.Jpeg);
            CreateCaptureRequest();


            CameraPreviewStateListener cameraPreviewStateListener = new CameraPreviewStateListener
            {
                OnConfiguredAction = cameraCaptureSession =>
                {
                    lock (mCameraStateLock)
                    {
                        CaptureRequest.Builder mPreviewRequestBuilder = mCameraDevice.CreateCaptureRequest(CameraTemplate.Preview);
                        mPreviewRequestBuilder.AddTarget(mOutputSurface);
                        cameraCaptureSession.SetRepeatingRequest(mPreviewRequestBuilder.Build(), null, mBackgroundHandler);
                        mCaptureSession = cameraCaptureSession;
                    }
                    
                },
                OnConfigureFailedAction = captureSession =>
                {
                    CloseCamera();
                },
                OnClosedAction = captureSession =>
                {
                }
               
            };

            /* ***********************************************************
            * CreateCaptureSession constructor with multiple arguments is deprecated.
            * It has been replaced by passing a SessionConfiguration (as below).
            * However, I'm using the deprecated version, so it is backward compatable with older 
            * android devices.
            *
                SessionConfiguration config = new SessionConfiguration((int)SessionType.Regular,
                new List<OutputConfiguration>() {
                new OutputConfiguration(mOutputSurface),
                new OutputConfiguration(mJpegImageReader.Surface),
                new OutputConfiguration(mRawImageReader.Surface)},
                Executors.NewSingleThreadExecutor(),
                cameraPreviewStateListener
                );

                mCameraDevice.CreateCaptureSession(config);
            */

            // Create a CameraCaptureSession for camera preview.
            mCameraDevice.CreateCaptureSession(new List<Surface>() {
                    mOutputSurface,
                    mJpegImageReader.Surface,
                    mRawImageReader.Surface},
                    cameraPreviewStateListener, mBackgroundHandler);

            /**************************************************************/
        }

        void CreateCaptureRequest()
        {
            CaptureRequest.Builder captureBuilder = mCameraDevice.CreateCaptureRequest(CameraTemplate.StillCapture);
            captureBuilder.AddTarget(mJpegImageReader.Surface);
            captureBuilder.AddTarget(mRawImageReader.Surface);
            captureBuilder.Set(CaptureRequest.JpegOrientation, (int)mCharacteristics.Get(CameraCharacteristics.SensorOrientation));
            mCaptureRequest = captureBuilder.Build();

            mCaptureListener = new CameraCaptureListener
            {
                OnCaptureStartedAction = (session, request, timestamp, frameNumber) => { },
                OnCaptureCompletedAction = (session, request, result) =>
                {
                    mTotalCaptureResult = result;
                    /*
                     * There is a timing issue around when a capture is fully completed (i.e. raw image, jpeg image, and meta data all available)
                     * OnCaptureCompleted is sometimes fired before OnImageAvailable is fired for the jpeg image.
                     * So we need to check if the ImageDataModel is complete in both OnCaptureCompleted and in OnImageAvailable
                     * and only proceed if all the data is available
                     */
                    if (mImageDataModel.RawImagePixelData != null && mImageDataModel.JpegImage != null && mTotalCaptureResult != null)
                    {
                        OnImageCaptureCompleted();
                    }
                },
                OnCaptureFailedAction = (session, request, failure) => { },
                OnCaptureSequenceCompletedAction = (session, sequenceID, frameNumber) => {  }
            };
        }

        ImageReader CreateImageReader(OnImageAvailableListener imageAvailableListener, ImageFormatType imageFormat)
        {
            StreamConfigurationMap map = (StreamConfigurationMap)mCharacteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);
            Size[] sizes = map.GetOutputSizes((int)imageFormat);

            Size imageSize;
            if (imageFormat == ImageFormatType.RawSensor)
            {
                // Get largest raw image
                imageSize = sizes.OrderByDescending(element => element.Width * element.Height).First();
            }
            else
            {
                // Get thumbnail size jpeg
                imageSize = sizes.OrderBy(x => System.Math.Abs(x.Width * (x.Height - 512))).First();
            }

            ImageReader imageReader = ImageReader.NewInstance(imageSize.Width, imageSize.Height, imageFormat, 1);
            imageReader.SetOnImageAvailableListener(imageAvailableListener, mBackgroundHandler);

            return imageReader;
        }

        public void TakeRAWImage()
        {
            mCaptureSession.StopRepeating();

            mImageDataModel = new HydroColorRawImageData();
            mTotalCaptureResult = null;
            // After capture is completed callbacks in both CaptureCallback and OnImageAvailableListener are called
            mCaptureSession.Capture(mCaptureRequest, mCaptureListener, mBackgroundHandler);
            
        }

        void OnImageAvailable(ImageReader reader)
        {
            Image newImage = reader.AcquireNextImage();
            Image.Plane[] planes = newImage.GetPlanes();
            ByteBuffer buff = planes[0].Buffer;
            buff.Rewind();
            byte[] imageData = new byte[buff.Remaining()];
            buff.Get(imageData, 0, imageData.Length);

            if (newImage.Format == ImageFormatType.RawSensor)
            {
                mImageDataModel.RawImagePixelData = imageData;
                mImageDataModel.RowStride = planes[0].RowStride;
                mImageDataModel.ImageWidth = newImage.Width;
                mImageDataModel.ImageHeight = newImage.Height;
              
            }
            else if (newImage.Format == ImageFormatType.Jpeg)
            {
                mImageDataModel.JpegImage = imageData;
            }

            newImage.Close();
            newImage.Dispose();
            planes[0].Dispose();
            buff.Dispose();

            /*
            * There is a timing issue around when a capture is fully completed (i.e. raw image, jpeg image, and meta data all available)
            * OnCaptureCompleted is sometimes fired before OnImageAvailable is fired for the jpeg image.
            * So we need to check if the ImageDataModel is complete in both OnCaptureCompleted and in OnImageAvailable
            * and only proceed if all the data is available
            */
            if (mImageDataModel.RawImagePixelData != null && mImageDataModel.JpegImage != null && mTotalCaptureResult != null)
            {
                OnImageCaptureCompleted();
            }
        }

        private void OnImageCaptureCompleted()
        {

            // SensorDynamicBlackLevel may be more accurate, but not availible on older operating systems
            // ((float[]) result.Get(CaptureResult.SensorDynamicBlackLevel)).Cast<UInt16>().ToArray();
            BlackLevelPattern SensorBlackLevel = (BlackLevelPattern)mCharacteristics.Get(CameraCharacteristics.SensorBlackLevelPattern);

            SensorInfoColorFilterArrangement pixelFormat = (SensorInfoColorFilterArrangement)(int)mCharacteristics.Get(CameraCharacteristics.SensorInfoColorFilterArrangement);
            BayerFilterType bayerFilter;
            ColorChannelData<double> BlackLevel = new();
            switch (pixelFormat)
            {
                case SensorInfoColorFilterArrangement.Bggr: /* Bayer 10-bit Little-Endian, packed in 16-bits, ordered B G B G... alternating with G R G R... */
                    bayerFilter = BayerFilterType.BGGR;
                    BlackLevel.Red = (UInt16)SensorBlackLevel.GetOffsetForIndex(1, 1); // args are column, row
                    BlackLevel.Green = (UInt16)SensorBlackLevel.GetOffsetForIndex(1, 0);
                    BlackLevel.Blue = (UInt16)SensorBlackLevel.GetOffsetForIndex(0, 0);
                    break;
                case SensorInfoColorFilterArrangement.Gbrg: /* Bayer 10-bit Little-Endian, packed in 16-bits, ordered G B G B... alternating with R G R G... */
                    bayerFilter = BayerFilterType.GBRG;
                    BlackLevel.Red = (UInt16)SensorBlackLevel.GetOffsetForIndex(0, 1); // args are column, row
                    BlackLevel.Green = (UInt16)SensorBlackLevel.GetOffsetForIndex(0, 0);
                    BlackLevel.Blue = (UInt16)SensorBlackLevel.GetOffsetForIndex(1, 0);
                    break;
                case SensorInfoColorFilterArrangement.Grbg: /* Bayer 10-bit Little-Endian, packed in 16-bits, ordered G R G R... alternating with B G B G... */
                    bayerFilter = BayerFilterType.GRBG;
                    BlackLevel.Red = (UInt16)SensorBlackLevel.GetOffsetForIndex(1, 0); // args are column, row
                    BlackLevel.Green = (UInt16)SensorBlackLevel.GetOffsetForIndex(0, 0);
                    BlackLevel.Blue = (UInt16)SensorBlackLevel.GetOffsetForIndex(0, 1);
                    break;
                case SensorInfoColorFilterArrangement.Rggb: /* Bayer 10-bit Little-Endian, packed in 16-bits, ordered R G R G... alternating with G B G B... */
                    bayerFilter = BayerFilterType.RGGB;
                    BlackLevel.Red = (UInt16)SensorBlackLevel.GetOffsetForIndex(0, 0); // args are column, row
                    BlackLevel.Green = (UInt16)SensorBlackLevel.GetOffsetForIndex(1, 0);
                    BlackLevel.Blue = (UInt16)SensorBlackLevel.GetOffsetForIndex(1, 1);
                    break;
                default:
                    bayerFilter = BayerFilterType.Unknown;
                    break;
            }

            mImageDataModel.BayerFilterPattern = bayerFilter;
            mImageDataModel.BlackLevel = BlackLevel;
            mImageDataModel.ExposureTime = (float)mTotalCaptureResult.Get(CaptureResult.SensorExposureTime) / 1000000000;
            mImageDataModel.SensorSensitivity = (int)mTotalCaptureResult.Get(CaptureResult.SensorSensitivity);

            ImageCaptureEventArgs capArgs = new ImageCaptureEventArgs()
            {
                ImageData = mImageDataModel
            };

            ImageCaptureCompletedEvent.Invoke(this, capArgs);
        }

        public void SetTextureView(AutoFitTextureView textureView)
        {
            mTextureView = textureView;
            ConfigureTextureViewSize(GetRearFacingCameraCharacteristics());
        }

        public AutoFitTextureView GetTextureView()
        {
            return mTextureView;
        }

        Size ConfigureTextureViewSize(CameraCharacteristics cameraCharacteristics)
        {
            if (cameraCharacteristics == null)
            {
                return new Size(0, 0);
            }

            var map = (StreamConfigurationMap)cameraCharacteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);

            // For still image captures, we always use the largest available size.
            Size largestJpeg = (Size)Collections.Max(Arrays.AsList(map.GetOutputSizes((int)ImageFormatType.Jpeg)),
                                   new CompareSizesByArea());

            // Swap the view dimensions as needed if they are rotated relative to
            // the sensor.
            int sensorOrientation = (int)cameraCharacteristics.Get(CameraCharacteristics.SensorOrientation);
            bool swappedDimensions = sensorOrientation == 90 || sensorOrientation == 270;
            int rotatedViewWidth = mTextureView.Width;
            int rotatedViewHeight = mTextureView.Height;
            if (swappedDimensions)
            {
                rotatedViewWidth = mTextureView.Height;
                rotatedViewHeight = mTextureView.Width;
            }


            // Find the best preview size for these view dimensions and configured JPEG size.
            Size previewSize = ChooseOptimalSize(map.GetOutputSizes(Class.FromType(typeof(SurfaceTexture))),
                                   rotatedViewWidth, rotatedViewHeight, largestJpeg);

            if (swappedDimensions)
            {
                mTextureView.SetAspectRatio(
                    previewSize.Height, previewSize.Width);
            }
            else
            {
                mTextureView.SetAspectRatio(
                    previewSize.Width, previewSize.Height);
            }

            // height and width of the buffer needs to match the image height and width, not the 
            // texture view. So the dimensions are not swapped.
            mTextureView.SurfaceTexture.SetDefaultBufferSize(previewSize.Width, previewSize.Height);

            return previewSize;

        }

        Size ChooseOptimalSize(Size[] choices, int width, int height, Size aspectRatio)
        {
            // Collect the supported resolutions that are at least as big as the preview Surface
            List<Size> bigEnough = new List<Size>();
            int w = aspectRatio.Width;
            int h = aspectRatio.Height;
            foreach (Size option in choices)
            {
                if (option.Height == option.Width * h / w &&
                    option.Width >= width && option.Height >= height)
                {
                    bigEnough.Add(option);
                }
            }

            // Pick the smallest of those, assuming we found any
            if (bigEnough.Count > 0)
            {
                return (Size)Collections.Min(bigEnough, new CompareSizesByArea());
            }
            else
            {
                // None found
                return choices[0];
            }
        }



        void StartBackgroundThread()
        {
            mBackgroundThread = new HandlerThread("CameraBackground");
            mBackgroundThread.Start();
            lock (mCameraStateLock)
            {
                mBackgroundHandler = new Handler(mBackgroundThread.Looper);
            }
        }
        void StopBackgroundThread()
        {
            
            try
            {
                mBackgroundThread.QuitSafely();
                mBackgroundThread.Join();
                mBackgroundThread = null;
                lock (mCameraStateLock)
                {
                    mBackgroundHandler = null;
                }
            }
            catch 
            {
            }
        }
        public void CloseCamera()
        {
            if (mCameraDevice != null)
            {
                lock (mCameraStateLock)
                {
                    mCameraDevice.Close();
                    mCameraDevice = null;
                }
            }
            StopBackgroundThread();
        }
    }

    class CompareSizesByArea : Java.Lang.Object, IComparator
    {
        public int Compare(Size lhs, Size rhs)
        {
            // We cast here to ensure the multiplications won't overflow
            return Long.Signum((long)lhs.Width * lhs.Height -
            (long)rhs.Width * rhs.Height);
        }

        int IComparator.Compare(Java.Lang.Object lhs, Java.Lang.Object rhs)
        {
            return 0;
        }

        bool IComparator.Equals(Java.Lang.Object @object)
        {
            return false;
        }
    }
}
