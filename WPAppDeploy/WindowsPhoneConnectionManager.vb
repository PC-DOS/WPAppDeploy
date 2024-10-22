Imports System.Threading.Tasks
Imports WindowsPhone.Tools
Imports Microsoft.Phone.Tools.Deploy
Imports Microsoft.SmartDevice.Connectivity
Imports Microsoft.SmartDevice.Connectivity.Interface
Imports Microsoft.SmartDevice.MultiTargeting.Connectivity
Public Class WindowsPhoneConnectionManager
    Public Structure PhoneDevice
        Public ConnectedDevice As IDevice
        Public RawDevice As ConnectableDevice
    End Structure
    Private _PhoneConnectionManager As New MultiTargetingConnectivity(System.Globalization.CultureInfo.CurrentCulture.LCID)
    Private _IsPhoneConnected As Boolean
    Private _ConnectedPhone As New PhoneDevice

    Private Class PhoneConnectingThread
        Private _PhoneToConnect As ConnectableDevice
        Private _ConnectedPhone As IDevice
        Public Sub New(PhoneToConnect As ConnectableDevice)
            _PhoneToConnect = PhoneToConnect
        End Sub
        Public Function ConnectToPhone() As IDevice
            _ConnectedPhone = _PhoneToConnect.Connect()
            Return _ConnectedPhone
        End Function
        Public Property PhoneToConnect As ConnectableDevice
            Get
                Return _PhoneToConnect
            End Get
            Set(value As ConnectableDevice)
                _PhoneToConnect = value
            End Set
        End Property
        Public ReadOnly Property ConnectedPhone As IDevice
            Get
                Return _ConnectedPhone
            End Get
        End Property
    End Class

    Public Sub New()
        _IsPhoneConnected = False
    End Sub
    Public Function EnumerateDevices() As List(Of ConnectableDevice)
        Dim PhoneList As New List(Of ConnectableDevice)
        For Each Phone As ConnectableDevice In _PhoneConnectionManager.GetConnectableDevices()
            PhoneList.Add(Phone)
        Next
        Return PhoneList
    End Function

    Public Sub DiconnectFromPhone()
        Try
            _ConnectedPhone.ConnectedDevice.Disconnect()
        Catch ex As Exception

        End Try
        _IsPhoneConnected = False
    End Sub

    Public Sub ConnectToPhone(PhoneToConnect As ConnectableDevice)
        Try
            Dim ConnectedDevice As New PhoneDevice
            With ConnectedDevice
                .RawDevice = PhoneToConnect
                .ConnectedDevice = PhoneToConnect.Connect()
            End With
            _ConnectedPhone = ConnectedDevice
            _IsPhoneConnected = True
        Catch ex As Exception
            Throw ex
            _IsPhoneConnected = False
        End Try
    End Sub

    Public Async Function ConnectToPhoneAsync(PhoneToConnect As ConnectableDevice) As Task
        Try
            Dim PhoneConnector As New PhoneConnectingThread(PhoneToConnect)
            Dim ConnectionTask As Task(Of IDevice) = Await Task.Factory.StartNew(Function() As IDevice
                                                                                     Return PhoneConnector.ConnectToPhone()
                                                                                 End Function)
            _ConnectedPhone.ConnectedDevice = ConnectionTask.Result
            _ConnectedPhone.RawDevice = PhoneToConnect
            _IsPhoneConnected = True
        Catch ex As Exception
            Throw ex
            _IsPhoneConnected = False
        End Try
    End Function

    Public Function InstallAppPackage(AppPackagePath As String) As IRemoteApplication
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

        Return _ConnectedPhone.ConnectedDevice.InstallApplication(AppGuid, _
                                                             AppGuid, _
                                                             AppGenre, _
                                                             AppIconPath, _
                                                             AppPackagePath)
    End Function
    Public ReadOnly Property IsPhoneConnected() As Boolean
        Get
            Return _IsPhoneConnected
        End Get
    End Property

    Public ReadOnly Property ConnectedPhone() As PhoneDevice
        Get
            If _IsPhoneConnected Then
                Return _ConnectedPhone
            Else
                Return Nothing
            End If
        End Get
    End Property
End Class
