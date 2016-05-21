Imports System.IO
Imports System.Runtime.InteropServices
Imports Microsoft.Win32
Imports Display_Driver_Uninstaller.Win32

Module RegistryAccess
    Public Class RegistryPermissions
        <DllImport("advapi32.dll", SetLastError:=True)> _
        Private Shared Function GetSecurityInfo(handle As IntPtr, objectType As SE_OBJECT_TYPE, securityInfo As SECURITY_INFORMATION, ByRef sidOwner As IntPtr, ByRef sidGroup As IntPtr, ByRef dacl As IntPtr, _
        ByRef sacl As IntPtr, ByRef securityDescriptor As IntPtr) As Integer
        End Function

        <DllImport("advapi32", CharSet:=CharSet.Unicode, SetLastError:=True)> _
        Private Shared Function ConvertSidToStringSid(sid As IntPtr, ByRef sidString As IntPtr) As Boolean
        End Function

        <DllImport("kernel32.dll", SetLastError:=True)> _
        Friend Shared Function LocalFree(handle As IntPtr) As IntPtr
        End Function

        Private Enum SE_OBJECT_TYPE
            SE_UNKNOWN_OBJECT_TYPE
            SE_FILE_OBJECT
            SE_SERVICE
            SE_PRINTER
            SE_REGISTRY_KEY
            SE_LMSHARE
            SE_KERNEL_OBJECT
            SE_WINDOW_OBJECT
            SE_DS_OBJECT
            SE_DS_OBJECT_ALL
            SE_PROVIDER_DEFINED_OBJECT
            SE_WMIGUID_OBJECT
            SE_REGISTRY_WOW64_32KEY
        End Enum

        Private Enum SECURITY_INFORMATION
            OWNER_SECURITY_INFORMATION = 1
            GROUP_SECURITY_INFORMATION = 2
            DACL_SECURITY_INFORMATION = 4
            SACL_SECURITY_INFORMATION = 8
        End Enum

        Public Shared Sub test2()
            'Dim fileStream As FileStream = Nothing
            Dim registrykey As RegistryKey
            Dim ownerSid As IntPtr
            Dim groupSid As IntPtr
            Dim dacl As IntPtr
            Dim sacl As IntPtr
            Dim securityDescriptor As IntPtr = IntPtr.Zero

            Dim returnValue As Integer = 0
            Dim success As Boolean = False

			ACL.AddPriviliges(ACL.SE.SECURITY_NAME, ACL.SE.BACKUP_NAME, ACL.SE.RESTORE_NAME, ACL.SE.TAKE_OWNERSHIP_NAME)
			registrykey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\ATI", RegistryKeyPermissionCheck.ReadWriteSubTree, Security.AccessControl.RegistryRights.TakeOwnership)

			Try
				'fileStream = File.Open("C:\Test\Test.txt", FileMode.Open)

				returnValue = GetSecurityInfo(getRegistryKeyHandle(registrykey), SE_OBJECT_TYPE.SE_REGISTRY_KEY, SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION, ownerSid, groupSid, dacl, sacl, securityDescriptor)

				'returnValue = GetSecurityInfo(fileStream.Handle, SE_OBJECT_TYPE.SE_FILE_OBJECT, SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION Or SECURITY_INFORMATION.DACL_SECURITY_INFORMATION, ownerSid, groupSid, dacl, _
				'	sacl, securityDescriptor)

				Dim sidString As IntPtr = IntPtr.Zero
				success = ConvertSidToStringSid(ownerSid, sidString)
				MsgBox(Marshal.PtrToStringAuto(sidString))
				Marshal.FreeHGlobal(sidString)
			Finally
				LocalFree(securityDescriptor)
				'fileStream.Close()
			End Try
		End Sub

		Public Shared Function Enumeratesubkeys(regions As RegistryKey, subkey As String) As Integer

			'Dim fileStream As FileStream = Nothing
			Dim registrykey As RegistryKey
			Dim securityDescriptor As IntPtr = IntPtr.Zero

			Dim returnValue As Integer = 0
			Dim success As Boolean = False

			ACL.AddPriviliges(ACL.SE.SECURITY_NAME, ACL.SE.BACKUP_NAME, ACL.SE.RESTORE_NAME, ACL.SE.TAKE_OWNERSHIP_NAME)

			registrykey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\ATI", RegistryKeyPermissionCheck.ReadWriteSubTree, Security.AccessControl.RegistryRights.EnumerateSubKeys)

			Try
				'fileStream = File.Open("C:\Test\Test.txt", FileMode.Open)

				Return registrykey.SubKeyCount()

				'returnValue = GetSecurityInfo(fileStream.Handle, SE_OBJECT_TYPE.SE_FILE_OBJECT, SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION Or SECURITY_INFORMATION.DACL_SECURITY_INFORMATION, ownerSid, groupSid, dacl, _
				'	sacl, securityDescriptor)
			Catch ex As Exception
				Return 0
			End Try

		End Function
    End Class

    <DllImport("advapi32.dll", CharSet:=CharSet.Auto)> _
    Public Function RegOpenKeyEx(hKey As IntPtr, subKey As String, ulOptions As Integer, samDesired As Integer, ByRef phkResult As IntPtr) As Integer
    End Function

    Public Enum RegWow64Options
        None = 0
        KEY_WOW64_64KEY = &H100
        KEY_WOW64_32KEY = &H200
    End Enum

    Public Enum RegRights
        ReadKey = 131097
        WriteKey = 131078
    End Enum

    Private Sub exampleTransformKeytoRegistryKey2()
        Dim hKeyChild As IntPtr
        Dim hKeyParent As IntPtr = getRegistryKeyHandle(Registry.LocalMachine)

        If hKeyParent <> IntPtr.Zero Then
            Dim result As Integer = RegOpenKeyEx(getRegistryKeyHandle(Registry.LocalMachine), "SOFTWARE\Microsoft", 0, CInt(RegRights.ReadKey) Or CInt(RegWow64Options.KEY_WOW64_32KEY), hKeyChild)

            If result = 0 Then
                ' hKeyChild has been retrieved
                ' now convert hKeyChild to RegistryKey keyChild

                ' work with keyChild...
                Dim keyChild As RegistryKey = getKeyToRegistryKey(hKeyChild, False, True)
            End If
        End If
    End Sub

    Private Function getKeyToRegistryKey(hKey As IntPtr, writable As Boolean, ownsHandle As Boolean) As RegistryKey
        'Get the BindingFlags for private contructors
        Dim privateConstructors As System.Reflection.BindingFlags = System.Reflection.BindingFlags.Instance Or System.Reflection.BindingFlags.NonPublic
        'Get the Type for the SafeRegistryHandle
        Dim safeRegistryHandleType As Type = GetType(Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid).Assembly.[GetType]("Microsoft.Win32.SafeHandles.SafeRegistryHandle")
        'Get the array of types matching the args of the ctor we want
        Dim safeRegistryHandleCtorTypes As Type() = New Type() {GetType(IntPtr), GetType(Boolean)}
        'Get the constructorinfo for our object
        Dim safeRegistryHandleCtorInfo As System.Reflection.ConstructorInfo = safeRegistryHandleType.GetConstructor(privateConstructors, Nothing, safeRegistryHandleCtorTypes, Nothing)
        'Invoke the constructor, getting us a SafeRegistryHandle
        Dim safeHandle As [Object] = safeRegistryHandleCtorInfo.Invoke(New [Object]() {hKey, ownsHandle})

        'Get the type of a RegistryKey
        Dim registryKeyType As Type = GetType(RegistryKey)
        'Get the array of types matching the args of the ctor we want
        Dim registryKeyConstructorTypes As Type() = New Type() {safeRegistryHandleType, GetType(Boolean)}
        'Get the constructorinfo for our object
        Dim registryKeyCtorInfo As System.Reflection.ConstructorInfo = registryKeyType.GetConstructor(privateConstructors, Nothing, registryKeyConstructorTypes, Nothing)
        'Invoke the constructor, getting us a RegistryKey
        Dim resultKey As RegistryKey = DirectCast(registryKeyCtorInfo.Invoke(New [Object]() {safeHandle, writable}), RegistryKey)
        'return the resulting key
        Return resultKey
    End Function

    Private Function getRegistryKeyHandle(registryKey As RegistryKey) As IntPtr
        Dim registryKeyType As Type = GetType(RegistryKey)
        Dim fieldInfo As System.Reflection.FieldInfo = registryKeyType.GetField("hkey", System.Reflection.BindingFlags.NonPublic Or System.Reflection.BindingFlags.Instance)

        Dim handle As SafeHandle = DirectCast(fieldInfo.GetValue(registryKey), SafeHandle)
        Dim dangerousHandle As IntPtr = handle.DangerousGetHandle()
        Return dangerousHandle
    End Function

End Module
