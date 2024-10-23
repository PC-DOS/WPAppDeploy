Imports MahApps.Metro.Controls.Dialogs
Imports System.IO
Imports System.Threading
Imports System.Threading.Tasks
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

    Private Function OpenWindowAsync(Of TWindow As {System.Windows.Window, New})() As Task
        Dim TaskComp As New TaskCompletionSource(Of Object)

        'Create child window container
        Dim WinThread As New Thread(Sub()
                                        Dim WindowInstance As New TWindow
                                        'Close target window's event loop when closed
                                        AddHandler WindowInstance.Closed, Sub()
                                                                              System.Windows.Threading.Dispatcher.ExitAllFrames()
                                                                          End Sub
                                        'Show window in separated thread
                                        WindowInstance.Show()
                                        'Start window's event loop
                                        System.Windows.Threading.Dispatcher.Run()
                                        'Set task's result
                                        TaskComp.SetResult(Nothing)
                                    End Sub)

        'Allow sub thread to be exited when calling Application.Current.Shutdown()
        WinThread.IsBackground = True
        'Allow UI dispatching
        WinThread.SetApartmentState(ApartmentState.STA)
        'Start sub thread
        WinThread.Start()

        'Returns task object
        Return TaskComp.Task
    End Function

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
        Dim ConnectingProgress As ProgressDialogController
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
            ConnectingProgress = Await DialogManager.ShowProgressAsync(Me, "Connecting", "Connecting to """ & lstPhones.SelectedItem.ToString().Replace(vbCrLf, " ") & """...")
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

    Private Sub RefreshAppDeployHistory()
        lstAppDeployHistory.ItemsSource = EmptyList
        lstAppDeployHistory.ItemsSource = AppHistoryList
    End Sub

    Private Sub AddAppDeployHistory(PackagePath As String)
        If Not IsCurrentPackageInHistoryList(PackagePath) And PackagePath.Trim().Length <> 0 Then
            AppHistoryList.Add(PackagePath)
        End If
        RefreshAppDeployHistory()
    End Sub

    Private Sub RefreshAppDeploymentLog()
        lstAppDeployLog.ItemsSource = EmptyList
        lstAppDeployLog.ItemsSource = AppDeploymentLog
        lstAppDeployLog.SelectedIndex = lstAppDeployLog.Items.Count - 1
        Try
            lstAppDeployLog.ScrollIntoView(lstAppDeployLog.SelectedItem)
        Catch ex As Exception
            Exit Sub
        End Try
    End Sub

    Private Sub WriteAppDeploymentLog(LogText As String)
        'Write log
        AppDeploymentLog.Add(LogText)

        'Update display
        RefreshAppDeploymentLog()
    End Sub

    Private Sub AddSelectedPackagesToPendingList()
        If lstAppDeployHistory.SelectedItems.Count > 0 Then
            'Add selected package to pending list
            For Each AppPackage In lstAppDeployHistory.SelectedItems
                If Not txtAppsToDeploy.Text.EndsWith(vbCrLf) And Not txtAppsToDeploy.Text.Trim().Length = 0 Then
                    txtAppsToDeploy.Text = txtAppsToDeploy.Text & vbCrLf
                End If
                txtAppsToDeploy.Text = txtAppsToDeploy.Text & AppPackage.ToString() & vbCrLf
            Next
        End If
    End Sub

    Private Sub UpdateControlStatus()
        'Device list
        If lstPhones.SelectedIndex >= 0 Then
            btnConnect.IsEnabled = True
        Else
            btnConnect.IsEnabled = False
        End If

        'Pending list
        If txtAppsToDeploy.Text.Trim().Length > 0 Then
            btnInstall.IsEnabled = True
        Else
            btnInstall.IsEnabled = False
        End If

        'App history
        If lstAppDeployHistory.SelectedItems.Count > 0 Then
            btnAddToPendingList.IsEnabled = True
            btnRemoveSelectedHistoryEntry.IsEnabled = True
        Else
            btnAddToPendingList.IsEnabled = False
            btnRemoveSelectedHistoryEntry.IsEnabled = False
        End If
    End Sub

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        'Find connectable phones
        UpdatePhoneList()

        'Load app deployment history
        AppHistoryList = SettingsProvider.LoadAppDeployHistoryList()
        RefreshAppDeployHistory()

        'UI update
        UpdateControlStatus()
    End Sub

    Private Sub MainWindow_Closing(sender As Object, e As ComponentModel.CancelEventArgs) Handles Me.Closing
        'Save history
        SettingsProvider.SaveAppDeployHistoryList(AppHistoryList)
    End Sub

    Private Sub btnRefreshPhone_Click(sender As Object, e As RoutedEventArgs) Handles btnRefreshPhone.Click
        UpdatePhoneList()
    End Sub

    Private Sub btnConnect_Click(sender As Object, e As RoutedEventArgs) Handles btnConnect.Click
        ConnectToSelectedPhone()
    End Sub

    Private Sub btnUWPDeployment_Click(sender As Object, e As RoutedEventArgs) Handles btnUWPDeployment.Click
        OpenWindowAsync(Of UWPDeployerWindow)()
    End Sub

    Private Sub btnBrowse_Click(sender As Object, e As RoutedEventArgs) Handles btnBrowse.Click
        Dim AppPackageBrowseDialog As New CommonOpenFileDialog
        With AppPackageBrowseDialog
            .EnsureFileExists = True
            .Filters.Add(New CommonFileDialogFilter("應用程式封包", ".xap;.appx;.appxbundle"))
            .Filters.Add(New CommonFileDialogFilter("所有檔案", ".*"))
            .Multiselect = True
        End With
        If AppPackageBrowseDialog.ShowDialog(Me) = CommonFileDialogResult.Ok Then
            'Add selected package to pending list
            For Each AppPackagePath As String In AppPackageBrowseDialog.FileNames
                txtAppsToDeploy.Text = txtAppsToDeploy.Text & AppPackagePath.Trim() & vbCrLf
            Next
        End If
    End Sub

    Private Async Sub btnClearPendingList_Click(sender As Object, e As RoutedEventArgs) Handles btnClearPendingList.Click
        Dim MsgBoxResult As MessageDialogResult
        MsgBoxResult = Await ShowMessageAsync("Clear pending list", _
                                              "Are you sure to clear the pending list?", _
                                              MessageDialogStyle.AffirmativeAndNegative)
        If MsgBoxResult = MessageDialogResult.Affirmative Then
            txtAppsToDeploy.Text = ""
        End If
    End Sub

    Private Async Sub btnInstall_Click(sender As Object, e As RoutedEventArgs) Handles btnInstall.Click
        If PhoneManager.IsPhoneConnected Then
            If txtAppsToDeploy.Text.Trim().Length > 0 Then
                Dim InstallingProgress As ProgressDialogController
                InstallingProgress = Await ShowProgressAsync("Installing", _
                                                           "Installing apps to connected device")

                'Parse pending list
                Dim PendingListOrig() As String
                PendingListOrig = txtAppsToDeploy.Text.Split(New Char() {vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries)

                'Filter empty paths
                Dim PendingList As New List(Of String)
                For Each AppPackage As String In PendingListOrig
                    If AppPackage.Trim().Length > 0 Then
                        PendingList.Add(AppPackage.Trim())
                    End If
                Next

                'Counters
                Dim PackageCount As Integer = PendingList.Count
                Dim SuccessCount As Integer = 0
                Dim FailureCount As Integer = 0
                prgAppDeployment.Minimum = 0
                prgAppDeployment.Maximum = PackageCount
                prgAppDeployment.Value = SuccessCount + FailureCount
                AppDeploymentLog.Clear()
                RefreshAppDeploymentLog()

                'Install apps
                For Each AppPackage As String In PendingList
                    Try
                        'Update message
                        InstallingProgress.SetMessage("Installing """ & AppPackage & """...")

                        'Install package asynchronously
                        Await PhoneManager.InstallAppPackageAsync(AppPackage)

                        'Update log
                        WriteAppDeploymentLog("Installed package """ & AppPackage & """ successfully.")
                        AddAppDeployHistory(AppPackage)
                        SuccessCount += 1
                    Catch ex As Exception
                        'Logging
                        WriteAppDeploymentLog("Failed installing package """ & AppPackage & """. " & ex.Message)
                        FailureCount += 1
                    End Try

                    'Update progress
                    prgAppDeployment.Value = SuccessCount + FailureCount
                    InstallingProgress.SetProgress((SuccessCount + FailureCount) / PackageCount)
                Next

                'Close overlay
                Await InstallingProgress.CloseAsync()

                'Save history list
                SettingsProvider.SaveAppDeployHistoryList(AppHistoryList)
            Else
                Await ShowMessageAsync("No packages selected", "Please add at least 1 app package to start installing.")
            End If
        Else
            Await ShowMessageAsync("No connected devices", "Please connected to a device before installing apps.")
        End If
    End Sub

    Private Sub btnAddPackage_Click(sender As Object, e As RoutedEventArgs) Handles btnAddPackage.Click
        Dim AppPackageBrowseDialog As New CommonOpenFileDialog
        With AppPackageBrowseDialog
            .EnsureFileExists = True
            .Filters.Add(New CommonFileDialogFilter("應用程式封包", ".xap;.appx;.appxbundle"))
            .Filters.Add(New CommonFileDialogFilter("所有檔案", ".*"))
            .Multiselect = True
        End With
        If AppPackageBrowseDialog.ShowDialog(Me) = CommonFileDialogResult.Ok Then
            'Add selected package to pending list
            For Each AppPackagePath As String In AppPackageBrowseDialog.FileNames
                AddAppDeployHistory(AppPackagePath)
            Next
            SettingsProvider.SaveAppDeployHistoryList(AppHistoryList)
        End If
    End Sub

    Private Sub btnAddToPendingList_Click(sender As Object, e As RoutedEventArgs) Handles btnAddToPendingList.Click
        AddSelectedPackagesToPendingList()
    End Sub

    Private Async Sub btnRemoveSelectedHistoryEntry_Click(sender As Object, e As RoutedEventArgs) Handles btnRemoveSelectedHistoryEntry.Click
        If lstAppDeployHistory.SelectedItems.Count > 0 Then
            Dim MsgBoxResult As MessageDialogResult
            MsgBoxResult = Await ShowMessageAsync("Remove history entry", _
                                                  "Are you sure to remove selected history items?", _
                                                  MessageDialogStyle.AffirmativeAndNegative)
            If MsgBoxResult = MessageDialogResult.Affirmative Then
                For Each AppPackage In lstAppDeployHistory.SelectedItems
                    AppHistoryList.Remove(AppPackage.ToString())
                Next
                RefreshAppDeployHistory()
            End If
        End If
    End Sub

    Private Async Sub btnClearHistoryList_Click(sender As Object, e As RoutedEventArgs) Handles btnClearHistoryList.Click
        Dim MsgBoxResult As MessageDialogResult
        MsgBoxResult = Await ShowMessageAsync("Clear pending list", _
                                              "Are you sure to clear the pending list?", _
                                              MessageDialogStyle.AffirmativeAndNegative)
        If MsgBoxResult = MessageDialogResult.Affirmative Then
            AppHistoryList.Clear()
            SettingsProvider.SaveAppDeployHistoryList(AppHistoryList)
            RefreshAppDeployHistory()
        End If
    End Sub

    Private Sub lstPhones_KeyUp(sender As Object, e As KeyEventArgs) Handles lstPhones.KeyUp
        If e.Key = Key.Enter Then
            ConnectToSelectedPhone()
        End If
    End Sub

    Private Sub lstPhones_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs) Handles lstPhones.MouseDoubleClick
        ConnectToSelectedPhone()
    End Sub

    Private Sub lstPhones_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles lstPhones.SelectionChanged
        UpdateControlStatus()
    End Sub

    Private Sub txtAppsToDeploy_TextChanged(sender As Object, e As TextChangedEventArgs) Handles txtAppsToDeploy.TextChanged
        UpdateControlStatus()
    End Sub

    Private Sub lstAppDeployHistory_KeyUp(sender As Object, e As KeyEventArgs) Handles lstAppDeployHistory.KeyUp
        If e.Key = Key.Enter Then
            AddSelectedPackagesToPendingList()
        End If
    End Sub

    Private Sub lstAppDeployHistory_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs) Handles lstAppDeployHistory.MouseDoubleClick
        AddSelectedPackagesToPendingList()
    End Sub

    Private Sub lstAppDeployHistory_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles lstAppDeployHistory.SelectionChanged
        UpdateControlStatus()
    End Sub
End Class
