Public Class wpfSetting

    Private wMain As wpfMain = CType(Application.Current.MainWindow, wpfMain)
    Public Property StartPageName As String
    Public Property ReloadNeeded As Boolean

    Private Sub wpfSetting_Initialized(sender As Object, e As EventArgs) Handles Me.Initialized
        Me.Left = wMain.Left + 50
        Me.Top = wMain.Top + 50
        Me.Icon = Application.Icon
    End Sub

    Private Sub wpfSetting_Loaded(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
        SwitchPage(StartPageName)
        lbxMenu.Focus()
    End Sub

    Public Sub SwitchPage(PageName As String)
        Select Case PageName
            Case "About"
                lbxMenu.SelectedIndex = 0
            Case "License"
                lbxMenu.SelectedIndex = 1
            Case "Registr"
                lbxMenu.SelectedIndex = 2
        End Select
    End Sub

    Private Sub lbxMenu_SelectionChanged(sender As System.Object, e As System.Windows.Controls.SelectionChangedEventArgs) Handles lbxMenu.SelectionChanged
        Dim item As StackPanel = CType(lbxMenu.SelectedItem, StackPanel)
        If IsNothing(item) = False Then
            Select Case item.Tag.ToString
                Case "About"
                    FramePage.Navigate(New ppfAbout)
                Case "License"
                    FramePage.Navigate(New ppfLicense)
                Case "Registr"
                    FramePage.Navigate(New ppfRegistr)
            End Select
        End If
    End Sub

End Class
