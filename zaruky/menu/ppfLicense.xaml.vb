Class ppfLicense

    Private PicturePath As String
    Private wMain As wpfMain = CType(Application.Current.MainWindow, wpfMain)
    Private wSetting As wpfSetting = Application.SettingWindow()
    Private dcZaruky As New zarukyContext(SdfConnection)

#Region " Loaded "

    Private Sub ppfAbout_Loaded(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
        Select Case Verze
            Case 4
                rbtFree.IsChecked = True
                rbtDonation.IsEnabled = False
                rbtTrial.IsEnabled = False
                rbtFree.IsEnabled = True
                btnApply.Visibility = Windows.Visibility.Hidden
            Case 2
                rbtTrial.IsChecked = True
            Case 1
                rbtFree.IsChecked = True
                If TrialRunOut Then rbtTrial.IsEnabled = False
        End Select
    End Sub

    Private Sub rbtFree_Click(sender As Object, e As RoutedEventArgs) Handles rbtFree.Checked, rbtTrial.Checked, rbtDonation.Checked
        Dim rbt As RadioButton = CType(sender, RadioButton)
        btnApply.Content = "Aktivovat"
        btnApply.IsEnabled = True
        Select Case rbt.Content.ToString
            Case "Freeware"
                If Verze = 1 Then btnApply.IsEnabled = False
                LoadLicense(1)
            Case "Trialware"
                If Verze = 2 Then btnApply.IsEnabled = False
                LoadLicense(2)
            Case "Donationware"
                btnApply.Content = "Registrovat"
                LoadLicense(4)
        End Select
    End Sub

    Private Sub LoadLicense(ByVal Version As Integer)
        txtLicense.Text = myString.FromBytes(myFile.ReadEmbeddedResource(If(Lge, "CZ", "EN") & "-" & Version.ToString & "-License.txt"))
    End Sub

#End Region

#Region " Change License "

    Private Sub btnApply_Click(sender As Object, e As RoutedEventArgs) Handles btnApply.Click
        btnApply.IsEnabled = False
        If rbtDonation.IsChecked Then
            wSetting.SwitchPage("Registr")
        Else
            Verze = If(rbtFree.IsChecked, 1, 2)
            Dim FormDialog = New wpfDialog(wSetting, "Změna licence provedena. Program je není " + If(rbtFree.IsChecked, "Freeware.", "Trialware."), "Aktivace licence", wpfDialog.Ikona.ok, "Zavřít")
            FormDialog.ShowDialog()
            wSetting.ReloadNeeded = True
            wSetting.Close()
        End If
    End Sub

#End Region

End Class
