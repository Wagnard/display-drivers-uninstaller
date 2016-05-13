Option Strict On

Imports System.Security.Principal
Imports System.ComponentModel
Imports System.Reflection
Imports System.Security
Imports System.Text
Imports System.IO

Imports System.Runtime.InteropServices
Imports System.Runtime.CompilerServices
Imports System.Runtime.ConstrainedExecution

Imports Microsoft.Win32
Imports Microsoft.Win32.SafeHandles


<ComVisible(False)>
Public Module Win32Native
    Friend Const LINE_LEN As Int32 = 256
    Friend Const MAX_LEN As Int32 = 260
    Friend ReadOnly CRLF As String = Environment.NewLine

    Friend ReadOnly Is64 As Boolean = False
    Friend ReadOnly IsAdmin As Boolean = False
    Friend ReadOnly DefaultCharSize As Int32 = Marshal.SystemDefaultCharSize
    Friend ReadOnly DefaultCharSizeU As UInt32 = CUInt(DefaultCharSize)
    Friend ReadOnly NullChar() As Char = New Char() {CChar(vbNullChar)}

#Region "Errors"
    Friend Const APPLICATION_ERROR_MASK = &H20000000UI
    Friend Const ERROR_SEVERITY_ERROR = &HC0000000UI

    <Flags()>
    Public Enum [Errors] As UInteger
        BAD_INTERFACE_INSTALLSECT = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H21DUI
        BAD_SECTION_NAME_LINE = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or 1UI
        BAD_SERVICE_INSTALLSECT = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H217UI
        CANT_LOAD_CLASS_ICON = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H20CUI
        CANT_REMOVE_DEVINST = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H232UI
        CLASS_MISMATCH = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H201UI
        DEVICE_INTERFACE_ACTIVE = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H21BUI
        DEVICE_INTERFACE_REMOVED = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H21CUI
        DEVINFO_DATA_LOCKED = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H213UI
        DEVINFO_LIST_LOCKED = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H212UI
        DEVINFO_NOT_REGISTERED = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H208UI
        DEVINSTALL_QUEUE_NONNATIVE = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H230UI
        DEVINST_ALREADY_EXISTS = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H207UI
        DI_BAD_PATH = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H214UI
        DI_DONT_INSTALL = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H22BUI
        DI_DO_DEFAULT = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H20EUI
        DI_NOFILECOPY = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H20FUI
        DI_POSTPROCESSING_REQUIRED = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H226UI
        DUPLICATE_FOUND = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H202UI
        EXPECTED_SECTION_NAME = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or 0UI
        FILEQUEUE_LOCKED = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H216UI
        GENERAL_SYNTAX = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or 3UI
        INVALID_CLASS = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H206UI
        INVALID_CLASS_INSTALLER = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H20DUI
        INVALID_COINSTALLER = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H227UI
        INVALID_DEVINST_NAME = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H205UI
        INVALID_FILTER_DRIVER = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H22CUI
        INVALID_HWPROFILE = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H210UI
        INVALID_INF_LOGCONFIG = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H22AUI
        INVALID_MACHINENAME = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H220UI
        INVALID_PROPPAGE_PROVIDER = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H224UI
        INVALID_REFERENCE_STRING = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H21FUI
        INVALID_REG_PROPERTY = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H209UI
        KEY_DOES_NOT_EXIST = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H204UI
        LINE_NOT_FOUND = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H102UI
        MACHINE_UNAVAILABLE = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H222UI
        NON_WINDOWS_DRIVER = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H22EUI
        NON_WINDOWS_NT_DRIVER = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H22DUI
        NOT_DISABLEABLE = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H231UI
        NOT_INSTALLED = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H1000UI
        NO_ASSOCIATED_CLASS = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H200UI
        NO_ASSOCIATED_SERVICE = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H219UI
        NO_BACKUP = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H103UI
        NO_CATALOG_FOR_OEM_INF = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H22FUI
        NO_CLASSINSTALL_PARAMS = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H215UI
        NO_CLASS_DRIVER_LIST = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H218UI
        NO_COMPAT_DRIVERS = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H228UI
        NO_CONFIGMGR_SERVICES = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H223UI
        NO_DEFAULT_DEVICE_INTERFACE = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H21AUI
        NO_DEVICE_ICON = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H229UI
        NO_DEVICE_SELECTED = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H211UI
        NO_DRIVER_SELECTED = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H203UI
        NO_INF = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H20AUI
        NO_SUCH_DEVICE_INTERFACE = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H225UI
        NO_SUCH_DEVINST = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H20BUI
        NO_SUCH_INTERFACE_CLASS = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H21EUI
        REMOTE_COMM_FAILURE = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H221UI
        SECTION_NAME_TOO_LONG = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or 2UI
        SECTION_NOT_FOUND = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H101UI
        WRONG_INF_STYLE = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H100UI

        ACCESS_DENIED = 5UI
        INVALID_DATA = 13UI
        INSUFFICIENT_BUFFER = 122UI
        NO_MORE_ITEMS = 259UI
        INVALID_USER_BUFFER = 1784UI
    End Enum

#End Region

#Region "Structures"
    <StructLayout(LayoutKind.Explicit)>
    Friend Structure EvilInteger
        <FieldOffset(0)>
        Public Int32 As Int32
        <FieldOffset(0)>
        Public UInt32 As UInt32
    End Structure
#End Region

#Region "Functions"

    ' <Extension()>
    Public Function GetDescription(ByVal EnumConstant As [Enum]) As String
        Dim fi As FieldInfo = EnumConstant.GetType().GetField(EnumConstant.ToString())
        Dim attr() As DescriptionAttribute = DirectCast(fi.GetCustomAttributes(GetType(DescriptionAttribute), False), DescriptionAttribute())

        If attr.Length > 0 Then
            Return attr(0).Description
        Else
            Return EnumConstant.ToString()
        End If
    End Function

    ' <Extension()>
    Public Function ToStringArray(Of T)(ByVal e As [Enum]) As String()
        Dim eNames As List(Of String) = New List(Of String)(10)
        Dim flags As UInt32 = Convert.ToUInt32(e)

        If flags = 0UI Then
            Return Nothing
        End If

        For Each value As T In [Enum].GetValues(GetType(T))
            Dim bit As UInt32 = Convert.ToUInt32(value)

            If (flags And bit) = bit Then
                eNames.Add(value.ToString())
            End If
        Next

        Return eNames.ToArray()
    End Function

    ' <Extension()>
    Friend Function IntPtrAdd(ByVal ptr As IntPtr, ByVal offSet As Int64) As IntPtr
        Return New IntPtr(ptr.ToInt64() + offSet)
    End Function

    ' <Extension()>
    Friend Function GetVersion(ByVal version As UInt64) As String
        Dim bytes() As Byte = BitConverter.GetBytes(version)
        Dim format As String = If(BitConverter.IsLittleEndian, "{0}.{1}.{2}.{3}", "{3}.{2}.{1}.{0}")

        Return String.Format(format,
          BitConverter.ToInt16(bytes, 0).ToString(),
          BitConverter.ToInt16(bytes, 2).ToString(),
          BitConverter.ToInt16(bytes, 4).ToString(),
          BitConverter.ToInt16(bytes, 6).ToString())
    End Function

    ' <Extension()>
    Friend Function GetUInt32(ByVal int As Int32) As UInt32
        Return (New EvilInteger() With {.Int32 = int}).UInt32
    End Function

    ' <Extension()>
    Friend Function GetInt32(ByVal int As UInt32) As Int32
        Return (New EvilInteger() With {.UInt32 = int}).Int32
    End Function

    Friend Sub ShowException(ByVal ex As Exception)
        If TypeOf (ex) Is Win32Exception Then
            Dim e As UInt32 = GetUInt32(DirectCast(ex, Win32Exception).NativeErrorCode)
            Dim detailMsg As String = Nothing

            If GetErrorMessage(e, detailMsg) Then
                MessageBox.Show(String.Format("{0}{2}{2}Error code: {1}{2}{3}{2}{2}{4}", detailMsg, e.ToString(), CRLF, ex.Message, ex.StackTrace), "Win32Exception!")
            Else
                MessageBox.Show(String.Format("Error code: {0}{1}{2}{1}{1}{3}", e.ToString(), CRLF, ex.Message, ex.StackTrace), "Win32Exception!")
            End If
        Else
            MessageBox.Show(ex.Message & CRLF & CRLF & If(ex.TargetSite IsNot Nothing, ex.TargetSite.Name, "<null>") & CRLF & CRLF & ex.Source & CRLF & CRLF & ex.StackTrace, "Exception!")
        End If
    End Sub

    Friend Sub CheckWin32Error(ByVal success As Boolean)
        If Not success Then
            Throw New Win32Exception()
        End If
    End Sub

    Friend Function GetLastWin32Error() As Int32
        Return Marshal.GetLastWin32Error()
    End Function

    Friend Function GetLastWin32ErrorU() As UInt32
        Return GetUInt32(Marshal.GetLastWin32Error())
    End Function

    Friend Function GetErrorMessage(ByVal errCode As UInt32, ByRef message As String) As Boolean
        Select Case errCode
            Case Errors.ACCESS_DENIED
                message = "You have no rights... run as Admin!"
                Return True
            Case Errors.NO_SUCH_DEVINST
                message = "Device doesn't exists!"
                Return True
            Case Else
                message = String.Empty
                Return False
        End Select
    End Function
#End Region

    Friend Class StructPtr
        Implements IDisposable

        Private _disposed As Boolean
        Private _ptr As IntPtr
        Private _objSize As New EvilInteger

        Public ReadOnly Property Ptr As IntPtr
            Get
                Return _ptr
            End Get
        End Property
        Public ReadOnly Property ObjSize As Int32
            Get
                Return _objSize.Int32
            End Get
        End Property
        Public ReadOnly Property ObjSizeU As UInt32
            Get
                Return _objSize.UInt32
            End Get
        End Property

        Public Sub New(ByVal obj As Object, Optional ByVal size As UInt32 = 0UI)
            If Ptr = Nothing Then
                If (size <= 0UI) Then
                    _objSize.Int32 = Marshal.SizeOf(obj)
                Else
                    _objSize.UInt32 = size
                End If

                _ptr = Marshal.AllocHGlobal(ObjSize)
                Marshal.StructureToPtr(obj, _ptr, False)
            Else
                _ptr = IntPtr.Zero
            End If
        End Sub

        Protected Overridable Sub Dispose(ByVal disposing As Boolean)
            If Not _disposed Then
                If disposing Then

                End If

                If _ptr <> IntPtr.Zero Then
                    Marshal.FreeHGlobal(Ptr)
                    _ptr = IntPtr.Zero
                End If

            End If

            _disposed = True
        End Sub

        Protected Overrides Sub Finalize()
            Dispose(False)
            MyBase.Finalize()
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub
    End Class

End Module

Namespace SetupAPI
    <ComVisible(False)>
    Public Module SetupAPI
        Private Const LINE_LEN As Int32 = 256
        Private Const MAX_LEN As Int32 = 260
        Friend ReadOnly CRLF As String = Environment.NewLine

        Private ReadOnly Is64 As Boolean = False
        Private ReadOnly IsAdmin As Boolean = False
        Private ReadOnly DefaultCharSize As Int32 = Marshal.SystemDefaultCharSize
        Private ReadOnly DefaultCharSizeU As UInt32 = CUInt(DefaultCharSize)
        Private ReadOnly NullChar() As Char = New Char() {CChar(vbNullChar)}

        Sub New()
            Is64 = Tools.ProcessIs64
            IsAdmin = Tools.UserHasAdmin
        End Sub


#Region "CM"
       
        <Flags()>
        Private Enum CR As UInteger
            SUCCESS = &H0UI
            [DEFAULT] = &H1UI
            OUT_OF_MEMORY = &H2UI
            INVALID_POINTER = &H3UI
            INVALID_FLAG = &H4UI
            INVALID_DEVNODE = &H5UI
            INVALID_DEVINST = INVALID_DEVNODE
            INVALID_RES_DES = &H6UI
            INVALID_LOG_CONF = &H7UI
            INVALID_ARBITRATOR = &H8UI
            INVALID_NODELIST = &H9UI
            DEVNODE_HAS_REQS = &HAUI
            DEVINST_HAS_REQS = DEVNODE_HAS_REQS
            INVALID_RESOURCEID = &HBUI
            DLVXD_NOT_FOUND = &HCUI
            NO_SUCH_DEVNODE = &HDUI
            NO_SUCH_DEVINST = NO_SUCH_DEVNODE
            NO_MORE_LOG_CONF = &HEUI
            NO_MORE_RES_DES = &HFUI
            ALREADY_SUCH_DEVNODE = &H10UI
            ALREADY_SUCH_DEVINST = ALREADY_SUCH_DEVNODE
            INVALID_RANGE_LIST = &H11UI
            INVALID_RANGE = &H12UI
            FAILURE = &H13UI
            NO_SUCH_LOGICAL_DEV = &H14UI
            CREATE_BLOCKED = &H15UI
            NOT_SYSTEM_VM = &H16UI
            REMOVE_VETOED = &H17UI
            APM_VETOED = &H18UI
            INVALID_LOAD_TYPE = &H19UI
            BUFFER_SMALL = &H1AUI
            NO_ARBITRATOR = &H1BUI
            NO_REGISTRY_HANDLE = &H1CUI
            REGISTRY_ERROR = &H1DUI
            INVALID_DEVICE_ID = &H1EUI
            INVALID_DATA = &H1FUI
            INVALID_API = &H20UI
            DEVLOADER_NOT_READY = &H21UI
            NEED_RESTART = &H22UI
            NO_MORE_HW_PROFILES = &H23UI
            DEVICE_NOT_THERE = &H24UI
            NO_SUCH_VALUE = &H25UI
            WRONG_TYPE = &H26UI
            INVALID_PRIORITY = &H27UI
            NOT_DISABLEABLE = &H28UI
            FREE_RESOURCES = &H29UI
            QUERY_VETOED = &H2AUI
            CANT_SHARE_IRQ = &H2BUI
            NO_DEPENDENT = &H2CUI
            SAME_RESOURCES = &H2DUI
            NO_SUCH_REGISTRY_KEY = &H2EUI
            INVALID_MACHINENAME = &H2FUI
            REMOTE_COMM_FAILURE = &H30UI
            MACHINE_UNAVAILABLE = &H31UI
            NO_CM_SERVICES = &H32UI
            ACCESS_DENIED = &H33UI
            CALL_NOT_IMPLEMENTED = &H34UI
            INVALID_PROPERTY = &H35UI
            DEVICE_INTERFACE_ACTIVE = &H36UI
            NO_SUCH_DEVICE_INTERFACE = &H37UI
            INVALID_REFERENCE_STRING = &H38UI
            INVALID_CONFLICT_LIST = &H39UI
            INVALID_INDEX = &H3AUI
            INVALID_STRUCTURE_SIZE = &H3B
        End Enum

        <Flags()>
        Private Enum DN As UInteger
            ROOT_ENUMERATED = &H1UI
            DRIVER_LOADED = &H2UI
            ENUM_LOADED = &H4UI
            STARTED = &H8UI
            MANUAL = &H10UI
            NEED_TO_ENUM = &H20UI
            NOT_FIRST_TIME = &H40UI
            HARDWARE_ENUM = &H80UI
            LIAR = &H100UI
            HAS_MARK = &H200UI
            HAS_PROBLEM = &H400UI
            FILTERED = &H800UI
            MOVED = &H1000UI
            DISABLEABLE = &H2000UI
            REMOVABLE = &H4000UI
            PRIVATE_PROBLEM = &H8000UI
            MF_PARENT = &H10000UI
            MF_CHILD = &H20000UI
            WILL_BE_REMOVED = &H40000UI
        End Enum

        <Flags()>
        Private Enum CM_PROB As UInteger
            NOT_CONFIGURED = &H1UI
            DEVLOADER_FAILED = &H2UI
            OUT_OF_MEMORY = &H3UI
            ENTRY_IS_WRONG_TYPE = &H4UI
            LACKED_ARBITRATOR = &H5UI
            BOOT_CONFIG_CONFLICT = &H6UI
            FAILED_FILTER = &H7UI
            DEVLOADER_NOT_FOUND = &H8UI
            INVALID_DATA = &H9UI
            FAILED_START = &HAUI
            LIAR = &HBUI
            NORMAL_CONFLICT = &HCUI
            NOT_VERIFIED = &HDUI
            NEED_RESTART = &HEUI
            REENUMERATION = &HFUI
            PARTIAL_LOG_CONF = &H10UI
            UNKNOWN_RESOURCE = &H11UI
            REINSTALL = &H12UI
            REGISTRY = &H13UI
            VXDLDR = &H14UI
            WILL_BE_REMOVED = &H15UI
            DISABLED = &H16UI
            DEVLOADER_NOT_READY = &H17UI
            DEVICE_NOT_THERE = &H18UI
            MOVED = &H19UI
            TOO_EARLY = &H1AUI
            NO_VALID_LOG_CONF = &H1BUI
            FAILED_INSTALL = &H1CUI
            HARDWARE_DISABLED = &H1DUI
            CANT_SHARE_IRQ = &H1EUI
            FAILED_ADD = &H1FUI
            DISABLED_SERVICE = &H20UI
            TRANSLATION_FAILED = &H21UI
            NO_SOFTCONFIG = &H22UI
            BIOS_TABLE = &H23UI
            IRQ_TRANSLATION_FAILED = &H24UI
            FAILED_DRIVER_ENTRY = &H25UI
            DRIVER_FAILED_PRIOR_UNLOAD = &H26UI
            DRIVER_FAILED_LOAD = &H27UI
            DRIVER_SERVICE_KEY_INVALID = &H28UI
            LEGACY_SERVICE_NO_DEVICES = &H29UI
            DUPLICATE_DEVICE = &H2AUI
            FAILED_POST_START = &H2BUI
            HALTED = &H2CUI
            PHANTOM = &H2DUI
            SYSTEM_SHUTDOWN = &H2EUI
            HELD_FOR_EJECT = &H2FUI
            DRIVER_BLOCKED = &H30UI
            REGISTRY_TOO_LARGE = &H31
        End Enum

        <DllImport("CfgMgr32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Private Function CM_Get_Parent(
           <[Out]()> ByRef pdnDevInst As UInt32,
           <[In]()> ByVal dnDevInst As UInt32,
           <[In]()> ByVal ulFlags As UInt32) As UInt32
        End Function

        <DllImport("CfgMgr32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Private Function CM_Get_Child(
           <[Out]()> ByRef pdnDevInst As UInt32,
           <[In]()> ByVal DevInst As UInt32,
           <[In]()> ByVal ulFlags As UInt32) As UInt32
        End Function

        <DllImport("CfgMgr32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Private Function CM_Get_Sibling(
           <[Out]()> ByRef pdnDevInst As UInt32,
           <[In]()> ByVal DevInst As UInt32,
           <[In]()> ByVal ulFlags As UInt32) As UInt32
        End Function

        <DllImport("CfgMgr32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Private Function CM_Get_Device_ID(
           <[In]()> ByVal dnDevInst As UInt32,
           <[Out](), MarshalAs(UnmanagedType.LPWStr)> ByVal Buffer As StringBuilder,
           <[In]()> ByRef BufferLen As UInt32,
           <[In]()> ByVal ulFlags As UInt32) As UInt32
        End Function

        <DllImport("CfgMgr32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Private Function CM_Get_DevNode_Status(
           <[Out]()> ByRef pulStatus As UInt32,
           <[Out]()> ByRef pulProblemNumber As UInt32,
           <[In]()> ByVal dnDevInst As UInt32,
           <[In]()> ByVal ulFlags As UInt32) As UInt32
        End Function

#End Region

#Region "Enums"
        <Flags()>
        Private Enum INSTALLFLAG As UInteger
            NULL = &H0UI
            FORCE = &H1UI
            [READONLY] = &H2UI
            NONINTERACTIVE = &H4UI
            BITS = &H7UI
        End Enum

        <Flags()>
        Private Enum CM_DEVCAP As UInteger
            LOCKSUPPORTED = &H1UI
            EJECTSUPPORTED = &H2UI
            REMOVABLE = &H4UI
            DOCKDEVICE = &H8UI
            UNIQUEID = &H10UI
            SILENTINSTALL = &H20UI
            RAWDEVICEOK = &H40UI
            SURPRISEREMOVALOK = &H80UI
            HARDWAREDISABLED = &H100UI
            NONDYNAMIC = &H200UI
        End Enum

        <Flags()>
        Private Enum DEVICE_INSTALL_STATE As UInteger
            Installed = &H0UI
            NeedsReinstall = &H1UI
            FailedInstall = &H2UI
            FinishInstall = &H3UI
        End Enum

        <Flags()>
        Private Enum CONFIGFLAGS As UInteger
            DISABLED = &H1UI
            REMOVED = &H2UI
            MANUAL_INSTALL = &H4UI
            IGNORE_BOOT_LC = &H8UI
            NET_BOOT = &H16UI
            REINSTALL = &H32UI
            FAILEDINSTALL = &H64UI
            CANTSTOPACHILD = &H128UI
            OKREMOVEROM = &H256UI
            NOREMOVEEXIT = &H512UI
        End Enum

        <Flags()>
        Private Enum SPDIT As UInteger
            SPDIT_NODRIVER = &H0UI
            CLASSDRIVER = &H1UI
            COMPATDRIVER = &H2UI
        End Enum

        <Flags()>
        Private Enum SetupUOInfFlags As UInteger
            NONE = &H0UI
            SUOI_FORCEDELETE = &H1UI
        End Enum

        <Flags()>
        Private Enum DIGCF As UInteger
            [DEFAULT] = &H1UI
            PRESENT = &H2UI
            ALLCLASSES = &H4UI
            PROFILE = &H8UI
            DEVICEINTERFACE = &H10UI
        End Enum

        <Flags()>
        Private Enum SPDRP As UInteger
            ''' <summary>DeviceDesc (R/W)</summary>
            DEVICEDESC = &H0UI

            ''' <summary>HardwareID (R/W)</summary>
            HARDWAREID = &H1UI

            ''' <summary>CompatibleIDs (R/W)</summary>
            COMPATIBLEIDS = &H2UI

            ''' <summary>unused</summary>
            UNUSED0 = &H3UI

            ''' <summary>Service (R/W)</summary>
            SERVICE = &H4UI

            ''' <summary>unused</summary>
            UNUSED1 = &H5UI

            ''' <summary>unused</summary>
            UNUSED2 = &H6UI

            ''' <summary>Class (R--tied to ClassGUID)</summary>
            [CLASS] = &H7UI

            ''' <summary>ClassGUID (R/W)</summary>
            CLASSGUID = &H8UI

            ''' <summary>Driver (R/W)</summary>
            DRIVER = &H9UI

            ''' <summary>ConfigFlags (R/W)</summary>
            CONFIGFLAGS = &HAUI

            ''' <summary>Mfg (R/W)</summary>
            MFG = &HBUI

            ''' <summary>FriendlyName (R/W)</summary>
            FRIENDLYNAME = &HCUI

            ''' <summary>LocationInformation (R/W)</summary>
            LOCATION_INFORMATION = &HDUI

            ''' <summary>PhysicalDeviceObjectName (R)</summary>
            PHYSICAL_DEVICE_OBJECT_NAME = &HEUI

            ''' <summary>Capabilities (R)</summary>
            CAPABILITIES = &HFUI

            ''' <summary>UiNumber (R)</summary>
            UI_NUMBER = &H10UI

            ''' <summary>UpperFilters (R/W)</summary>
            UPPERFILTERS = &H11UI

            ''' <summary>LowerFilters (R/W)</summary>
            LOWERFILTERS = &H12UI

            ''' <summary>BusTypeGUID (R)</summary>
            BUSTYPEGUID = &H13UI

            ''' <summary>LegacyBusType (R)</summary>
            LEGACYBUSTYPE = &H14UI

            ''' <summary>BusNumber (R)</summary>
            BUSNUMBER = &H15UI

            ''' <summary>Enumerator Name (R)</summary>
            ENUMERATOR_NAME = &H16UI

            ''' <summary>Security (R/W binary form)</summary>
            SECURITY = &H17UI

            ''' <summary>Security (W SDS form)</summary>
            SECURITY_SDS = &H18UI

            ''' <summary>Device Type (R/W)</summary>
            DEVTYPE = &H19UI

            ''' <summary>Device is exclusive-access (R/W)</summary>
            EXCLUSIVE = &H1AUI

            ''' <summary>Device Characteristics (R/W)</summary>
            CHARACTERISTICS = &H1BUI

            ''' <summary>Device Address (R)</summary>
            ADDRESS = &H1CUI

            ''' <summary>UiNumberDescFormat (R/W)</summary>
            UI_NUMBER_DESC_FORMAT = &H1DUI

            ''' <summary>Device Power Data (R)</summary>
            DEVICE_POWER_DATA = &H1EUI

            ''' <summary>Removal Policy (R)</summary>
            REMOVAL_POLICY = &H1FUI

            ''' <summary>Hardware Removal Policy (R)</summary>
            REMOVAL_POLICY_HW_DEFAULT = &H20UI

            ''' <summary>Removal Policy Override (RW)</summary>
            REMOVAL_POLICY_OVERRIDE = &H21UI

            ''' <summary>Device Install State (R)</summary>
            INSTALL_STATE = &H22UI

            ''' <summary>Device Location Paths (R)</summary>
            LOCATION_PATHS = &H23UI
        End Enum

        <Flags()>
        Private Enum DIF As UInteger
            SELECTDEVICE = &H1UI
            INSTALLDEVICE = &H2UI
            ASSIGNRESOURCES = &H3UI
            PROPERTIES = &H4UI
            REMOVE = &H5UI
            FIRSTTIMESETUP = &H6UI
            FOUNDDEVICE = &H7UI
            SELECTCLASSDRIVERS = &H8UI
            VALIDATECLASSDRIVERS = &H9UI
            INSTALLCLASSDRIVERS = &HAUI
            CALCDISKSPACE = &HBUI
            DESTROYPRIVATEDATA = &HCUI
            VALIDATEDRIVER = &HDUI
            DETECT = &HFUI
            INSTALLWIZARD = &H10UI
            DESTROYWIZARDDATA = &H11UI
            PROPERTYCHANGE = &H12UI
            ENABLECLASS = &H13UI
            DETECTVERIFY = &H14UI
            INSTALLDEVICEFILES = &H15UI
            UNREMOVE = &H16UI
            SELECTBESTCOMPATDRV = &H17UI
            ALLOW_INSTALL = &H18UI
            REGISTERDEVICE = &H19UI
            NEWDEVICEWIZARD_PRESELECT = &H1AUI
            NEWDEVICEWIZARD_SELECT = &H1BUI
            NEWDEVICEWIZARD_PREANALYZE = &H1CUI
            NEWDEVICEWIZARD_POSTANALYZE = &H1DUI
            NEWDEVICEWIZARD_FINISHINSTALL = &H1EUI
            UNUSED1 = &H1FUI
            INSTALLINTERFACES = &H20UI
            DETECTCANCEL = &H21UI
            REGISTER_COINSTALLERS = &H22UI
            ADDPROPERTYPAGE_ADVANCED = &H23UI
            ADDPROPERTYPAGE_BASIC = &H24UI
            RESERVED1 = &H25UI
            TROUBLESHOOTER = &H26UI
            POWERMESSAGEWAKE = &H27UI
            ADDREMOTEPROPERTYPAGE_ADVANCED = &H28UI
            UPDATEDRIVER_UI = &H29UI
            FINISHINSTALL_ACTION = &H2AUI
            RESERVED2 = &H30UI
        End Enum

        <Flags()>
        Private Enum DICS As UInteger
            ENABLE = &H1UI
            DISABLE = &H2UI
            PROPCHANGE = &H3UI
            START = &H4UI
            [STOP] = &H5UI
        End Enum

        <Flags()>
        Private Enum DI As UInteger
            REMOVEDEVICE_GLOBAL = &H1UI
            REMOVEDEVICE_CONFIGSPECIFIC = &H2UI
            UNREMOVEDEVICE_CONFIGSPECIFIC = &H2UI
            SHOWOEM = &H1UI
            SHOWCOMPAT = &H2UI
            SHOWCLASS = &H4UI
            SHOWALL = &H7UI
            NOVCP = &H8UI
            DIDCOMPAT = &H10UI
            DIDCLASS = &H20UI
            AUTOASSIGNRES = &H40UI
            NEEDRESTART = &H80UI
            NEEDREBOOT = &H100UI
            NOBROWSE = &H200UI
            MULTMFGS = &H400UI
            DISABLED = &H800UI
            GENERALPAGE_ADDED = &H1000UI
            RESOURCEPAGE_ADDED = &H2000UI
            PROPERTIES_CHANGE = &H4000UI
            INF_IS_SORTED = &H8000UI
            ENUMSINGLEINF = &H10000UI
            DONOTCALLCONFIGMG = &H20000UI
            INSTALLDISABLED = &H40000UI
            COMPAT_FROM_CLASS = &H80000UI
            CLASSINSTALLPARAMS = &H100000UI
            NODEFAULTACTION = &H200000UI
            QUIETINSTALL = &H800000UI
            NOFILECOPY = &H1000000UI
            FORCECOPY = &H2000000UI
            DRIVERPAGE_ADDED = &H4000000UI
            USECI_SELECTSTRINGS = &H8000000UI
            OVERRIDE_INFFLAGS = &H10000000UI
            PROPS_NOCHANGEUSAGE = &H20000000UI
            NOSELECTICONS = &H40000000UI
            NOWRITE_IDS = &H80000000UI
        End Enum

        <Flags()>
        Private Enum DI_FLAGSEX As UInteger
            RESERVED2 = &H1UI
            RESERVED3 = &H2UI
            CI_FAILED = &H4UI
            FINISHINSTALL_ACTION = &H8UI
            DIDINFOLIST = &H10UI
            DIDCOMPATINFO = &H20UI
            FILTERCLASSES = &H40UI
            SETFAILEDINSTALL = &H80UI
            DEVICECHANGE = &H100UI
            ALWAYSWRITEIDS = &H200UI
            PROPCHANGE_PENDING = &H400UI
            ALLOWEXCLUDEDDRVS = &H800UI
            NOUIONQUERYREMOVE = &H1000UI
            USECLASSFORCOMPAT = &H2000UI
            RESERVED4 = &H4000UI
            NO_DRVREG_MODIFY = &H8000UI
            IN_SYSTEM_SETUP = &H10000UI
            INET_DRIVER = &H20000UI
            APPENDDRIVERLIST = &H40000UI
            PREINSTALLBACKUP = &H80000UI
            BACKUPONREPLACE = &H100000UI
            DRIVERLIST_FROM_URL = &H200000UI
            RESERVED1 = &H400000UI
            EXCLUDE_OLD_INET_DRIVERS = &H800000UI
            POWERPAGE_ADDED = &H1000000UI
            FILTERSIMILARDRIVERS = &H2000000UI
            INSTALLEDDRIVER = &H4000000UI
            NO_CLASSLIST_NODE_MERGE = &H8000000UI
            ALTPLATFORM_DRVSEARCH = &H10000000UI
            RESTART_DEVICE_ONLY = &H20000000UI
            RECURSIVESEARCH = &H40000000UI
            SEARCH_PUBLISHED_INFS = &H80000000UI
        End Enum

        <Flags()>
        Private Enum DICS_FLAG As UInteger
            [GLOBAL] = &H1UI
            CONFIGSPECIFIC = &H2UI
            CONFIGGENERAL = &H4UI
        End Enum

#End Region

#Region "Structures_SetupAPI_X64"

        <StructLayout(LayoutKind.Sequential, Pack:=8, CharSet:=CharSet.Unicode)>
        Private Structure SP_DEVINFO_DATA_X64
            Public cbSize As UInt32
            Public ClassGuid As Guid
            Public DevInst As UInt32
            Public Reserved As IntPtr
        End Structure

        <StructLayout(LayoutKind.Sequential, Pack:=8, CharSet:=CharSet.Unicode)>
        Private Structure SP_CLASSINSTALL_HEADER_X64
            Public cbSize As UInt32
            Public InstallFunction As UInt32
        End Structure

        <StructLayout(LayoutKind.Sequential, Pack:=8, CharSet:=CharSet.Unicode)>
        Private Structure SP_PROPCHANGE_PARAMS_X64
            Public ClassInstallHeader As SP_CLASSINSTALL_HEADER_X64
            Public StateChange As UInt32
            Public Scope As UInt32
            Public HwProfile As UInt32
        End Structure

        <StructLayout(LayoutKind.Sequential, Pack:=8, CharSet:=CharSet.Unicode)>
        Private Structure SP_DRVINFO_DATA_X64
            Public cbSize As UInt32
            Public DriverType As UInt32
            Public Reserved As IntPtr
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=LINE_LEN)>
            Public Description As String
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=LINE_LEN)>
            Public MfgName As String
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=LINE_LEN)>
            Public ProviderName As String
            Public DriverDate As System.Runtime.InteropServices.ComTypes.FILETIME
            Public DriverVersion As UInt64
        End Structure

        <StructLayout(LayoutKind.Sequential, Pack:=8, CharSet:=CharSet.Unicode)>
        Private Structure SP_DRVINFO_DETAIL_DATA_X64
            Public cbSize As UInt32
            Public InfDate As System.Runtime.InteropServices.ComTypes.FILETIME
            Public CompatIDsOffset As UInt32
            Public CompatIDsLength As UInt32
            Public Reserved As IntPtr
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=LINE_LEN)>
            Public SectionName As String
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=MAX_LEN)>
            Public InfFileName As String
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=LINE_LEN)>
            Public DrvDescription As String
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=1)>
            Public HardwareID As String
        End Structure

        <StructLayout(LayoutKind.Sequential, Pack:=8, CharSet:=CharSet.Unicode)>
        Private Structure SP_DEVINSTALL_PARAMS_X64
            Public cbSize As UInt32
            Public Flags As UInt32
            Public FlagsEx As UInt32
            Public hwndParent As IntPtr
            Public InstallMsgHandler As IntPtr
            Public InstallMsgHandlerContext As IntPtr
            Public FileQueue As IntPtr
            Public ClassInstallReserved As IntPtr
            Public Reserved As UInt32
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=MAX_LEN)>
            Public DriverPath As String
        End Structure

#End Region

#Region "Structures_SetupAPI_X86"
        <StructLayout(LayoutKind.Sequential, Pack:=1, CharSet:=CharSet.Unicode)>
        Private Structure SP_DEVINFO_DATA_X86
            Public cbSize As UInt32
            Public ClassGuid As Guid
            Public DevInst As UInt32
            Public Reserved As IntPtr
        End Structure

        <StructLayout(LayoutKind.Sequential, Pack:=1, CharSet:=CharSet.Unicode)>
        Private Structure SP_CLASSINSTALL_HEADER_X86
            Public cbSize As UInt32
            Public InstallFunction As UInt32
        End Structure

        <StructLayout(LayoutKind.Sequential, Pack:=1, CharSet:=CharSet.Unicode)>
        Private Structure SP_PROPCHANGE_PARAMS_X86
            Public ClassInstallHeader As SP_CLASSINSTALL_HEADER_X86
            Public StateChange As UInt32
            Public Scope As UInt32
            Public HwProfile As UInt32
        End Structure

        <StructLayout(LayoutKind.Sequential, Pack:=1, CharSet:=CharSet.Unicode)>
        Private Structure SP_DRVINFO_DATA_X86
            Public cbSize As UInt32
            Public DriverType As UInt32
            Public Reserved As IntPtr
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=LINE_LEN)>
            Public Description As String
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=LINE_LEN)>
            Public MfgName As String
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=LINE_LEN)>
            Public ProviderName As String
            Public DriverDate As System.Runtime.InteropServices.ComTypes.FILETIME
            Public DriverVersion As UInt64
        End Structure

        <StructLayout(LayoutKind.Sequential, Pack:=1, CharSet:=CharSet.Unicode)>
        Private Structure SP_DRVINFO_DETAIL_DATA_X86
            Public cbSize As UInt32
            Public InfDate As System.Runtime.InteropServices.ComTypes.FILETIME
            Public CompatIDsOffset As UInt32
            Public CompatIDsLength As UInt32
            Public Reserved As IntPtr
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=LINE_LEN)>
            Public SectionName As String
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=MAX_LEN)>
            Public InfFileName As String
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=LINE_LEN)>
            Public DrvDescription As String
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=1)>
            Public HardwareID As String
        End Structure

        <StructLayout(LayoutKind.Sequential, Pack:=1, CharSet:=CharSet.Unicode)>
        Private Structure SP_DEVINSTALL_PARAMS_X86
            Public cbSize As UInt32
            Public Flags As UInt32
            Public FlagsEx As UInt32
            Public hwndParent As IntPtr
            Public InstallMsgHandler As IntPtr
            Public InstallMsgHandlerContext As IntPtr
            Public FileQueue As IntPtr
            Public ClassInstallReserved As IntPtr
            Public Reserved As UInt32
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=MAX_LEN)>
            Public DriverPath As String
        End Structure


#End Region

#Region "P/Invoke"

        <DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Private Function SetupDiClassGuidsFromName(
            <[In]()> ByVal ClassName As String,
            <[Out]()> ByRef ClassGuidList As Guid,
            <[In]()> ByRef ClassGuidListSize As UInt32,
            <[Out]()> ByRef RequiredSize As UInt32) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function

        <DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        <ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)>
        Private Function SetupDiDestroyDeviceInfoList(
            <[In]()> ByVal DeviceInfoSet As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function

        <DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Private Function SetupDiGetClassDevs(
            <[In](), [Optional]()> ByRef ClassGuid As Guid,
            <[In](), [Optional](), MarshalAs(UnmanagedType.LPWStr)> ByVal Enumerator As String,
            <[In](), [Optional]()> ByVal hwndParent As IntPtr,
            <[In]()> ByVal Flags As UInt32) As SafeDeviceHandle
        End Function

        <DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Private Function SetupDiEnumDeviceInfo(
           <[In]()> ByVal DeviceInfoSet As SafeDeviceHandle,
           <[In]()> ByVal MemberIndex As UInt32,
           <[Out]()> ByVal DeviceInfoData As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function

        <DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Private Function SetupDiEnumDriverInfo(
            <[In]()> ByVal DeviceInfoSet As SafeDeviceHandle,
            <[In](), [Optional]()> ByVal deviceInfoData As IntPtr,
            <[In]()> ByVal DriverType As UInt32,
            <[In]()> ByVal MemberIndex As UInt32,
            <[Out]()> ByVal DriverInfoData As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function

        <DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Private Function SetupDiGetDeviceRegistryProperty(
            <[In]()> ByVal DeviceInfoSet As SafeDeviceHandle,
            <[In]()> ByVal DeviceInfoData As IntPtr,
            <[In]()> ByVal [Property] As UInt32,
            <[Out](), [Optional]()> ByRef PropertyRegDataType As RegistryValueKind,
            <[In](), Out(), [Optional]()> ByVal PropertyBuffer() As Byte,
            <[In]()> ByVal PropertyBufferSize As UInt32,
            <[Out](), [Optional]()> ByRef RequiredSize As UInt32) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function

        <DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Private Function SetupDiBuildDriverInfoList(
            <[In]()> ByVal DeviceInfoSet As SafeDeviceHandle,
            <[In](), [Out]()> ByVal DeviceInfoData As IntPtr,
            <[In]()> ByVal DriverType As UInt32) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function

        <DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Private Function SetupDiGetDriverInfoDetail(
            <[In]()> ByVal DeviceInfoSet As SafeDeviceHandle,
            <[In](), [Optional]()> ByVal DeviceInfoData As IntPtr,
            <[In]()> ByVal DriverInfoData As IntPtr,
            <[In](), [Out]()> ByVal DriverInfoDetailData As IntPtr,
            <[In]()> ByVal DriverInfoDetailDataSize As UInt32,
            <[Out](), [Optional]()> ByRef RequiredSize As UInt32) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function

        <DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Private Function SetupDiSetClassInstallParams(
            <[In]()> ByVal DeviceInfoSet As SafeDeviceHandle,
            <[In](), [Optional]()> ByVal DeviceInfoData As IntPtr,
            <[In](), [Optional]()> ByVal classInstallParams As IntPtr,
            <[In]()> ByVal ClassInstallParamsSize As UInt32) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function

        <DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Private Function SetupDiGetClassInstallParams(
            <[In]()> ByVal DeviceInfoSet As SafeDeviceHandle,
            <[In](), [Optional]()> ByVal DeviceInfoData As IntPtr,
            <[Out](), [Optional]()> ByVal ClassInstallParams As IntPtr,
            <[In]()> ByVal ClassInstallParamsSize As UInt32,
            <[Out](), [Optional]()> ByRef RequiredSize As UInt32) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function

        <DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Private Function SetupDiGetDeviceInstallParams(
            <[In]()> ByVal DeviceInfoSet As SafeDeviceHandle,
            <[In](), [Optional]()> ByVal DeviceInfoData As IntPtr,
            <[Out]()> ByVal DeviceInstallParams As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function

        <DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Private Function SetupDiSetDeviceInstallParams(
            <[In]()> ByVal DeviceInfoSet As SafeDeviceHandle,
            <[In](), [Optional]()> ByVal DeviceInfoData As IntPtr,
            <[In]()> ByVal DeviceInstallParams As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function

        <DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Private Function SetupDiChangeState(
            <[In]()> ByVal DeviceInfoSet As SafeDeviceHandle,
            <[In](), [Out]()> ByVal DeviceInfoData As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function

        <DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Private Function SetupDiCallClassInstaller(
            <[In]()> ByVal InstallFunction As UInt32,
            <[In]()> ByVal DeviceInfoSet As SafeDeviceHandle,
            <[In](), [Optional]()> ByVal DeviceInfoData As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function

        <DllImport("setupapi.dll", CharSet:=CharSet.Ansi, SetLastError:=True)>
        Private Function SetupUninstallOEMInf(
            <[In](), MarshalAs(UnmanagedType.LPStr)> ByVal InfFileName As String,
            <[In]()> ByVal Flags As UInt32,
            <[In]()> ByVal Reserved As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function

        <DllImport("newdev.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Private Function UpdateDriverForPlugAndPlayDevices(
            <[In](), [Optional]()> ByVal hwndParent As IntPtr,
            <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal HardwareId As String,
            <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal FullInfPath As String,
            <[In]()> ByVal InstallFlags As UInt32,
            <[Out](), [Optional](), MarshalAs(UnmanagedType.Bool)> ByRef bRebootRequired As Boolean) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function

#End Region

#Region "Functions"

        Public Function TEST_GetDevices(ByVal filter As String, ByVal text As String) As List(Of Device)
            Dim Devices As List(Of Device) = New List(Of Device)(500)

            Try
                Dim nullGuid As Guid = Guid.Empty
                Dim hardwareIds(0) As String
                Dim lowerfilters(0) As String
                Dim friendlyname As String
                Dim desc As String = Nothing
                Dim className As String = Nothing
                Dim match As Boolean = False

                Using infoSet As SafeDeviceHandle = SetupDiGetClassDevs(nullGuid, Nothing, IntPtr.Zero, CUInt(DIGCF.ALLCLASSES))
                    CheckWin32Error(Not infoSet.IsInvalid)

                    Dim ptrDevInfo As StructPtr = Nothing
                    Try
                        If Is64 Then
                            ptrDevInfo = New StructPtr(New SP_DEVINFO_DATA_X64() With {.cbSize = GetUInt32(Marshal.SizeOf(GetType(SP_DEVINFO_DATA_X64)))})
                        Else
                            ptrDevInfo = New StructPtr(New SP_DEVINFO_DATA_X86() With {.cbSize = GetUInt32(Marshal.SizeOf(GetType(SP_DEVINFO_DATA_X86)))})
                        End If

                        Dim i As UInt32 = 0UI

                        While True
                            If Not SetupDiEnumDeviceInfo(infoSet, i, ptrDevInfo.Ptr) Then
                                If GetLastWin32ErrorU() = Errors.NO_MORE_ITEMS Then
                                    Exit While
                                Else
                                    CheckWin32Error(False)
                                End If
                            End If

                            i += 1UI
                            match = False
                            desc = Nothing
                            className = Nothing
                            hardwareIds = Nothing
                            lowerfilters = Nothing
                            friendlyname = Nothing

                            If Not String.IsNullOrEmpty(text) Then
                                Select Case filter
                                    Case "Device_ClassName"
                                        className = GetStringProperty(infoSet, ptrDevInfo.Ptr, SPDRP.CLASS)

                                        If Not String.IsNullOrEmpty(className) AndAlso className.IndexOf(text, StringComparison.OrdinalIgnoreCase) <> -1 Then
                                            match = True
                                        End If
                                        Exit Select
                                    Case "Device_Description"
                                        desc = GetStringProperty(infoSet, ptrDevInfo.Ptr, SPDRP.DEVICEDESC)

                                        If Not String.IsNullOrEmpty(desc) AndAlso desc.IndexOf(text, StringComparison.OrdinalIgnoreCase) <> -1 Then
                                            match = True
                                        End If
                                        Exit Select
                                    Case "Device_HardwareID"
                                        hardwareIds = GetMultiStringProperty(infoSet, ptrDevInfo.Ptr, SPDRP.HARDWAREID)
                                        If hardwareIds IsNot Nothing Then
                                            For Each hdID As String In hardwareIds
                                                If hdID.IndexOf(text, StringComparison.OrdinalIgnoreCase) <> -1 Then
                                                    match = True
                                                    Exit Select
                                                End If
                                            Next
                                        End If
                                    Case "Device_LowerFilters"
                                        lowerfilters = GetMultiStringProperty(infoSet, ptrDevInfo.Ptr, SPDRP.LOWERFILTERS)
                                        If lowerfilters IsNot Nothing Then
                                            For Each LFs As String In lowerfilters
                                                If LFs.IndexOf(text, StringComparison.OrdinalIgnoreCase) <> -1 Then
                                                    match = True
                                                    Exit Select
                                                End If
                                            Next
                                        End If
                                    Case "Device_FriendlyName"
                                        friendlyname = GetStringProperty(infoSet, ptrDevInfo.Ptr, SPDRP.FRIENDLYNAME)
                                        If friendlyname IsNot Nothing Then
                                            For Each FRn As String In friendlyname
                                                If FRn.IndexOf(text, StringComparison.OrdinalIgnoreCase) <> -1 Then
                                                    match = True
                                                    Exit Select
                                                End If
                                            Next
                                        End If

                                        Exit Select
                                End Select
                            Else
                                match = True
                            End If

                            If match Then
                                If (hardwareIds Is Nothing) Then hardwareIds = GetMultiStringProperty(infoSet, ptrDevInfo.Ptr, SPDRP.HARDWAREID)
                                If (lowerfilters Is Nothing) Then lowerfilters = GetMultiStringProperty(infoSet, ptrDevInfo.Ptr, SPDRP.LOWERFILTERS)
                                If (friendlyname Is Nothing) Then friendlyname = GetStringProperty(infoSet, ptrDevInfo.Ptr, SPDRP.FRIENDLYNAME)
                                If (desc Is Nothing) Then desc = GetStringProperty(infoSet, ptrDevInfo.Ptr, SPDRP.DEVICEDESC)
                                If (className Is Nothing) Then className = GetStringProperty(infoSet, ptrDevInfo.Ptr, SPDRP.CLASS)

                                Dim d As Device = New Device() With
                                {
                                 .Description = desc,
                                 .ClassName = className,
                                 .HardwareIDs = hardwareIds,
                                 .LowerFilters = lowerfilters,
                                 .FriendlyName = friendlyname
                                }

                                GetDeviceDetails(infoSet, ptrDevInfo.Ptr, d)
                                GetDriverDetails(infoSet, ptrDevInfo.Ptr, d)
                                GetDeviceCMDetails(ptrDevInfo.Ptr, d)

                                Devices.Add(d)
                            End If

                        End While
                    Finally
                        If ptrDevInfo IsNot Nothing Then

                            ptrDevInfo.Dispose()
                        End If
                    End Try
                End Using

                Return Devices
            Catch ex As Exception
                ShowException(ex)
            End Try

            Return Nothing
        End Function

        Public Sub TEST_RemoveDevice(ByVal hardwareIDFilter As String)
            If Not IsAdmin Then
                Throw New SecurityException("Admin priviliges required!")
            End If

            If String.IsNullOrEmpty(hardwareIDFilter) Then
                MessageBox.Show("Empty Hardware ID Filter!")
                Return
            End If

            Try
                Dim nullGuid As Guid = Guid.Empty
                Dim hardwareIds(0) As String
                Dim found As Boolean = False
                Dim device As Device = Nothing

                Using infoSet As SafeDeviceHandle = SetupDiGetClassDevs(nullGuid, Nothing, IntPtr.Zero, CUInt(DIGCF.ALLCLASSES))
                    CheckWin32Error(Not infoSet.IsInvalid)

                    Dim ptrDevInfo As StructPtr = Nothing
                    Try
                        If Is64 Then
                            ptrDevInfo = New StructPtr(New SP_DEVINFO_DATA_X64() With {.cbSize = CUInt(Marshal.SizeOf(GetType(SP_DEVINFO_DATA_X64)))})
                        Else
                            ptrDevInfo = New StructPtr(New SP_DEVINFO_DATA_X86() With {.cbSize = CUInt(Marshal.SizeOf(GetType(SP_DEVINFO_DATA_X86)))})
                        End If

                        Dim i As UInt32 = 0UI

                        While True
                            If Not SetupDiEnumDeviceInfo(infoSet, i, ptrDevInfo.Ptr) Then
                                If GetLastWin32ErrorU() = Errors.NO_MORE_ITEMS Then
                                    Exit While
                                Else
                                    CheckWin32Error(False)
                                End If
                            End If

                            i += 1UI

                            hardwareIds = GetMultiStringProperty(infoSet, ptrDevInfo.Ptr, SPDRP.HARDWAREID)

                            If hardwareIds Is Nothing Then
                                Continue While
                            End If

                            For Each hdID As String In hardwareIds
                                If hdID.IndexOf(hardwareIDFilter, StringComparison.OrdinalIgnoreCase) <> -1 Then
                                    device = New Device() With
                                    {
                                        .Description = GetStringProperty(infoSet, ptrDevInfo.Ptr, SPDRP.DEVICEDESC),
                                        .ClassGuid = GetStringProperty(infoSet, ptrDevInfo.Ptr, SPDRP.CLASSGUID),
                                        .CompatibleIDs = GetMultiStringProperty(infoSet, ptrDevInfo.Ptr, SPDRP.COMPATIBLEIDS),
                                        .HardwareIDs = hardwareIds
                                    }

                                    GetDriverDetails(infoSet, ptrDevInfo.Ptr, device)

                                    Dim msgResult As MessageBoxResult = MessageBox.Show(
                                        String.Format("Are you sure you want to remove device:{2}{2}{3}{0}\r\n\r\nHardware IDs{2}{2}{3}{1}", device.Description, String.Join(CRLF & vbTab, device.HardwareIDs), CRLF, vbTab),
                                        "Warning!",
                                        MessageBoxButton.YesNoCancel,
                                        MessageBoxImage.Warning)

                                    If msgResult = MessageBoxResult.Yes Then
                                        found = True
                                        Exit For
                                    ElseIf (msgResult = MessageBoxResult.No) Then
                                        Exit For
                                    Else
                                        Return
                                    End If
                                End If
                            Next

                            If found Then
                                Exit While
                            End If
                        End While

                        If Not found OrElse device Is Nothing Then
                            Return
                        End If

                        If MessageBox.Show(String.Format("CONFIRM!!!{2}{2}Are you sure you want to remove device:{2}{2}{3}{0}\r\n\r\nHardware IDs{2}{2}{3}{1}", device.Description, String.Join(CRLF & vbTab, device.HardwareIDs), CRLF, vbTab),
                               "Warning!",
                               MessageBoxButton.YesNo,
                               MessageBoxImage.Warning) <> MessageBoxResult.Yes Then
                            Return
                        End If

                        If device.OemInfs IsNot Nothing Then
                            For Each inf As String In device.OemInfs
                                If Not File.Exists(inf) Then
                                    Continue For
                                End If

                                Dim infName As String = Path.GetFileName(inf)

                                If CheckIsOemInf(infName) Then
                                    Dim attrs As FileAttributes = File.GetAttributes(inf)

                                    If (attrs And FileAttributes.ReadOnly) = FileAttributes.ReadOnly Then
                                        File.SetAttributes(inf, attrs And Not FileAttributes.ReadOnly)
                                    End If

                                    CheckWin32Error(SetupUninstallOEMInf(infName, CUInt(SetupUOInfFlags.SUOI_FORCEDELETE), IntPtr.Zero))
                                Else
                                    MessageBox.Show(String.Format("Inf isn't Oem's, skipping Inf uninstall for '{0}' !", inf), "Device removal", MessageBoxButton.OK, MessageBoxImage.Information)
                                End If
                            Next
                        End If

                        CheckWin32Error(SetupDiCallClassInstaller(CUInt(DIF.REMOVE), infoSet, ptrDevInfo.Ptr))

                        If RebootRequired(infoSet, ptrDevInfo.Ptr) Then
                            If MessageBox.Show(String.Format("Reboot required!{0}Reboot now?", CRLF), "Device removed!", MessageBoxButton.YesNo, MessageBoxImage.Information) = MessageBoxResult.Yes Then
                                Reboot()
                            End If
                        Else
                            MessageBox.Show(String.Format("Reboot not required!{0}NOTE: Windows XP doesn't 'set' reboot flag even if reboot required", "Device removed!", CRLF))
                        End If
                    Finally
                        If ptrDevInfo IsNot Nothing Then
                            ptrDevInfo.Dispose()
                        End If
                    End Try
                End Using
            Catch ex As Exception
                ShowException(ex)
            End Try
        End Sub

        Public Sub TEST_EnableDevice(ByVal hardwareIDFilter As String, ByVal enable As Boolean)
            If Not IsAdmin Then
                Throw New SecurityException("Admin priviliges required!")
            End If

            If String.IsNullOrEmpty(hardwareIDFilter) Then
                MessageBox.Show("Empty Hardware ID Filter!")
                Return
            End If

            Try
                Dim nullGuid As Guid = Guid.Empty
                Dim hardwareIds(0) As String
                Dim found As Boolean = False
                Dim device As Device = Nothing

                Using infoSet As SafeDeviceHandle = SetupDiGetClassDevs(nullGuid, Nothing, IntPtr.Zero, CUInt(DIGCF.ALLCLASSES))
                    CheckWin32Error(Not infoSet.IsInvalid)

                    Dim ptrDevInfo As StructPtr = Nothing
                    Try
                        If Is64 Then
                            ptrDevInfo = New StructPtr(New SP_DEVINFO_DATA_X64() With {.cbSize = CUInt(Marshal.SizeOf(GetType(SP_DEVINFO_DATA_X64)))})
                        Else
                            ptrDevInfo = New StructPtr(New SP_DEVINFO_DATA_X86() With {.cbSize = CUInt(Marshal.SizeOf(GetType(SP_DEVINFO_DATA_X86)))})
                        End If

                        Dim i As UInt32 = 0UI

                        While True
                            If Not SetupDiEnumDeviceInfo(infoSet, i, ptrDevInfo.Ptr) Then
                                If GetLastWin32ErrorU() = Errors.NO_MORE_ITEMS Then
                                    Exit While
                                Else
                                    CheckWin32Error(False)
                                End If
                            End If

                            i += 1UI
                            hardwareIds = GetMultiStringProperty(infoSet, ptrDevInfo.Ptr, SPDRP.HARDWAREID)

                            If hardwareIds Is Nothing Then
                                Continue While
                            End If

                            For Each hdID As String In hardwareIds
                                If hdID.IndexOf(hardwareIDFilter, StringComparison.OrdinalIgnoreCase) <> -1 Then
                                    device = New Device() With
                                    {
                                        .Description = GetStringProperty(infoSet, ptrDevInfo.Ptr, SPDRP.DEVICEDESC),
                                        .ClassGuid = GetStringProperty(infoSet, ptrDevInfo.Ptr, SPDRP.CLASSGUID),
                                        .CompatibleIDs = GetMultiStringProperty(infoSet, ptrDevInfo.Ptr, SPDRP.COMPATIBLEIDS),
                                        .HardwareIDs = hardwareIds
                                    }

                                    GetDeviceDetails(infoSet, ptrDevInfo.Ptr, device)

                                    Dim msgResult As MessageBoxResult = MessageBox.Show(
                                       String.Format("Are you sure you want to {0} device:{3}{3}{4}{1}\r\n\r\nHardware IDs{3}{3}{4}{2}", If(enable, "enable", "disable"), device.Description, String.Join(CRLF & vbTab, device.HardwareIDs), CRLF, vbTab),
                                        "Warning!",
                                        MessageBoxButton.YesNoCancel,
                                        MessageBoxImage.Warning)

                                    If msgResult = MessageBoxResult.Yes Then
                                        found = True
                                        Exit For
                                    ElseIf (msgResult = MessageBoxResult.No) Then
                                        Exit For
                                    Else
                                        Return
                                    End If
                                End If
                            Next

                            If found Then
                                Exit While
                            End If
                        End While

                        If Not found OrElse device Is Nothing Then
                            Return
                        End If

                        If MessageBox.Show(String.Format("CONFIRM!!!{3}Are you sure you want to {0} device:{3}{3}{4}{1}\r\n\r\nHardware IDs{3}{3}{4}{2}", If(enable, "enable", "disable"), device.Description, String.Join(CRLF & vbTab, device.HardwareIDs), CRLF, vbTab),
                            "Warning!",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning) <> MessageBoxResult.Yes Then

                            Return
                        End If

                        Dim cfgFlags As UInt32 = GetUInt32Property(infoSet, ptrDevInfo.Ptr, SPDRP.CONFIGFLAGS)

                        If Not enable AndAlso ((cfgFlags And CUInt(CONFIGFLAGS.DISABLED)) = CUInt(CONFIGFLAGS.DISABLED)) Then
                            MessageBox.Show("Device is already disabled!", "Device disable")
                            Return
                        ElseIf enable AndAlso ((cfgFlags And CUInt(CONFIGFLAGS.DISABLED)) = 0UI) Then
                            MessageBox.Show("Device is already enabled!", "Device enable")
                            Return
                        End If

                        Dim ptrSetParams As StructPtr = Nothing

                        Try
                            If (Is64) Then
                                ptrSetParams = New StructPtr(New SP_PROPCHANGE_PARAMS_X64() With
                                {
                                    .StateChange = CUInt(If(enable, DICS.ENABLE, DICS.DISABLE)),
                                    .Scope = CUInt(DICS_FLAG.GLOBAL),
                                    .HwProfile = 0UI,
                                    .ClassInstallHeader = New SP_CLASSINSTALL_HEADER_X64() With
                                    {
                                        .cbSize = CUInt(Marshal.SizeOf(GetType(SP_CLASSINSTALL_HEADER_X64))),
                                        .InstallFunction = CUInt(DIF.PROPERTYCHANGE)
                                    }
                                })
                            Else
                                ptrSetParams = New StructPtr(New SP_PROPCHANGE_PARAMS_X86() With
                                {
                                    .StateChange = CUInt(If(enable, DICS.ENABLE, DICS.DISABLE)),
                                    .Scope = CUInt(DICS_FLAG.GLOBAL),
                                    .HwProfile = 0UI,
                                    .ClassInstallHeader = New SP_CLASSINSTALL_HEADER_X86() With
                                    {
                                        .cbSize = CUInt(Marshal.SizeOf(GetType(SP_CLASSINSTALL_HEADER_X86))),
                                        .InstallFunction = CUInt(DIF.PROPERTYCHANGE)
                                    }
                                })
                            End If

                            CheckWin32Error(SetupDiSetClassInstallParams(infoSet, ptrDevInfo.Ptr, ptrSetParams.Ptr, CUInt(ptrSetParams.ObjSize)))

                            CheckWin32Error(SetupDiChangeState(infoSet, ptrDevInfo.Ptr))

                            If RebootRequired(infoSet, ptrDevInfo.Ptr) Then
                                If MessageBox.Show(String.Format("Reboot required!{0}Reboot now?", CRLF), "Device removed!", MessageBoxButton.YesNo, MessageBoxImage.Information) = MessageBoxResult.Yes Then
                                    Reboot()
                                End If
                            Else
                                MessageBox.Show(String.Format("Reboot not required!{0}NOTE: Windows XP doesn't 'set' reboot flag even if reboot required", CRLF), "Device removed!")
                            End If
                        Finally
                            If ptrSetParams IsNot Nothing Then
                                ptrSetParams.Dispose()
                            End If
                        End Try
                    Finally
                        If ptrDevInfo IsNot Nothing Then
                            ptrDevInfo.Dispose()
                        End If
                    End Try
                End Using
            Catch ex As Exception
                ShowException(ex)
            End Try
        End Sub

        Public Sub TEST_UpdateDevice(ByVal hardwareIDFilter As String, ByVal infFile As String, Optional ByVal force As Boolean = False)
            If String.IsNullOrEmpty(infFile) OrElse Not File.Exists(infFile) Then
                Throw New ArgumentException("Empty infFile or infFile doesn't exists!", "infFile")
                Return
            End If

            If String.IsNullOrEmpty(hardwareIDFilter) Then
                Throw New ArgumentException("Empty Hardware ID Filter!", "hardwareIDFilter")
                Return
            End If

            Try
                Dim nullGuid As Guid = Guid.Empty
                Dim hardwareIds(0) As String
                Dim found As Boolean = False
                Dim device As Device = Nothing

                Using infoSet As SafeDeviceHandle = SetupDiGetClassDevs(nullGuid, Nothing, IntPtr.Zero, CUInt(DIGCF.ALLCLASSES))
                    CheckWin32Error(Not infoSet.IsInvalid)

                    Dim ptrDevInfo As StructPtr = Nothing
                    Try
                        If Is64 Then
                            ptrDevInfo = New StructPtr(New SP_DEVINFO_DATA_X64() With {.cbSize = CUInt(Marshal.SizeOf(GetType(SP_DEVINFO_DATA_X64)))})
                        Else
                            ptrDevInfo = New StructPtr(New SP_DEVINFO_DATA_X86() With {.cbSize = CUInt(Marshal.SizeOf(GetType(SP_DEVINFO_DATA_X86)))})
                        End If

                        Dim i As UInt32 = 0UI

                        While True
                            If Not SetupDiEnumDeviceInfo(infoSet, i, ptrDevInfo.Ptr) Then
                                If GetLastWin32ErrorU() = Errors.NO_MORE_ITEMS Then
                                    Exit While
                                Else
                                    CheckWin32Error(False)
                                End If
                            End If

                            i += 1UI
                            hardwareIds = GetMultiStringProperty(infoSet, ptrDevInfo.Ptr, SPDRP.HARDWAREID)

                            If hardwareIds Is Nothing Then
                                Continue While
                            End If

                            For Each hdID As String In hardwareIds
                                If hdID.IndexOf(hardwareIDFilter, StringComparison.OrdinalIgnoreCase) <> -1 Then
                                    device = New Device() With
                                    {
                                     .Description = GetStringProperty(infoSet, ptrDevInfo.Ptr, SPDRP.DEVICEDESC),
                                     .ClassGuid = GetStringProperty(infoSet, ptrDevInfo.Ptr, SPDRP.CLASSGUID),
                                     .CompatibleIDs = GetMultiStringProperty(infoSet, ptrDevInfo.Ptr, SPDRP.COMPATIBLEIDS),
                                     .HardwareIDs = hardwareIds
                                    }

                                    GetDeviceDetails(infoSet, ptrDevInfo.Ptr, device)

                                    Dim msgResult As MessageBoxResult = MessageBox.Show(
                                       String.Format("Are you sure you want to update device:{2}{2}{3}{0}{2}Inf file: {2}{2}{3}{4}{2}Hardware IDs{2}{2}{3}{1}", device.Description, String.Join(CRLF & vbTab, device.HardwareIDs), CRLF, vbTab, infFile),
                                     "Warning!",
                                     MessageBoxButton.YesNoCancel,
                                     MessageBoxImage.Warning)

                                    If msgResult = MessageBoxResult.Yes Then
                                        found = True
                                        Exit For
                                    ElseIf (msgResult = MessageBoxResult.No) Then
                                        Exit For
                                    Else
                                        Return
                                    End If
                                End If
                            Next

                            If found Then
                                Exit While
                            End If
                        End While

                        If Not found OrElse device Is Nothing Then
                            Return
                        End If

                        If MessageBox.Show(String.Format("CONFIRM!!!{2}{2}Are you sure you want to update device:{2}{2}{3}{0}{2}Inf file: {2}{2}{3}{4}{2}Hardware IDs{2}{2}{3}{1}", device.Description, String.Join(CRLF & vbTab, device.HardwareIDs), CRLF, vbTab, infFile),
                              "Warning!",
                         MessageBoxButton.YesNo,
                         MessageBoxImage.Warning) <> MessageBoxResult.Yes Then

                            Return
                        End If

                        If force Then
                            Dim requiresReboot As Boolean
                            If Not UpdateDriverForPlugAndPlayDevices(IntPtr.Zero, device.HardwareIDs(0), infFile, CUInt(INSTALLFLAG.FORCE), requiresReboot) Then
                                MessageBox.Show("The function found a match for the HardwareId value, but the specified driver was not a better match" + CRLF + "than the current driver and the caller did not specify the INSTALLFLAG_FORCE flag.")
                                Return
                            Else
                                CheckWin32Error(False)
                            End If
                        Else
                            Dim requiresReboot As Boolean
                            If Not UpdateDriverForPlugAndPlayDevices(IntPtr.Zero, device.HardwareIDs(0), infFile, CUInt(INSTALLFLAG.NULL), requiresReboot) Then
                                MessageBox.Show("The function found a match for the HardwareId value, but the specified driver was not a better match" + CRLF + "than the current driver and the caller did not specify the INSTALLFLAG_FORCE flag.")
                                Return
                            Else
                                CheckWin32Error(False)
                            End If

                            If requiresReboot Then
                                If MessageBox.Show(String.Format("Reboot required!{0}Reboot now?", CRLF), "Device removed!", MessageBoxButton.YesNo, MessageBoxImage.Information) = MessageBoxResult.Yes Then
                                    Reboot()
                                End If
                            Else
                                MessageBox.Show(String.Format("Reboot not required!{0}NOTE: Windows XP doesn't 'set' reboot flag even if reboot required", CRLF), "Device removed!")
                            End If
                        End If
                    Finally
                        If ptrDevInfo IsNot Nothing Then
                            ptrDevInfo.Dispose()
                        End If
                    End Try
                End Using
            Catch ex As Exception
                ShowException(ex)
            End Try
        End Sub

        '***Not Working***
        '***Not Working***
        '***Not Working***
        '***Not Working***
        ' REVERSED FOR CLEANING FROM CODE ***Not Working*** venID is not used.
        ' eg.  venID = VEN_10DE  = for more accurate result, otherwise AMD + Nvidia (physx card) => both removed (both has 'Display' as class)
        ' Class GUID is also good choice (can be used as ROOT deviceSet => loop only gpus)
        ' may change

        '***Not Working***
        Public Function GetDevices(ByVal className As String, Optional ByVal venID As String = Nothing) As List(Of Device)
            If Not Application.Data.IsDebug Then
                Throw New NotImplementedException()
            End If

            Dim SiblingDevicesToFind As List(Of Device) = New List(Of Device)(5)
            Dim Devices As List(Of Device) = New List(Of Device)(5)
            Dim d As Device
            Dim devInst As UInt32

            Application.Log.AddMessage("Beginning of GetDevices")

            Try
                Dim nullGuid As Guid = Guid.Empty
                Dim devClass As String
                Dim device As Device = Nothing

                Using infoSet As SafeDeviceHandle = SetupDiGetClassDevs(nullGuid, Nothing, IntPtr.Zero, CUInt(DIGCF.ALLCLASSES))
                    CheckWin32Error(Not infoSet.IsInvalid)

                    Dim ptrDevInfo As StructPtr = Nothing
                    Try
                        If Is64 Then
                            ptrDevInfo = New StructPtr(New SP_DEVINFO_DATA_X64() With {.cbSize = GetUInt32(Marshal.SizeOf(GetType(SP_DEVINFO_DATA_X64)))})
                        Else
                            ptrDevInfo = New StructPtr(New SP_DEVINFO_DATA_X86() With {.cbSize = GetUInt32(Marshal.SizeOf(GetType(SP_DEVINFO_DATA_X86)))})
                        End If

                        Dim i As UInt32 = 0UI

                        While True
                            If Not SetupDiEnumDeviceInfo(infoSet, i, ptrDevInfo.Ptr) Then
                                If GetLastWin32ErrorU() = Errors.NO_MORE_ITEMS Then
                                    Exit While
                                Else
                                    CheckWin32Error(False)
                                End If
                            End If

                            i += 1UI

                            devClass = GetStringProperty(infoSet, ptrDevInfo.Ptr, SPDRP.CLASS)

                            If Not IsNullOrWhitespace(devClass) AndAlso devClass.Equals(className, StringComparison.OrdinalIgnoreCase) Then
                                If Is64 Then
                                    devInst = DirectCast(Marshal.PtrToStructure(ptrDevInfo.Ptr, GetType(SP_DEVINFO_DATA_X64)), SP_DEVINFO_DATA_X64).DevInst
                                Else
                                    devInst = DirectCast(Marshal.PtrToStructure(ptrDevInfo.Ptr, GetType(SP_DEVINFO_DATA_X86)), SP_DEVINFO_DATA_X86).DevInst
                                End If

                                d = New Device() With
                                {
                                    .devInst = devInst,
                                    .ClassName = devClass
                                }

                                GetDeviceDetails(infoSet, ptrDevInfo.Ptr, d)
                                GetDriverDetails(infoSet, ptrDevInfo.Ptr, d)

                                GetDeviceCMDetails(ptrDevInfo.Ptr, d)

                                GetSiblings(d)

                                Devices.Add(d)

                                If d.SiblingDevices IsNot Nothing AndAlso d.SiblingDevices.Count > 0 Then
                                    For Each sibling In d.SiblingDevices
                                        SiblingDevicesToFind.Add(sibling)
                                    Next
                                End If
                            End If
                        End While

                        Return Devices
                    Finally
                        If ptrDevInfo IsNot Nothing Then
                            ptrDevInfo.Dispose()
                        End If
                    End Try
                End Using
            Catch ex As Exception
                Application.Log.AddException(ex)
                Return New List(Of Device)(0)
            Finally
                If SiblingDevicesToFind.Count > 0 Then
                    GetDevicesByID(SiblingDevicesToFind)
                End If

                Application.Log.AddMessage("End of GetDevices")
            End Try
        End Function

        Private Function GetSubDevices(ByVal device As Device) As List(Of Device)
            Dim Devices As List(Of Device) = New List(Of Device)(5)

            Try
                Dim nullGuid As Guid = Guid.Empty
                Dim d As Device = Nothing
                Dim devInst As UInt32

                Using infoSet As SafeDeviceHandle = SetupDiGetClassDevs(nullGuid, device.DeviceID, IntPtr.Zero, CUInt(DIGCF.ALLCLASSES))
                    CheckWin32Error(Not infoSet.IsInvalid)

                    Dim ptrDevInfo As StructPtr = Nothing
                    Try
                        If Is64 Then
                            ptrDevInfo = New StructPtr(New SP_DEVINFO_DATA_X64() With {.cbSize = GetUInt32(Marshal.SizeOf(GetType(SP_DEVINFO_DATA_X64)))})
                        Else
                            ptrDevInfo = New StructPtr(New SP_DEVINFO_DATA_X86() With {.cbSize = GetUInt32(Marshal.SizeOf(GetType(SP_DEVINFO_DATA_X86)))})
                        End If

                        Dim i As UInt32 = 0UI

                        While True
                            If Not SetupDiEnumDeviceInfo(infoSet, i, ptrDevInfo.Ptr) Then
                                If GetLastWin32ErrorU() = Errors.NO_MORE_ITEMS Then
                                    Exit While
                                Else
                                    CheckWin32Error(False)
                                End If
                            End If

                            i += 1UI

                            If Is64 Then
                                devInst = DirectCast(Marshal.PtrToStructure(ptrDevInfo.Ptr, GetType(SP_DEVINFO_DATA_X64)), SP_DEVINFO_DATA_X64).DevInst
                            Else
                                devInst = DirectCast(Marshal.PtrToStructure(ptrDevInfo.Ptr, GetType(SP_DEVINFO_DATA_X86)), SP_DEVINFO_DATA_X86).DevInst
                            End If

                            If device.devInst <> devInst Then
                                d = New Device() With
                                {
                                    .devInst = devInst,
                                    .DeviceID = GetDeviceID(devInst)
                                }

                                Devices.Add(d)
                            End If
                        End While

                        Return Devices
                    Finally
                        If ptrDevInfo IsNot Nothing Then
                            ptrDevInfo.Dispose()
                        End If
                    End Try
                End Using
            Catch ex As Exception
                Application.Log.AddException(ex)
                Return New List(Of Device)(0)
            End Try
        End Function

        Private Sub GetDevicesByID(ByRef devList As List(Of Device))
            Try
                Dim nullGuid As Guid = Guid.Empty
                Dim device As Device = Nothing

                Using infoSet As SafeDeviceHandle = SetupDiGetClassDevs(nullGuid, Nothing, IntPtr.Zero, CUInt(DIGCF.ALLCLASSES))
                    CheckWin32Error(Not infoSet.IsInvalid)

                    Dim ptrDevInfo As StructPtr = Nothing
                    Try
                        If Is64 Then
                            ptrDevInfo = New StructPtr(New SP_DEVINFO_DATA_X64() With {.cbSize = GetUInt32(Marshal.SizeOf(GetType(SP_DEVINFO_DATA_X64)))})
                        Else
                            ptrDevInfo = New StructPtr(New SP_DEVINFO_DATA_X86() With {.cbSize = GetUInt32(Marshal.SizeOf(GetType(SP_DEVINFO_DATA_X86)))})
                        End If

                        Dim i As UInt32 = 0UI

                        While True
                            If Not SetupDiEnumDeviceInfo(infoSet, i, ptrDevInfo.Ptr) Then
                                If GetLastWin32ErrorU() = Errors.NO_MORE_ITEMS Then
                                    Exit While
                                Else
                                    CheckWin32Error(False)
                                End If
                            End If

                            i += 1UI


                            Dim d As Device = New Device()
                            Dim devInst As UInt32

                            If Is64 Then
                                devInst = DirectCast(Marshal.PtrToStructure(ptrDevInfo.Ptr, GetType(SP_DEVINFO_DATA_X64)), SP_DEVINFO_DATA_X64).DevInst
                            Else
                                devInst = DirectCast(Marshal.PtrToStructure(ptrDevInfo.Ptr, GetType(SP_DEVINFO_DATA_X86)), SP_DEVINFO_DATA_X86).DevInst
                            End If

                            d.DeviceID = GetDeviceID(devInst)

                            For Each sDev As Device In devList
                                If Not IsNullOrWhitespace(d.DeviceID) AndAlso Not IsNullOrWhitespace(sDev.DeviceID) AndAlso sDev.DeviceID.Equals(d.DeviceID, StringComparison.OrdinalIgnoreCase) Then
                                    GetDeviceDetails(infoSet, ptrDevInfo.Ptr, sDev)
                                    GetDriverDetails(infoSet, ptrDevInfo.Ptr, sDev)

                                    GetDeviceCMDetails(ptrDevInfo.Ptr, sDev)
                                End If
                            Next
                        End While
                    Finally
                        If ptrDevInfo IsNot Nothing Then
                            ptrDevInfo.Dispose()
                        End If
                    End Try
                End Using
            Catch ex As Exception
                Application.Log.AddException(ex)
            End Try
        End Sub

        ' REVERSED FOR CLEANING FROM CODE
        Public Sub UninstallDevice(ByRef device As Device)
            Application.Log.AddMessage("Beginning of UninstallDevice")

            Try
                If device Is Nothing Then
                    Application.Log.AddWarningMessage("Cancelling! Empty device!")
                    Return
                End If

                If device.HardwareIDs Is Nothing OrElse device.HardwareIDs.Length = 0 Then
                    Application.Log.AddWarningMessage("Cancelling! empty or no Hardware IDs!")
                    Return
                End If

                Dim logEntry As LogEntry = Application.Log.CreateEntry()
                logEntry.Message = "Device: " & device.Description
                logEntry.Add("Hardware IDs", String.Join(CRLF, device.HardwareIDs))

                If device.OemInfs IsNot Nothing Then
                    logEntry.Add("infFiles", String.Join(CRLF, device.OemInfs))
                Else
                    logEntry.Add("infFile", "No inf(s) associated")
                End If

                Application.Log.Add(logEntry)

                Dim nullGuid As Guid = Guid.Empty
                Dim hardwareIds(0) As String
                Dim found As Boolean = False


                Using infoSet As SafeDeviceHandle = SetupDiGetClassDevs(nullGuid, Nothing, IntPtr.Zero, CUInt(DIGCF.ALLCLASSES))
                    CheckWin32Error(Not infoSet.IsInvalid)

                    Dim ptrDevInfo As StructPtr = Nothing
                    Try
                        If Is64 Then
                            ptrDevInfo = New StructPtr(New SP_DEVINFO_DATA_X64() With {.cbSize = CUInt(Marshal.SizeOf(GetType(SP_DEVINFO_DATA_X64)))})
                        Else
                            ptrDevInfo = New StructPtr(New SP_DEVINFO_DATA_X86() With {.cbSize = CUInt(Marshal.SizeOf(GetType(SP_DEVINFO_DATA_X86)))})
                        End If

                        Dim i As UInt32 = 0UI

                        While True
                            If Not SetupDiEnumDeviceInfo(infoSet, i, ptrDevInfo.Ptr) Then
                                If GetLastWin32ErrorU() = Errors.NO_MORE_ITEMS Then
                                    Exit While
                                Else
                                    CheckWin32Error(False)
                                End If
                            End If

                            i += 1UI
                            hardwareIds = GetMultiStringProperty(infoSet, ptrDevInfo.Ptr, SPDRP.HARDWAREID)

                            If hardwareIds Is Nothing Then
                                Continue While
                            End If

                            For Each hdID As String In hardwareIds
                                If hdID.Equals(device.HardwareIDs(0), StringComparison.OrdinalIgnoreCase) Then
                                    GetDriverDetails(infoSet, ptrDevInfo.Ptr, device) ' Updating DriverDetails (if changed during app running time)
                                    found = True
                                    Exit For
                                End If
                            Next

                            If found Then
                                Exit While
                            End If
                        End While

                        If Not found Then
                            Return
                        End If

                        If device.OemInfs IsNot Nothing Then
                            Dim logInfs As LogEntry = Application.Log.CreateEntry()
                            logInfs.Message = "Uninstalling OEM Inf(s)."

                            For Each inf As String In device.OemInfs
                                If Not File.Exists(inf) Then
                                    logInfs.Add(inf, "File not exists")
                                    Continue For
                                End If

                                Dim infName As String = Path.GetFileName(inf)

                                If CheckIsOemInf(infName) Then 'Useless check since only OEM infs are added, but just incase ( see: GetDriverDetails )
                                    Dim attrs As FileAttributes = File.GetAttributes(inf)

                                    If (attrs And FileAttributes.ReadOnly) = FileAttributes.ReadOnly Then
                                        File.SetAttributes(inf, attrs And Not FileAttributes.ReadOnly)
                                    End If

                                    If SetupUninstallOEMInf(infName, CUInt(SetupUOInfFlags.SUOI_FORCEDELETE), IntPtr.Zero) Then
                                        logInfs.Add(inf, "Uninstalled!")
                                    Else
                                        logInfs.Add(inf, "Uninstalling failed! See exceptions for details!")

                                        Dim logInfEx As LogEntry = Application.Log.CreateEntry()
                                        logInfEx.AddException(New Win32Exception())
                                        logInfEx.Add("InfFile", inf)

                                        Application.Log.Add(logInfEx)
                                    End If
                                End If
                            Next

                            Application.Log.Add(logInfs)
                        End If

                        Dim logDevice As LogEntry = Application.Log.CreateEntry()
                        logDevice.Message = "Uninstalling device"
                        logDevice.Add("Description", device.Description)
                        logDevice.Add("Class", device.ClassName)
                        logDevice.Add("HardwareID", String.Join(CRLF, device.HardwareIDs))
                        Application.Log.Add(logDevice)

                        If SetupDiCallClassInstaller(CUInt(DIF.REMOVE), infoSet, ptrDevInfo.Ptr) Then
                            Application.Log.AddMessage("Device uninstalled!")
                        Else
                            Dim logDeviceEx As LogEntry = Application.Log.CreateEntry()
                            logDeviceEx.AddException(New Win32Exception())
                            logDeviceEx.Message = "Device uninstalling failed!"
                            Application.Log.Add(logDeviceEx)
                        End If
                    Finally
                        If ptrDevInfo IsNot Nothing Then
                            ptrDevInfo.Dispose()
                        End If
                    End Try
                End Using
            Catch ex As Exception
                Application.Log.AddException(ex)
            Finally
                Application.Log.AddMessage("End of UninstallDevice")
            End Try
        End Sub

        ' RESERVED FOR CLEANING FROM CODE
        Public Sub RemoveInf(ByRef oem As OemINF, ByVal force As Boolean)

            If oem Is Nothing Then
                Application.Log.AddWarningMessage("Cancelling! oem is nothing")
                Return
            End If

            If oem.FileName Is Nothing OrElse oem.FileName.Length = 0 Then
                Application.Log.AddWarningMessage("Cancelling! empty or no oem info!")
                Return
            End If
            Dim logEntry As LogEntry = Application.Log.CreateEntry()
            logEntry.Message = "Class: " & oem.FileName
            logEntry.Add("Provider", oem.Provider)
            logEntry.Add("Class", oem.Class)
            Application.Log.Add(logEntry)
            Dim infName As String = Path.GetFileName(oem.FileName)
            Dim logInfs As LogEntry = Application.Log.CreateEntry()
            logInfs.Message = "Uninstalling OEM Inf(s). " + infName
            Dim attrs As FileAttributes = File.GetAttributes(oem.FileName)

            If (attrs And FileAttributes.ReadOnly) = FileAttributes.ReadOnly Then
                File.SetAttributes(oem.FileName, attrs And Not FileAttributes.ReadOnly)
            End If

            If force Then
                If SetupUninstallOEMInf(infName, CUInt(SetupUOInfFlags.SUOI_FORCEDELETE), IntPtr.Zero) Then
                    logInfs.Add(oem.FileName, "Uninstalled!")
                Else

					Dim logInfEx As LogEntry = Application.Log.CreateEntry()
					logInfEx.AddException(New Win32Exception())
					logInfEx.Add("InfFile", oem.FileName)

					Application.Log.Add(logInfEx)

				End If
            Else
			If SetupUninstallOEMInf(infName, CUInt(SetupUOInfFlags.NONE), IntPtr.Zero) Then
				logInfs.Add(oem.FileName, "Uninstalled!")
				Else
					If GetLastWin32Error() = 0 Then
						logInfs.Add(oem.FileName, "Uninstalling failed! OEM Still in use")
					Else
						logInfs.Add(oem.FileName, "Uninstalling failed! See exceptions for details!")

						Dim logInfEx As LogEntry = Application.Log.CreateEntry()
						logInfEx.AddException(New Win32Exception())
						logInfEx.Add("InfFile", oem.FileName)

						Application.Log.Add(logInfEx)
					End If
				End If
			End If
			Application.Log.Add(logInfs)
			Application.Log.AddMessage("End of UninstallDevice")
        End Sub
        ' REVERSED FOR CLEANING FROM CODE

        Public Sub UpdateDevice(ByRef device As Device)

        End Sub



        Private Function GetClassGUIDsFromClassName(ByVal className As String) As Guid()
            Dim requiredSize As UInt32 = 0
            Dim guidArray(0) As Guid


            CheckWin32Error(SetupDiClassGuidsFromName(className, guidArray(0), GetUInt32(guidArray.Length), requiredSize))

            If requiredSize > 1 Then
                ReDim guidArray(GetInt32(requiredSize))

                CheckWin32Error(SetupDiClassGuidsFromName(className, guidArray(0), GetUInt32(guidArray.Length), requiredSize))
            End If

            Return guidArray
        End Function

        Private Function GetProperty(ByVal infoSet As SafeDeviceHandle, ByVal ptrDevInfo As IntPtr, ByVal [property] As SPDRP, ByRef bytes() As Byte, ByRef regType As RegistryValueKind, ByRef size As Int32) As Boolean
            Dim requiredSize As UInt32 = 1024UI

            If Not SetupDiGetDeviceRegistryProperty(infoSet, ptrDevInfo, CUInt([property]), regType, bytes, GetUInt32(bytes.Length), requiredSize) Then

                Select Case GetLastWin32ErrorU()
                    Case Errors.INSUFFICIENT_BUFFER
                        ReDim bytes(CInt(requiredSize))
                    Case Errors.INVALID_DATA
                        bytes = Nothing
                        Return False
                    Case Errors.NO_SUCH_DEVINST
                        bytes = Nothing
                        Return False
                    Case Else
                        Throw New Win32Exception()
                End Select
            End If

            CheckWin32Error(SetupDiGetDeviceRegistryProperty(infoSet, ptrDevInfo, CUInt([property]), regType, bytes, GetUInt32(bytes.Length), requiredSize))
            size = GetInt32(requiredSize)

            Return True
        End Function

        Private Function GetStringProperty(ByVal infoSet As SafeDeviceHandle, ByVal ptrDevInfo As IntPtr, ByVal [property] As SPDRP) As String
            Dim buffer(0) As Byte
            Dim size As Int32 = 0
            Dim regType As RegistryValueKind

            If Not GetProperty(infoSet, ptrDevInfo, [property], buffer, regType, size) Then
                Return Nothing
            End If

            If regType = RegistryValueKind.MultiString Then
                Return Encoding.Unicode.GetString(buffer, 0, size - DefaultCharSize).TrimEnd(NullChar).Replace(vbNullChar, CRLF)
            Else
                Return Encoding.Unicode.GetString(buffer, 0, size - DefaultCharSize).TrimEnd(NullChar)
            End If
        End Function

        Private Function GetMultiStringProperty(ByVal infoSet As SafeDeviceHandle, ByVal ptrDevInfo As IntPtr, ByVal [property] As SPDRP) As String()
            Dim buffer(0) As Byte
            Dim size As Int32 = 0
            Dim regType As RegistryValueKind

            If Not GetProperty(infoSet, ptrDevInfo, [property], buffer, regType, size) Then
                Return Nothing
            End If

            If regType = RegistryValueKind.MultiString Then
                Return Encoding.Unicode.GetString(buffer, 0, size - DefaultCharSize).TrimEnd(NullChar).Split(NullChar, StringSplitOptions.RemoveEmptyEntries)
            Else
                Return New String() {Encoding.Unicode.GetString(buffer, 0, size - DefaultCharSize).TrimEnd(NullChar)}
            End If
        End Function

        Private Function GetUInt32Property(ByVal infoSet As SafeDeviceHandle, ByVal ptrDevInfo As IntPtr, ByVal [property] As SPDRP) As UInt32
            Dim buffer(0) As Byte
            Dim size As Int32 = 0
            Dim regType As RegistryValueKind

            If Not GetProperty(infoSet, ptrDevInfo, [property], buffer, regType, size) Then
                Return 0
            End If

            Return BitConverter.ToUInt32(buffer, 0)
        End Function

        Private Function GetInstallParamsFlags(ByVal infoSet As SafeDeviceHandle, ByVal ptrDevInfo As IntPtr) As UInt32
            Dim ptrGetParams As StructPtr = Nothing

            Try
                If (Is64) Then
                    Dim installParams64 As SP_DEVINSTALL_PARAMS_X64 = New SP_DEVINSTALL_PARAMS_X64() With {.cbSize = GetUInt32(Marshal.SizeOf(GetType(SP_DEVINSTALL_PARAMS_X64)))}

                    ptrGetParams = New StructPtr(installParams64)
                    CheckWin32Error(SetupDiGetDeviceInstallParams(infoSet, ptrDevInfo, ptrGetParams.Ptr))

                    Return installParams64.Flags
                Else
                    Dim installParams86 As SP_DEVINSTALL_PARAMS_X86 = New SP_DEVINSTALL_PARAMS_X86() With {.cbSize = GetUInt32(Marshal.SizeOf(GetType(SP_DEVINSTALL_PARAMS_X86)))}

                    ptrGetParams = New StructPtr(installParams86)
                    CheckWin32Error(SetupDiGetDeviceInstallParams(infoSet, ptrDevInfo, ptrGetParams.Ptr))

                    Return installParams86.Flags
                End If
            Finally
                If ptrGetParams IsNot Nothing Then
                    ptrGetParams.Dispose()
                End If
            End Try
        End Function

        Private Sub GetDeviceDetails(ByVal infoSet As SafeDeviceHandle, ByVal ptrDevInfo As IntPtr, ByRef device As Device)
            If device.ClassGuid Is Nothing Then device.ClassGuid = GetStringProperty(infoSet, ptrDevInfo, SPDRP.CLASSGUID)
            If device.Description Is Nothing Then device.Description = GetStringProperty(infoSet, ptrDevInfo, SPDRP.DEVICEDESC)
            If device.FriendlyName Is Nothing Then device.FriendlyName = GetStringProperty(infoSet, ptrDevInfo, SPDRP.FRIENDLYNAME)
            If device.CompatibleIDs Is Nothing Then device.CompatibleIDs = GetMultiStringProperty(infoSet, ptrDevInfo, SPDRP.COMPATIBLEIDS)
            If device.InstallState Is Nothing Then device.InstallState = GetDescription(DirectCast(GetUInt32Property(infoSet, ptrDevInfo, SPDRP.INSTALL_STATE), DEVICE_INSTALL_STATE))
            If device.LowerFilters Is Nothing Then device.LowerFilters = GetMultiStringProperty(infoSet, ptrDevInfo, SPDRP.LOWERFILTERS)
            If device.Capabilities Is Nothing Then device.Capabilities = ToStringArray(Of CM_DEVCAP)(DirectCast(GetUInt32Property(infoSet, ptrDevInfo, SPDRP.CAPABILITIES), CM_DEVCAP))
            If device.InstallFlags Is Nothing Then device.InstallFlags = ToStringArray(Of DI)(DirectCast(GetInstallParamsFlags(infoSet, ptrDevInfo), DI))
            If device.ConfigFlags Is Nothing Then device.ConfigFlags = ToStringArray(Of CONFIGFLAGS)(DirectCast(GetUInt32Property(infoSet, ptrDevInfo, SPDRP.CONFIGFLAGS), CONFIGFLAGS))
        End Sub

        Private Sub GetDeviceCMDetails(ByVal ptrDevInfo As IntPtr, ByRef device As Device)
            If device.devInst = 0UI Then
                If (Is64) Then
                    device.devInst = DirectCast(Marshal.PtrToStructure(ptrDevInfo, GetType(SP_DEVINFO_DATA_X64)), SP_DEVINFO_DATA_X64).DevInst
                Else
                    device.devInst = DirectCast(Marshal.PtrToStructure(ptrDevInfo, GetType(SP_DEVINFO_DATA_X86)), SP_DEVINFO_DATA_X86).DevInst
                End If
            End If

            device.DeviceID = GetDeviceID(device.devInst)

            Dim pulStatus As UInt32 = 0UI
            Dim pulProblemNumber As UInt32 = 0UI

            Dim result2 As CR = DirectCast(CM_Get_DevNode_Status(pulStatus, pulProblemNumber, device.devInst, 0UI), CR)

            If result2 = CR.SUCCESS Then
                device.DevStatus = ToStringArray(Of DN)(DirectCast(pulStatus, DN))
                device.DevProblems = ToStringArray(Of CM_PROB)(DirectCast(pulProblemNumber, CM_PROB))
            ElseIf result2 = CR.NO_SUCH_DEVINST Then
            Else
                CheckWin32Error(False)
            End If
        End Sub

        Private Sub GetDriverDetails(ByVal infoSet As SafeDeviceHandle, ByVal ptrDevInfo As IntPtr, ByRef device As Device)
            If device.DriverInfo IsNot Nothing AndAlso device.DriverInfo.Count > 0 Then
                device.DriverInfo.Clear()
            End If

            If SetupDiBuildDriverInfoList(infoSet, ptrDevInfo, CUInt(SPDIT.COMPATDRIVER)) Then
                Dim oemInfs As List(Of String) = New List(Of String)(5)

                Dim ptrDrvInfoData As StructPtr = Nothing
                Dim i As UInt32 = 0UI
                Dim bytes(0) As Byte

                Try
                    If (Is64) Then
                        ptrDrvInfoData = New StructPtr(New SP_DRVINFO_DATA_X64() With {.cbSize = GetUInt32(Marshal.SizeOf(GetType(SP_DRVINFO_DATA_X64)))})
                    Else
                        ptrDrvInfoData = New StructPtr(New SP_DRVINFO_DATA_X86() With {.cbSize = GetUInt32(Marshal.SizeOf(GetType(SP_DRVINFO_DATA_X86)))})
                    End If

                    While True
                        If SetupDiEnumDriverInfo(infoSet, ptrDevInfo, CUInt(SPDIT.COMPATDRIVER), i, ptrDrvInfoData.Ptr) Then
                            i += 1UI

                            Dim ptrDrvInfoDetailData As StructPtr = Nothing

                            Try
                                If (Is64) Then
                                    ptrDrvInfoDetailData = New StructPtr(New SP_DRVINFO_DETAIL_DATA_X64() With {.cbSize = GetUInt32(Marshal.SizeOf(GetType(SP_DRVINFO_DETAIL_DATA_X64)))})
                                Else
                                    ptrDrvInfoDetailData = New StructPtr(New SP_DRVINFO_DETAIL_DATA_X86() With {.cbSize = GetUInt32(Marshal.SizeOf(GetType(SP_DRVINFO_DETAIL_DATA_X86)))})
                                End If

                                Dim reqSize As UInt32 = 0UI
                                Dim drvInfo As DriverInfo = New DriverInfo()

                                If Not SetupDiGetDriverInfoDetail(infoSet, ptrDevInfo, ptrDrvInfoData.Ptr, ptrDrvInfoDetailData.Ptr, GetUInt32(ptrDrvInfoDetailData.ObjSize), reqSize) Then
                                    If GetLastWin32ErrorU() <> Errors.INSUFFICIENT_BUFFER Then
                                        CheckWin32Error(False)
                                    Else
                                        Dim ptrDrvInfoDetailData2 As StructPtr = Nothing

                                        Try
                                            If Is64 Then
                                                ptrDrvInfoDetailData2 = New StructPtr(New SP_DRVINFO_DETAIL_DATA_X64() With {.cbSize = GetUInt32(Marshal.SizeOf(GetType(SP_DRVINFO_DETAIL_DATA_X64)))}, reqSize)

                                                If SetupDiGetDriverInfoDetail(infoSet, ptrDevInfo, ptrDrvInfoData.Ptr, ptrDrvInfoDetailData2.Ptr, CUInt(ptrDrvInfoDetailData2.ObjSize), reqSize) Then
                                                    Dim ptrOffsetHardwareID As IntPtr = Marshal.OffsetOf(GetType(SP_DRVINFO_DETAIL_DATA_X64), "HardwareID")

                                                    Dim OffsetHardwareID As Int64 = ptrOffsetHardwareID.ToInt64()
                                                    Dim HardwareIDLength As Int32 = CInt((CLng(reqSize) - OffsetHardwareID) / CLng(DefaultCharSize))

                                                    Dim ptrHardwareID As IntPtr = IntPtrAdd(ptrDrvInfoDetailData2.Ptr, OffsetHardwareID)

                                                    Dim result As SP_DRVINFO_DETAIL_DATA_X64 = DirectCast(Marshal.PtrToStructure(ptrDrvInfoDetailData2.Ptr, GetType(SP_DRVINFO_DETAIL_DATA_X64)), SP_DRVINFO_DETAIL_DATA_X64)
                                                    result.HardwareID = Marshal.PtrToStringAuto(ptrHardwareID, HardwareIDLength)

                                                    If result.CompatIDsOffset > 1UI Then
                                                        drvInfo.HardwareID = Marshal.PtrToStringAuto(ptrHardwareID)
                                                    End If

                                                    If result.CompatIDsLength > 0UI Then
                                                        Dim ptrCompatibleIDs As IntPtr = IntPtrAdd(ptrHardwareID, CLng(result.CompatIDsOffset * DefaultCharSizeU))
                                                        drvInfo.CompatibleIDs = Marshal.PtrToStringAuto(ptrCompatibleIDs, GetInt32(result.CompatIDsLength)).Split(NullChar, StringSplitOptions.RemoveEmptyEntries)
                                                    End If
                                                Else
                                                    CheckWin32Error(False)
                                                End If
                                            Else
                                                ptrDrvInfoDetailData2 = New StructPtr(New SP_DRVINFO_DETAIL_DATA_X86() With {.cbSize = GetUInt32(Marshal.SizeOf(GetType(SP_DRVINFO_DETAIL_DATA_X86)))}, reqSize)

                                                If SetupDiGetDriverInfoDetail(infoSet, ptrDevInfo, ptrDrvInfoData.Ptr, ptrDrvInfoDetailData2.Ptr, GetUInt32(ptrDrvInfoDetailData2.ObjSize), reqSize) Then
                                                    Dim ptrOffsetHardwareID As IntPtr = Marshal.OffsetOf(GetType(SP_DRVINFO_DETAIL_DATA_X86), "HardwareID")

                                                    Dim OffsetHardwareID As Int64 = ptrOffsetHardwareID.ToInt64()
                                                    Dim HardwareIDLength As Int32 = CInt((CLng(reqSize) - OffsetHardwareID) / CLng(DefaultCharSize))

                                                    Dim ptrHardwareID As IntPtr = IntPtrAdd(ptrDrvInfoDetailData2.Ptr, OffsetHardwareID)

                                                    Dim result As SP_DRVINFO_DETAIL_DATA_X86 = DirectCast(Marshal.PtrToStructure(ptrDrvInfoDetailData2.Ptr, GetType(SP_DRVINFO_DETAIL_DATA_X86)), SP_DRVINFO_DETAIL_DATA_X86)
                                                    result.HardwareID = Marshal.PtrToStringAuto(ptrHardwareID, HardwareIDLength)

                                                    If result.CompatIDsOffset > 1UI Then
                                                        drvInfo.HardwareID = Marshal.PtrToStringAuto(ptrHardwareID)
                                                    End If

                                                    If result.CompatIDsLength > 0UI Then
                                                        Dim ptrCompatibleIDs As IntPtr = IntPtrAdd(ptrHardwareID, CLng(result.CompatIDsOffset * DefaultCharSizeU))
                                                        drvInfo.CompatibleIDs = Marshal.PtrToStringAuto(ptrCompatibleIDs, GetInt32(result.CompatIDsLength)).Split(NullChar, StringSplitOptions.RemoveEmptyEntries)
                                                    End If
                                                Else
                                                    CheckWin32Error(False)
                                                End If
                                            End If

                                            If (Is64) Then

                                                Dim drvData64 As SP_DRVINFO_DATA_X64 = DirectCast(Marshal.PtrToStructure(ptrDrvInfoData.Ptr, GetType(SP_DRVINFO_DATA_X64)), SP_DRVINFO_DATA_X64)
                                                Dim drvDetailData64 As SP_DRVINFO_DETAIL_DATA_X64 = DirectCast(Marshal.PtrToStructure(ptrDrvInfoDetailData2.Ptr, GetType(SP_DRVINFO_DETAIL_DATA_X64)), SP_DRVINFO_DETAIL_DATA_X64)

                                                bytes = BitConverter.GetBytes(drvData64.DriverVersion)

                                                drvInfo.MfgName = drvData64.MfgName
                                                drvInfo.ProviderName = drvData64.ProviderName
                                                drvInfo.Description = drvData64.Description
                                                drvInfo.DriverDate = FileTimeToDateTime(drvData64.DriverDate)
                                                drvInfo.DriverVersion = String.Format("{3}.{2}.{1}.{0}",
                                                  BitConverter.ToInt16(bytes, 0).ToString(),
                                                  BitConverter.ToInt16(bytes, 2).ToString(),
                                                  BitConverter.ToInt16(bytes, 4).ToString(),
                                                  BitConverter.ToInt16(bytes, 6).ToString())
                                                drvInfo.InfFileName = drvDetailData64.InfFileName
                                                drvInfo.InfDate = FileTimeToDateTime(drvDetailData64.InfDate)

                                            Else
                                                Dim drvData86 As SP_DRVINFO_DATA_X86 = DirectCast(Marshal.PtrToStructure(ptrDrvInfoData.Ptr, GetType(SP_DRVINFO_DATA_X86)), SP_DRVINFO_DATA_X86)
                                                Dim drvDetailData86 As SP_DRVINFO_DETAIL_DATA_X86 = DirectCast(Marshal.PtrToStructure(ptrDrvInfoDetailData2.Ptr, GetType(SP_DRVINFO_DETAIL_DATA_X86)), SP_DRVINFO_DETAIL_DATA_X86)

                                                bytes = BitConverter.GetBytes(drvData86.DriverVersion)

                                                drvInfo.MfgName = drvData86.MfgName
                                                drvInfo.ProviderName = drvData86.ProviderName
                                                drvInfo.Description = drvData86.Description
                                                drvInfo.DriverDate = FileTimeToDateTime(drvData86.DriverDate)
                                                drvInfo.DriverVersion = String.Format("{3}.{2}.{1}.{0}",
                                                  BitConverter.ToInt16(bytes, 0).ToString(),
                                                  BitConverter.ToInt16(bytes, 2).ToString(),
                                                  BitConverter.ToInt16(bytes, 4).ToString(),
                                                  BitConverter.ToInt16(bytes, 6).ToString())
                                                drvInfo.InfFileName = drvDetailData86.InfFileName
                                                drvInfo.InfDate = FileTimeToDateTime(drvDetailData86.InfDate)
                                            End If

                                            If CheckIsOemInf(Path.GetFileName(drvInfo.InfFileName)) Then
                                                oemInfs.Add(drvInfo.InfFileName)
                                            End If

                                            device.DriverInfo.Add(drvInfo)
                                        Finally
                                            If ptrDrvInfoDetailData2 IsNot Nothing Then
                                                ptrDrvInfoDetailData2.Dispose()
                                            End If
                                        End Try
                                    End If
                                End If
                            Finally
                                If ptrDrvInfoDetailData IsNot Nothing Then
                                    ptrDrvInfoDetailData.Dispose()
                                End If
                            End Try
                        Else
                            If GetLastWin32ErrorU() <> Errors.NO_MORE_ITEMS Then
                                CheckWin32Error(False)
                            Else
                                Exit While
                            End If
                        End If
                    End While

                    device.OemInfs = oemInfs.ToArray()
                Finally
                    If ptrDrvInfoData IsNot Nothing Then
                        ptrDrvInfoData.Dispose()
                    End If
                End Try
            Else
                CheckWin32Error(False)
            End If
        End Sub

        Private Function GetDeviceID(ByVal devInst As UInt32) As String
            Dim result As CR
            Dim deviceID As New StringBuilder(MAX_LEN)

            While True
                result = DirectCast(CM_Get_Device_ID(devInst, deviceID, GetUInt32(deviceID.Capacity), 0UI), CR)

                If result = CR.BUFFER_SMALL Then
                    deviceID.EnsureCapacity(deviceID.Capacity * 2)
                ElseIf result = CR.SUCCESS Then
                    Return deviceID.ToString()
                Else
                    CheckWin32Error(False)
                End If
            End While

            Return Nothing
        End Function

        Private Sub GetSiblings(ByVal device As Device)
            Dim result As CR
            Dim devInstParent As UInt32 = 0UI
            Dim devInstChild As UInt32 = 0UI
            Dim devInstSibling As UInt32 = 0UI
            result = DirectCast(CM_Get_Parent(devInstParent, device.devInst, 0UI), CR)

            If result = CR.SUCCESS Then
                result = DirectCast(CM_Get_Child(devInstChild, devInstParent, 0UI), CR)

                While True
                    result = DirectCast(CM_Get_Sibling(devInstSibling, devInstChild, 0UI), CR)

                    If result = CR.SUCCESS Then
                        device.SiblingDevices.Add(New Device() With {.devInst = devInstSibling, .DeviceID = GetDeviceID(devInstSibling)})
                      
                        devInstChild = devInstSibling
                    ElseIf result = CR.NO_SUCH_DEVINST Then
                        Exit While
                    End If
                End While
           
            ElseIf result = CR.NO_SUCH_DEVINST Then
            Else
                CheckWin32Error(False)
            End If

            Dim pulStatus As UInt32 = 0UI
            Dim pulProblemNumber As UInt32 = 0UI
        End Sub

        Private Function RebootRequired(ByVal infoSet As SafeDeviceHandle, ByVal ptrDevInfo As IntPtr) As Boolean
            Return RebootRequired(GetInstallParamsFlags(infoSet, ptrDevInfo))
        End Function

        Private Function RebootRequired(ByVal installParamsFlags As UInt32) As Boolean
            If ((installParamsFlags And CUInt(DI.NEEDREBOOT)) = CUInt(DI.NEEDREBOOT) Or
               (installParamsFlags And CUInt(DI.NEEDRESTART)) = CUInt(DI.NEEDRESTART)) Then
                Return True
            Else
                Return False
            End If
        End Function

        Private Sub Reboot()

            Dim p As Process = New Process() With
            {
                .StartInfo = New ProcessStartInfo() With
                {
                    .FileName = "shutdown",
                    .Arguments = "/r /t 0",
                    .WindowStyle = ProcessWindowStyle.Hidden,
                    .UseShellExecute = True,
                    .CreateNoWindow = True,
                    .RedirectStandardOutput = False
                }
            }

            p.Start()
            p.WaitForExit()
            p.Close()
        End Sub

        Private Function CheckIsOemInf(ByVal infName As String) As Boolean
            If String.IsNullOrEmpty(infName) Then
                Return False
            End If

            If infName.Length > 7 AndAlso infName.StartsWith("oem", StringComparison.OrdinalIgnoreCase) AndAlso infName.EndsWith(".inf", StringComparison.OrdinalIgnoreCase) Then
                For i As Int32 = 3 To infName.Length - 5 Step 1
                    If Not Char.IsDigit(infName(i)) Then
                        Return False
                    End If
                Next

                Return True
            End If

            Return False
        End Function

        Private Function FileTimeToDateTime(ByVal time As System.Runtime.InteropServices.ComTypes.FILETIME) As DateTime
            Dim high As UInt64 = CType(time.dwHighDateTime, UInt64)
            Dim low As UInt32 = GetUInt32(time.dwLowDateTime)
            Dim FILETIME As Int64 = CLng(((high << 32) + low))

            Return DateTime.FromFileTimeUtc(FILETIME)
        End Function

#End Region

#Region "Classes"

        Public Class Device
            Friend devInst As UInt32
            Private _hardwareIDs As String()
            Private _lowerfilters As String()
            Private _friendlyname As String
            Private _compatibleIDs As String()
            Private _RregInf As String
            Private _oemInfs As String()
            Private _description As String
            Private _classGuid As String
            Private _className As String
            Private _deviceID As String
            Private _driverInfo As List(Of DriverInfo)
            Private _siblingDevices As List(Of Device)


            Private _installState As String
            Private _installFlags As String()
            Private _capabilities As String()
            Private _configFlags As String()

            Private _devProblems As String()
            Private _devStatus As String()

            Public Property HardwareIDs As String()
                Get
                    Return _hardwareIDs
                End Get
                Friend Set(value As String())
                    _hardwareIDs = value
                End Set
            End Property

            Public Property LowerFilters As String()
                Get
                    Return _lowerfilters
                End Get
                Friend Set(value As String())
                    _lowerfilters = value
                End Set
            End Property

            Public Property FriendlyName As String
                Get
                    Return _friendlyname
                End Get
                Friend Set(value As String)
                    _friendlyname = value
                End Set
            End Property
            Public Property CompatibleIDs As String()
                Get
                    Return _compatibleIDs
                End Get
                Friend Set(value As String())
                    _compatibleIDs = value
                End Set
            End Property
            Public Property RegInf As String
                Get
                    Return _RregInf
                End Get
                Friend Set(value As String)
                    _RregInf = value
                End Set
            End Property
            Public Property OemInfs As String()
                Get
                    Return _oemInfs
                End Get
                Friend Set(value As String())
                    _oemInfs = value
                End Set
            End Property
            Public Property Description As String
                Get
                    Return _description
                End Get
                Friend Set(value As String)
                    _description = value
                End Set
            End Property
            Public Property ClassGuid As String
                Get
                    Return _classGuid
                End Get
                Friend Set(value As String)
                    _classGuid = value
                End Set
            End Property
            Public Property ClassName As String
                Get
                    Return _className
                End Get
                Friend Set(value As String)
                    _className = value
                End Set
            End Property
            Public Property DeviceID As String
                Get
                    Return _deviceID
                End Get
                Friend Set(value As String)
                    _deviceID = value
                End Set
            End Property
            Public Property DriverInfo As List(Of DriverInfo)
                Get
                    Return _driverInfo
                End Get
                Friend Set(value As List(Of DriverInfo))
                    _driverInfo = value
                End Set
            End Property
            Public Property SiblingDevices As List(Of Device)
                Get
                    Return _siblingDevices
                End Get
                Friend Set(value As List(Of Device))
                    _siblingDevices = value
                End Set
            End Property
            Public Property InstallState As String
                Get
                    Return _installState
                End Get
                Friend Set(value As String)
                    _installState = value
                End Set
            End Property
            Public Property InstallFlags As String()
                Get
                    Return _installFlags
                End Get
                Friend Set(value As String())
                    _installFlags = value
                End Set
            End Property
            Public Property Capabilities As String()
                Get
                    Return _capabilities
                End Get
                Friend Set(value As String())
                    _capabilities = value
                End Set
            End Property
            Public Property ConfigFlags As String()
                Get
                    Return _configFlags
                End Get
                Friend Set(value As String())
                    _configFlags = value
                End Set
            End Property

            Public Property DevProblems As String()
                Get
                    Return _devProblems
                End Get
                Friend Set(value As String())
                    _devProblems = value
                End Set
            End Property

            Public Property DevStatus As String()
                Get
                    Return _devStatus
                End Get
                Friend Set(value As String())
                    _devStatus = value
                End Set
            End Property

            Friend Sub New()
                _driverInfo = New List(Of DriverInfo)(5)
                _siblingDevices = New List(Of Device)(2)
            End Sub
        End Class

        Public Class DriverInfo
            Private _mfgName As String
            Private _providerName As String
            Private _description As String
            Private _driverVersion As String
            Private _driverDate As DateTime
            Private _infFileName As String
            Private _infDate As DateTime
            Private _hardwareID As String
            Private _compatibleIDs As String()

            Public Property MfgName As String
                Get
                    Return _mfgName
                End Get
                Friend Set(value As String)
                    _mfgName = value
                End Set
            End Property
            Public Property ProviderName As String
                Get
                    Return _providerName
                End Get
                Friend Set(value As String)
                    _providerName = value
                End Set
            End Property
            Public Property Description As String
                Get
                    Return _description
                End Get
                Friend Set(value As String)
                    _description = value
                End Set
            End Property
            Public Property DriverVersion As String
                Get
                    Return _driverVersion
                End Get
                Friend Set(value As String)
                    _driverVersion = value
                End Set
            End Property
            Public Property DriverDate As DateTime
                Get
                    Return _driverDate
                End Get
                Friend Set(value As DateTime)
                    _driverDate = value
                End Set
            End Property
            Public Property InfFileName As String
                Get
                    Return _infFileName
                End Get
                Friend Set(value As String)
                    _infFileName = value
                End Set
            End Property
            Public Property InfDate As DateTime
                Get
                    Return _infDate
                End Get
                Friend Set(value As DateTime)
                    _infDate = value
                End Set
            End Property
            Public Property HardwareID As String
                Get
                    Return _hardwareID
                End Get
                Friend Set(value As String)
                    _hardwareID = value
                End Set
            End Property
            Public Property CompatibleIDs As String()
                Get
                    Return _compatibleIDs
                End Get
                Friend Set(value As String())
                    _compatibleIDs = value
                End Set
            End Property

            Friend Sub New()
                _compatibleIDs = Nothing
            End Sub

        End Class

        Private Class SafeDeviceHandle
            Inherits SafeHandleMinusOneIsInvalid

            Private Sub New()
                MyBase.New(True)
            End Sub

            Private Sub New(ByVal preexistingHandle As IntPtr, ByVal ownsHandle As Boolean)
                MyBase.New(ownsHandle)
                SetHandle(preexistingHandle)
            End Sub

            <SecurityCritical()>
            Protected Overrides Function ReleaseHandle() As Boolean
                Return SetupDiDestroyDeviceInfoList(handle)
            End Function
        End Class

#End Region

    End Module

    Public Class InfError
        Private Const ERROR_LINE_NOT_FOUND As UInt32 = &H800F0102UI
        Private Const ERROR_NO_MORE_ITEMS As UInt32 = &H80070103UI
        Private Const ERROR_INVALID_PARAMETER As UInt32 = &H80070057UI

        Private Const FACILITY_SETUPAPI As UInt32 = 15UI
        Private Const FACILITY_WIN32 As UInt32 = 7UI

        Protected _lastError As UInt32 = 0UI
        Protected _lastMessage As String

        Public ReadOnly Property LastError As UInt32
            Get
                Return _lastError
            End Get
        End Property

        Public ReadOnly Property LastMessage As String
            Get
                Return If(_lastMessage, "Success")
            End Get
        End Property

        Protected Sub ResetError()
            _lastError = 0UI
            _lastMessage = Nothing
        End Sub

        Protected Function GetLastError() As UInt32
            Return GetLastError(True)
        End Function

        Protected Function GetLastError(ByVal setLastError As Boolean) As UInt32
            Dim hresult As UInt32 = HRESULT_FROM_SETUPAPI(GetLastWin32ErrorU())

            If setLastError Then
                _lastError = hresult
                _lastMessage = New System.ComponentModel.Win32Exception(GetInt32(_lastError)).Message
            End If

            Return hresult
        End Function

        Private Shared Function HRESULT_FROM_WIN32(ByVal x As UInt32) As UInt32
            Return If(x <= 0,
                      x,
                      ((x And &HFFFFUI) Or (FACILITY_WIN32 << 16UI) Or &H80000000UI))
        End Function

        Private Shared Function HRESULT_FROM_SETUPAPI(ByVal x As UInt32) As UInt32
            Return CUInt(If(((x And (APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR)) = (APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR)),
             (((x And &HFFFFUI) Or (FACILITY_SETUPAPI << 16UI) Or &H80000000UI)),
             HRESULT_FROM_WIN32(x)))
        End Function

    End Class

    Public Class InfFile
        Inherits InfError
        Implements IDisposable

        Private Class PrivateSection
            Inherits InfLine

            Public Sub New(ByVal context As NativeMethods.INFCONTEXT, ByVal section As String, ByVal file As String)
                _context = context
                _section = section
                _file = file
            End Sub
        End Class

        Private _file As String
        Private _handle As IntPtr = NativeMethods.INVALID_HANDLE
        Private _disposed As Boolean

        Public ReadOnly Property FilePath As String
            Get
                Return If(_file, "<null>")
            End Get
        End Property

        Public ReadOnly Property IsOpen As Boolean
            Get
                Return _handle <> NativeMethods.INVALID_HANDLE
            End Get
        End Property

        Public Sub New(ByVal path As String)
            _file = path
        End Sub

        Public Function Open(ByRef errorLineNumber As UInt32) As UInt32
            _handle = NativeMethods.SetupOpenInfFile(_file, Nothing, (NativeMethods.INF_STYLE_OLDNT Or NativeMethods.INF_STYLE_WIN4), errorLineNumber)

            If (_handle = NativeMethods.INVALID_HANDLE) Then
                GetLastError()
                _lastMessage = String.Format("Error opening file '{0}',{3}Bad line = {1},{3}Message = ""{2}""", FilePath, errorLineNumber, LastMessage, CRLF)
            Else
                ResetError()
                errorLineNumber = 0UI
            End If

            Return LastError
        End Function

        Public Function Open() As UInt32
            Dim errLine As UInt32
            Return Open(errLine)
        End Function

        Public Sub Close()
            If _handle <> NativeMethods.INVALID_HANDLE Then
                NativeMethods.SetupCloseInfFile(_handle)

                _handle = NativeMethods.INVALID_HANDLE
            End If
        End Sub

        Public Function FindFirstKey(ByVal section As String, ByVal key As String) As InfLine
            Dim retval As PrivateSection = Nothing
            Dim sectionContext As NativeMethods.INFCONTEXT = New NativeMethods.INFCONTEXT

            If Not NativeMethods.SetupFindFirstLine(_handle, section, key, sectionContext) Then
                GetLastError()
                _lastMessage = String.Format("Error finding key '{0}' in section '{1}' of file '{2}',{4}Message = ""{3}""", If(key, "<null>"), section, FilePath, LastMessage, CRLF)
            Else
                ResetError()
                retval = New PrivateSection(sectionContext, section, _file)
            End If

            Return retval
        End Function

        Public Overrides Function ToString() As String
            Return FilePath
        End Function

        Protected Overrides Sub Finalize()
            Dispose(False)
        End Sub

        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not _disposed Then
                If disposing Then

                End If

                Close()
            End If

            _disposed = True
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

    End Class

    Public Class InfLine
        Inherits InfError

        Protected _context As NativeMethods.INFCONTEXT = New NativeMethods.INFCONTEXT
        Protected _section As String
        Protected _file As String

        Public ReadOnly Property Section As String
            Get
                Return If(_section, "<null>")
            End Get
        End Property

        Public ReadOnly Property FilePath As String
            Get
                Return If(_file, "<null>")
            End Get
        End Property

        Protected Sub New()
        End Sub

        Public Function GetLineText(ByRef line As String) As UInt32
            _lastError = InternalGetLineText(_lastMessage, line)

            Return LastError
        End Function

        Public Function GetLineText() As String
            Dim line As String = Nothing

            GetLineText(line)

            Return line
        End Function

        Public Function GetString(ByVal fieldNum As Int32, ByRef strVal As String) As UInt32
            Dim requiredSize As UInt32 = 0UI
            Dim builder As StringBuilder = New StringBuilder(100)
            Dim lineText As String = Nothing
            Dim msg As String = Nothing

            ResetError()

            Dim setupRetVal As Boolean = NativeMethods.SetupGetStringField(_context, fieldNum, builder, builder.Capacity, requiredSize)

            If requiredSize > builder.Capacity Then
                builder.Capacity = CType(requiredSize, Int32)

                setupRetVal = NativeMethods.SetupGetStringField(_context, fieldNum, builder, builder.Capacity, requiredSize)
            End If

            If Not setupRetVal Then
                strVal = Nothing
                GetLastError()

                If (InternalGetLineText(msg, lineText) <> 0) Then lineText = "<unknown>"

                _lastMessage = String.Format("Error reading string value from field {0} of line '{1}'{5}in section [{2}] in file '{3}',{5}Message = ""{4}""", fieldNum, lineText, Section, FilePath, LastMessage, CRLF)
            Else
                strVal = builder.ToString()
            End If

            Return LastError
        End Function

        Public Function GetString(ByVal fieldNum As Int32) As String
            Dim val As String = Nothing

            GetString(fieldNum, val)

            Return val
        End Function

        Private Function InternalGetLineText(ByRef errorMessage As String, ByRef line As String) As UInt32
            Dim requiredSize As UInt32 = 0
            Dim builder As StringBuilder = New StringBuilder(200)
            Dim rc As UInt32

            Dim setupRetVal As Boolean = NativeMethods.SetupGetLineText(_context, IntPtr.Zero, Nothing, Nothing, builder, GetUInt32(builder.Capacity), requiredSize)

            If requiredSize > builder.Capacity Then
                builder.Capacity = CType(requiredSize, Int32)
                setupRetVal = NativeMethods.SetupGetLineText(_context, IntPtr.Zero, Nothing, Nothing, builder, GetUInt32(builder.Capacity), requiredSize)
            End If

            If Not setupRetVal Then
                rc = GetLastError(False)
                errorMessage = String.Format("Error reading INF line text in file '{0}',{2}Message = ""{1}""", FilePath, New System.ComponentModel.Win32Exception(GetInt32(rc)).Message, CRLF)
                line = Nothing
            Else
                rc = 0
                errorMessage = Nothing
                line = builder.ToString()
            End If

            Return rc
        End Function

        Public Overrides Function ToString() As String
            Return String.Format("Section = {0}, FilePath = {1}", Section, FilePath)
        End Function

    End Class

    Public Module NativeMethods
        Friend ReadOnly INVALID_HANDLE As IntPtr = New IntPtr(-1)

        Friend Const INF_STYLE_OLDNT As UInt32 = &H1UI
        Friend Const INF_STYLE_WIN4 As UInt32 = &H2UI

        <StructLayout(LayoutKind.Sequential)>
        Public Structure INFCONTEXT
            Dim Inf As IntPtr
            Dim CurrentInf As IntPtr
            Dim Section As UInt32
            Dim Line As UInt32
        End Structure

        <DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Public Function SetupGetLineText(
            <[In]()> ByRef context As INFCONTEXT,
            <[In]()> ByVal InfHandle As IntPtr,
            <[In]()> ByVal Section As String,
            <[In]()> ByVal [Key] As String,
            <[In](), [Out]()> ByVal ReturnBuffer As StringBuilder,
            <[In]()> ByVal ReturnBufferSize As UInt32,
            <[In](), [Out]()> ByRef RequiredSize As UInt32) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function

        <DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Public Function SetupGetStringField(
            <[In]()> ByRef context As INFCONTEXT,
            <[In]()> ByVal fieldIndex As Int32,
            <[In](), [Out]()> ByVal ReturnBuffer As StringBuilder,
            <[In]()> ByVal ReturnBufferSize As Int32,
            <[Out]()> ByRef RequiredSize As UInt32) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function

        <DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Public Function SetupOpenInfFile(
            <[In]()> <MarshalAs(UnmanagedType.LPWStr)> ByVal FileName As String,
            <[In]()> <MarshalAs(UnmanagedType.LPWStr)> ByVal InfClass As String,
            <[In]()> ByVal InfStyle As UInt32,
            <[In]()> ByRef ErrorLine As UInt32) As IntPtr
        End Function

        <DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Public Sub SetupCloseInfFile(
            <[In]()> ByVal InfHandle As IntPtr)
        End Sub

        <DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Public Function SetupFindFirstLine(
            <[In]()> ByVal InfHandle As IntPtr,
            <[In]()> ByVal Section As String,
            <[In]()> ByVal Key As String,
            <[In](), [Out]()> ByRef context As INFCONTEXT) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function

        <DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Public Function SetupFindNextMatchLine(
            <[In]()> ByRef contextIn As INFCONTEXT,
            <[In]()> ByVal key As String, <MarshalAs(UnmanagedType.Struct)>
            <[In](), [Out]()> ByRef contextOut As INFCONTEXT) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function

    End Module

End Namespace