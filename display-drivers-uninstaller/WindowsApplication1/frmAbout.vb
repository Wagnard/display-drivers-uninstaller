'    Display driver Uninstaller (DDU) a driver uninstaller / Cleaner for Windows
'    Copyright (C) <2013>  <DDU dev team>

'    This program is free software: you can redistribute it and/or modify
'    it under the terms of the GNU General Public License as published by
'    the Free Software Foundation, either version 3 of the License, or
'    (at your option) any later version.

'    This program is distributed in the hope that it will be useful,
'    but WITHOUT ANY WARRANTY; without even the implied warranty of
'    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
'    GNU General Public License for more details.

'    You should have received a copy of the GNU General Public License
'    along with DDU.  If not, see <http://www.gnu.org/licenses/>.

Imports System.IO
Imports System.Text

Public Class frmAbout

	Private Sub about_Load(sender As Object, e As EventArgs) Handles MyBase.Load
		Language.TranslateForm(Me)
		Me.Size = New System.Drawing.Size(Label1.Width + 10, Label1.Height + 110)
		PictureBox1.Location = New System.Drawing.Size((Me.Size.Width / 2) - (PictureBox1.Size.Width / 2), PictureBox1.Location.Y)
	End Sub
End Class