Imports System.Windows.Threading

Class ppfAbout

    Private PicturePath As String
    Private wMain As wpfMain = CType(Application.Current.MainWindow, wpfMain)
    Private wSetting As wpfSetting = Application.SettingWindow()

#Region " Loaded "

    Private Sub ppfAbout_Loaded(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
        lblApp.Text = Application.CompanyName & "  " & Application.ProductName & "  verze " & Application.Version
        lblCop.Text = "copyright ©2000-" & mySystem.BuildYear.ToString & "  " & Application.LegalCopyright
        txtWindows.Text = "Windows " + mySystem.Current.Name

        If RegSubjekt.Name = "" Then
            gbxLicense.Header = "Tento software je Freeware"
        Else
            lblJmeno.Text = RegSubjekt.Name + If(RegSubjekt.ICO = "", "", ",  IČO  " + RegSubjekt.ICO)
            lblAdresa.Text = RegSubjekt.Email
        End If

    End Sub

#End Region

#Region " Hyperlinks "

    Private Sub txtWindows_MouseLeftButtonUp(sender As System.Object, e As System.Windows.Input.MouseButtonEventArgs) Handles txtWindows.MouseLeftButtonUp
        System.Diagnostics.Process.Start("winver")
    End Sub

    Private Sub WEB_MouseLeftButtonUp(sender As System.Object, e As System.Windows.Input.MouseButtonEventArgs) Handles txtWeb.MouseLeftButtonUp
        myLink.Start(wSetting, "http://vb.jantac.net")
    End Sub

    Private Sub MAIL_MouseLeftButtonUp(sender As System.Object, e As System.Windows.Input.MouseButtonEventArgs) Handles txtMail.MouseLeftButtonUp
        myLink.Start(wSetting, "mailto:zdenek@jantac.net")
    End Sub

    Private Sub lbl_MouseEnter(sender As System.Object, e As System.Windows.Input.MouseEventArgs) Handles txtMail.MouseEnter, txtWeb.MouseEnter, txtWindows.MouseEnter
        Me.Cursor = Cursors.Hand
    End Sub

    Private Sub lbl_MouseLeave(sender As System.Object, e As System.Windows.Input.MouseEventArgs) Handles txtMail.MouseLeave, txtWeb.MouseLeave, txtWindows.MouseLeave
        Me.Cursor = Cursors.Arrow
    End Sub
#End Region

End Class
