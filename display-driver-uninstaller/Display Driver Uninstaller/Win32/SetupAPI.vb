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

Namespace Win32
	<ComVisible(False)>
	Friend Module Win32Native
		Friend Const LINE_LEN As Int32 = 256
		Friend Const MAX_LEN As Int32 = 260

		Friend ReadOnly CRLF As String = Environment.NewLine
		Friend ReadOnly DefaultCharSize As Int32 = Marshal.SystemDefaultCharSize
		Friend ReadOnly DefaultCharSizeU As UInt32 = GetUInt32(DefaultCharSize)
		Friend ReadOnly NullChar() As Char = New Char() {CChar(vbNullChar)}

#Region "Errors"
		Private Const APPLICATION_ERROR_MASK = &H20000000UI
		Private Const ERROR_SEVERITY_ERROR = &HC0000000UI

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

			NOT_ALL_ASSIGNED = &H514
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

	<ComVisible(False)>
	Partial Public Class SetupAPI
		Private Shared ReadOnly Is64 As Boolean = False
		Private Shared ReadOnly IsAdmin As Boolean = False

		Shared Sub New()
			Is64 = WinAPI.Is64
			IsAdmin = WinAPI.IsAdmin
		End Sub


#Region "Enums"
		<Flags()>
		Private Enum INSTALLFLAG As UInteger
			''' <summary>Not used.</summary>
			NULL = &H0UI

			''' <summary>If this flag is set and the function finds a device that matches the HardwareId value, 
			''' the function installs new drivers for the device whether better drivers already exist on the computer.
			''' 
			''' Important  Use this flag only with extreme caution. Setting this flag can cause an older driver to be installed over a newer driver,
			''' if a user runs the vendor's application after newer drivers are available.</summary>
			FORCE = &H1UI

			''' <summary>
			''' If this flag is set, the function will not copy, rename, or delete any installation files.
			''' Use of this flag should be limited to environments in which file access is restricted or impossible, such as an "embedded" operating system.
			''' </summary>
			[READONLY] = &H2UI

			''' <summary>
			''' If this flag is set, the function will return FALSE when any attempt to display UI is detected.
			''' Set this flag only if the function will be called from a component (such as a service) that cannot display UI. 
			''' 
			''' If this flag is set and a UI display is attempted, the device can be left in an indeterminate state.
			''' </summary>
			NONINTERACTIVE = &H4UI
		End Enum

		<Flags()>
		Private Enum DEVICE_INSTALL_STATE As UInteger
			''' <summary>The device is installed.</summary>
			Installed = &H0UI

			''' <summary>The system will try to reinstall the device on a later enumeration.</summary>
			NeedsReinstall = &H1UI

			''' <summary>The device did not install properly.</summary>
			FailedInstall = &H2UI

			''' <summary>The installation of this device is not yet complete.</summary>
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
			''' <summary>Not used.</summary>
			NODRIVER = &H0UI

			''' <summary>Enumerate a class driver list. 
			''' This driver list type must be specified if DeviceInfoData is not specified.</summary>
			CLASSDRIVER = &H1UI

			''' <summary>Enumerate a list of compatible drivers for the specified device. 
			''' This driver list type can be specified only if DeviceInfoData is also specified.</summary>
			COMPATDRIVER = &H2UI
		End Enum

		''' <summary>The SetupUninstallOEMInf function first checks whether there are any devices installed using the .inf file.
		''' A device does not need to be present to be detected as using the .inf file.</summary>
		<Flags()>
		Private Enum SetupUOInfFlags As UInteger
			''' <summary>If this flag is set and the function finds a currently installed device that was installed 
			''' using this .inf file, the .inf file is not removed.</summary>
			NONE = &H0UI

			''' <summary>If this flag is set, the .inf file is removed whether the function finds a device that was installed with this .inf file.</summary>
			SUOI_FORCEDELETE = &H1UI
		End Enum

		<Flags()>
		Private Enum DIGCF As UInteger
			''' <summary>Return only the device that is associated with the system default device interface,
			''' if one is set, for the specified device interface classes.</summary>
			[DEFAULT] = &H1UI

			''' <summary>Return only devices that are currently present in a system.</summary>
			PRESENT = &H2UI

			''' <summary>Return a list of installed devices for all device setup classes or all device interface classes.</summary>
			ALLCLASSES = &H4UI

			''' <summary>Return only devices that are a part of the current hardware profile.</summary>
			PROFILE = &H8UI

			''' <summary>Return devices that support device interfaces for the specified device interface classes.
			''' This flag must be set in the Flags parameter if the Enumerator parameter specifies a device instance ID.</summary>
			DEVICEINTERFACE = &H10UI
		End Enum

		<Flags()>
		Private Enum SPDRP As UInteger
			''' <summary>The function retrieves a REG_SZ string that contains the description of a device. </summary>
			DEVICEDESC = &H0UI

			''' <summary>The function retrieves a REG_MULTI_SZ string that contains the list of hardware IDs for a device.
			''' For information about hardware IDs, see Device Identification Strings.</summary>
			HARDWAREID = &H1UI

			''' <summary>The function retrieves a REG_MULTI_SZ string that contains the list of compatible IDs for a device.
			''' For information about compatible IDs, see Device Identification Strings.</summary>
			COMPATIBLEIDS = &H2UI

			''' <summary>unused</summary>
			UNUSED0 = &H3UI

			''' <summary>The function retrieves a REG_SZ string that contains the service name for a device.</summary>
			SERVICE = &H4UI

			''' <summary>unused</summary>
			UNUSED1 = &H5UI

			''' <summary>unused</summary>
			UNUSED2 = &H6UI

			''' <summary>The function retrieves a REG_SZ string that contains the device setup class of a device.</summary>
			[CLASS] = &H7UI

			''' <summary>The function retrieves a REG_SZ string that contains the GUID that represents the device setup class of a device.</summary>
			CLASSGUID = &H8UI

			''' <summary>The function retrieves a string that identifies the device's software key (sometimes called the driver key).
			''' For more information about driver keys, see Registry Trees and Keys for Devices and Drivers.</summary>
			DRIVER = &H9UI

			''' <summary>The function retrieves a bitwise OR of a device's configuration flags in a DWORD value.
			''' The configuration flags are represented by the CONFIGFLAG_Xxx bitmasks that are defined in Regstr.h.</summary>
			CONFIGFLAGS = &HAUI

			''' <summary>The function retrieves a REG_SZ string that contains the name of the device manufacturer.</summary>
			MFG = &HBUI

			''' <summary>The function retrieves a REG_SZ string that contains the friendly name of a device.</summary>
			FRIENDLYNAME = &HCUI

			''' <summary>The function retrieves a REG_SZ string that contains the hardware location of a device.</summary>
			LOCATION_INFORMATION = &HDUI

			''' <summary>The function retrieves a REG_SZ string that contains the name that is associated with the device's PDO. 
			''' For more information, see IoCreateDevice.</summary>
			PHYSICAL_DEVICE_OBJECT_NAME = &HEUI

			''' <summary>The function retrieves a bitwise OR of the following CM_DEVCAP_Xxx flags in a DWORD.
			''' The device capabilities that are represented by these flags correspond to the device capabilities 
			''' that are represented by the members of the DEVICE_CAPABILITIES structure. 
			''' 
			''' The CM_DEVCAP_Xxx constants are defined in Cfgmgr32.h.</summary>
			CAPABILITIES = &HFUI

			''' <summary>The function retrieves a DWORD value set to the value of the UINumber member of the device's DEVICE_CAPABILITIES structure.</summary>
			UI_NUMBER = &H10UI

			''' <summary>The function retrieves a REG_MULTI_SZ string that contains the names of a device's upper filter drivers.</summary>
			UPPERFILTERS = &H11UI

			''' <summary>The function retrieves a REG_MULTI_SZ string that contains the names of a device's lower-filter drivers.</summary>
			LOWERFILTERS = &H12UI

			''' <summary>The function retrieves the GUID for the device's bus type.</summary>
			BUSTYPEGUID = &H13UI

			''' <summary>The function retrieves the device's legacy bus type as an INTERFACE_TYPE value (defined in Wdm.h and Ntddk.h).</summary>
			LEGACYBUSTYPE = &H14UI

			''' <summary>The function retrieves the device's bus number.</summary>
			BUSNUMBER = &H15UI

			''' <summary>The function retrieves a REG_SZ string that contains the name of the device's enumerator.</summary>
			ENUMERATOR_NAME = &H16UI

			''' <summary>The function retrieves a SECURITY_DESCRIPTOR structure for a device.</summary>
			SECURITY = &H17UI

			''' <summary>The function retrieves a REG_SZ string that contains the device's security descriptor.
			''' For information about security descriptor strings, see Security Descriptor Definition Language (Windows).
			''' For information about the format of security descriptor strings, see Security Descriptor Definition Language (Windows).</summary>
			SECURITY_SDS = &H18UI

			''' <summary>The function retrieves a DWORD value that represents the device's type. 
			''' For more information, see Specifying Device Types.</summary>
			DEVTYPE = &H19UI

			''' <summary>The function retrieves a DWORD value that indicates whether a user can obtain exclusive use of the device.
			''' The returned value is one if exclusive use is allowed, or zero otherwise. For more information, see IoCreateDevice.</summary>
			EXCLUSIVE = &H1AUI

			''' <summary>The function retrieves a bitwise OR of a device's characteristics flags in a DWORD.
			''' For a description of these flags, which are defined in Wdm.h and Ntddk.h,
			''' see the DeviceCharacteristics parameter of the IoCreateDevice function.</summary>
			CHARACTERISTICS = &H1BUI

			''' <summary>The function retrieves the device's address.</summary>
			ADDRESS = &H1CUI

			''' <summary>The function retrieves a format string (REG_SZ) used to display the UINumber value.</summary>
			UI_NUMBER_DESC_FORMAT = &H1DUI

			''' <summary>(Windows XP and later) The function retrieves a CM_POWER_DATA structure that contains the device's power management information.</summary>
			DEVICE_POWER_DATA = &H1EUI

			''' <summary>(Windows XP and later) The function retrieves the device's current removal policy as a DWORD
			''' that contains one of the CM_REMOVAL_POLICY_Xxx values that are defined in Cfgmgr32.h.</summary>
			REMOVAL_POLICY = &H1FUI

			''' <summary>(Windows XP and later) The function retrieves the device's hardware-specified default removal policy as a DWORD
			''' that contains one of the CM_REMOVAL_POLICY_Xxx values that are defined in Cfgmgr32.h.</summary>
			REMOVAL_POLICY_HW_DEFAULT = &H20UI

			''' <summary>(Windows XP and later) The function retrieves the device's override removal policy (if it exists) from the registry,
			''' as a DWORD that contains one of the CM_REMOVAL_POLICY_Xxx values that are defined in Cfgmgr32.h.</summary>
			REMOVAL_POLICY_OVERRIDE = &H21UI

			''' <summary>(Windows XP and later) The function retrieves a DWORD value that indicates the installation state of a device.
			''' The installation state is represented by one of the CM_INSTALL_STATE_Xxx values that are defined in Cfgmgr32.h.
			''' The CM_INSTALL_STATE_Xxx values correspond to the DEVICE_INSTALL_STATE enumeration values. </summary>
			INSTALL_STATE = &H22UI

			''' <summary>(Windows Server 2003 and later) The function retrieves a REG_MULTI_SZ string that represents the location of the device in the device tree.</summary>
			LOCATION_PATHS = &H23UI
		End Enum

		<Flags()>
		Private Enum DIF As UInteger
			''' <summary>A DIF_SELECTDEVICE request allows an installer to participate in selecting the driver for a device.</summary>
			SELECTDEVICE = &H1UI

			''' <summary>A DIF_INSTALLDEVICE request allows an installer to perform tasks before and/or after the device is installed.</summary>
			INSTALLDEVICE = &H2UI

			''' <summary>This DIF code is reserved for system use. Vendor-supplied installers must not handle this request. </summary>
			ASSIGNRESOURCES = &H3UI

			''' <summary>This DIF code is obsolete and no longer supported in Microsoft Windows 2000 and later versions of Windows.
			''' To supply custom property pages for a device, an installer handles the DIF_ADDPROPERTYPAGE_ADVANCED request.</summary>
			PROPERTIES = &H4UI

			''' <summary>A DIF_REMOVE request notifies an installer that Windows is about to remove a device and gives the installer an opportunity to prepare for the removal.</summary>
			REMOVE = &H5UI

			''' <summary>This DIF code is reserved for system use. Vendor-supplied installers must not handle this request.</summary>
			FIRSTTIMESETUP = &H6UI

			''' <summary>Not used.</summary>
			FOUNDDEVICE = &H7UI

			''' <summary>This DIF code is obsolete and no longer supported in Microsoft Windows 2000 and later versions of Windows.</summary>
			SELECTCLASSDRIVERS = &H8UI

			''' <summary>This DIF code is obsolete and no longer supported in Microsoft Windows 2000 and later versions of Windows.</summary>
			VALIDATECLASSDRIVERS = &H9UI

			''' <summary>This DIF code is reserved for system use. Vendor-supplied installers must not handle this request.</summary>
			INSTALLCLASSDRIVERS = &HAUI

			''' <summary>This DIF code is reserved for system use. Vendor-supplied installers must not handle this request.</summary>
			CALCDISKSPACE = &HBUI

			''' <summary>A DIF_DESTROYPRIVATEDATA request directs a class installer to free any memory or resources it allocated and
			''' stored in the ClassInstallReserved field of the SP_DEVINSTALL_PARAMS structure.</summary>
			DESTROYPRIVATEDATA = &HCUI

			''' <summary>his DIF code is obsolete and no longer supported in Microsoft Windows 2000 and later versions of Windows.</summary>
			VALIDATEDRIVER = &HDUI

			''' <summary>A DIF_DETECT request directs an installer to detect non-PnP devices of a particular class
			''' and add the devices to the device information set. This request is used for non-PnP devices.</summary>
			DETECT = &HFUI

			''' <summary>This DIF code is obsolete and no longer supported in Microsoft Windows 2000 and later versions of Windows.
			''' For PnP devices, Windows uses the DIF_NEWDEVICEWIZARD_XXX requests instead, such as DIF_NEWDEVICEWIZARD_FINISHINSTALL.</summary>
			INSTALLWIZARD = &H10UI

			''' <summary>This DIF code is obsolete and no longer supported in Microsoft Windows 2000 and later versions of Windows.
			''' Windows uses the DIF_NEWDEVICEWIZARD_XXX requests instead, such as DIF_NEWDEVICEWIZARD_FINISHINSTALL.</summary>
			DESTROYWIZARDDATA = &H11UI

			''' <summary>A DIF_PROPERTYCHANGE request notifies the installer that the device's properties are changing.
			''' The device is being enabled, disabled, started, stopped, or some item on a property page has changed.
			''' This DIF request gives the installer an opportunity to participate in the change.</summary>
			PROPERTYCHANGE = &H12UI

			''' <summary>This DIF code is reserved for system use. Vendor-supplied installers must not handle this request.</summary>
			ENABLECLASS = &H13UI

			''' <summary>This DIF code is reserved for system use. Vendor-supplied installers must not handle this request.</summary>
			DETECTVERIFY = &H14UI

			''' <summary>A DIF_INSTALLDEVICEFILES request allows an installer to participate in copying the files to support a device
			''' or to make a list of the files for a device. The device files include files for the selected driver,
			''' any device interfaces, and any co-installers.</summary>
			INSTALLDEVICEFILES = &H15UI

			''' <summary>A DIF_UNREMOVE request notifies the installer that Windows is about to reinstate a device in a given hardware profile
			''' and gives the installer an opportunity to participate in the operation. Windows only sends this request for non-PnP devices.</summary>
			UNREMOVE = &H16UI

			''' <summary>A DIF_SELECTBESTCOMPATDRV request allows an installer to select the best driver from the device information element's compatible driver list.</summary>
			SELECTBESTCOMPATDRV = &H17UI

			''' <summary>A DIF_ALLOW_INSTALL request asks the installers for a device whether Windows can proceed to install the device.</summary>
			ALLOW_INSTALL = &H18UI

			''' <summary>The DIF_REGISTERDEVICE request allows an installer to participate in registering a newly created device instance with the PnP manager.
			''' Windows sends this DIF request for non-PnP devices.</summary>
			REGISTERDEVICE = &H19UI

			''' <summary>A DIF_NEWDEVICEWIZARD_PRESELECT request allows an installer to supply wizard pages that Windows displays to the user before it
			''' displays the select-driver page. This request is only used during manual installation of non-PnP devices.</summary>
			NEWDEVICEWIZARD_PRESELECT = &H1AUI

			''' <summary>A DIF_NEWDEVICEWIZARD_SELECT request allows an installer to supply custom wizard page(s) that replace the standard select-driver page.
			''' This request is only used during manual installation of non-PnP devices.</summary>
			NEWDEVICEWIZARD_SELECT = &H1BUI

			''' <summary>A DIF_NEWDEVICEWIZARD_PREANALYZE request allows an installer to supply wizard pages that Windows displays to the user before it
			''' displays the analyze page. This request is only used during manual installation of non-PnP devices.</summary>
			NEWDEVICEWIZARD_PREANALYZE = &H1CUI

			''' <summary>A DIF_NEWDEVICEWIZARD_POSTANALYZE request allows an installer to supply wizard pages that Windows displays to
			''' the user after the device node (devnode) is registered but before Windows installs the drivers for the device. 
			''' This request is only used during manual installation of non-PnP devices.</summary>
			NEWDEVICEWIZARD_POSTANALYZE = &H1DUI

			''' <summary>A DIF_NEWDEVICEWIZARD_FINISHINSTALL request allows an installer to supply finish-install wizard pages that Windows displays
			''' to the user after a device is installed but before Windows displays the standard finish page. Windows sends this request when it 
			''' installs Plug and Play (PnP) devices and when an administrator uses the Add Hardware Wizard to install non-PnP devices.</summary>
			NEWDEVICEWIZARD_FINISHINSTALL = &H1EUI

			''' <summary>Not used.</summary>
			UNUSED1 = &H1FUI

			''' <summary>A DIF_INSTALLINTERFACES request allows an installer to participate in the registration of the device interfaces for a device.</summary>
			INSTALLINTERFACES = &H20UI

			''' <summary>This DIF code is reserved for system use. Vendor-supplied installers must not handle this request.</summary>
			DETECTCANCEL = &H21UI

			''' <summary>A DIF_REGISTER_COINSTALLERS request allows an installer to participate in the registration of device co-installers.</summary>
			REGISTER_COINSTALLERS = &H22UI

			''' <summary>A DIF_ADDPROPERTYPAGE_ADVANCED request allows an installer to supply one or more custom property pages for a device.</summary>
			ADDPROPERTYPAGE_ADVANCED = &H23UI

			''' <summary>This DIF code is reserved for system use. Vendor-supplied installers must not handle this request.</summary>
			ADDPROPERTYPAGE_BASIC = &H24UI

			''' <summary>Not used.</summary>
			RESERVED1 = &H25UI

			''' <summary>The DIF_TROUBLESHOOTER request allows an installer to start a troubleshooter for a device
			''' or to return CHM and HTM troubleshooter files for Windows to start.</summary>
			TROUBLESHOOTER = &H26UI

			''' <summary>A DIF_POWERMESSAGEWAKE request allows an installer to supply custom text that
			''' Windows displays on the power management properties page of the device properties.</summary>
			POWERMESSAGEWAKE = &H27UI

			''' <summary>A DIF_ADDPROPERTYPAGE_ADVANCED request allows an installer to supply one or more custom property pages for a device.</summary>
			ADDREMOTEPROPERTYPAGE_ADVANCED = &H28UI

			''' <summary>This DIF code is reserved for system use. Vendor-supplied installers must not handle this request. </summary>
			UPDATEDRIVER_UI = &H29UI

			''' <summary>A DIF_FINISHINSTALL_ACTION request allows an installer to run finish-install actions in an
			''' interactive administrator context after all other device installation operations have completed.</summary>
			FINISHINSTALL_ACTION = &H2AUI

			''' <summary>Not used.</summary>
			RESERVED2 = &H30UI
		End Enum

		<Flags()>
		Private Enum DICS As UInteger
			''' <summary>The device is being enabled.
			''' 
			''' For this state change, Windows enables the device if the DICS_FLAG_GLOBAL flag is specified.
			''' 
			''' If the DICS_FLAG_CONFIGSPECIFIC flag is specified and the current hardware profile is specified then Windows enables the device.
			''' If the DICS_FLAG_CONFIGSPECIFIC is specified and not the current hardware profile then Windows sets some flags in the registry
			''' and does not change the device's state. Windows will change the device state when the specified profile becomes the current profile.
			''' </summary>
			ENABLE = &H1UI

			''' <summary>The device is being disabled.
			''' 
			''' For this state change, Windows disables the device if the DICS_FLAG_GLOBAL flag is specified.
			''' 
			''' If the DICS_FLAG_CONFIGSPECIFIC flag is specified and the current hardware profile is specified then Windows disables the device.
			''' If the DICS_FLAG_CONFIGSPECIFIC is specified and not the current hardware profile then Windows sets some flags in the registry
			''' and does not change the device's state.</summary>
			DISABLE = &H2UI

			''' <summary>The properties of the device have changed.
			''' 
			''' For this state change, Windows ignores the Scope information as long it is a valid value, and stops and restarts the device.</summary>
			PROPCHANGE = &H3UI

			''' <summary>
			''' 
			''' The device is being started (if the request is for the currently active hardware profile).
			''' 
			''' DICS_START must be DICS_FLAG_CONFIGSPECIFIC. You cannot perform that change globally.
			''' 
			''' Windows only starts the device if the current hardware profile is specified. Otherwise,
			''' Windows sets a registry flag and does not change the state of the device.</summary>
			START = &H4UI

			''' <summary>The device is being stopped. 
			''' The driver stack will be unloaded and the CSCONFIGFLAG_DO_NOT_START flag will be set for the device.
			'''
			''' DICS_STOP must be DICS_FLAG_CONFIGSPECIFIC. You cannot perform that change globally.
			'''
			''' Windows only stops the device if the current hardware profile is specified. 
			''' Otherwise, Windows sets a registry flag and does not change the state of the device.</summary>
			[STOP] = &H5UI
		End Enum

		<Flags()>
		Private Enum DI As UInteger
			REMOVEDEVICE_GLOBAL = &H1UI
			REMOVEDEVICE_CONFIGSPECIFIC = &H2UI
			UNREMOVEDEVICE_CONFIGSPECIFIC = &H2UI

			''' <summary>Set to allow support for OEM disks. If this flag is set, the operating system presents a "Have Disk"
			''' button on the Select Device page. This flag is set, by default, in system-supplied wizards.</summary>
			SHOWOEM = &H1UI

			''' <summary>Reserved.</summary>
			SHOWCOMPAT = &H2UI

			''' <summary>Reserved.</summary>
			SHOWCLASS = &H4UI

			''' <summary>Reserved.</summary>
			SHOWALL = &H7UI

			''' <summary>Set to disable creation of a new copy queue. 
			''' Use the caller-supplied copy queue in SP_DEVINSTALL_PARAMS.FileQueue.</summary>
			NOVCP = &H8UI

			''' <summary>Set if SetupDiBuildDriverInfoList has already built a list of compatible drivers for this device.
			''' If this list has already been built, it contains all the driver information and this flag is always set.
			''' SetupDiDestroyDriverInfoList clears this flag when it deletes a compatible driver list.
			''' 
			''' This flag is only set in device installation parameters that are associated with a particular device information element,
			''' not in parameters for a device information set as a whole.
			''' 
			''' This flag is read-only. Only the operating system sets this flag.</summary>
			DIDCOMPAT = &H10UI

			''' <summary>Set if SetupDiBuildDriverInfoList has already built a list of the drivers for this class of device.
			''' If this list has already been built, it contains all the driver information and this flag is always set.
			''' SetupDiDestroyDriverInfoList clears this flag when it deletes a list of drivers for a class.
			''' 
			''' This flag is read-only. Only the operating system sets this flag.</summary>
			DIDCLASS = &H20UI

			''' <summary>Reserved.</summary>
			AUTOASSIGNRES = &H40UI

			''' <summary>The same as DI_NEEDREBOOT.</summary>
			NEEDRESTART = &H80UI

			''' <summary>For NT-based operating systems, this flag is set if the device requires that the computer be restarted after
			''' device installation or a device state change. A class installer or co-installer can set this flag at any time during
			''' device installation, if the installer determines that a restart is necessary.</summary>
			NEEDREBOOT = &H100UI

			''' <summary>Set to disable browsing when the user is selecting an OEM disk path. 
			''' A device installation application sets this flag to constrain a user to only installing from the installation media location.</summary>
			NOBROWSE = &H200UI

			''' <summary> Set by SetupDiBuildDriverInfoList if a list of drivers for a device setup class contains drivers that are provided by multiple manufacturers.
			'''
			''' This flag is read-only. Only the operating system sets this flag.</summary>
			MULTMFGS = &H400UI

			''' <summary>Reserved.</summary>
			DISABLED = &H800UI

			''' <summary>Reserved.</summary>
			GENERALPAGE_ADDED = &H1000UI

			''' <summary>Set by a class installer or co-installer if the installer supplies a page that replaces the system-supplied resource properties page. 
			''' If this flag is set, the operating system does not display the system-supplied resource page.</summary>
			RESOURCEPAGE_ADDED = &H2000UI

			''' <summary>Set by Device Manager if a device's properties were changed, which requires an update of the installer's user interface.</summary>
			PROPERTIES_CHANGE = &H4000UI

			''' <summary>Set to indicate that the Select Device page should list drivers in the order in which 
			''' they appear in the INF file, instead of sorting them alphabetically. </summary>
			INF_IS_SORTED = &H8000UI

			''' <summary>Set if installers and other device installation components should only search the INF file specified by SP_DEVINSTALL_PARAMS.DriverPath.
			''' If this flag is set, DriverPath contains the path of a single INF file instead of a path of a directory.</summary>
			ENUMSINGLEINF = &H10000UI

			''' <summary>Set if the configuration manager should not be called to remove or reenumerate devices during the execution of certain device
			''' installation functions (for example, SetupDiInstallDevice).
			''' 
			''' If this flag is set, device installation applications, class installers, and co-installers must not call the following functions:
			''' CM_Reenumerate_DevNode(_Ex)
			''' CM_Query_And_Remove_SubTree(_Ex)
			''' CM_Setup_DevNode(_Ex)
			''' CM_Set_HW_Prof_Flags(_Ex)
			''' CM_Enable_DevNode(_Ex)
			''' CM_Disable_DevNode(_Ex)</summary>
			DONOTCALLCONFIGMG = &H20000UI

			''' <summary>Set if the device should be installed in a disabled state by default. 
			''' To be recognized, this flag must be set before Windows calls the default handler for the DIF_INSTALLDEVICE request.</summary>
			INSTALLDISABLED = &H40000UI

			''' <summary>Set to force SetupDiBuildDriverInfoList to build a device's list of compatible drivers
			''' from its class driver list instead of the INF file.</summary>
			COMPAT_FROM_CLASS = &H80000UI

			''' <summary>Set to use the Class Install parameters. SetupDiSetClassInstallParams sets this flag when the caller
			''' specifies parameters and clears the flag when the caller specifies a NULL parameters pointer. </summary>
			CLASSINSTALLPARAMS = &H100000UI

			NODEFAULTACTION = &H200000UI

			''' <summary>Set if the device installer functions must be silent and use default choices wherever possible.
			''' Class installers and co-installers must not display any UI if this flag is set.</summary>
			QUIETINSTALL = &H800000UI

			''' <summary>Set if device installation applications and components, such as SetupDiInstallDevice, should skip file copying.</summary>
			NOFILECOPY = &H1000000UI

			FORCECOPY = &H2000000UI

			''' <summary>Set by a class installer or co-installer if the installer supplies a page that replaces the system-supplied driver properties page.
			''' If this flag is set, the operating system does not display the system-supplied driver page.</summary>
			DRIVERPAGE_ADDED = &H4000000UI

			''' <summary>Set if a class installer or co-installer supplied strings that should be used during SetupDiSelectDevice.</summary>
			USECI_SELECTSTRINGS = &H8000000UI

			''' <summary>Reserved.</summary>
			OVERRIDE_INFFLAGS = &H10000000UI

			''' <summary>Obsolete.</summary>
			PROPS_NOCHANGEUSAGE = &H20000000UI

			''' <summary>Obsolete.</summary>
			NOSELECTICONS = &H40000000UI

			''' <summary>Set to prevent SetupDiInstallDevice from writing the INF-specified hardware IDs and compatible IDs to the device
			''' properties for the device node (devnode). This flag should only be set for root-enumerated devices.
			'''
			''' This flag overrides the DI_FLAGSEX_ALWAYSWRITEIDS flag.</summary>
			NOWRITE_IDS = &H80000000UI
		End Enum

		<Flags()>
		Private Enum DI_FLAGSEX As UInteger
			''' <summary>Reserved.</summary>
			RESERVED2 = &H1UI

			''' <summary>Reserved.</summary>
			RESERVED3 = &H2UI

			''' <summary>Set by the operating system if a class installer failed to load or start. This flag is read-only.</summary>
			CI_FAILED = &H4UI

			FINISHINSTALL_ACTION = &H8UI

			''' <summary>Windows has built a list of driver nodes that includes all the drivers that are listed in the INF files of the specified setup class. 
			''' If the specified setup class is NULL because the HDEVINFO set or device has no associated class,
			''' the list includes all driver nodes from all available INF files. This flag is read-only.</summary>
			DIDINFOLIST = &H10UI

			''' <summary>Windows has built a list of driver nodes that are compatible with the device. This flag is read-only.</summary>
			DIDCOMPATINFO = &H20UI

			''' <summary>If set, SetupDiBuildClassInfoList will check for class inclusion filters.
			''' This means that a device will not be included in the class list if its class is marked as NoInstallClass.</summary>
			FILTERCLASSES = &H40UI

			''' <summary>Set if the installation failed. If this flag is set, the SetupDiInstallDevice function just sets the FAILEDINSTALL flag
			''' in the device's ConfigFlags registry value. If DI_FLAGSEX_SETFAILEDINSTALL is set, co-installers must return NO_ERROR in
			''' response to DIF_INSTALLDEVICE, while class installers must return NO_ERROR or ERROR_DI_DO_DEFAULT.</summary>
			SETFAILEDINSTALL = &H80UI

			DEVICECHANGE = &H100UI

			''' <summary>If set and the DI_NOWRITE_IDS flag is clear, always write hardware and compatible IDs to the device properties for the devnode.
			''' This flag should only be set for root-enumerated devices.</summary>
			ALWAYSWRITEIDS = &H200UI

			''' <summary>If set, the user made changes to one or more device property sheets. The property-page provider typically sets this flag.
			'''
			''' When the user closes the device property sheet, Device Manager checks the DI_FLAGSEX_PROPCHANGE_PENDING flag. 
			''' If it is set, Device Manager clears this flag, sets the DI_PROPERTIES_CHANGE flag, and sends a DIF_PROPERTYCHANGE request
			''' to the installers to notify them that something has changed.</summary>
			PROPCHANGE_PENDING = &H400UI

			''' <summary>If set, include drivers that were marked "Exclude From Select."
			'''
			''' For example, if this flag is set, SetupDiSelectDevice displays drivers that have the Exclude From Select state
			''' and SetupDiBuildDriverInfoList includes Exclude From Select drivers in the requested driver list.
			'''
			''' A driver is "Exclude From Select" if either it is marked ExcludeFromSelect in the INF file or it is a driver for a device whose 
			''' whole setup class is marked NoInstallClass or NoUseClass in the class installer INF. Drivers for PnP devices are typically "Exclude From Select";
			''' PnP devices should not be manually installed. To build a list of driver files for a PnP device a caller of SetupDiBuildDriverInfoList must set this flag.</summary>
			ALLOWEXCLUDEDDRVS = &H800UI

			''' <summary>Obsolete.</summary>
			NOUIONQUERYREMOVE = &H1000UI

			''' <summary>Filter INF files on the device's setup class when building a list of compatible drivers. 
			''' If a device's setup class is known, setting this flag reduces the time that is required to build a
			''' list of compatible drivers when searching INF files that are not precompiled. 
			''' 
			''' This flag is ignored if DI_COMPAT_FROM_CLASS is set.</summary>
			USECLASSFORCOMPAT = &H2000UI

			''' <summary>Reserved.</summary>
			RESERVED4 = &H4000UI

			''' <summary>Do not process the AddReg and DelReg entries for the device's hardware and software (driver) keys. 
			''' That is, the AddReg and DelReg entries in the INF file DDInstall and DDInstall.HW sections.</summary>
			NO_DRVREG_MODIFY = &H8000UI

			''' <summary>If set, installation is occurring during initial system setup. This flag is read-only.</summary>
			IN_SYSTEM_SETUP = &H10000UI

			''' <summary>If set, the driver was obtained from the Internet. Windows will not use the device's INF to install future devices
			''' because Windows cannot guarantee that it can retrieve the driver files again from the Internet.</summary>
			INET_DRIVER = &H20000UI

			''' <summary>If set, SetupDiBuildDriverInfoList appends a new driver list to an existing list.
			''' This flag is relevant when searching multiple locations.</summary>
			APPENDDRIVERLIST = &H40000UI

			''' <summary>Reserved.</summary>
			PREINSTALLBACKUP = &H80000UI

			''' <summary>Reserved.</summary>
			BACKUPONREPLACE = &H100000UI

			''' <summary>If set, build the driver list from INF(s) retrieved from the URL that is specified in SP_DEVINSTALL_PARAMS.DriverPath.
			''' If the DriverPath is an empty string, use the Windows Update website.
			''' 
			''' Currently, the operating system does not support URLs. 
			''' Use this flag to direct SetupDiBuildDriverInfoList to search the Windows Update website.
			''' 
			''' Do not set this flag if DI_QUIETINSTALL is set.</summary>
			DRIVERLIST_FROM_URL = &H200000UI

			''' <summary>Reserved.</summary>
			RESERVED1 = &H400000UI

			''' <summary>If set, do not include old Internet drivers when building a driver list.
			''' This flag should be set any time that you are building a list of potential drivers for a device.
			''' You can clear this flag if you are just getting a list of drivers currently installed for a device.</summary>
			EXCLUDE_OLD_INET_DRIVERS = &H800000UI

			''' <summary>If set, an installer added their own page for the power properties dialog.
			''' The operating system will not display the system-supplied power properties page.
			''' This flag is only relevant if the device supports power management.</summary>
			POWERPAGE_ADDED = &H1000000UI

			''' <summary>(Windows XP and later.) If set, SetupDiBuildDriverInfoList includes "similar" drivers when building a class driver list.
			''' A "similar" driver is one for which one of the hardware IDs or compatible IDs in the INF file partially (or completely) matches
			''' one of the hardware IDs or compatible IDs of the hardware.</summary>
			FILTERSIMILARDRIVERS = &H2000000UI

			''' <summary>(Windows XP and later.) If set, SetupDiBuildDriverInfoList includes only the currently installed driver
			''' when creating a list of class drivers or device-compatible drivers.</summary>
			INSTALLEDDRIVER = &H4000000UI

			NO_CLASSLIST_NODE_MERGE = &H8000000UI
			ALTPLATFORM_DRVSEARCH = &H10000000UI
			RESTART_DEVICE_ONLY = &H20000000UI
			RECURSIVESEARCH = &H40000000UI
			SEARCH_PUBLISHED_INFS = &H80000000UI
		End Enum

		<Flags()>
		Private Enum DICS_FLAG As UInteger
			''' <summary>Make the change in all hardware profiles.</summary>
			[GLOBAL] = &H1UI

			''' <summary>Make the change in the specified profile only.</summary>
			CONFIGSPECIFIC = &H2UI

			''' <summary>Obsolete. Not used.</summary>
			CONFIGGENERAL = &H4UI
		End Enum

		<Flags()>
		Private Enum CM_REENUMERATE As UInteger
			''' <summary>Reenumeration should occur asynchronously. 
			''' The call to this function returns immediately after the PnP manager receives the reenumeration request. 
			''' If this flag is set, the CM_REENUMERATE_SYNCHRONOUS flag should not also be set.</summary>
			ASYNCHRONOUS = &H0UI

			''' <summary>pecifies default reenumeration behavior, in which reenumeration occurs synchronously. 
			''' This flag is functionally equivalent to CM_REENUMERATE_SYNCHRONOUS.</summary>
			NORMAL = &H1UI

			''' <summary>Specifies that Plug and Play should make another attempt to install any devices in the specified subtree 
			''' that have been detected but are not yet configured, or are marked as needing reinstallation, or for which installation 
			''' must be completed. This flag can be set along with either the CM_REENUMERATE_SYNCHRONOUS flag or the CM_REENUMERATE_ASYNCHRONOUS flag. 
			''' This flag must be used with extreme caution, because it can cause the PnP manager to prompt the user to perform installation of any such devices. 
			''' Currently, only components such as Device Manager and Hardware Wizard use this flag, to allow the user to retry installation of devices that 
			''' might already have been detected but are not currently installed.</summary>
			RETRY_INSTALLATION = &H2UI

			''' <summary>Reenumeration should occur synchronously. 
			''' The call to this function returns when all devices in the specified subtree have been reenumerated. 
			''' If this flag is set, the CM_REENUMERATE_ASYNCHRONOUS flag should not also be set. This flag is functionally equivalent to CM_REENUMERATE_NORMAL.</summary>
			SYNCHRONOUS = &H4UI
		End Enum

		<Flags()>
		Private Enum CM_LOCATE As UInteger
			''' <summary>The function retrieves the device instance handle for the specified device only if the device is currently configured in the device tree.</summary>
			DEVNODE_NORMAL = &H0UI

			''' <summary>The function retrieves a device instance handle for the specified device if the device is currently configured in the device tree 
			''' or the device is a nonpresent device that is not currently configured in the device tree.</summary>
			DEVNODE_PHANTOM = &H1UI

			''' <summary>The function retrieves a device instance handle for the specified device if the device is currently configured in the device tree 
			''' or in the process of being removed from the device tree. If the device is in the process of being removed, the function cancels the removal of the device.</summary>
			DEVNODE_CANCELREMOVE = &H2UI

			''' <summary>Not used.</summary>
			DEVNODE_NOVALIDATION = &H4UI
		End Enum

		<Flags()>
		Private Enum CM_DEVCAP As UInteger
			''' <summary>Specifies whether the device supports physical-device locking that prevents device ejection.
			''' This member pertains to ejecting the device from its slot, rather than ejecting a piece of removable media from the device.</summary>
			LOCKSUPPORTED = &H1UI

			''' <summary>Specifies whether the device supports software-controlled device ejection while the system is in the PowerSystemWorking state.
			''' This member pertains to ejecting the device from its slot, rather than ejecting a piece of removable media from the device.</summary>
			EJECTSUPPORTED = &H2UI

			''' <summary>Specifies whether the device can be dynamically removed from its immediate parent.
			''' If Removable is set to TRUE, the device does not belong to the same physical object as its parent.
			''' 
			''' If Removable is set to TRUE, the device is displayed in the Unplug or Eject Hardware program,
			''' unless SurpriseRemovalOK is also set to TRUE.</summary>
			REMOVABLE = &H4UI

			''' <summary>Specifies whether the device is a docking peripheral.</summary>
			DOCKDEVICE = &H8UI

			''' <summary>Specifies whether the device's instance ID is unique system-wide.
			''' This bit is clear if the instance ID is unique only within the scope of the bus. For more information, see Device Identification Strings.</summary>
			UNIQUEID = &H10UI

			''' <summary>Specifies whether Device Manager should suppress all installation dialog boxes;
			''' except required dialog boxes such as "no compatible drivers found."</summary>
			SILENTINSTALL = &H20UI

			''' <summary>Specifies whether the driver for the underlying bus can drive the device if there is no function driver
			''' (for example, SCSI devices in pass-through mode). This mode of operation is called raw mode.</summary>
			RAWDEVICEOK = &H40UI

			''' <summary>Specifies whether the function driver for the device can handle the case where the device is removed before Windows
			''' can send IRP_MN_QUERY_REMOVE_DEVICE to it. If SurpriseRemovalOK is set to TRUE, the device can be safely removed from its
			''' immediate parent regardless of the state that its driver is in.
			'''
			''' For example, a standard USB mouse does not maintain any state in its hardware and thus can be safely removed at any time.
			''' However, an external hard disk whose driver caches writes in memory cannot be safely removed without first letting the driver
			''' flush its cache to the hardware</summary>
			SURPRISEREMOVALOK = &H80UI

			''' <summary>When set, this flag specifies that the device's hardware is disabled.
			''' 
			''' A device's parent bus driver or a bus filter driver sets this flag when such a driver determines that the device hardware is disabled.
			''' The PnP manager sends one IRP_MN_QUERY_CAPABILITIES IRP right after a device is enumerated and sends another after the device has been started.
			''' The PnP manager only checks this bit right after the device is enumerated. Once the device is started, this bit is ignored.</summary>
			HARDWAREDISABLED = &H100UI

			''' <summary>Reserved for future use.</summary>
			NONDYNAMIC = &H200UI
		End Enum

		Private Enum CR As UInteger
			''' <summary>Success.</summary>
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

		Private Enum CM_PROB As UInteger
			''' <summary>There is a device on the system for which there is no ConfigFlags registry entry.
			''' This means no driver is installed. Typically this means an INF file could not be found.</summary>
			NOT_CONFIGURED = &H1UI

			DEVLOADER_FAILED = &H2UI

			''' <summary>Running out of memory − the system is probably running low on system memory. </summary>
			OUT_OF_MEMORY = &H3UI

			ENTRY_IS_WRONG_TYPE = &H4UI
			LACKED_ARBITRATOR = &H5UI
			BOOT_CONFIG_CONFLICT = &H6UI
			FAILED_FILTER = &H7UI
			DEVLOADER_NOT_FOUND = &H8UI

			''' <summary>Invalid device IDs have been detected.</summary>
			INVALID_DATA = &H9UI

			''' <summary>The device failed to start.</summary>
			FAILED_START = &HAUI

			LIAR = &HBUI

			''' <summary>Two devices have been assigned the same I/O ports, the same interrupt,
			''' or the same DMA channel (either by the BIOS, the operating system, or a combination of the two).</summary>
			NORMAL_CONFLICT = &HCUI

			NOT_VERIFIED = &HDUI

			''' <summary>The system must be restarted.</summary>
			NEED_RESTART = &HEUI

			REENUMERATION = &HFUI

			''' <summary>The device is only partially configured.</summary>
			PARTIAL_LOG_CONF = &H10UI

			UNKNOWN_RESOURCE = &H11UI

			''' <summary>Drivers must be reinstalled.</summary>
			REINSTALL = &H12UI

			''' <summary>A registry problem was detected.</summary>
			REGISTRY = &H13UI

			VXDLDR = &H14UI

			''' <summary>The system will remove the device.</summary>
			WILL_BE_REMOVED = &H15UI

			''' <summary>The device is disabled.</summary>
			DISABLED = &H16UI


			DEVLOADER_NOT_READY = &H17UI

			''' <summary>The device does not seem to be present.</summary>
			DEVICE_NOT_THERE = &H18UI

			MOVED = &H19UI
			TOO_EARLY = &H1AUI
			NO_VALID_LOG_CONF = &H1BUI

			''' <summary>The device's drivers are not installed.</summary>
			FAILED_INSTALL = &H1CUI

			''' <summary>The device is disabled.</summary>
			HARDWARE_DISABLED = &H1DUI

			CANT_SHARE_IRQ = &H1EUI

			''' <summary>A driver's attempt to add a device failed.</summary>
			FAILED_ADD = &H1FUI

			''' <summary>The driver has been disabled.</summary>
			DISABLED_SERVICE = &H20UI

			''' <summary>Resource translation failed for the device.</summary>
			TRANSLATION_FAILED = &H21UI

			''' <summary>The device requires a forced configuration.</summary>
			NO_SOFTCONFIG = &H22UI

			''' <summary>The MPS table is bad and has to be updated.</summary>
			BIOS_TABLE = &H23UI

			''' <summary>The IRQ translation failed for the device.</summary>
			IRQ_TRANSLATION_FAILED = &H24UI

			''' <summary>The driver returned failure from its DriverEntry routine.</summary>
			FAILED_DRIVER_ENTRY = &H25UI

			''' <summary>The driver could not be loaded because a previous instance is still loaded.</summary>
			DRIVER_FAILED_PRIOR_UNLOAD = &H26UI

			''' <summary>The driver could not be loaded.</summary>
			DRIVER_FAILED_LOAD = &H27UI

			''' <summary>Information in the registry's service key for the driver is invalid.</summary>
			DRIVER_SERVICE_KEY_INVALID = &H28UI

			''' <summary>A driver was loaded but Windows cannot find the device.</summary>
			LEGACY_SERVICE_NO_DEVICES = &H29UI

			''' <summary>A duplicate device was detected.</summary>
			DUPLICATE_DEVICE = &H2AUI

			''' <summary>A driver has reported a device failure.</summary>
			FAILED_POST_START = &H2BUI

			''' <summary>The device has been stopped.</summary>
			HALTED = &H2CUI

			''' <summary>The device is not present.</summary>
			PHANTOM = &H2DUI

			''' <summary>The device is not available because the system is shutting down.</summary>
			SYSTEM_SHUTDOWN = &H2EUI

			''' <summary>The device has been prepared for ejection.</summary>
			HELD_FOR_EJECT = &H2FUI

			''' <summary>The system will not load the driver because it is listed in the Windows Driver Protection database supplied by Windows Update.</summary>
			DRIVER_BLOCKED = &H30UI

			''' <summary>The registry is too large.</summary>
			REGISTRY_TOO_LARGE = &H31

			''' <summary>Device properties cannot be set.</summary>
			SETPROPERTIES_FAILED = &H32

			''' <summary>The device did not start because it has a dependency on another device that has not started.</summary>
			WAITING_ON_DEPENDENCY = &H33

			''' <summary>The device did not start on a 64-bit version of Windows because it has a driver that is not digitally signed.
			''' For more information about how to sign drivers, see Driver Signing.</summary>
			UNSIGNED_DRIVER = &H34
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
		<DllImport("CfgMgr32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
		Private Shared Function CM_Get_Parent(
 <[Out]()> ByRef pdnDevInst As UInt32,
 <[In]()> ByVal dnDevInst As UInt32,
 <[In]()> ByVal ulFlags As UInt32) As UInt32
		End Function

		<DllImport("CfgMgr32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
		Private Shared Function CM_Get_Child(
   <[Out]()> ByRef pdnDevInst As UInt32,
   <[In]()> ByVal DevInst As UInt32,
   <[In]()> ByVal ulFlags As UInt32) As UInt32
		End Function

		<DllImport("CfgMgr32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
		Private Shared Function CM_Get_Sibling(
   <[Out]()> ByRef pdnDevInst As UInt32,
   <[In]()> ByVal DevInst As UInt32,
   <[In]()> ByVal ulFlags As UInt32) As UInt32
		End Function

		<DllImport("CfgMgr32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
		Private Shared Function CM_Get_Device_ID(
  <[In]()> ByVal dnDevInst As UInt32,
  <[Out](), MarshalAs(UnmanagedType.LPWStr)> ByVal Buffer As StringBuilder,
  <[In]()> ByRef BufferLen As UInt32,
  <[In]()> ByVal ulFlags As UInt32) As UInt32
		End Function

		<DllImport("CfgMgr32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
		Private Shared Function CM_Get_DevNode_Status(
  <[Out]()> ByRef pulStatus As UInt32,
  <[Out]()> ByRef pulProblemNumber As UInt32,
  <[In]()> ByVal dnDevInst As UInt32,
  <[In]()> ByVal ulFlags As UInt32) As UInt32
		End Function

		<DllImport("CfgMgr32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
		Private Shared Function CM_Reenumerate_DevNode(
  <[In]()> ByVal dnDevInst As UInt32,
  <[In]()> ByVal ulFlags As UInt32) As UInt32
		End Function

		<DllImport("CfgMgr32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
		Private Shared Function CM_Locate_DevNode(
   <[Out]()> ByRef dnDevInst As UInt32,
   <[In](), [Optional](), MarshalAs(UnmanagedType.LPWStr)> ByVal pDeviceID As String,
   <[In]()> ByVal ulFlags As UInt32) As UInt32
		End Function



		<DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
		Private Shared Function SetupDiClassGuidsFromName(
   <[In]()> ByVal ClassName As String,
   <[Out]()> ByRef ClassGuidList As Guid,
   <[In]()> ByRef ClassGuidListSize As UInt32,
   <[Out]()> ByRef RequiredSize As UInt32) As <MarshalAs(UnmanagedType.Bool)> Boolean
		End Function

		<DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
		<ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)>
		Private Shared Function SetupDiDestroyDeviceInfoList(
   <[In]()> ByVal DeviceInfoSet As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
		End Function

		<DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
		Private Shared Function SetupDiGetClassDevs(
   <[In](), [Optional]()> ByRef ClassGuid As Guid,
   <[In](), [Optional](), MarshalAs(UnmanagedType.LPWStr)> ByVal Enumerator As String,
   <[In](), [Optional]()> ByVal hwndParent As IntPtr,
   <[In]()> ByVal Flags As UInt32) As SafeDeviceHandle
		End Function

		<DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
		Private Shared Function SetupDiEnumDeviceInfo(
  <[In]()> ByVal DeviceInfoSet As SafeDeviceHandle,
  <[In]()> ByVal MemberIndex As UInt32,
  <[Out]()> ByVal DeviceInfoData As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
		End Function

		<DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
		Private Shared Function SetupDiEnumDriverInfo(
   <[In]()> ByVal DeviceInfoSet As SafeDeviceHandle,
   <[In](), [Optional]()> ByVal deviceInfoData As IntPtr,
   <[In]()> ByVal DriverType As UInt32,
   <[In]()> ByVal MemberIndex As UInt32,
   <[Out]()> ByVal DriverInfoData As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
		End Function

		<DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
		Private Shared Function SetupDiGetDeviceRegistryProperty(
   <[In]()> ByVal DeviceInfoSet As SafeDeviceHandle,
   <[In]()> ByVal DeviceInfoData As IntPtr,
   <[In]()> ByVal [Property] As UInt32,
   <[Out](), [Optional]()> ByRef PropertyRegDataType As RegistryValueKind,
   <[In](), Out(), [Optional]()> ByVal PropertyBuffer() As Byte,
   <[In]()> ByVal PropertyBufferSize As UInt32,
   <[Out](), [Optional]()> ByRef RequiredSize As UInt32) As <MarshalAs(UnmanagedType.Bool)> Boolean
		End Function

		<DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
		Private Shared Function SetupDiBuildDriverInfoList(
   <[In]()> ByVal DeviceInfoSet As SafeDeviceHandle,
   <[In](), [Out]()> ByVal DeviceInfoData As IntPtr,
   <[In]()> ByVal DriverType As UInt32) As <MarshalAs(UnmanagedType.Bool)> Boolean
		End Function

		<DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
		Private Shared Function SetupDiGetDriverInfoDetail(
   <[In]()> ByVal DeviceInfoSet As SafeDeviceHandle,
   <[In](), [Optional]()> ByVal DeviceInfoData As IntPtr,
   <[In]()> ByVal DriverInfoData As IntPtr,
   <[In](), [Out]()> ByVal DriverInfoDetailData As IntPtr,
   <[In]()> ByVal DriverInfoDetailDataSize As UInt32,
   <[Out](), [Optional]()> ByRef RequiredSize As UInt32) As <MarshalAs(UnmanagedType.Bool)> Boolean
		End Function

		<DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
		Private Shared Function SetupDiSetClassInstallParams(
   <[In]()> ByVal DeviceInfoSet As SafeDeviceHandle,
   <[In](), [Optional]()> ByVal DeviceInfoData As IntPtr,
   <[In](), [Optional]()> ByVal classInstallParams As IntPtr,
   <[In]()> ByVal ClassInstallParamsSize As UInt32) As <MarshalAs(UnmanagedType.Bool)> Boolean
		End Function

		<DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
		Private Shared Function SetupDiGetClassInstallParams(
   <[In]()> ByVal DeviceInfoSet As SafeDeviceHandle,
   <[In](), [Optional]()> ByVal DeviceInfoData As IntPtr,
   <[Out](), [Optional]()> ByVal ClassInstallParams As IntPtr,
   <[In]()> ByVal ClassInstallParamsSize As UInt32,
   <[Out](), [Optional]()> ByRef RequiredSize As UInt32) As <MarshalAs(UnmanagedType.Bool)> Boolean
		End Function

		<DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
		Private Shared Function SetupDiGetDeviceInstallParams(
   <[In]()> ByVal DeviceInfoSet As SafeDeviceHandle,
   <[In](), [Optional]()> ByVal DeviceInfoData As IntPtr,
   <[Out]()> ByVal DeviceInstallParams As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
		End Function

		<DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
		Private Shared Function SetupDiSetDeviceInstallParams(
   <[In]()> ByVal DeviceInfoSet As SafeDeviceHandle,
   <[In](), [Optional]()> ByVal DeviceInfoData As IntPtr,
   <[In]()> ByVal DeviceInstallParams As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
		End Function

		<DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
		Private Shared Function SetupDiChangeState(
   <[In]()> ByVal DeviceInfoSet As SafeDeviceHandle,
   <[In](), [Out]()> ByVal DeviceInfoData As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
		End Function

		<DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
		Private Shared Function SetupDiCallClassInstaller(
   <[In]()> ByVal InstallFunction As UInt32,
   <[In]()> ByVal DeviceInfoSet As SafeDeviceHandle,
   <[In](), [Optional]()> ByVal DeviceInfoData As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
		End Function

		<DllImport("setupapi.dll", CharSet:=CharSet.Ansi, SetLastError:=True)>
		Private Shared Function SetupUninstallOEMInf(
   <[In](), MarshalAs(UnmanagedType.LPStr)> ByVal InfFileName As String,
   <[In]()> ByVal Flags As UInt32,
   <[In]()> ByVal Reserved As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
		End Function

		<DllImport("newdev.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
		Private Shared Function UpdateDriverForPlugAndPlayDevices(
   <[In](), [Optional]()> ByVal hwndParent As IntPtr,
   <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal HardwareId As String,
   <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal FullInfPath As String,
   <[In]()> ByVal InstallFlags As UInt32,
   <[Out](), [Optional](), MarshalAs(UnmanagedType.Bool)> ByRef bRebootRequired As Boolean) As <MarshalAs(UnmanagedType.Bool)> Boolean
		End Function

#End Region

#Region "Functions"

		Public Shared Function TEST_GetDevices(ByVal filter As String, ByVal text As String) As List(Of Device)
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
								GetDeviceExtendedDetails(ptrDevInfo.Ptr, d)

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

		Public Shared Sub TEST_RemoveDevice(ByVal hardwareIDFilter As String)
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
							For Each inf As Inf In device.OemInfs
								If Not File.Exists(inf.FileName) Then
									Continue For
								End If

								Dim infName As String = Path.GetFileName(inf.FileName)

								If CheckIsOemInf(infName) Then
									Dim attrs As FileAttributes = File.GetAttributes(inf.FileName)

									If (attrs And FileAttributes.ReadOnly) = FileAttributes.ReadOnly Then
										File.SetAttributes(inf.FileName, attrs And Not FileAttributes.ReadOnly)
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

		Public Shared Sub TEST_EnableDevice(ByVal hardwareIDFilter As String, ByVal enable As Boolean)
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
								  .cbSize = GetUInt32(Marshal.SizeOf(GetType(SP_CLASSINSTALL_HEADER_X64))),
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
								  .cbSize = GetUInt32(Marshal.SizeOf(GetType(SP_CLASSINSTALL_HEADER_X86))),
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

		Public Shared Sub TEST_UpdateDevice(ByVal device As Device, ByVal infFile As String, Optional ByVal force As Boolean = False)
			If String.IsNullOrEmpty(infFile) OrElse Not File.Exists(infFile) Then
				Throw New ArgumentException("Empty infFile or infFile doesn't exists!", "infFile")
				Return
			End If

			If device.HardwareIDs Is Nothing OrElse device.HardwareIDs.Length = 0 OrElse String.IsNullOrEmpty(device.HardwareIDs(0)) Then
				Throw New ArgumentException("Empty Hardware ID Filter!", "hardwareIDFilter")
				Return
			End If

			Try
				Dim nullGuid As Guid = Guid.Empty
				Dim hardwareIds(0) As String
				Dim found As Boolean = False
				Dim dev As Device = Nothing

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
							hardwareIds = GetMultiStringProperty(infoSet, ptrDevInfo.Ptr, SPDRP.HARDWAREID)

							If hardwareIds Is Nothing Then
								Continue While
							End If

							For Each hdID As String In hardwareIds
								If hdID.IndexOf(device.HardwareIDs(0), StringComparison.OrdinalIgnoreCase) <> -1 Then
									dev = New Device() With
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




		' REVERSED FOR CLEANING FROM CODE
		Public Shared Function GetDevices(ByVal className As String, Optional ByVal vendorID As String = Nothing, Optional ByVal getDetails As Boolean = True) As List(Of Device)
			Try
				If IsNullOrWhitespace(className) Then
					Throw New ArgumentNullException("className")
				End If

				Dim SiblingDevicesToFind As List(Of Device) = New List(Of Device)(5)
				Dim Devices As List(Of Device) = New List(Of Device)(5)
				Dim nullGuid As Guid = Nothing

				Using infoSet As SafeDeviceHandle = SetupDiGetClassDevs(nullGuid, Nothing, IntPtr.Zero, DIGCF.ALLCLASSES)
					CheckWin32Error(Not infoSet.IsInvalid)

					Dim ptrDevInfo As StructPtr = Nothing
					Try
						If Is64 Then
							ptrDevInfo = New StructPtr(New SP_DEVINFO_DATA_X64() With {.cbSize = GetUInt32(Marshal.SizeOf(GetType(SP_DEVINFO_DATA_X64)))})
						Else
							ptrDevInfo = New StructPtr(New SP_DEVINFO_DATA_X86() With {.cbSize = GetUInt32(Marshal.SizeOf(GetType(SP_DEVINFO_DATA_X86)))})
						End If

						Dim i As UInt32 = 0UI
						Dim device As Device = Nothing
						Dim devClass As String = Nothing
						Dim devInst As UInt32

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

								device = New Device() With
								{
								 .devInst = devInst,
								 .ClassName = devClass,
								 .DeviceID = GetDeviceID(devInst)
								}

								If vendorID IsNot Nothing AndAlso Not StrContainsAny(device.DeviceID, True, vendorID) Then
									Continue While
								End If

								Devices.Add(device)
							End If
						End While

						If getDetails Then
							If Devices IsNot Nothing AndAlso Devices.Count > 0 Then
								For Each dev As Device In Devices
									GetSiblings(dev)

									If dev.SiblingDevices IsNot Nothing AndAlso dev.SiblingDevices.Length > 0 Then
										UpdateDevicesByID(dev.SiblingDevices)
									End If
								Next

								UpdateDevicesByID(Devices)
							End If

							Dim logEntry As LogEntry = Application.Log.CreateEntry()
							logEntry.Message = String.Format("Devices found: {0}", Devices.Count.ToString())
							logEntry.Add("-> ClassName", className)
							logEntry.Add(KvP.Empty)

							logEntry.AddDevices(Devices.ToArray())

							Application.Log.Add(logEntry)
						End If

						Return Devices
					Finally
						If ptrDevInfo IsNot Nothing Then
							ptrDevInfo.Dispose()
						End If

						If SiblingDevicesToFind.Count > 0 Then
							UpdateDevicesByID(SiblingDevicesToFind)
						End If
					End Try
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex, "GetDevices failed!")
				Return New List(Of Device)(0)
			Finally

			End Try
		End Function

		' REVERSED FOR CLEANING FROM CODE
		Public Shared Sub UninstallDevice(ByVal device As Device)
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
					Dim sb As StringBuilder = New StringBuilder()

					For Each inf As Inf In device.OemInfs
						sb.AppendLine(inf.FileName)
					Next

					logEntry.Add("infFiles", sb.ToString())
				Else
					logEntry.Add("infFile", "No inf(s) associated")
				End If

				Application.Log.Add(logEntry)

				Dim nullGuid As Guid = Guid.Empty
				Dim hardwareIds(0) As String
				Dim found As Boolean = False


				Using infoSet As SafeDeviceHandle = SetupDiGetClassDevs(nullGuid, Nothing, IntPtr.Zero, DIGCF.ALLCLASSES)
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

							For Each inf As Inf In device.OemInfs
								If Not File.Exists(inf.FileName) Then
									logInfs.Add(inf.FileName, "File not exists")
									Continue For
								End If

								Dim infName As String = Path.GetFileName(inf.FileName)

								If CheckIsOemInf(infName) Then 'Useless check since only OEM infs are added, but just incase ( see: GetDriverDetails )
									Dim attrs As FileAttributes = File.GetAttributes(inf.FileName)

									If (attrs And FileAttributes.ReadOnly) = FileAttributes.ReadOnly Then
										File.SetAttributes(inf.FileName, attrs And Not FileAttributes.ReadOnly)
									End If

									If SetupUninstallOEMInf(infName, CUInt(SetupUOInfFlags.SUOI_FORCEDELETE), IntPtr.Zero) Then
										logInfs.Add(inf.FileName, "Uninstalled!")
									Else
										logInfs.Add(inf.FileName, "Uninstalling failed! See exceptions for details!")

										Dim logInfEx As LogEntry = Application.Log.CreateEntry()
										logInfEx.AddException(New Win32Exception())
										logInfEx.Add("InfFile", inf.FileName)

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
		Public Shared Sub RemoveInf(ByVal oem As Inf, ByVal force As Boolean)
			Try
				If oem Is Nothing Then
					Throw New ArgumentNullException("oem", "Cancelling removal of Inf file. Oem is nothing!")
				End If

				If Not oem.FileExists Then
					Throw New ArgumentException("Cancelling removal of Inf file. Inf has empty filename, file doesn't exists!", "oem")
				End If

				If Not oem.IsValid Then
					Throw New ArgumentException("Cancelling removal of Inf file. Inf is corrupted!", "oem")
				End If

				Dim logEntry As LogEntry = Application.Log.CreateEntry()
				logEntry.Message = "InfFile: " & oem.FileName
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
						logInfs.Add(oem.FileName, "Uninstalling failed!")
						logInfs.AddException(New Win32Exception(GetLastWin32Error()), False)
					End If
				Else
					If SetupUninstallOEMInf(infName, CUInt(SetupUOInfFlags.NONE), IntPtr.Zero) Then
						logInfs.Add(oem.FileName, "Uninstalled!")
					Else
						Dim errcode As Int32 = GetLastWin32Error()

						If errcode = 0 Then
							logInfs.Add(oem.FileName, "Uninstalling failed! OEM still in use")
						Else
							logInfs.Add(oem.FileName, "Uninstalling failed! See exceptions for details!")
							logInfs.AddException(New Win32Exception(errcode), False)
						End If
					End If
				End If

				Application.Log.Add(logInfs)
			Catch ex As Exception
				Application.Log.AddWarning(ex)
			End Try
		End Sub

		' REVERSED FOR CLEANING FROM CODE
		Public Shared Sub UpdateDeviceInf(ByVal device As Device, ByVal infFile As String, Optional ByVal force As Boolean = False)
			Dim logEntry As LogEntry = Application.Log.CreateEntry()
			logEntry.Message = "Updating Inf for device '" & device.Description & "'"

			Try
				Dim inf As Inf = New Inf(infFile)

				If Not inf.IsValid Then
					Throw New ArgumentException("Empty infFile or infFile doesn't exists!", "infFile")
				End If

				If device.HardwareIDs Is Nothing OrElse device.HardwareIDs.Length = 0 OrElse String.IsNullOrEmpty(device.HardwareIDs(0)) Then
					Throw New ArgumentException("Empty Hardware ID Filter!", "hardwareIDFilter")
				End If

				Dim nullGuid As Guid = Guid.Empty
				Dim hardwareIds(0) As String
				Dim found As Boolean = False
				Dim dev As Device = Nothing

				Using infoSet As SafeDeviceHandle = SetupDiGetClassDevs(nullGuid, Nothing, IntPtr.Zero, DIGCF.ALLCLASSES)
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

							hardwareIds = GetMultiStringProperty(infoSet, ptrDevInfo.Ptr, SPDRP.HARDWAREID)

							If hardwareIds Is Nothing Then
								Continue While
							End If

							For Each hdID As String In hardwareIds
								If hdID.IndexOf(device.HardwareIDs(0), StringComparison.OrdinalIgnoreCase) <> -1 Then
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

						Dim requiresReboot As Boolean

						If UpdateDriverForPlugAndPlayDevices(IntPtr.Zero, device.HardwareIDs(0), infFile, If(force, CUInt(INSTALLFLAG.FORCE), CUInt(INSTALLFLAG.NULL)), requiresReboot) Then
							logEntry.Add("Successfully updated!")
							Return
						Else
							Dim errCode As UInt32 = GetLastWin32ErrorU()

							If errCode = Errors.NO_SUCH_DEVINST Then
								logEntry.Add("The value specified for HardwareId does not match any device on the system. That is, the device is not plugged in.")
								logEntry.Type = LogType.Warning

								'	The value specified for HardwareId does not match any device on the system. 
								'	That is, the device is not plugged in.
							ElseIf errCode = Errors.NO_MORE_ITEMS Then
								logEntry.Add("The function found a match for the HardwareId value, but the specified driver was not a better match than the current driver and the caller did not specify the INSTALLFLAG_FORCE flag.")
								logEntry.Type = LogType.Warning

								'	The function found a match for the HardwareId value, but the specified driver was 
								'	not a better match than the current driver and the caller did not specify the INSTALLFLAG_FORCE flag. 
							Else
								Throw New Win32Exception(GetInt32(errCode))
							End If
						End If
					Finally
						If ptrDevInfo IsNot Nothing Then
							ptrDevInfo.Dispose()
						End If
					End Try
				End Using
			Catch ex As Exception
				logEntry.AddException(ex)
			Finally
				Application.Log.Add(logEntry)
			End Try
		End Sub

		' REVERSED FOR CLEANING FROM CODE
		Public Shared Sub ReScanDevices()
			Dim result As UInt32
			Dim devInstRoot As UInt32

			Try
				ACL.AddPriviliges(ACL.SE.LOAD_DRIVER_NAME)

				result = CM_Locate_DevNode(devInstRoot, Nothing, CM_LOCATE.DEVNODE_NORMAL Or CM_LOCATE.DEVNODE_PHANTOM)

				If result = CR.SUCCESS Then
					result = CM_Reenumerate_DevNode(devInstRoot, 0UI)

					If result = CR.SUCCESS Then
						Application.Log.AddMessage("ReScan of devices successfully completed!")
					Else : Throw New Win32Exception(GetLastWin32Error())
					End If
				Else : Throw New Win32Exception(GetLastWin32Error())
				End If
			Catch ex As Exception
				Application.Log.AddException(ex, "ReScan of devices failed!")
			End Try
		End Sub



		Private Shared Sub UpdateDevicesByID(ByVal devList As IEnumerable(Of Device))
			Try
				Dim nullGuid As Guid = Guid.Empty
				Dim device As Device = Nothing
				Dim devInst As UInt32

				Using infoSet As SafeDeviceHandle = SetupDiGetClassDevs(nullGuid, Nothing, IntPtr.Zero, DIGCF.ALLCLASSES)
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

							For Each sDev As Device In devList
								If sDev.devInst = devInst Then
									GetDeviceDetails(infoSet, ptrDevInfo.Ptr, sDev)
									GetDriverDetails(infoSet, ptrDevInfo.Ptr, sDev)
									GetDeviceExtendedDetails(ptrDevInfo.Ptr, sDev)

									devInst = 0UI
									Continue While
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
				Application.Log.AddException(ex, "Updating devices details by ID has failed!")
			End Try
		End Sub

		Private Shared Function GetProperty(ByVal infoSet As SafeDeviceHandle, ByVal ptrDevInfo As IntPtr, ByVal [property] As SPDRP, ByRef bytes() As Byte, ByRef regType As RegistryValueKind, ByRef size As Int32) As Boolean
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

		Private Shared Function GetStringProperty(ByVal infoSet As SafeDeviceHandle, ByVal ptrDevInfo As IntPtr, ByVal [property] As SPDRP) As String
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

		Private Shared Function GetMultiStringProperty(ByVal infoSet As SafeDeviceHandle, ByVal ptrDevInfo As IntPtr, ByVal [property] As SPDRP) As String()
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

		Private Shared Function GetUInt32Property(ByVal infoSet As SafeDeviceHandle, ByVal ptrDevInfo As IntPtr, ByVal [property] As SPDRP) As UInt32
			Dim buffer(0) As Byte
			Dim size As Int32 = 0
			Dim regType As RegistryValueKind

			If Not GetProperty(infoSet, ptrDevInfo, [property], buffer, regType, size) Then
				Return 0UI
			End If

			Return BitConverter.ToUInt32(buffer, 0)
		End Function




		Private Shared Function GetInstallParamsFlags(ByVal infoSet As SafeDeviceHandle, ByVal ptrDevInfo As IntPtr) As UInt32
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

		Private Shared Sub GetDeviceDetails(ByVal infoSet As SafeDeviceHandle, ByVal ptrDevInfo As IntPtr, ByVal device As Device)
			If device.DeviceID Is Nothing Then device.DeviceID = GetDeviceID(device.devInst)
			If device.ClassName Is Nothing Then device.ClassName = GetStringProperty(infoSet, ptrDevInfo, SPDRP.CLASS)
			If device.ClassGuid Is Nothing Then device.ClassGuid = GetStringProperty(infoSet, ptrDevInfo, SPDRP.CLASSGUID)
			If device.Description Is Nothing Then device.Description = GetStringProperty(infoSet, ptrDevInfo, SPDRP.DEVICEDESC)
			If device.FriendlyName Is Nothing Then device.FriendlyName = GetStringProperty(infoSet, ptrDevInfo, SPDRP.FRIENDLYNAME)
			If device.HardwareIDs Is Nothing Then device.HardwareIDs = GetMultiStringProperty(infoSet, ptrDevInfo, SPDRP.HARDWAREID)
			If device.CompatibleIDs Is Nothing Then device.CompatibleIDs = GetMultiStringProperty(infoSet, ptrDevInfo, SPDRP.COMPATIBLEIDS)
			If device.InstallState Is Nothing Then device.InstallState = GetDescription(DirectCast(GetUInt32Property(infoSet, ptrDevInfo, SPDRP.INSTALL_STATE), DEVICE_INSTALL_STATE))
			If device.LowerFilters Is Nothing Then device.LowerFilters = GetMultiStringProperty(infoSet, ptrDevInfo, SPDRP.LOWERFILTERS)
			If device.Capabilities Is Nothing Then device.Capabilities = ToStringArray(Of CM_DEVCAP)(DirectCast(GetUInt32Property(infoSet, ptrDevInfo, SPDRP.CAPABILITIES), CM_DEVCAP))
			If device.InstallFlags Is Nothing Then device.InstallFlags = ToStringArray(Of DI)(DirectCast(GetInstallParamsFlags(infoSet, ptrDevInfo), DI))
			If device.ConfigFlags Is Nothing Then device.ConfigFlags = ToStringArray(Of CONFIGFLAGS)(DirectCast(GetUInt32Property(infoSet, ptrDevInfo, SPDRP.CONFIGFLAGS), CONFIGFLAGS))
		End Sub

		Private Shared Sub GetDeviceExtendedDetails(ByVal ptrDevInfo As IntPtr, ByVal device As Device)
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

				If (pulStatus And DN.HAS_PROBLEM) = DN.HAS_PROBLEM Then
					device.DevProblems = ToStringArray(Of CM_PROB)(DirectCast(pulProblemNumber, CM_PROB))
				Else
					device.DevProblems = Nothing
				End If

			ElseIf result2 = CR.NO_SUCH_DEVINST Then
			Else
				CheckWin32Error(False)
			End If
		End Sub

		Private Shared Sub GetDriverDetails(ByVal infoSet As SafeDeviceHandle, ByVal ptrDevInfo As IntPtr, ByVal device As Device)
			If device.DriverInfo IsNot Nothing Then
				device.DriverInfo = Nothing
			End If

			If SetupDiBuildDriverInfoList(infoSet, ptrDevInfo, CUInt(SPDIT.COMPATDRIVER)) Then
				Dim oemInfs As List(Of Inf) = New List(Of Inf)(5)

				Dim ptrDrvInfoData As StructPtr = Nothing
				Dim i As UInt32 = 0UI
				Dim bytes(0) As Byte
				Dim drvInfos As New List(Of DriverInfo)(5)

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

												drvInfo.InfFile = New Inf(drvDetailData64.InfFileName) With {.InstallDate = FileTimeToDateTime(drvDetailData64.InfDate)}
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

												drvInfo.InfFile = New Inf(drvDetailData86.InfFileName) With {.InstallDate = FileTimeToDateTime(drvDetailData86.InfDate)}
											End If

											If CheckIsOemInf(Path.GetFileName(drvInfo.InfFile.FileName)) Then
												oemInfs.Add(drvInfo.InfFile)
											End If

											drvInfos.Add(drvInfo)
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
					device.DriverInfo = drvInfos.ToArray()
				Finally
					If ptrDrvInfoData IsNot Nothing Then
						ptrDrvInfoData.Dispose()
					End If
				End Try
			Else
				CheckWin32Error(False)
			End If
		End Sub

		Private Shared Function GetDeviceID(ByVal devInst As UInt32) As String
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

		Private Shared Sub GetSiblings(ByVal device As Device)
			Try
				Dim result As UInt32
				Dim devInstParent As UInt32 = 0UI
				Dim devInstChild As UInt32 = 0UI
				Dim devInstSibling As UInt32 = 0UI
				Dim siblingDevices As New List(Of Device)(5)
				Dim contains As Boolean = False

				result = CM_Get_Parent(devInstParent, device.devInst, 0UI)

				If result = CR.SUCCESS Then
					result = CM_Get_Child(devInstChild, devInstParent, 0UI)

					If result <> CR.SUCCESS Then
						If result = CR.NO_SUCH_DEVINST Then
							Return
						Else : Throw New Win32Exception(GetLastWin32Error())
						End If
					End If

					If devInstChild <> device.devInst Then
						siblingDevices.Add(
						  New Device() With {
						   .devInst = devInstChild
						  })
					End If

					While True
						result = CM_Get_Sibling(devInstSibling, devInstChild, 0UI)
						contains = False

						If result = CR.SUCCESS Then
							If devInstSibling <> device.devInst Then
								For Each sibling As Device In siblingDevices
									If sibling.devInst = devInstSibling Then
										contains = True
										Exit For
									End If
								Next

								If Not contains Then
									siblingDevices.Add(
									 New Device() With
									 {
									   .devInst = devInstSibling
									 })
								End If
							End If

							devInstChild = devInstSibling
						ElseIf result = CR.NO_SUCH_DEVINST Then
							Exit While
						Else
							Throw New Win32Exception(GetLastWin32Error())
						End If
					End While

					If siblingDevices.Count > 0 Then
						device.SiblingDevices = siblingDevices.ToArray()
					End If
				ElseIf result = CR.NO_SUCH_DEVINST Then
					Return
				End If
			Catch ex As Exception
				Application.Log.AddException(ex, "Getting device's siblings has failed!")
			End Try
		End Sub

		Private Shared Function RebootRequired(ByVal infoSet As SafeDeviceHandle, ByVal ptrDevInfo As IntPtr) As Boolean
			Return RebootRequired(GetInstallParamsFlags(infoSet, ptrDevInfo))
		End Function

		Private Shared Function RebootRequired(ByVal installParamsFlags As UInt32) As Boolean
			If ((installParamsFlags And DI.NEEDREBOOT) = DI.NEEDREBOOT Or
			(installParamsFlags And DI.NEEDRESTART) = DI.NEEDRESTART) Then
				Return True
			Else
				Return False
			End If
		End Function

		Private Shared Sub Reboot()
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

		Private Shared Function CheckIsOemInf(ByVal infName As String) As Boolean
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

		Private Shared Function FileTimeToDateTime(ByVal time As System.Runtime.InteropServices.ComTypes.FILETIME) As DateTime
			Dim high As UInt64 = CType(time.dwHighDateTime, UInt64)
			Dim low As UInt32 = GetUInt32(time.dwLowDateTime)

			Dim FILETIME As Int64 = CLng(((high << 32) + low))

			Return DateTime.FromFileTimeUtc(FILETIME)
		End Function

#End Region

#Region "Classes"

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

		Public Class Device
			Friend devInst As UInt32
			Public Property HasDetails As Boolean

			Private _hardwareIDs As String()
			Private _lowerfilters As String()
			Private _friendlyname As String
			Private _compatibleIDs As String()
			Private _description As String
			Private _classGuid As String
			Private _className As String
			Private _deviceID As String
			Private _driverInfo As DriverInfo()
			Private _siblingDevices() As Device
			Private _installState As String
			Private _installFlags As String()
			Private _capabilities As String()
			Private _configFlags As String()
			Private _devProblems As String()
			Private _devStatus As String()
			Private _oemInfs As Inf()

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
			Public Property DriverInfo As DriverInfo()
				Get
					Return _driverInfo
				End Get
				Friend Set(value As DriverInfo())
					_driverInfo = value
				End Set
			End Property
			Public Property SiblingDevices As Device()
				Get
					Return _siblingDevices
				End Get
				Friend Set(value As Device())
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
			Public Property OemInfs As Inf()
				Get
					Return _oemInfs
				End Get
				Friend Set(value As Inf())
					_oemInfs = value
				End Set
			End Property

			Friend Sub New()
				_driverInfo = Nothing
				_siblingDevices = Nothing
				_oemInfs = Nothing
			End Sub

			Public Overrides Function ToString() As String
				Return String.Format("{0} - (dev: {1})", Description, devInst.ToString())
			End Function
		End Class

		Public Class DriverInfo
			Private _mfgName As String
			Private _providerName As String
			Private _description As String
			Private _driverVersion As String
			Private _driverDate As DateTime
			Private _infFile As Inf
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
			Public Property InfFile As Inf
				Get
					Return _infFile
				End Get
				Friend Set(value As Inf)
					_infFile = value
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

#End Region

	End Class
End Namespace