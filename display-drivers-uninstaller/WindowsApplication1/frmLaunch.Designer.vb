<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmLaunch
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
		Me.btnClose = New System.Windows.Forms.Button()
		Me.btnAccept = New System.Windows.Forms.Button()
		Me.cbBootOption = New System.Windows.Forms.ComboBox()
		Me.pbLogo = New System.Windows.Forms.PictureBox()
		Me.lblLaunchOption = New System.Windows.Forms.Label()
		Me.lblNotSafeMode = New System.Windows.Forms.Label()
		CType(Me.pbLogo, System.ComponentModel.ISupportInitialize).BeginInit()
		Me.SuspendLayout()
		'
		'btnClose
		'
		Me.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel
		Me.btnClose.Location = New System.Drawing.Point(252, 235)
		Me.btnClose.Name = "btnClose"
		Me.btnClose.Size = New System.Drawing.Size(93, 39)
		Me.btnClose.TabIndex = 0
		Me.btnClose.Text = "Close"
		Me.btnClose.UseVisualStyleBackColor = True
		'
		'btnAccept
		'
		Me.btnAccept.DialogResult = System.Windows.Forms.DialogResult.OK
		Me.btnAccept.Location = New System.Drawing.Point(12, 235)
		Me.btnAccept.Name = "btnAccept"
		Me.btnAccept.Size = New System.Drawing.Size(93, 39)
		Me.btnAccept.TabIndex = 1
		Me.btnAccept.Tag = "0001"
		Me.btnAccept.Text = "Launch"
		Me.btnAccept.UseVisualStyleBackColor = True
		'
		'cbBootOption
		'
		Me.cbBootOption.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
		Me.cbBootOption.FormattingEnabled = True
		Me.cbBootOption.Items.AddRange(New Object() {"Normal", "Safe Mode (Recommended)", "Safe Mode With Networking"})
		Me.cbBootOption.Location = New System.Drawing.Point(12, 151)
		Me.cbBootOption.Name = "cbBootOption"
		Me.cbBootOption.Size = New System.Drawing.Size(333, 21)
		Me.cbBootOption.TabIndex = 2
		'
		'pbLogo
		'
		Me.pbLogo.BackgroundImage = Global.WindowsApplication1.My.Resources.Resources.ddu_logo3
		Me.pbLogo.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom
		Me.pbLogo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
		Me.pbLogo.Dock = System.Windows.Forms.DockStyle.Top
		Me.pbLogo.InitialImage = Nothing
		Me.pbLogo.Location = New System.Drawing.Point(0, 0)
		Me.pbLogo.Name = "pbLogo"
		Me.pbLogo.Size = New System.Drawing.Size(349, 121)
		Me.pbLogo.TabIndex = 4
		Me.pbLogo.TabStop = False
		'
		'lblLaunchOption
		'
		Me.lblLaunchOption.AutoSize = True
		Me.lblLaunchOption.Location = New System.Drawing.Point(9, 135)
		Me.lblLaunchOption.Name = "lblLaunchOption"
		Me.lblLaunchOption.Size = New System.Drawing.Size(75, 13)
		Me.lblLaunchOption.TabIndex = 5
		Me.lblLaunchOption.Text = "Launch option"
		'
		'lblNotSafeMode
		'
		Me.lblNotSafeMode.AutoSize = True
		Me.lblNotSafeMode.ForeColor = System.Drawing.Color.Red
		Me.lblNotSafeMode.Location = New System.Drawing.Point(9, 192)
		Me.lblNotSafeMode.Name = "lblNotSafeMode"
		Me.lblNotSafeMode.Size = New System.Drawing.Size(247, 26)
		Me.lblNotSafeMode.TabIndex = 6
		Me.lblNotSafeMode.Text = "You are not in safe mode. It is highly recommended" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "reboot into Safe Mode to avoi" & _
		  "d possible issues."
		'
		'frmLaunch
		'
		Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
		Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
		Me.CancelButton = Me.btnClose
		Me.ClientSize = New System.Drawing.Size(349, 278)
		Me.ControlBox = False
		Me.Controls.Add(Me.lblNotSafeMode)
		Me.Controls.Add(Me.lblLaunchOption)
		Me.Controls.Add(Me.pbLogo)
		Me.Controls.Add(Me.cbBootOption)
		Me.Controls.Add(Me.btnAccept)
		Me.Controls.Add(Me.btnClose)
		Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
		Me.Name = "frmLaunch"
		Me.ShowIcon = False
		Me.ShowInTaskbar = False
		Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
		Me.Text = "Launch option"
		CType(Me.pbLogo, System.ComponentModel.ISupportInitialize).EndInit()
		Me.ResumeLayout(False)
		Me.PerformLayout()

	End Sub
	Friend WithEvents btnClose As System.Windows.Forms.Button
	Friend WithEvents btnAccept As System.Windows.Forms.Button
	Friend WithEvents cbBootOption As System.Windows.Forms.ComboBox
	Friend WithEvents pbLogo As System.Windows.Forms.PictureBox
	Friend WithEvents lblLaunchOption As System.Windows.Forms.Label
    Friend WithEvents lblNotSafeMode As System.Windows.Forms.Label
End Class
