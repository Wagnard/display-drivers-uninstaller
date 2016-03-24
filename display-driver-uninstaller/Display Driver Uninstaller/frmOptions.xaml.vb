Public Class frmOptions

	Private Sub frmOptions_Loaded(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
		Languages.TranslateForm(Me)
	End Sub

    Private Sub btnClose_Click(sender As Object, e As RoutedEventArgs) Handles btnClose.Click
        Me.Close()
    End Sub

    Private Sub lblRemAMDDirs_Checked(sender As Object, e As RoutedEventArgs) Handles lblRemAMDDirs.Click
        frmMain.settings.setconfig("removecamd", CStr(Application.Settings.RemoveAMDDirs))
    End Sub

    Private Sub lblRemAMDKMPFD_Checked(sender As Object, e As RoutedEventArgs) Handles lblRemAMDKMPFD.Click
        frmMain.settings.setconfig("removeamdkmpfd", CStr(Application.Settings.RemoveAMDKMPFD))
    End Sub

    Private Sub lblRemCrimsonCache_Checked(sender As Object, e As RoutedEventArgs) Handles lblRemCrimsonCache.Click
        frmMain.settings.setconfig("removedxcache", CStr(Application.Settings.RemoveCrimsonCache))
    End Sub

    Private Sub lblRemAMDAudioBus_Checked(sender As Object, e As RoutedEventArgs) Handles lblRemAMDAudioBus.Click
        frmMain.settings.setconfig("removeamdaudiobus", CStr(Application.Settings.RemoveAMDAudioBus))
    End Sub

    Private Sub lblRemNvidiaDirs_Checked(sender As Object, e As RoutedEventArgs) Handles lblRemNvidiaDirs.Click
        frmMain.settings.setconfig("removecnvidia", CStr(Application.Settings.RemoveNvidiaDirs))
    End Sub

    Private Sub lblRemPhysX_Checked(sender As Object, e As RoutedEventArgs) Handles lblRemPhysX.Click
        frmMain.settings.setconfig("removephysx", CStr(Application.Settings.RemovePhysX))
    End Sub

    Private Sub lblRem3DtvPlay_Checked(sender As Object, e As RoutedEventArgs) Handles lblRem3DtvPlay.Click
        frmMain.settings.setconfig("remove3dtvplay", CStr(Application.Settings.Remove3DTVPlay))
    End Sub

    Private Sub lblRemGFE_Checked(sender As Object, e As RoutedEventArgs) Handles lblRemGFE.Click
        frmMain.settings.setconfig("removegfe", CStr(Application.Settings.RemoveGFE))
    End Sub

    Private Sub lblRemMonitors_Checked(sender As Object, e As RoutedEventArgs) Handles lblRemMonitors.Click
        frmMain.settings.setconfig("removemonitor", CStr(Application.Settings.RemoveMonitors))
    End Sub

    Private Sub lblsaveLogs_Checked(sender As Object, e As RoutedEventArgs) Handles lblsaveLogs.Click
        frmMain.settings.setconfig("logbox", CStr(Application.Settings.SaveLogs))
    End Sub

    Private Sub lblcreateRestorePoint_Checked(sender As Object, e As RoutedEventArgs) Handles lblcreateRestorePoint.Click
        frmMain.settings.setconfig("systemrestore", CStr(Application.Settings.CreateRestorePoint))
    End Sub

    Private Sub lblshowSafeModeMsg_Checked(sender As Object, e As RoutedEventArgs) Handles lblshowSafeModeMsg.Click
        frmMain.settings.setconfig("showsafemodebox", CStr(Application.Settings.ShowSafeModeMsg))
    End Sub

    Private Sub lblUseRoamingCfg_Checked(sender As Object, e As RoutedEventArgs) Handles lblUseRoamingCfg.Click
        frmMain.settings.setconfig("roamingcfg", CStr(Application.Settings.UseRoamingConfig))
    End Sub

    Private Sub lblDontCheckUpdates_Checked(sender As Object, e As RoutedEventArgs) Handles lblDontCheckUpdates.Click
        frmMain.settings.setconfig("donotcheckupdatestartup", CStr(Application.Settings.DontCheckUpdates))
    End Sub
End Class
