﻿<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Form1
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Form1))
        Me.Button1 = New System.Windows.Forms.Button()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.TextBox1 = New System.Windows.Forms.TextBox()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.ComboBox1 = New System.Windows.Forms.ComboBox()
        Me.Label7 = New System.Windows.Forms.Label()
        Me.CheckBox1 = New System.Windows.Forms.CheckBox()
        Me.CheckBox2 = New System.Windows.Forms.CheckBox()
        Me.MenuStrip1 = New System.Windows.Forms.MenuStrip()
        Me.ToolStripMenuItem1 = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToSToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.VisitDDUHomepageToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.SVNToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.VisitGuru3dNVIDIAThreadToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.VisitGuru3dAMDThreadToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.VisitGeforceThreadToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.AboutToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.Button2 = New System.Windows.Forms.Button()
        Me.Button3 = New System.Windows.Forms.Button()
        Me.CheckBox3 = New System.Windows.Forms.CheckBox()
        Me.Label10 = New System.Windows.Forms.Label()
        Me.BackgroundWorker1 = New System.ComponentModel.BackgroundWorker()
        Me.Label11 = New System.Windows.Forms.Label()
        Me.Button4 = New System.Windows.Forms.Button()
        Me.ComboBox2 = New System.Windows.Forms.ComboBox()
        Me.Label12 = New System.Windows.Forms.Label()
        Me.PictureBox2 = New System.Windows.Forms.PictureBox()
        Me.PictureBox1 = New System.Windows.Forms.PictureBox()
        Me.CheckBox4 = New System.Windows.Forms.CheckBox()
        Me.CheckBox5 = New System.Windows.Forms.CheckBox()
        Me.MenuStrip1.SuspendLayout()
        CType(Me.PictureBox2, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'Button1
        '
        Me.Button1.Location = New System.Drawing.Point(12, 92)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(195, 70)
        Me.Button1.TabIndex = 0
        Me.Button1.UseVisualStyleBackColor = True
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(5, 23)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(142, 13)
        Me.Label1.TabIndex = 1
        Me.Label1.Text = "Detected Windows Version: "
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(170, 23)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(10, 13)
        Me.Label2.TabIndex = 2
        Me.Label2.Text = "-"
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(170, 36)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(10, 13)
        Me.Label3.TabIndex = 3
        Me.Label3.Text = "-"
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Font = New System.Drawing.Font("Microsoft Sans Serif", 7.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label4.ForeColor = System.Drawing.Color.DarkSlateGray
        Me.Label4.Location = New System.Drawing.Point(6, 453)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(0, 13)
        Me.Label4.TabIndex = 4
        '
        'TextBox1
        '
        Me.TextBox1.Location = New System.Drawing.Point(12, 344)
        Me.TextBox1.Multiline = True
        Me.TextBox1.Name = "TextBox1"
        Me.TextBox1.ReadOnly = True
        Me.TextBox1.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.TextBox1.Size = New System.Drawing.Size(379, 92)
        Me.TextBox1.TabIndex = 6
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Location = New System.Drawing.Point(12, 328)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(61, 13)
        Me.Label5.TabIndex = 7
        Me.Label5.Text = "Active Log:"
        '
        'ComboBox1
        '
        Me.ComboBox1.DisplayMember = "(none)"
        Me.ComboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.ComboBox1.FormattingEnabled = True
        Me.ComboBox1.Items.AddRange(New Object() {"NVIDIA", "AMD", "INTEL"})
        Me.ComboBox1.Location = New System.Drawing.Point(171, 52)
        Me.ComboBox1.Name = "ComboBox1"
        Me.ComboBox1.Size = New System.Drawing.Size(121, 21)
        Me.ComboBox1.TabIndex = 9
        '
        'Label7
        '
        Me.Label7.AutoSize = True
        Me.Label7.Location = New System.Drawing.Point(5, 55)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(111, 13)
        Me.Label7.TabIndex = 10
        Me.Label7.Text = "Select Graphic Driver:"
        '
        'CheckBox1
        '
        Me.CheckBox1.AutoSize = True
        Me.CheckBox1.Location = New System.Drawing.Point(222, 277)
        Me.CheckBox1.Name = "CheckBox1"
        Me.CheckBox1.Size = New System.Drawing.Size(229, 17)
        Me.CheckBox1.TabIndex = 12
        Me.CheckBox1.Text = "Remove C:\AMD and or C:\NVIDIA folders"
        Me.CheckBox1.UseVisualStyleBackColor = True
        '
        'CheckBox2
        '
        Me.CheckBox2.AutoSize = True
        Me.CheckBox2.Location = New System.Drawing.Point(222, 299)
        Me.CheckBox2.Name = "CheckBox2"
        Me.CheckBox2.Size = New System.Drawing.Size(235, 17)
        Me.CheckBox2.TabIndex = 13
        Me.CheckBox2.Text = "Save log files in startup path (recommended)"
        Me.CheckBox2.UseVisualStyleBackColor = True
        '
        'MenuStrip1
        '
        Me.MenuStrip1.Dock = System.Windows.Forms.DockStyle.Right
        Me.MenuStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ToolStripMenuItem1})
        Me.MenuStrip1.Location = New System.Drawing.Point(570, 0)
        Me.MenuStrip1.Name = "MenuStrip1"
        Me.MenuStrip1.Size = New System.Drawing.Size(30, 526)
        Me.MenuStrip1.TabIndex = 14
        Me.MenuStrip1.Text = "MenuStrip1"
        '
        'ToolStripMenuItem1
        '
        Me.ToolStripMenuItem1.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ToSToolStripMenuItem, Me.VisitDDUHomepageToolStripMenuItem, Me.SVNToolStripMenuItem, Me.VisitGuru3dNVIDIAThreadToolStripMenuItem, Me.VisitGuru3dAMDThreadToolStripMenuItem, Me.VisitGeforceThreadToolStripMenuItem, Me.AboutToolStripMenuItem})
        Me.ToolStripMenuItem1.Name = "ToolStripMenuItem1"
        Me.ToolStripMenuItem1.Size = New System.Drawing.Size(17, 19)
        Me.ToolStripMenuItem1.Text = "?"
        '
        'ToSToolStripMenuItem
        '
        Me.ToSToolStripMenuItem.Name = "ToSToolStripMenuItem"
        Me.ToSToolStripMenuItem.Size = New System.Drawing.Size(219, 22)
        Me.ToSToolStripMenuItem.Text = "ToS"
        '
        'VisitDDUHomepageToolStripMenuItem
        '
        Me.VisitDDUHomepageToolStripMenuItem.Name = "VisitDDUHomepageToolStripMenuItem"
        Me.VisitDDUHomepageToolStripMenuItem.Size = New System.Drawing.Size(219, 22)
        Me.VisitDDUHomepageToolStripMenuItem.Text = "Visit official DDU Website"
        '
        'SVNToolStripMenuItem
        '
        Me.SVNToolStripMenuItem.Name = "SVNToolStripMenuItem"
        Me.SVNToolStripMenuItem.Size = New System.Drawing.Size(219, 22)
        Me.SVNToolStripMenuItem.Text = "Visit SVN Website"
        '
        'VisitGuru3dNVIDIAThreadToolStripMenuItem
        '
        Me.VisitGuru3dNVIDIAThreadToolStripMenuItem.Name = "VisitGuru3dNVIDIAThreadToolStripMenuItem"
        Me.VisitGuru3dNVIDIAThreadToolStripMenuItem.Size = New System.Drawing.Size(219, 22)
        Me.VisitGuru3dNVIDIAThreadToolStripMenuItem.Text = "Visit Guru3d NVIDIA Thread"
        '
        'VisitGuru3dAMDThreadToolStripMenuItem
        '
        Me.VisitGuru3dAMDThreadToolStripMenuItem.Name = "VisitGuru3dAMDThreadToolStripMenuItem"
        Me.VisitGuru3dAMDThreadToolStripMenuItem.Size = New System.Drawing.Size(219, 22)
        Me.VisitGuru3dAMDThreadToolStripMenuItem.Text = "Visit Guru3d AMD Thread"
        '
        'VisitGeforceThreadToolStripMenuItem
        '
        Me.VisitGeforceThreadToolStripMenuItem.Name = "VisitGeforceThreadToolStripMenuItem"
        Me.VisitGeforceThreadToolStripMenuItem.Size = New System.Drawing.Size(219, 22)
        Me.VisitGeforceThreadToolStripMenuItem.Text = "Visit Geforce Thread"
        '
        'AboutToolStripMenuItem
        '
        Me.AboutToolStripMenuItem.Name = "AboutToolStripMenuItem"
        Me.AboutToolStripMenuItem.Size = New System.Drawing.Size(219, 22)
        Me.AboutToolStripMenuItem.Text = "About"
        '
        'Button2
        '
        Me.Button2.Location = New System.Drawing.Point(12, 168)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(195, 70)
        Me.Button2.TabIndex = 15
        Me.Button2.UseVisualStyleBackColor = True
        '
        'Button3
        '
        Me.Button3.Location = New System.Drawing.Point(12, 244)
        Me.Button3.Name = "Button3"
        Me.Button3.Size = New System.Drawing.Size(195, 70)
        Me.Button3.TabIndex = 16
        Me.Button3.UseVisualStyleBackColor = True
        '
        'CheckBox3
        '
        Me.CheckBox3.AutoSize = True
        Me.CheckBox3.Checked = True
        Me.CheckBox3.CheckState = System.Windows.Forms.CheckState.Checked
        Me.CheckBox3.Location = New System.Drawing.Point(222, 255)
        Me.CheckBox3.Name = "CheckBox3"
        Me.CheckBox3.Size = New System.Drawing.Size(99, 17)
        Me.CheckBox3.TabIndex = 17
        Me.CheckBox3.Text = "Remove PhysX"
        Me.CheckBox3.UseVisualStyleBackColor = True
        Me.CheckBox3.Visible = False
        '
        'Label10
        '
        Me.Label10.AutoSize = True
        Me.Label10.Font = New System.Drawing.Font("Microsoft Sans Serif", 7.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label10.Location = New System.Drawing.Point(2, 76)
        Me.Label10.Name = "Label10"
        Me.Label10.Size = New System.Drawing.Size(234, 13)
        Me.Label10.TabIndex = 21
        Me.Label10.Text = "Using DDU in safemode is recommended"
        '
        'BackgroundWorker1
        '
        '
        'Label11
        '
        Me.Label11.AutoSize = True
        Me.Label11.Location = New System.Drawing.Point(5, 504)
        Me.Label11.Name = "Label11"
        Me.Label11.Size = New System.Drawing.Size(0, 13)
        Me.Label11.TabIndex = 22
        '
        'Button4
        '
        Me.Button4.Location = New System.Drawing.Point(397, 374)
        Me.Button4.Name = "Button4"
        Me.Button4.Size = New System.Drawing.Size(191, 62)
        Me.Button4.TabIndex = 23
        Me.Button4.UseVisualStyleBackColor = True
        '
        'ComboBox2
        '
        Me.ComboBox2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.ComboBox2.FormattingEnabled = True
        Me.ComboBox2.Location = New System.Drawing.Point(467, 501)
        Me.ComboBox2.Name = "ComboBox2"
        Me.ComboBox2.Size = New System.Drawing.Size(121, 21)
        Me.ComboBox2.TabIndex = 24
        '
        'Label12
        '
        Me.Label12.AutoSize = True
        Me.Label12.Location = New System.Drawing.Point(402, 504)
        Me.Label12.Name = "Label12"
        Me.Label12.Size = New System.Drawing.Size(61, 13)
        Me.Label12.TabIndex = 25
        Me.Label12.Text = "Language :"
        '
        'PictureBox2
        '
        Me.PictureBox2.ErrorImage = Nothing
        Me.PictureBox2.Image = Global.WindowsApplication1.My.Resources.Resources.Nvidia_GeForce_Logo
        Me.PictureBox2.Location = New System.Drawing.Point(333, 92)
        Me.PictureBox2.Name = "PictureBox2"
        Me.PictureBox2.Size = New System.Drawing.Size(158, 126)
        Me.PictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage
        Me.PictureBox2.TabIndex = 20
        Me.PictureBox2.TabStop = False
        '
        'PictureBox1
        '
        Me.PictureBox1.Image = Global.WindowsApplication1.My.Resources.Resources.btn_donate_LG
        Me.PictureBox1.InitialImage = CType(resources.GetObject("PictureBox1.InitialImage"), System.Drawing.Image)
        Me.PictureBox1.Location = New System.Drawing.Point(495, 468)
        Me.PictureBox1.Name = "PictureBox1"
        Me.PictureBox1.Size = New System.Drawing.Size(93, 28)
        Me.PictureBox1.TabIndex = 18
        Me.PictureBox1.TabStop = False
        '
        'CheckBox4
        '
        Me.CheckBox4.AutoSize = True
        Me.CheckBox4.Checked = True
        Me.CheckBox4.CheckState = System.Windows.Forms.CheckState.Checked
        Me.CheckBox4.Location = New System.Drawing.Point(222, 233)
        Me.CheckBox4.Name = "CheckBox4"
        Me.CheckBox4.Size = New System.Drawing.Size(120, 17)
        Me.CheckBox4.TabIndex = 26
        Me.CheckBox4.Text = "Remove 3DTV Play"
        Me.CheckBox4.UseVisualStyleBackColor = True
        Me.CheckBox4.Visible = False
        '
        'CheckBox5
        '
        Me.CheckBox5.AutoSize = True
        Me.CheckBox5.Checked = True
        Me.CheckBox5.CheckState = System.Windows.Forms.CheckState.Checked
        Me.CheckBox5.Location = New System.Drawing.Point(222, 321)
        Me.CheckBox5.Name = "CheckBox5"
        Me.CheckBox5.Size = New System.Drawing.Size(15, 14)
        Me.CheckBox5.TabIndex = 27
        Me.CheckBox5.UseVisualStyleBackColor = True
        '
        'Form1
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(600, 526)
        Me.Controls.Add(Me.CheckBox5)
        Me.Controls.Add(Me.CheckBox4)
        Me.Controls.Add(Me.Label12)
        Me.Controls.Add(Me.ComboBox2)
        Me.Controls.Add(Me.Button4)
        Me.Controls.Add(Me.Label11)
        Me.Controls.Add(Me.Label10)
        Me.Controls.Add(Me.PictureBox2)
        Me.Controls.Add(Me.PictureBox1)
        Me.Controls.Add(Me.CheckBox3)
        Me.Controls.Add(Me.Button3)
        Me.Controls.Add(Me.Button2)
        Me.Controls.Add(Me.CheckBox2)
        Me.Controls.Add(Me.CheckBox1)
        Me.Controls.Add(Me.Label7)
        Me.Controls.Add(Me.ComboBox1)
        Me.Controls.Add(Me.Label5)
        Me.Controls.Add(Me.TextBox1)
        Me.Controls.Add(Me.Label4)
        Me.Controls.Add(Me.Label3)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.Button1)
        Me.Controls.Add(Me.MenuStrip1)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MainMenuStrip = Me.MenuStrip1
        Me.MaximizeBox = False
        Me.Name = "Form1"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Display Driver Uninstaller (DDU)"
        Me.MenuStrip1.ResumeLayout(False)
        Me.MenuStrip1.PerformLayout()
        CType(Me.PictureBox2, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents Button1 As System.Windows.Forms.Button
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents TextBox1 As System.Windows.Forms.TextBox
    Friend WithEvents Label5 As System.Windows.Forms.Label
    Friend WithEvents ComboBox1 As System.Windows.Forms.ComboBox
    Friend WithEvents Label7 As System.Windows.Forms.Label
    Friend WithEvents CheckBox1 As System.Windows.Forms.CheckBox
    Friend WithEvents CheckBox2 As System.Windows.Forms.CheckBox
    Friend WithEvents MenuStrip1 As System.Windows.Forms.MenuStrip
    Friend WithEvents ToolStripMenuItem1 As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents AboutToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents Button2 As System.Windows.Forms.Button
    Friend WithEvents Button3 As System.Windows.Forms.Button
    Friend WithEvents CheckBox3 As System.Windows.Forms.CheckBox
    Friend WithEvents PictureBox1 As System.Windows.Forms.PictureBox
    Friend WithEvents VisitGuru3dNVIDIAThreadToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents VisitGuru3dAMDThreadToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents VisitGeforceThreadToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents PictureBox2 As System.Windows.Forms.PictureBox
    Friend WithEvents SVNToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents Label10 As System.Windows.Forms.Label
    Friend WithEvents BackgroundWorker1 As System.ComponentModel.BackgroundWorker
    Friend WithEvents Label11 As System.Windows.Forms.Label
    Friend WithEvents Button4 As System.Windows.Forms.Button
    Friend WithEvents ComboBox2 As System.Windows.Forms.ComboBox
    Friend WithEvents Label12 As System.Windows.Forms.Label
    Friend WithEvents ToSToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents CheckBox4 As System.Windows.Forms.CheckBox
    Friend WithEvents VisitDDUHomepageToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents CheckBox5 As System.Windows.Forms.CheckBox

End Class
