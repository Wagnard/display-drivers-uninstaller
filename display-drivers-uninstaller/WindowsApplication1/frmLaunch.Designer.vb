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
        Me.Label1 = New System.Windows.Forms.Label()
        Me.lblText = New System.Windows.Forms.Label()
        CType(Me.pbLogo, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'btnClose
        '
        Me.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.btnClose.Location = New System.Drawing.Point(268, 251)
        Me.btnClose.Name = "btnClose"
        Me.btnClose.Size = New System.Drawing.Size(93, 39)
        Me.btnClose.TabIndex = 0
        Me.btnClose.Text = "Close"
        Me.btnClose.UseVisualStyleBackColor = True
        '
        'btnAccept
        '
        Me.btnAccept.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.btnAccept.Location = New System.Drawing.Point(12, 251)
        Me.btnAccept.Name = "btnAccept"
        Me.btnAccept.Size = New System.Drawing.Size(93, 39)
        Me.btnAccept.TabIndex = 1
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
        Me.cbBootOption.Size = New System.Drawing.Size(349, 21)
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
        Me.pbLogo.Size = New System.Drawing.Size(365, 121)
        Me.pbLogo.TabIndex = 4
        Me.pbLogo.TabStop = False
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(9, 135)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(75, 13)
        Me.Label1.TabIndex = 5
        Me.Label1.Text = "Launch option"
        '
        'lblText
        '
        Me.lblText.AutoSize = True
        Me.lblText.ForeColor = System.Drawing.Color.Red
        Me.lblText.Location = New System.Drawing.Point(9, 199)
        Me.lblText.Name = "lblText"
        Me.lblText.Size = New System.Drawing.Size(247, 26)
        Me.lblText.TabIndex = 6
        Me.lblText.Text = "You are not in safe mode. It is highly recommended" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "reboot into Safe Mode to avoi" & _
    "d possible issues."
        '
        'frmLaunch
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.btnClose
        Me.ClientSize = New System.Drawing.Size(365, 294)
        Me.ControlBox = False
        Me.Controls.Add(Me.lblText)
        Me.Controls.Add(Me.Label1)
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
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents lblText As System.Windows.Forms.Label
End Class
