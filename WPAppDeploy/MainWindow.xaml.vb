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
    Const PhoneNameMode As String = "{Name}" & vbCrLf & "(ID={ID})" & vbCrLf & "{IsEmulator}"


    'Phone connection management
    Dim PhoneManager As New WindowsPhoneConnectionManager
    Dim PhoneList As New List(Of ConnectableDevice)
    Dim EmptyList As New List(Of String)

    'Application deploying management
    Dim AppHistoryList As New List(Of String)
    Dim AppDeploymentLog As New List(Of String)

    'Events
    Private Event PhoneConnected()

    Private Function IsCurrentPackageInHistoryList(PackagePath As String) As Boolean
        For Each HistoryEntry As String In AppHistoryList
            If PackagePath.Trim().ToUpper = HistoryEntry.Trim().ToUpper() Then
                Return True
            End If
        Next
        Return False
    End Function

    Private Sub UpdatePhoneList()
        'Enumerate available phones
        Try
            Dim PhoneNameList As New List(Of String)
            PhoneList.Clear()
            For Each Device In PhoneManager.EnumerateDevices()
                PhoneList.Add(Device)
                PhoneNameList.Add(PhoneNameMode.Replace("{Name}", Device.Name).Replace("{ID}", Device.Id).Replace("{IsEmulator}", IIf(Device.IsEmulator, " (Emulator)", "(Real Device)")))
            Next
            lstPhones.ItemsSource = PhoneNameList
        Catch ex As Exception
            lstPhones.ItemsSource = EmptyList
        End Try
    End Sub

    Private Async Sub ConnectToSelectedPhone()
        Dim ConnectingProgress As MahApps.Metro.Controls.Dialogs.ProgressDialogController
        Dim ResultMessage As String = " : Unknown error"
        If lstPhones.SelectedIndex >= 0 Then
            'Disconnect
            Try
                If PhoneManager.IsPhoneConnected Then
                    PhoneManager.DiconnectFromPhone()
                End If
            Catch ex As Exception

            End Try
            'Connect
            ConnectingProgress = Await DialogManager.ShowProgressAsync(Me, "Connecting", "Connecting to """ & lstPhones.SelectedItem.ToString() & """...")
            ConnectingProgress.SetIndeterminate()
            Try
                Await PhoneManager.ConnectToPhoneAsync(PhoneList(lstPhones.SelectedIndex))
            Catch ex As Exception
                ResultMessage = ex.HResult.ToString & ": " & ex.Message
            End Try
            Await ConnectingProgress.CloseAsync()
            'Check result
            If Not PhoneManager.IsPhoneConnected Then
                Await ShowMessageAsync("Connection failed", "Unable to connect to """ & lstPhones.SelectedItem.ToString().Replace(vbCrLf, " ") & """..." & vbCrLf & "Error " & ResultMessage)
            Else
                RaiseEvent PhoneConnected()
            End If
        End If
    End Sub

    Private Sub UpdatePhoneInfo() Handles Me.PhoneConnected
        lblDeviceName.Text = PhoneManager.ConnectedPhone.RawDevice.Name
        lblDeviceID.Text = PhoneManager.ConnectedPhone.RawDevice.Id
        lblIsEmulator.Text = IIf(PhoneManager.ConnectedPhone.RawDevice.IsEmulator(), "是", "否")
        Dim PhoneInfo As ISystemInfo = PhoneManager.ConnectedPhone.ConnectedDevice.GetSystemInfo()
        lblOSBuild.Text = PhoneInfo.OSMajor.ToString() & "." & PhoneInfo.OSMinor.ToString() & "." & PhoneInfo.OSBuildNo.ToString()
        lblProcessorArchitecture.Text = PhoneInfo.ProcessorArchitecture
        lblProcessorInstructionSet.Text = PhoneInfo.InstructionSet
        lblProcessorCount.Text = PhoneInfo.NumberOfProcessors.ToString()
        lblRAMSize.Text = PhoneInfo.AvailPhys.ToString() & " / " & PhoneInfo.TotalPhys.ToString()
        lblVirtualRAMSize.Text = PhoneInfo.AvailVirtual.ToString() & " / " & PhoneInfo.TotalVirtual.ToString()
    End Sub

    Private Sub WriteAppDeploymentLog(LogText As String)
        'Write log
        AppDeploymentLog.Add(LogText)

        'Update display
        lstAppDeploymentHistory.ItemsSource = EmptyList
        lstAppDeploymentHistory.ItemsSource = AppDeploymentLog
        lstAppDeploymentHistory.SelectedIndex = lstAppDeploymentHistory.Items.Count - 1
        Try
            lstAppDeploymentHistory.ScrollIntoView(lstAppDeploymentHistory.SelectedItem)
        Catch ex As Exception
            Exit Sub
        End Try
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
        If PhoneManager.IsPhoneConnected Then
            Dim AppPackagePath As String = "D:\WP8ApplicationCollection\Repack\QQ.xap"
            AppPackagePath = "D:\Program Files\WPHackingTools\vcREG1.6\vcREG_1_6_W10M_PCMod.xap"
            'AppPackagePath = "D:\WP8ApplicationCollection\Repack\42722sim756.LightSensor_2015.918.1119.0_neutral_~_ggbebs9x2d86a.appxbundle"

            PhoneManager.InstallAppPackage(AppPackagePath)
        End If
    End Sub
End Class
