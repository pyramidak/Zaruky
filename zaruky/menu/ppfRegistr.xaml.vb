Imports System.Net.Mail

Class ppfRegistr

#Region " Properties "

    Private wMain As wpfMain = CType(Application.Current.MainWindow, wpfMain)
    Private wSetting As wpfSetting = Application.SettingWindow()

#End Region

#Region " Load "

    Private Sub ppfRegistr_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        txtWindows.Text = mySystem.GetProductID
        If txtWindows.Text = "" Then
            ckbWindows.IsEnabled = False
        ElseIf txtWindows.Text = "YKHFT-KW986-GK4PY-FDWYH-7TP9F" Or txtWindows.Text = "00371-OEM-8992671-00004" _
            Or txtWindows.Text = "TY4CG-JDJH7-VJ2WF-DY4X9-HCFC6" Or txtWindows.Text = "00330-80000-00000-AA055" Then
            txtWindows.Text = "číslo systému není jedinečné"
            ckbWindows.IsEnabled = False
        Else
            ckbWindows.IsChecked = True
        End If
        cbxHarddisk.ItemsSource = mySystem.Harddisks.OrderBy(Function(a) a.Letter)
        If cbxHarddisk.Items.Count = 0 Then
            ckbHarddisk.IsEnabled = False
            cbxHarddisk.IsEnabled = False
            ckbDrive.IsChecked = True
        Else
            Try
                cbxHarddisk.SelectedItem = mySystem.Harddisks.FirstOrDefault(Function(x) x.Letter = mySystem.DiskLetter)
            Catch ex As Exception
            End Try
            If cbxHarddisk.SelectedItem Is Nothing Then
                cbxHarddisk.SelectedIndex = 0
            End If
            ckbHarddisk.IsChecked = True
            mDriveCombo.NoFixed = True
        End If
        mDriveCombo.NoRemovable = True : mDriveCombo.NoCdrom = True : mDriveCombo.NoFloppy = True
        mDriveCombo.Reload()
        If mDriveCombo.cbxDisks.Items.Count = 0 Then
            mDriveCombo.cbxDisks.IsEnabled = False
            ckbDrive.IsChecked = False
            ckbDrive.IsEnabled = False
        Else
            mDriveCombo.cbxDisks.SelectedIndex = 0
        End If
        txtAdresa.Text = myRegister.GetValue(HKEY.CURRENT_USER, "Software\pyramidak", "UserEmail", "")
        txtRule.Text = "Licence musí být registrována k nějaké unikátní hodnotě jako je třeba sériové číslo disku. Nalevo si můžete vybrat dvě možnosti. Pokud program nenajde alespoň jednu platnou hodnotu, spustí se jako Freeware verze. Pokud změníte systém i disk, nezbude vám než provézt novou registraci, přispět na vývoj programu znovu."
    End Sub

    Private Sub ckbWindows_Unchecked(sender As Object, e As RoutedEventArgs) Handles ckbWindows.Unchecked
        If ckbHarddisk.IsChecked = False Then ckbHarddisk.IsChecked = True
        UpdatePrice()
    End Sub

    Private Sub ckbDrive_Checked(sender As Object, e As RoutedEventArgs) Handles ckbDrive.Checked, ckbDrive.Unchecked
        ckbHarddisk.IsChecked = Not ckbDrive.IsChecked
        UpdatePrice()
    End Sub

    Private Sub ckbHarddisk_Checked(sender As Object, e As RoutedEventArgs) Handles ckbHarddisk.Checked, ckbHarddisk.Unchecked
        If ckbDrive.IsEnabled Then
            ckbDrive.IsChecked = Not ckbHarddisk.IsChecked
        Else
            If ckbWindows.IsChecked = False And ckbHarddisk.IsChecked = False Then
                If ckbWindows.IsEnabled Then
                    ckbWindows.IsChecked = True
                Else
                    ckbHarddisk.IsChecked = True
                End If
            End If
        End If
        UpdatePrice()
    End Sub

    Private Sub UpdatePrice()
        txtMoney.Text = "300"
        If ckbHarddisk.IsChecked AndAlso cbxHarddisk.SelectedItem IsNot Nothing AndAlso CType(cbxHarddisk.SelectedItem, clsSystem.clsHarddisk).Type = DiskTypes.Flashdisk_8 Then
            txtMoney.Text = "500"
        End If
        If ckbDrive.IsChecked AndAlso mDriveCombo.SelectedDisk IsNot Nothing AndAlso mDriveCombo.SelectedDisk.Type = DiskTypes.Server_4 Then
            txtMoney.Text = "1000"
        End If
    End Sub

    Private Sub cbxHarddisk_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles cbxHarddisk.SelectionChanged
        UpdatePrice()
    End Sub

    Private Sub mDriveCombo_SelectionChanged(Disk As DriveCombo.clsDisk) Handles mDriveCombo.SelectionChanged
        UpdatePrice()
    End Sub

#End Region

#Region " Send "

    Private WithEvents ThreadWorker As New System.ComponentModel.BackgroundWorker
    Private myMail As New MailMessage
    Private Port As Boolean
    Private Chyba As Exception

    Private Sub ThreadWorker_DoWork(sender As Object, e As ComponentModel.DoWorkEventArgs) Handles ThreadWorker.DoWork
        Port = Not Port
        Dim smtp As New SmtpClient
        smtp.Host = "smtp.seznam.cz"
        smtp.Port = 587
        smtp.EnableSsl = True
        smtp.DeliveryMethod = SmtpDeliveryMethod.Network
        smtp.Timeout = 5000
        smtp.Credentials = New System.Net.NetworkCredential("pyramidak@post.cz", PasswordMail)
        Chyba = Nothing
        Try
            smtp.Send(myMail)
        Catch Ex As Exception
            Chyba = Ex 'časový limit operace vypršel má číslo chyby 5
        End Try
    End Sub

    Private Sub ThreadWorker_RunWorkerCompleted(sender As Object, e As ComponentModel.RunWorkerCompletedEventArgs) Handles ThreadWorker.RunWorkerCompleted
        Dim wDialog As wpfDialog = Nothing
        If Chyba Is Nothing Then
            wDialog = New wpfDialog(wSetting, "Žádost o registraci úspěšně odeslána.", wSetting.Title, wpfDialog.Ikona.ok, "Zavřít")
        Else
            btnSend.IsEnabled = True
            wDialog = New wpfDialog(wSetting, "Nepovedlo se odeslat žádost do mailboxu. " + NR + "Zkuste to prosím znovu nebo pošlete email ručně." + NR + NR + Chyba.Message, wSetting.Title, wpfDialog.Ikona.varovani, "Zavřít")
        End If
        wDialog.ShowDialog()
    End Sub

    Private Sub btnSend_Click(sender As Object, e As RoutedEventArgs) Handles btnSend.Click
        If CheckEntries() = False Then Exit Sub
        btnSend.IsEnabled = False
        If txtAdresa.Text.Contains("@") Then myRegister.CreateValue(HKEY.CURRENT_USER, "Software\pyramidak", "UserEmail", txtAdresa.Text)
        myMail.From = New MailAddress("pyramidak@post.cz", "pyramidak software")
        myMail.To.Add("pyramidak@post.cz")
        myMail.Subject = "Registrace Záruk"
        myMail.Body = RegNoteString(CType(cbxHarddisk.SelectedItem, clsSystem.clsHarddisk), mDriveCombo.SelectedDisk)
        myMail.IsBodyHtml = False
        ThreadWorker.RunWorkerAsync()
    End Sub

    Private Sub btnCopy_Click(sender As Object, e As RoutedEventArgs) Handles btnCopy.Click
        If CheckEntries() = False Then Exit Sub
        Try
            Clipboard.SetText(RegNoteString(CType(cbxHarddisk.SelectedItem, clsSystem.clsHarddisk), mDriveCombo.SelectedDisk))
            Dim wDialog = New wpfDialog(wSetting, "Obsah žádosti byl zkopírován do schránky Windowsu." + NR +
                                        "Žádost do vámi vytvořeného emailu vložíte CTRL + V." + NR +
                                        "Email zašlete na:     zdenek@jantac.net", "Zaslání žádosti", Nothing, "Rozumím")
            wDialog.ShowDialog()
        Catch
            Dim wDialog = New wpfDialog(wSetting, "Nepodařilo se přenést žádost do schránky Windowsu." + NR + "Zkuste prosím použít automatické zaslání žádosti.", wSetting.Title, wpfDialog.Ikona.varovani, "Zavřít")
            wDialog.ShowDialog()
        End Try
    End Sub

    Private Function CheckEntries() As Boolean
        If txtJmeno.Text = "" Then
            txtJmeno.Focus() : CheckEntries = False
            Dim wDialog As New wpfDialog(wSetting, "Vyplňte prosím vaše jméno nebo název společnosti, na kterou bude licence registrována.", wSetting.Title, wpfDialog.Ikona.varovani, "Zavřít")
            wDialog.ShowDialog()
            Return False
        ElseIf txtAdresa.Text.Contains("@") = False Then
            txtAdresa.Focus() : CheckEntries = False
            Dim wDialog As New wpfDialog(wSetting, "Vyplňte prosím vaši platnou emailovou adresu, ke které bude licence registrována.", wSetting.Title, wpfDialog.Ikona.varovani, "Zavřít")
            wDialog.ShowDialog()
            Return False
        ElseIf ckbDrive.IsChecked = False And ckbHarddisk.IsChecked = False And ckbWindows.IsChecked = False Then
            CheckEntries = False
            Dim wDialog As New wpfDialog(wSetting, "Musíte zaškrtnout alespoň jednu možnost, na co bude licence registrována.", wSetting.Title, wpfDialog.Ikona.varovani, "Zavřít")
            wDialog.ShowDialog()
            Return False
        End If
        myRegister.CreateValue(HKEY.CURRENT_USER, "Software\pyramidak", "UserEmail", txtAdresa.Text)
        Return True
    End Function
#End Region

#Region " Create Email or Note "
    'windows: 0 produkt id
    'hardware: 7 fixed, 8 removable

    Private Function RegNoteString(Drive As clsSystem.clsHarddisk, Disk As DriveCombo.clsDisk) As String
        Return "Obsah tohoto emailu pošlete, pokud již máte nebo" & NR _
            & "chcete program Záruky Pro v licenci Donationware." & NR & NR _
            & "Doplňte informace níže, ke kterým bude licence registrována." & NR & NR _
            & "Vaše jméno nebo název společnosti: " & If(txtJmeno.Text = "", "<doplňte>", txtJmeno.Text) & NR _
            & "IČO vaší společnosti: " & If(txtICO.Text = "", "<doplňte>", txtICO.Text) & NR _
            & "Vaše emailová adresa: " & If(txtAdresa.Text = "", "<doplňte>", txtAdresa.Text) & NR & NR _
            & "Registrovat na " & NR _
            & If(ckbWindows.IsChecked, "produktové číslo Windows: " & mySystem.GetProductID.Replace("-", "") & "-0" & NR, "") _
            & If(ckbWindows.IsChecked, "digitální číslo Windows: " & mySystem.GetDigitalProductID.Replace("-", "") & "-0" & NR, "") _
            & If(ckbHarddisk.IsChecked, "výrobní číslo disku: " & Drive.SerialNumber & "-" & CInt(Drive.Type).ToString & NR, "") _
            & If(ckbDrive.IsChecked, "číslo logického disku: " & Disk.Number & "-" & CInt(Disk.Type).ToString & NR, "") _
            & NR & "Výše příspěvku: " & txtMoney.Text & NR _
            & "Vygenerovala verze: " & Application.Version & If(Verze = 4, " Pro", "") & NR _
            & "Systém: Windows " & mySystem.Current.Name & " " & If(mySystem.Is64bit, "64bit", "32bit") & NR & NR _
            & "Zašlete na email: zdenek@jantac.net"
    End Function
#End Region

#Region " Show info "

    Private Sub ckbWindows_MouseEnter(sender As Object, e As MouseEventArgs) Handles ckbWindows.MouseEnter, txtWindows.MouseEnter
        txtInfo.Text = "Určitě nechte zatrženou registraci na produktové číslo Windows. Změnou hardwaru tak nepřijdete o licenci, pokud nezměníte produktové číslo Windows."
    End Sub

    Private Sub ckbHarddisk_MouseEnter(sender As Object, e As MouseEventArgs) Handles ckbHarddisk.MouseEnter, cbxHarddisk.MouseEnter
        txtInfo.Text = "Vyberte jeden pevný disk nebo flashdisk, ke kterému se bude vázat licence. Licence bude platit, pokud systém poskytne výrobní číslo. Na registrovaný disk můžete ukládat databázi, jinak vždy lze ukládat do složky Documents a do cloudu."
    End Sub

    Private Sub ckbDrive_MouseEnter(sender As Object, e As MouseEventArgs) Handles ckbDrive.MouseEnter, mDriveCombo.MouseEnter
        txtInfo.Text = "Vyberte logickou jednotku pro registraci na vzdálený disk na serveru nebo v případě nemožnosti registrovat na fyzický disk. V případě změny čísla vzdálené jednotky zašlete žádost znova - dvě změny jsou zdarma."
    End Sub

    Private Sub btnSend_MouseEnter(sender As Object, e As MouseEventArgs) Handles btnSend.MouseEnter
        txtInfo.Text = "Žádost bude (bezpečně SSL šifrování) poslána do mailboxu pyramidak@post.cz. Druhou možností je zaslání emailu ručně tlačítkem Zkopírovat žádost."
    End Sub

    Private Sub btnCopy_MouseEnter(sender As Object, e As MouseEventArgs) Handles btnCopy.MouseEnter
        txtInfo.Text = "Zpráva bude zkopírována do schránky Windows, kterou vložíte do vámi vytvořeného nového emailu pomocí CTRL + V. Email zašlete na zdenek@jantac.net."
    End Sub

    Private Sub txtJmeno_MouseEnter(sender As Object, e As MouseEventArgs) Handles txtJmeno.MouseEnter
        txtInfo.Text = "Jméno či název společnosti je povinný údaj."
    End Sub

    Private Sub txtAdresa_MouseEnter(sender As Object, e As MouseEventArgs) Handles txtAdresa.MouseEnter
        txtInfo.Text = "Váš email (kontakt na vás) je povinný údaj."
    End Sub

    Private Sub txtICO_MouseEnter(sender As Object, e As MouseEventArgs) Handles txtICO.MouseEnter
        txtInfo.Text = "IČO je povinný údaj pouze pokud registrujete společnost."
    End Sub



#End Region

#Region " Pay "
    Private Sub btnPay_MouseEnter(sender As Object, e As MouseEventArgs)
        txtInfo.Text = "Po zaslání žádosti o registraci můžete poslat platbu pomocí PayPal. Nemusíte mít účet u této služby, lze zaplatit kartou." + NR + NR + "Pokud chcete platit bankovním převodem, vyčkejte na odpověd na zaslanou žádost."
    End Sub

    Private Sub btnPay_Click(sender As Object, e As RoutedEventArgs)
        Dim sPayPal As String = "https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=zdenek%40jantac%2enet&lc=CZ&item_name=pyramidak%20Zaruky&amount=300%2e00&currency_code=CZK&bn=PP%2dDonationsBF%3apaypal%2esvg%3aNonHosted"
        sPayPal = sPayPal.Replace("=300", "=" + txtMoney.Text)
        myLink.Start(wSetting, sPayPal)
    End Sub
#End Region

End Class
