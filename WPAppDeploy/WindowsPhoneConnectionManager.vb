Imports System.Threading.Tasks
Imports WindowsPhone.Tools
Imports Microsoft.Phone.Tools.Deploy
Imports Microsoft.SmartDevice.Connectivity
Imports Microsoft.SmartDevice.Connectivity.Interface
Imports Microsoft.SmartDevice.MultiTargeting.Connectivity
Public Class WindowsPhoneConnectionManager
    ''' <summary>
    ''' Structure of a connected phone device.
    ''' </summary>
    ''' <remarks></remarks>
    Public Structure PhoneDevice
        ''' <summary>
        ''' Windows Phone device operating interface.
        ''' </summary>
        ''' <remarks></remarks>
        Public ConnectedDevice As IDevice
        ''' <summary>
        ''' Raw device model.
        ''' </summary>
        ''' <remarks></remarks>
        Public RawDevice As ConnectableDevice
    End Structure
    ''' <summary>
    ''' Internal manager of phone connectivity.
    ''' </summary>
    ''' <remarks></remarks>
    Private _PhoneConnectionManager As New MultiTargetingConnectivity(System.Globalization.CultureInfo.CurrentCulture.LCID)
    ''' <summary>
    ''' Flag of the presence of a connected phone.
    ''' </summary>
    ''' <remarks></remarks>
    Private _IsPhoneConnected As Boolean
    ''' <summary>
    ''' Instance of a connected phone.
    ''' </summary>
    ''' <remarks></remarks>
    Private _ConnectedPhone As New PhoneDevice

    ''' <summary>
    ''' Thread for asynchronous phone connecting.
    ''' </summary>
    ''' <remarks></remarks>
    Private Class PhoneConnectingThread
        ''' <summary>
        ''' Instance of the phone pending connection.
        ''' </summary>
        ''' <remarks></remarks>
        Private _PhoneToConnect As ConnectableDevice
        ''' <summary>
        ''' Instance of connedted phone.
        ''' </summary>
        ''' <remarks></remarks>
        Private _ConnectedPhone As IDevice
        ''' <summary>
        ''' Create a new child thread.
        ''' </summary>
        ''' <param name="PhoneToConnect">Instance of the phone to be connected.</param>
        ''' <remarks></remarks>
        Public Sub New(PhoneToConnect As ConnectableDevice)
            _PhoneToConnect = PhoneToConnect
        End Sub
        ''' <summary>
        ''' Connect to target.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function ConnectToPhone() As IDevice
            _ConnectedPhone = _PhoneToConnect.Connect()
            Return _ConnectedPhone
        End Function
        ''' <summary>
        ''' Instance of the phone to be connected.
        ''' </summary>
        ''' <value>Instance of the phone to be connected.</value>
        ''' <returns>Instance of the phone to be connected.</returns>
        ''' <remarks></remarks>
        Public Property PhoneToConnect As ConnectableDevice
            Get
                Return _PhoneToConnect
            End Get
            Set(value As ConnectableDevice)
                _PhoneToConnect = value
            End Set
        End Property
        ''' <summary>
        ''' Instance of connedted phone.
        ''' </summary>
        ''' <value>Instance of connedted phone.</value>
        ''' <returns>Instance of connedted phone.</returns>
        ''' <remarks></remarks>
        Public ReadOnly Property ConnectedPhone As IDevice
            Get
                Return _ConnectedPhone
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Create a new phone connection manager instance.
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub New()
        _IsPhoneConnected = False
    End Sub
    ''' <summary>
    ''' Enumerate available devices.
    ''' </summary>
    ''' <returns>List of available devices.</returns>
    ''' <remarks></remarks>
    Public Function EnumerateDevices() As List(Of ConnectableDevice)
        Dim PhoneList As New List(Of ConnectableDevice)
        For Each Phone As ConnectableDevice In _PhoneConnectionManager.GetConnectableDevices()
            PhoneList.Add(Phone)
        Next
        Return PhoneList
    End Function

    ''' <summary>
    ''' Disconnect from connected phone.
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub DiconnectFromPhone()
        Try
            _ConnectedPhone.ConnectedDevice.Disconnect()
        Catch ex As Exception

        End Try
        _IsPhoneConnected = False
    End Sub

    ''' <summary>
    ''' Connect to phone synchronously.
    ''' </summary>
    ''' <param name="PhoneToConnect">Instance of the phone to be connected.</param>
    ''' <remarks></remarks>
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

    ''' <summary>
    ''' Connect to phone asynchronously.
    ''' </summary>
    ''' <param name="PhoneToConnect">Instance of the phone to be connected.</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Async Function ConnectToPhoneAsync(PhoneToConnect As ConnectableDevice) As Task
        Try
            Dim PhoneConnector As New PhoneConnectingThread(PhoneToConnect)
            Await Task.Factory.StartNew(Sub()
                                            PhoneConnector.ConnectToPhone()
                                        End Sub)
            _ConnectedPhone.ConnectedDevice = PhoneConnector.ConnectedPhone
            _ConnectedPhone.RawDevice = PhoneToConnect
            _IsPhoneConnected = True
        Catch ex As Exception
            Throw ex
            _IsPhoneConnected = False
        End Try
    End Function

    ''' <summary>
    ''' Install an application package to connected phone.
    ''' </summary>
    ''' <param name="AppPackagePath">Path to the application package.</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
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

    ''' <summary>
    ''' Flag of the presence of a connected phone.
    ''' </summary>
    ''' <value></value>
    ''' <returns>Flag of the presence of a connected phone.</returns>
    ''' <remarks></remarks>
    Public ReadOnly Property IsPhoneConnected() As Boolean
        Get
            Return _IsPhoneConnected
        End Get
    End Property

    ''' <summary>
    ''' Instance of a connected phone.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
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
