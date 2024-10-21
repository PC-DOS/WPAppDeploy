Imports MahApps.Metro.Controls.Dialogs
Imports System.IO
Imports System.Windows.Media
Imports System.Windows.Media.Imaging
Imports Microsoft.Phone.Tools.Deploy
Imports Microsoft.WindowsAPICodePack.Dialogs
Imports Microsoft.SmartDevice.Connectivity
Imports Microsoft.SmartDevice.Connectivity.Interface
Imports Microsoft.SmartDevice.MultiTargeting.Connectivity
Imports WindowsPhone.Tools
Class MainWindow
    'Constants
    Const PhoneNameMode As String = "{Name} (ID={ID}){IsEmulator}"

    'Variables
    Dim PhoneManager As New MultiTargetingConnectivity(System.Globalization.CultureInfo.CurrentCulture.LCID)
    Dim PhoneList As New List(Of ConnectableDevice)
    Dim CurrentConnectedPhone As IDevice = Nothing
    Dim EmptyList As New List(Of String)
    Dim IsPhoneConnected As Boolean = False

    'Events
    Private Event PhoneConnected()

    Private Sub UpdatePhoneList()
        'Enumerate available phones
        Try
            Dim PhoneNameList As New List(Of String)
            PhoneList.Clear()
            For Each Device In PhoneManager.GetConnectableDevices(False)
                PhoneList.Add(Device)
                PhoneNameList.Add(PhoneNameMode.Replace("{Name}", Device.Name).Replace("{ID}", Device.Id).Replace("{IsEmulator}", IIf(Device.IsEmulator, " (Emulator)", "")))
            Next
            lstPhones.ItemsSource = PhoneNameList
        Catch ex As Exception
            lstPhones.ItemsSource = EmptyList
        End Try
    End Sub
    Private Async Sub ConnectToSelectedPhone()
        Dim ConnectingProgress As MahApps.Metro.Controls.Dialogs.ProgressDialogController
        Dim ResultMessage As String = "Unknown error"
        If lstPhones.SelectedIndex >= 0 Then
            'Disconnect
            Try
                If Not IsNothing(CurrentConnectedPhone) Then
                    CurrentConnectedPhone.Disconnect()
                End If
                IsPhoneConnected = False
            Catch ex As Exception
                IsPhoneConnected = False
            End Try
            'Connect
            Try
                ConnectingProgress = Await DialogManager.ShowProgressAsync(Me, "Connecting", "Connecting to """ & lstPhones.SelectedItem.ToString() & """...")
                System.Windows.Forms.Application.DoEvents()
                CurrentConnectedPhone = PhoneList(lstPhones.SelectedIndex).Connect()
                Await ConnectingProgress.CloseAsync()
                IsPhoneConnected = True
            Catch ex As Exception
                IsPhoneConnected = False
                ResultMessage = ex.HResult.ToString & ": " & ex.Message
            End Try
            'Check result
            If Not IsPhoneConnected Then
                Await ShowMessageAsync("Unable to connect to """ & lstPhones.SelectedItem.ToString() & """..." & vbCrLf & "Error " & ResultMessage, "Connection failed")
                IsPhoneConnected = False
            Else
                IsPhoneConnected = True
                RaiseEvent PhoneConnected()
            End If
        End If
    End Sub
    Private Sub UpdatePhoneInfo() Handles Me.PhoneConnected
        Dim PhoneInfo As ISystemInfo = CurrentConnectedPhone.GetSystemInfo()
        txtOSBuild.Text = PhoneInfo.OSMajor.ToString() & "." & PhoneInfo.OSMinor.ToString() & "." & PhoneInfo.OSBuildNo.ToString()
    End Sub

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        UpdatePhoneList()
    End Sub

    Private Sub btnRefreshPhone_Click(sender As Object, e As RoutedEventArgs) Handles btnRefreshPhone.Click
        UpdatePhoneList()
    End Sub

    Private Sub btnConnect_Click(sender As Object, e As RoutedEventArgs) Handles btnConnect.Click
        ConnectToSelectedPhone()
    End Sub

    Private Sub btnTestDeploy_Click(sender As Object, e As RoutedEventArgs) Handles btnTestDeploy.Click
        If IsPhoneConnected Then
            Dim AppPackagePath As String = "D:\WP8ApplicationCollection\Repack\QQ.xap"
            AppPackagePath = "D:\Program Files\WPHackingTools\vcREG1.6\vcREG_1_6_W10M_PCMod.xap"
            'AppPackagePath = "D:\WP8ApplicationCollection\Repack\42722sim756.LightSensor_2015.918.1119.0_neutral_~_ggbebs9x2d86a.appxbundle"

            Dim AppGuid As System.Guid
            Dim AppGenre As String
            Dim AppIconPath As String
            If AppPackagePath.ToUpper.EndsWith(".XAP") Then
                Dim AppManifest As New Xap(AppPackagePath)
                AppGuid = AppManifest.Guid
                AppGenre = "32"
                AppIconPath = AppManifest.Icon
            Else
                Dim AppManifest As IAppManifestInfo = Utils.ReadAppManifestInfoFromPackage(AppPackagePath)
                AppGuid = AppManifest.ProductId
                AppGenre = "32"
                AppIconPath = CInt(AppManifest.PackageType).ToString()
            End If

            CurrentConnectedPhone.InstallApplication(AppGuid, _
                                                     AppGuid, _
                                                     AppGenre, _
                                                     AppIconPath, _
                                                     AppPackagePath)
        End If
    End Sub
End Class
