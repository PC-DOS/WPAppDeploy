Public Class SettingsProvider

    Public Const ApplicationName As String = "WPAppDeploy"
    Public Const SettingsSectionName As String = "AppSettings"
    Public Const AppDeployHistoryListKey As String = "AppDeployHistoryList"
    Public Const AppDeployHistoryListDefVal As String = ""
    Public Const AppDeployHistoryListSeparator As String = "|"

    Public Shared Function LoadAppDeployHistoryList() As List(Of String)
        'Load setting from registry
        Dim HistoryListStringRaw As String = GetSetting(ApplicationName, _
                                               SettingsSectionName, _
                                               AppDeployHistoryListKey, _
                                               AppDeployHistoryListDefVal)

        'Generate history list
        Dim HistoryListString() As String = HistoryListStringRaw.Split(AppDeployHistoryListSeparator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
        Dim HistoryList As New List(Of String)
        For Each HistoryEntry As String In HistoryListString
            If HistoryEntry.Trim().Length <> 0 Then
                HistoryList.Add(HistoryEntry)
            End If
        Next

        Return HistoryList
    End Function

    Public Shared Sub SaveAppDeployHistoryList(HistoryList As List(Of String))
        'Generate string to save
        Dim HistoryListStringRaw As String = ""
        For Each HistoryEntry In HistoryList
            HistoryListStringRaw = HistoryListStringRaw & HistoryEntry & AppDeployHistoryListSeparator
        Next

        'Save settings to registry
        SaveSetting(ApplicationName, _
                    SettingsSectionName, _
                    AppDeployHistoryListKey, _
                    HistoryListStringRaw)
    End Sub
End Class
