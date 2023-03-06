![appicon_image](/HydroColor/Resources/Images/appicon_image.png?raw=true "appicon_image")

# HydroColor: A Water Quality App
HydroColor is a water quality application that uses the a smartphone's digital camera to determine the reflectance of natural water bodies.

For more information on how HydroColor works, see the following links:

[Simple Overview](https://misclab.umeoce.maine.edu/research/HydroColor.php)

[Leeuw, T.; Boss, E. The HydroColor App: Above Water Measurements of Remote Sensing Reflectance and Turbidity Using a Smartphone Camera. Sensors 2018, 18, 256.](https://www.mdpi.com/1424-8220/18/1/256)

![learn_more](/HydroColor/Resources/Images/learn_more.png?raw=true "learn_more")

## What Language is HydroColor Written In?
HydroColor is written in C# and uses the .NET MAUI framework. This framework allows the app to work across both Apple iOS and Android devices.

The same codebase is shared between the iOS and Android versions of the app, with the exception of the camera control. Platform specific code was required for controlling the camera on each platform.

The HydroColor code can be compiled and run using Visual Studio. The Community version of Visual Studio is free for individual developers.

## Where is the Code That Calculates Optical Products?
All of the code for calculating the optical products (e.g. reflectance, turbidity) is located in /Services/OpticalPropertiesCalculator.cs. 

You may want to also look into the platform specific CameraController.cs files to see how the camera data is collected and processed.

## License
This code is released under the GNU GPLv3 license. See the LICENSE file for more information.