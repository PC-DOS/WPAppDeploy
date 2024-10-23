# WPAppDeploy

Small tool to help deploying XAP/AppX/AppXBundle packages to your Windows Phone or Windows 10 Mobile devices.

Before starting, please ensure you have installed corresponding SDKs, and .Net Framework 4.5.

## Compatibility info

This program was tested on following PCs, with a same target device (NOKIA Lumia 1520, Windows 10 Mobile 10.0.15254.603):

* **Windows Server 2012 R2, with Windows Phone 8.1 SDK from Visual Studio 2013 Update 5, and Windows 10 SDK 10.0.14393.795**
    * Deployment of Windows Phone 8/8.1 XAP/AppX/AppXBundle are successful.
    * Deployment of UWP packages fails with `0xC00000BB`. When testing, `WinAppDeployCmd.exe` gives the same error.

* **Windows Server 10 Enterprise LTSC 2019, with Windows Phone 8.1 and Windows 10 SDK from Visual Studio 2015**
    * Deployment of Windows Phone 8/8.1 XAP/AppX/AppXBundle are successful.
    * Deployment of UWP packages are successful.