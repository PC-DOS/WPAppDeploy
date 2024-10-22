Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.Tools.Connectivity
Imports Microsoft.Tools.Deploy
Public Class Windows10ConnectionManager
    ''' <summary>
    ''' Structure of a connected phone device.
    ''' </summary>
    ''' <remarks></remarks>
    Public Structure PhoneDevice
        ''' <summary>
        ''' Windows Phone device operating interface.
        ''' </summary>
        ''' <remarks></remarks>
        Public ConnectedDevice As RemoteDeployClient
        ''' <summary>
        ''' Raw device model.
        ''' </summary>
        ''' <remarks></remarks>
        Public RawDevice As DiscoveredDeviceInfo
    End Structure
    ''' <summary>
    ''' Flag of the presence of a connected phone.
    ''' </summary>
    ''' <remarks></remarks>
    Private _IsPhoneConnected As Boolean
    ''' <summary>
    ''' Instance of a connected phone.
    ''' </summary>
    ''' <remarks></remarks>
    Private _ConnectedPhone As PhoneDevice

    Private Class PhoneDiscoveringThread
        ''' <summary>
        ''' Internal manager of phone connectivity.
        ''' </summary>
        ''' <remarks></remarks>
        Private _PhoneConnectionManager As New DeviceDiscoveryService
        ''' <summary>
        ''' Time out of device discovering.
        ''' </summary>
        ''' <remarks></remarks>
        Private _PhoneDiscoveryTimeOut As TimeSpan
        ''' <summary>
        ''' List of discovered devices.
        ''' </summary>
        ''' <remarks></remarks>
        Private _DevicesDiscovered As New List(Of DiscoveredDeviceInfo)
        ''' <summary>
        ''' Create new instance for device discovering.
        ''' </summary>
        ''' <param name="NewPhoneDiscoveryTimeOut"></param>
        ''' <remarks></remarks>
        Public Sub New(NewPhoneDiscoveryTimeOut As TimeSpan)
            _PhoneDiscoveryTimeOut = NewPhoneDiscoveryTimeOut
        End Sub
        ''' <summary>
        ''' Start discovering devices.
        ''' </summary>
        ''' <returns>List of discovered devices.</returns>
        ''' <remarks></remarks>
        Public Function StartDeviceDiscovering() As List(Of DiscoveredDeviceInfo)
            _PhoneConnectionManager.Timeout = _PhoneDiscoveryTimeOut
            AddHandler _PhoneConnectionManager.Discovered, Sub(sender As Object, e As DiscoveredEventArgs)
                                                               _DevicesDiscovered.Add(e.Info)
                                                           End Sub
            _PhoneConnectionManager.Start()
            Thread.Sleep(_PhoneDiscoveryTimeOut)
            _PhoneConnectionManager.Stop()
            Return _DevicesDiscovered
        End Function
        ''' <summary>
        ''' Time out of device discovering.
        ''' </summary>
        ''' <value>Time out of device discovering.</value>
        ''' <returns>Time out of device discovering.</returns>
        ''' <remarks></remarks>
        Public Property PhoneDiscoveryTimeOut As TimeSpan
            Get
                Return _PhoneDiscoveryTimeOut
            End Get
            Set(value As TimeSpan)
                _PhoneDiscoveryTimeOut = value
            End Set
        End Property
        ''' <summary>
        ''' List of discovered devices.
        ''' </summary>
        ''' <value></value>
        ''' <returns>List of discovered devices.</returns>
        ''' <remarks></remarks>
        Public ReadOnly Property DevicesDiscovered As List(Of DiscoveredDeviceInfo)
            Get
                Return _DevicesDiscovered
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Thread for asynchronous phone connecting.
    ''' </summary>
    ''' <remarks></remarks>
    Private Class PhoneConnectingThread
        ''' <summary>
        ''' Instance of the phone pending connection.
        ''' </summary>
        ''' <remarks></remarks>
        Private _PhoneToConnect As DiscoveredDeviceInfo
        ''' <summary>
        ''' Instance of connedted phone.
        ''' </summary>
        ''' <remarks></remarks>
        Private _ConnectedPhone As RemoteDeployClient
        ''' <summary>
        ''' Create a new instance for connecting.
        ''' </summary>
        ''' <param name="NewPhoneToConnect">Instance of the phone to be connected.</param>
        ''' <remarks></remarks>
        Public Sub New(Optional NewPhoneToConnect As DiscoveredDeviceInfo = Nothing)
            _PhoneToConnect = NewPhoneToConnect
            _ConnectedPhone = RemoteDeployClient.CreateRemoteDeployClient()
        End Sub
        ''' <summary>
        ''' Connect to target.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function ConnectToPhone() As RemoteDeployClient
            Dim ConnectionParam As New ConnectionOptions
            ConnectionParam.Guid = _PhoneToConnect.UniqueId
            ConnectionParam.ConnectTimeout = TimeSpan.FromSeconds(5)
            _ConnectedPhone.Connect(ConnectionParam)
            Return _ConnectedPhone
        End Function
        ''' <summary>
        ''' Instance of the phone to be connected.
        ''' </summary>
        ''' <value>Instance of the phone to be connected.</value>
        ''' <returns>Instance of the phone to be connected.</returns>
        ''' <remarks></remarks>
        Public Property PhoneToConnect As DiscoveredDeviceInfo
            Get
                Return _PhoneToConnect
            End Get
            Set(value As DiscoveredDeviceInfo)
                _PhoneToConnect = value
            End Set
        End Property
        ''' <summary>
        ''' Instance of connedted phone.
        ''' </summary>
        ''' <value>Instance of connedted phone.</value>
        ''' <returns>Instance of connedted phone.</returns>
        ''' <remarks></remarks>
        Public ReadOnly Property ConnectedPhone As RemoteDeployClient
            Get
                Return _ConnectedPhone
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Thread for asynchronous app installing
    ''' </summary>
    ''' <remarks></remarks>
    Private Class AppDeployingThread
        ''' <summary>
        ''' Instance of deploying target.
        ''' </summary>
        ''' <remarks></remarks>
        Private _PhoneToDeploy As RemoteDeployClient
        ''' <summary>
        ''' Path to app package.
        ''' </summary>
        ''' <remarks></remarks>
        Private _AppPackagePath As String
        ''' <summary>
        ''' Path list of current app's dependencies
        ''' </summary>
        ''' <remarks></remarks>
        Private _AppDependencies As New List(Of String)
        ''' <summary>
        ''' Path to current app's certificate
        ''' </summary>
        ''' <remarks></remarks>
        Private _AppCertPath As String
        ''' <summary>
        ''' Instance of deployed app package on remote.
        ''' </summary>
        ''' <remarks></remarks>
        Private _DeployedAppPackage As String
        ''' <summary>
        ''' Create a new instance for app deploying.
        ''' </summary>
        ''' <param name="NewPhoneToDeploy">Instance of deploying target.</param>
        ''' <param name="NewAppPackagePath">Path to app package.</param>
        ''' <remarks></remarks>
        Public Sub New(Optional NewPhoneToDeploy As RemoteDeployClient = Nothing, Optional NewAppPackagePath As String = "", Optional NewAppDependencies As List(Of String) = Nothing, Optional NewAppCertPath As String = "")
            _PhoneToDeploy = NewPhoneToDeploy
            _AppPackagePath = NewAppPackagePath
            _AppDependencies = NewAppDependencies
            _AppCertPath = NewAppCertPath
        End Sub
        ''' <summary>
        ''' Deploy app package to target
        ''' </summary>
        ''' <returns>Instance of deployed app package on remote.</returns>
        ''' <remarks></remarks>
        Public Function InstallAppPackage() As String
            'Install package
            _PhoneToDeploy.InstallAppx(InstallAppxOptions.Install, _AppPackagePath, _AppDependencies, _AppCertPath)
            _DeployedAppPackage = ""
            Return _DeployedAppPackage
        End Function
        ''' <summary>
        ''' Instance of deploying target.
        ''' </summary>
        ''' <value>Instance of deploying target.</value>
        ''' <returns>Instance of deploying target.</returns>
        ''' <remarks></remarks>
        Public Property PhoneToDeploy As RemoteDeployClient
            Get
                Return _PhoneToDeploy
            End Get
            Set(value As RemoteDeployClient)
                _PhoneToDeploy = value
            End Set
        End Property
        ''' <summary>
        ''' Path to app package.
        ''' </summary>
        ''' <value>Path to app package.</value>
        ''' <returns>Path to app package.</returns>
        ''' <remarks></remarks>
        Public Property AppPackagePath As String
            Get
                Return _AppPackagePath
            End Get
            Set(value As String)
                _AppPackagePath = AppPackagePath
            End Set
        End Property
        ''' <summary>
        ''' Instance of deployed app package on remote.
        ''' </summary>
        ''' <value></value>
        ''' <returns>Instance of deployed app package on remote.</returns>
        ''' <remarks></remarks>
        Public ReadOnly Property DeployedAppPackage As String
            Get
                Return _DeployedAppPackage
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
    ''' Enumerate available devices synchronously.
    ''' </summary>
    ''' <returns>List of available devices.</returns>
    ''' <remarks></remarks>
    Public Function EnumerateDevices() As List(Of DiscoveredDeviceInfo)
        Dim PhoneDiscoverer As New PhoneDiscoveringThread(TimeSpan.FromSeconds(5))
        PhoneDiscoverer.StartDeviceDiscovering()
        Return PhoneDiscoverer.DevicesDiscovered
    End Function

    ''' <summary>
    ''' Enumerate available devices asynchronously.
    ''' </summary>
    ''' <returns>List of available devices.</returns>
    ''' <remarks></remarks>
    Public Async Function EnumerateDevicesAsync() As Task(Of List(Of DiscoveredDeviceInfo))
        Dim PhoneDiscoverer As New PhoneDiscoveringThread(TimeSpan.FromSeconds(5))
        Await Task.Factory.StartNew(Sub()
                                        PhoneDiscoverer.StartDeviceDiscovering()
                                    End Sub)
        Return PhoneDiscoverer.DevicesDiscovered
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
    Public Function ConnectToPhone(PhoneToConnect As DiscoveredDeviceInfo) As PhoneDevice
        Try
            Dim PhoneConnector As New PhoneConnectingThread(PhoneToConnect)
            PhoneConnector.ConnectToPhone()
            _ConnectedPhone.ConnectedDevice = PhoneConnector.ConnectedPhone
            _ConnectedPhone.RawDevice = PhoneToConnect
            _IsPhoneConnected = True
            Return _ConnectedPhone
        Catch ex As Exception
            Throw ex
            _IsPhoneConnected = False
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Connect to phone asynchronously.
    ''' </summary>
    ''' <param name="PhoneToConnect">Instance of the phone to be connected.</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Async Function ConnectToPhoneAsync(PhoneToConnect As DiscoveredDeviceInfo) As Task(Of PhoneDevice)
        Try
            Dim PhoneConnector As New PhoneConnectingThread(PhoneToConnect)
            Await Task.Factory.StartNew(Sub()
                                            PhoneConnector.ConnectToPhone()
                                        End Sub)
            _ConnectedPhone.ConnectedDevice = PhoneConnector.ConnectedPhone
            _ConnectedPhone.RawDevice = PhoneToConnect
            _IsPhoneConnected = True
            Return _ConnectedPhone
        Catch ex As Exception
            Throw ex
            _IsPhoneConnected = False
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Install an application package to connected phone synchronously.
    ''' </summary>
    ''' <param name="AppPackagePath">Path to the application package.</param>
    ''' <remarks></remarks>
    Public Sub InstallAppPackage(AppPackagePath As String, AppDependencies As List(Of String), AppCertPath As String)
        Try
            Dim AppDeployer As New AppDeployingThread(_ConnectedPhone.ConnectedDevice, AppPackagePath, AppDependencies, AppCertPath)
            AppDeployer.InstallAppPackage()
        Catch ex As Exception
            Throw ex
        End Try
    End Sub

    ''' <summary>
    ''' Install an application package to connected phone asynchronously.
    ''' </summary>
    ''' <param name="AppPackagePath">Path to the application package.</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Async Function InstallAppPackageAsync(AppPackagePath As String, AppDependencies As List(Of String), AppCertPath As String) As Task
        Try
            Dim AppDeployer As New AppDeployingThread(_ConnectedPhone.ConnectedDevice, AppPackagePath, AppDependencies, AppCertPath)
            Await Task.Factory.StartNew(Sub()
                                            AppDeployer.InstallAppPackage()
                                        End Sub)
        Catch ex As Exception
            Throw ex
        End Try
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
