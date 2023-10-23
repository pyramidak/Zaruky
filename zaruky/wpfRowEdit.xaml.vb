Public Class wpfRowEdit

#Region " Properties "

    Private dcZaruky As zarukyContext
    Private WithEvents ZarukyView As BindingListCollectionView
    Private PocetRows As Integer
    Public Property EditDatabaze() As String
    Public Property SelectedID() As Integer
    Public Property TrialRunOut As Boolean
    Public Property PocetZmen As Integer

#End Region

#Region " Load "

    Private Sub wpfRowEdit_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
        If e.Key = Key.Escape Then Me.Close()
    End Sub

    Private Sub wpfRowEdit_Closed(sender As Object, e As EventArgs) Handles Me.Closed
        Nastaveni.KolonkyVypraznit = CBool(ckbClear.IsChecked)
        Nastaveni.KolonkyDelsi = CBool(ckbLength.IsChecked)
        Nastaveni.CombaPlnitVsemi = CBool(ckbCombo.IsChecked)
        Nastaveni.RowEditWidth = CInt(Me.ActualWidth)
        If ZarukyView.Count > 0 Then
            SelectedID = CType(ZarukyView.CurrentItem, Zaruky).ID
        End If
    End Sub

    Private Sub wpfRowEdit_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Me.Title = "Editační formulář databáze " + EditDatabaze.ToUpper
        If EditDatabaze.ToLower = "průkazy" Or EditDatabaze.ToLower = "doklady" Then
            lblRoky.Text = "Platnost roků"
            lblMesice.Text = "Platnost měsíců"
        Else
            lblRoky.Text = "Záruka roků"
            lblMesice.Text = "Záruka měsíců"
        End If

        Me.Width = Nastaveni.RowEditWidth
        ckbBold.IsChecked = Nastaveni.RowEditFontBold
        ckbClear.IsChecked = Nastaveni.KolonkyVypraznit
        ckbLength.IsChecked = Nastaveni.KolonkyDelsi
        ckbCislo.IsChecked = Nastaveni.CislovaniDokladu

        Dim Bind0 As New Binding("Vec")
        Bind0.Converter = New clsDataConverter
        Bind0.ConverterParameter = myMaxList.Item("Vec")
        cbxVec.SetBinding(ComboBox.TextProperty, Bind0)
        Dim Bind1 As New Binding("DatPoc")
        Bind1.Converter = New clsDataConverter
        Bind1.ConverterParameter = myMaxList.Item("DatPoc")
        Bind1.StringFormat = "d"
        txtDatPoc.SetBinding(TextBox.TextProperty, Bind1)
        Dim Bind2 As New Binding("DatKon")
        Bind2.Converter = New clsDataConverter
        Bind2.ConverterParameter = myMaxList.Item("DatKon")
        Bind2.StringFormat = "d"
        txtDatKon.SetBinding(TextBox.TextProperty, Bind2)

        dcZaruky = New zarukyContext(SdfConnection)
        proLoadDatabaze()
        proLoadCombos()
        PocetRows = (From a As Zaruky In dcZaruky.Zarukies Where a.Databaze <> EditDatabaze Select a.ID).Count
        Dim queryZaruky As IQueryable(Of Zaruky) = From a As Zaruky In dcZaruky.Zarukies Where a.Databaze = EditDatabaze Select a Order By a.OwnID
        Me.DataContext = queryZaruky
        ZarukyView = CType(CollectionViewSource.GetDefaultView(Me.DataContext), BindingListCollectionView)
        EnableAll(True)
        UpdateDisplay()
        If Not ZarukyView.Count = 0 Then
            Dim dr As Zaruky = queryZaruky.FirstOrDefault(Function(x) x.ID = SelectedID)
            If dr IsNot Nothing Then ZarukyView.MoveCurrentTo(dr)
        End If
    End Sub

    Private Function FullVerze() As Boolean
        Return If(Verze = 1, False, If(Verze = 4, True, If(PocetRows + ZarukyView.Count - 1 < coRows, True, False)))
    End Function

#Region " Databaze "

    Private Sub proLoadDatabaze()
        Dim queryDatabaze As IQueryable(Of Databaze) = From a As Databaze In dcZaruky.Databazes Select a
        If queryDatabaze.Count = 1 Then btnPresunout.IsEnabled = False
        For Each dr As Databaze In queryDatabaze
            If EditDatabaze = dr.Jmeno Then
                lblMena.Text = dr.Mena : lblMenaOpt.Text = dr.Mena
                lblVec.Text = dr.Vec
                lblSerNum.Text = dr.SerNum : ckbOwn.Content = dr.OwnCheck
                lblDatPoc.Text = dr.DatPoc : lblDatKon.Text = dr.DatKon : lblDatOpt.Text = dr.DatOpt
                lblDodavatel.Text = dr.Dodavatel : lblFaktura.Text = dr.Faktura : lblCena.Text = dr.Cena : lblCenaOpt.Text = dr.CenaOpt
                lblOptio1.Text = dr.Optio1 : lblOptio2.Text = dr.Optio2 : lblOptio3.Text = dr.Optio3 : lblOptio4.Text = dr.Optio4 : lblOptio5.Text = dr.Optio5
            Else
                Dim newItem As New MenuItem
                newItem.Header = dr.Jmeno
                btnPresunout.ContextMenu.Items.Add(newItem)
                AddHandler newItem.Click, AddressOf cmiPresunout_Click
            End If
        Next
    End Sub
#End Region

#Region " ComboBox "

    Private Sub proLoadCombos()
        Dim queryVec As IQueryable
        If ckbCombo.IsChecked Then
            queryVec = From a As Zaruky In dcZaruky.Zarukies Select a.Vec Distinct
        Else
            queryVec = From a As Zaruky In dcZaruky.Zarukies Where a.Databaze = EditDatabaze Select a.Vec Distinct
        End If
        cbxVec.ItemsSource = queryVec

        Dim queryDodavatel As IQueryable
        If ckbCombo.IsChecked Then
            queryDodavatel = From a As Zaruky In dcZaruky.Zarukies Select a.Dodavatel Distinct
        Else
            queryDodavatel = From a As Zaruky In dcZaruky.Zarukies Where a.Databaze = EditDatabaze Select a.Dodavatel Distinct
        End If
        cbxDodavatel.ItemsSource = queryDodavatel

        Dim queryOptio1 As IQueryable
        If ckbCombo.IsChecked Then
            queryOptio1 = From a As Zaruky In dcZaruky.Zarukies Select a.Optio1 Distinct
        Else
            queryOptio1 = From a As Zaruky In dcZaruky.Zarukies Where a.Databaze = EditDatabaze Select a.Optio1 Distinct
        End If
        cbxOptio1.ItemsSource = queryOptio1

        Dim queryOptio4 As IQueryable
        If ckbCombo.IsChecked Then
            queryOptio4 = From a As Zaruky In dcZaruky.Zarukies Select a.Optio4 Distinct
        Else
            queryOptio4 = From a As Zaruky In dcZaruky.Zarukies Where a.Databaze = EditDatabaze Select a.Optio4 Distinct
        End If
        cbxOptio4.ItemsSource = queryOptio4
    End Sub

#End Region

#Region " Font "

    Private Sub ckbBold_Checked(sender As Object, e As RoutedEventArgs) Handles ckbBold.Checked, ckbBold.Unchecked
        Font(If(ckbBold.IsChecked, FontWeights.Bold, FontWeights.Normal))
        Nastaveni.RowEditFontBold = CBool(ckbBold.IsChecked)
    End Sub

    Private Sub Font(ByVal Weight As FontWeight)
        txtID.FontWeight = Weight
        cbxVec.FontWeight = Weight
        txtSerNum.FontWeight = Weight
        txtDatPoc.FontWeight = Weight
        txtMesice.FontWeight = Weight
        txtRoky.FontWeight = Weight
        txtCena.FontWeight = Weight
        txtOptio2.FontWeight = Weight
        txtOptio3.FontWeight = Weight
        cbxDodavatel.FontWeight = Weight
        txtFaktura.FontWeight = Weight
        cbxOptio1.FontWeight = Weight
        txtDatOpt.FontWeight = Weight
        txtCenaOpt.FontWeight = Weight
        cbxOptio4.FontWeight = Weight
        txtOptio5.FontWeight = Weight
        txtDatKon.FontWeight = Weight
        lblMena.FontWeight = Weight
        lblMenaOpt.FontWeight = Weight
    End Sub

#End Region

#End Region

#Region " ContextMenu "

    Private Sub cmiMark_Click(sender As Object, e As RoutedEventArgs)
        Dim delRow As Zaruky = CType(ZarukyView.CurrentItem, Zaruky)
        If delRow.Databaze = EditDatabaze Then
            delRow.Smazano = Not delRow.Smazano
        Else
            delRow.Smazano = False
        End If
        delRow.Databaze = EditDatabaze
        UpdateDisplay()
    End Sub

    Private Sub cmiDelete_Click(sender As Object, e As RoutedEventArgs)
        If Not mFileBrowser.FilesCount = 0 Then
            Dim FormDialog = New wpfDialog(Me, "Spolu s položkou budou smazány i všechny přílohy." + NR + NR + "Smazat položku i přílohy?", Application.Title, wpfDialog.Ikona.dotaz, "Smazat", "Zrušit")
            If FormDialog.ShowDialog() = False Then Exit Sub
            mFileBrowser.DeleteAllFiles()
        End If
        ZarukyView.Remove(CType(ZarukyView.CurrentItem, Zaruky))
        PocetZmen += 1
    End Sub

    Private Sub OpenContextMenu(ByVal cmu As ContextMenu, control As UIElement)
        cmu.PlacementTarget = control
        cmu.Placement = Primitives.PlacementMode.Right
        cmu.IsOpen = True
    End Sub

    Private Sub btnSmazat_Click(sender As Object, e As RoutedEventArgs) Handles btnSmazat.Click
        OpenContextMenu(btnSmazat.ContextMenu, btnSmazat)
    End Sub

    Private Sub btnSmazat_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles btnSmazat.MouseDown
        If e.RightButton = MouseButtonState.Pressed Then
            OpenContextMenu(btnSmazat.ContextMenu, btnSmazat)
        End If
    End Sub

    Private Sub btnPresunout_Click(sender As Object, e As RoutedEventArgs) Handles btnPresunout.Click
        OpenContextMenu(btnPresunout.ContextMenu, btnPresunout)
    End Sub

    Private Sub btnPresunout_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles btnPresunout.MouseDown
        If e.RightButton = MouseButtonState.Pressed Then
            OpenContextMenu(btnPresunout.ContextMenu, btnPresunout)
        End If
    End Sub

    Private Sub btnPriloha_Click(sender As Object, e As RoutedEventArgs) Handles btnPriloha.Click
        OpenContextMenu(CType(mFileBrowser.FindResource("myMenu"), ContextMenu), btnPriloha)
    End Sub

    Private Sub btnPriloha_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles btnPriloha.MouseDown
        If e.RightButton = MouseButtonState.Pressed Then
            OpenContextMenu(CType(mFileBrowser.FindResource("myMenu"), ContextMenu), btnPriloha)
        End If
    End Sub

    Private Sub cmiPresunout_Click(sender As Object, e As RoutedEventArgs)
        Dim sChangeDatabaze As String = CType(sender, MenuItem).Header.ToString
        If myLogFile.OpenAccess(sChangeDatabaze, Me) Then
            Dim delRow As Zaruky = CType(ZarukyView.CurrentItem, Zaruky)
            'Dim fromDB As IQueryable(Of Databaze) = From a As Databaze In dcZaruky.Databazes Where a.Jmeno = EditDatabaze Select a
            'Dim toDB As IQueryable(Of Databaze) = From a As Databaze In dcZaruky.Databazes Where a.Jmeno = sChangeDatabaze Select a
            'If Not fromDB.First.Mena.ToLower = toDB.First.Mena.ToLower Then
            'Dim FormDialog = New wpfDialog(Me, "Cílová databáze má jinou měnu. Pokud chcete přepočítat cenu, napište číslo, kterým bude vynásobena cena položky (např. 0,32):", fromDB.First.Mena + " na " + toDB.First.Mena, "tuzka", "Přepočítat", "Přeskočit", True)
            'If FormDialog.ShowDialog() Then
            'If IsNumeric(FormDialog.Input) Then
            'delRow.Cena = CDec(delRow.Cena * CDec(FormDialog.Input))
            'delRow.CenaOpt = CDec(delRow.CenaOpt * CDec(FormDialog.Input))
            'End If
            'End If
            'End If
            delRow.NewID = mySQL.GetFreeOwnID(dcZaruky, If(Nastaveni.CislovaniDokladu, "", sChangeDatabaze), CInt(delRow.OwnID))
            delRow.Smazano = False
            delRow.Databaze = sChangeDatabaze
            UpdateDisplay()
        End If
    End Sub

#End Region

#Region " Validating "

    Private Sub ckbLength_Checked(sender As Object, e As RoutedEventArgs) Handles ckbLength.Checked, ckbLength.Unchecked
        EditableComboBox.SetMaxLength(cbxVec, myMaxList.GetLength("Vec", CBool(ckbLength.IsChecked)))
        txtSerNum.MaxLength = myMaxList.GetLength("SerNum", CBool(ckbLength.IsChecked))
        EditableComboBox.SetMaxLength(cbxDodavatel, myMaxList.GetLength("Dodavatel", CBool(ckbLength.IsChecked)))
        txtFaktura.MaxLength = myMaxList.GetLength("Faktura", CBool(ckbLength.IsChecked))
        EditableComboBox.SetMaxLength(cbxOptio1, myMaxList.GetLength("Optio1", CBool(ckbLength.IsChecked)))
        txtOptio2.MaxLength = myMaxList.GetLength("Optio2", CBool(ckbLength.IsChecked))
        txtOptio3.MaxLength = myMaxList.GetLength("Optio3", CBool(ckbLength.IsChecked))
        EditableComboBox.SetMaxLength(cbxOptio4, myMaxList.GetLength("Optio4", CBool(ckbLength.IsChecked)))
        txtOptio5.MaxLength = myMaxList.GetLength("Optio5", CBool(ckbLength.IsChecked))
    End Sub

    Private Sub txtID_Validating(ByVal sender As Object, ByVal e As DataTransferEventArgs) Handles txtID.TargetUpdated
        If txtID.IsEnabled = False Or txtID.Text = "" Then Exit Sub
        Dim newID As Integer = CInt(txtID.Text)
        If newID < 1 Then
            e.Handled = True
            CType(ZarukyView.CurrentItem, Zaruky).NewID = mySQL.GetFreeOwnID(dcZaruky, If(Nastaveni.CislovaniDokladu, "", EditDatabaze), newID)
            txtID.Text = CType(ZarukyView.CurrentItem, Zaruky).NewID.ToString
        End If
    End Sub

    Private Sub txtMesice_TextChanged(sender As Object, e As TextChangedEventArgs) Handles txtMesice.TextChanged
        If txtMesice.IsEnabled = False Or txtMesice.Text = "" Then Exit Sub
        FixRokMesic()
        Dim roky, mesice As Integer
        roky = CInt(txtRoky.Text) : mesice = CInt(txtMesice.Text)
        If mesice > 11 Then
            roky = CInt(roky + Int(mesice / 12))
            If roky > 85 Then roky = 85
            mesice = CInt(mesice - (12 * Int(mesice / 12)))
        End If
        txtRoky.Text = CStr(roky)
        txtMesice.Text = CStr(mesice)
        UpdateDatKon()
    End Sub

    Private Sub FixRokMesic()
        If IsNumeric(txtMesice.Text) = False Then txtMesice.Text = "0"
        If CInt(txtMesice.Text) < 0 Then txtMesice.Text = "0"
        If IsNumeric(txtRoky.Text) = False Then txtRoky.Text = "0"
        If CInt(txtRoky.Text) < 0 Then txtRoky.Text = "0"
    End Sub

    Private Sub txtRoky_TextChanged(sender As Object, e As TextChangedEventArgs) Handles txtRoky.TextChanged
        If txtRoky.IsEnabled = False Or txtRoky.Text = "" Then Exit Sub
        FixRokMesic()
        Dim roky As Integer = CInt(txtRoky.Text)
        If roky > 90 Then roky = 90
        txtRoky.Text = CStr(roky)
        UpdateDatKon()
    End Sub

    Private Sub txtDatPoc_TextChanged(sender As Object, e As TextChangedEventArgs) Handles txtDatPoc.TextChanged
        UpdateDatKon()
    End Sub

    Private Sub UpdateDatKon()
        If IsDate(txtDatPoc.Text) = False Then Exit Sub
        If IsNumeric(txtRoky.Text) = False Then txtRoky.Text = "0"
        If IsNumeric(txtMesice.Text) = False Then txtMesice.Text = "0"
        Dim mesice As Integer = CInt(txtRoky.Text) * 12 + CInt(txtMesice.Text)
        Dim dateKon As Date = DateAdd(DateInterval.Month, mesice, CDate(txtDatPoc.Text))
        RemoveHandler txtDatKon.TextChanged, AddressOf txtDatKon_TextChanged
        CType(ZarukyView.CurrentItem, Zaruky).DatKon = dateKon
        AddHandler txtDatKon.TextChanged, AddressOf txtDatKon_TextChanged
    End Sub

    Private Sub txtDatKon_TextChanged(sender As Object, e As TextChangedEventArgs) Handles txtDatKon.TextChanged
        If IsDate(txtDatKon.Text) = False Then Exit Sub
        SetRokyMesice(CType(ZarukyView.CurrentItem, Zaruky).DatPoc.Value, CDate(txtDatKon.Text))
    End Sub

    Private Sub SetRokyMesice(DatPoc As Date, DatKon As Date)
        Dim roky, mesice As Integer
        mesice = CInt(DateDiff(DateInterval.Month, DatPoc, DatKon))
        If mesice > 11 Then
            roky = CInt(roky + Int(mesice / 12))
            mesice = CInt(mesice - (12 * Int(mesice / 12)))
        End If
        txtRoky.Text = CStr(roky)
        txtMesice.Text = CStr(mesice)
    End Sub

#End Region

#Region " Navigation "

#Region " UpdateDisplay "

    Private Sub UpdateDisplay()
        Dim First, Last As Boolean

        If ZarukyView.Count = 0 Then
            txtPozice.Content = "není záznam"
            txtRoky.Text = "" : txtMesice.Text = ""
            EnableAll(False)
        Else
            Dim dr As Zaruky = CType(ZarukyView.CurrentItem, Zaruky)
            If dr.Vec Is Nothing Then Exit Sub
            Dim rState As String = "nezměněna"
            If dcZaruky.GetChangeSet.Inserts.Contains(dr) Then
                rState = "přidána"
            End If
            If dcZaruky.GetChangeSet.Deletes.Contains(dr) Then
                rState = "smazána"
            End If
            If dcZaruky.GetChangeSet.Updates.Contains(dr) Then
                rState = "upravena"
            End If

            Dim TT As New ToolTip
            If dr.Smazano Or Not dr.Databaze = EditDatabaze Then
                EnableAll(False)
                rState = If(dr.Smazano, "smazána", "přesunuta")
                btnSmazat.IsEnabled = True
                btnPresunout.IsEnabled = If(dr.Smazano And Not btnPresunout.ContextMenu.Items.Count = 0, FullVerze(), False)
                TT.Content = "Obnovit položku."
                CType(btnSmazat.ContextMenu.Items(0), MenuItem).Header = "Obnovit"
            Else
                EnableAll(True)
                TT.Content = "Smazat položku."
                CType(btnSmazat.ContextMenu.Items(0), MenuItem).Header = "Označit"
            End If
            btnSmazat.ToolTip = TT

            txtPozice.Content = rState & " " & (ZarukyView.CurrentPosition + 1).ToString() & ".  / " & ZarukyView.Count.ToString

            First = If(ZarukyView.CurrentPosition = 0, False, True)
            Last = If(ZarukyView.CurrentPosition = ZarukyView.Count - 1, False, True)

            SetRokyMesice(dr.DatPoc.Value, dr.DatKon.Value)
            'přílohy
            mFileBrowser.Slozka = myFolder.Join(myFolder.Join(ATTpath, dr.Databaze), dr.OwnID.ToString)
        End If

        btnFirst.IsEnabled = First
        btnBack.IsEnabled = First
        btnLast.IsEnabled = Last
        btnNext.IsEnabled = Last
    End Sub
#End Region

    Private Sub ZarukyView_CurrentChanged(sender As Object, e As EventArgs) Handles ZarukyView.CurrentChanged
        UpdateDisplay()
    End Sub

    Private Sub btnBack_Click(sender As Object, e As RoutedEventArgs) Handles btnBack.Click
        ZarukyView.MoveCurrentToPrevious()
    End Sub

    Private Sub btnFirst_Click(sender As Object, e As RoutedEventArgs) Handles btnFirst.Click
        ZarukyView.MoveCurrentToFirst()
    End Sub

    Private Sub btnNext_Click(sender As Object, e As RoutedEventArgs) Handles btnNext.Click
        ZarukyView.MoveCurrentToNext()
    End Sub

    Private Sub btnLast_Click(sender As Object, e As RoutedEventArgs) Handles btnLast.Click
        ZarukyView.MoveCurrentToLast()
    End Sub

#Region " Enable/Disable "

    Private Sub EnableAll(ByVal Enabled As Boolean)
        btnPriloha.IsEnabled = Enabled
        btnSmazat.IsEnabled = Enabled
        txtID.IsEnabled = Enabled
        cbxVec.IsEnabled = Enabled
        txtSerNum.IsEnabled = Enabled
        txtDatPoc.IsEnabled = Enabled
        txtDatKon.IsEnabled = Enabled
        txtMesice.IsEnabled = Enabled
        txtRoky.IsEnabled = Enabled
        txtCena.IsEnabled = Enabled
        txtOptio2.IsEnabled = Enabled
        txtOptio3.IsEnabled = Enabled
        ckbOwn.IsEnabled = Enabled
        For Each Item As MenuItem In CType(mFileBrowser.FindResource("myMenu"), ContextMenu).Items
            Item.IsEnabled = Enabled
        Next
        Dim isFull As Boolean = FullVerze()
        btnPresunout.IsEnabled = If(Enabled And Not btnPresunout.ContextMenu.Items.Count = 0, isFull, False)
        ckbLength.IsChecked = If(isFull, Nastaveni.KolonkyDelsi, False)
        ckbCombo.IsChecked = If(isFull, Nastaveni.CombaPlnitVsemi, False)
        ckbLength.IsEnabled = isFull : ckbCombo.IsEnabled = isFull
        cbxDodavatel.IsEnabled = If(Enabled, isFull, False) : cbxOptio1.IsEnabled = If(Enabled, isFull, False) : cbxOptio4.IsEnabled = If(Enabled, isFull, False)
        txtFaktura.IsEnabled = If(Enabled, isFull, False) : txtDatOpt.IsEnabled = If(Enabled, isFull, False) : txtOptio5.IsEnabled = If(Enabled, isFull, False)
        txtCenaOpt.IsEnabled = If(Enabled, isFull, False)
    End Sub

#End Region

#End Region

#Region " Pridat "

    Private Sub ckbCislo_Checked(sender As Object, e As RoutedEventArgs) Handles ckbCislo.Checked, ckbCislo.Unchecked
        Nastaveni.CislovaniDokladu = CBool(ckbCislo.IsChecked)
    End Sub

    Private Sub btnPridat_Click(sender As Object, e As RoutedEventArgs) Handles btnPridat.Click
        If Verze = 2 And PocetRows + ZarukyView.Count + 1 > coRows Then
            Dim FormDialog = New wpfDialog(Me, "Zkušební verze je omezena na " & coRows & " položek všech databází dohromady. Nyní můžete v hlavním okně přes tlačítko Registrace přepnout na Freeware licenci nebo si pořídit Pro verzi.", "Záruky - zkušební verze", wpfDialog.Ikona.heslo, "Zavřít")
            FormDialog.ShowDialog()
            btnPridat.IsEnabled = False
            Exit Sub
        End If
        Dim drOld As Zaruky = Nothing
        If ZarukyView.CurrentItem IsNot Nothing Then
            drOld = CType(ZarukyView.CurrentItem, Zaruky)
        End If
        Dim drNew As Zaruky = CType(ZarukyView.AddNew, Zaruky)
        drNew.Oznacil = False : drNew.OwnCheck = False : drNew.Smazano = False
        drNew.Vec = "<nová>" : drNew.Databaze = EditDatabaze
        drNew.DatPoc = Today : drNew.DatKon = Today.AddYears(2) : drNew.Upraveno = Now
        drNew.Cena = 0 : drNew.CenaOpt = 0
        drNew.OwnID = mySQL.GetFreeOwnID(dcZaruky, If(Nastaveni.CislovaniDokladu, "", EditDatabaze))
        drNew.NewID = drNew.OwnID
        If ckbClear.IsChecked And IsNothing(drOld) = False Then
            drNew.DatPoc = drOld.DatPoc : drNew.DatKon = drOld.DatKon
            drNew.Cena = drOld.Cena : drNew.Vec = drOld.Vec : drNew.SerNum = drOld.SerNum
            drNew.Optio2 = drOld.Optio2 : drNew.Optio3 = drOld.Optio3
            If Not Verze = 1 Then
                drNew.Dodavatel = drOld.Dodavatel : drNew.Faktura = drOld.Faktura
                drNew.CenaOpt = drOld.CenaOpt : drNew.DatOpt = drOld.DatOpt
                drNew.Optio1 = drOld.Optio1 : drNew.Optio4 = drOld.Optio4 : drNew.Optio5 = drOld.Optio5
                myFolder.Copy(ATTpath & EditDatabaze & "\" & drOld.OwnID, ATTpath & EditDatabaze & "\" & drNew.OwnID, False)
            End If
        End If
        ZarukyView.CommitNew()
        UpdateDisplay()
        cbxVec.Focus()
        PocetZmen += 1
    End Sub

#End Region

#Region " Save Cancel "

    Private Sub btnUlozit_Click(sender As Object, e As RoutedEventArgs) Handles btnUlozit.Click
        PocetZmen += 1
        btnUlozit.IsEnabled = False
        Call (New clsAttachment).UpdateFolders(dcZaruky, ZarukyView, EditDatabaze)
        Try
            dcZaruky.SubmitChanges()
        Catch Ex As Exception
            Dim FormDialog = New wpfDialog(Me, "Chyba při ukládání: " & NR & Ex.Message, Me.Title, wpfDialog.Ikona.chyba, "Zavřít")
            FormDialog.ShowDialog()
        End Try
        Me.Close()
    End Sub

    Private Sub btnNeukladat_Click(sender As Object, e As RoutedEventArgs) Handles btnNeukladat.Click
        For Each dr As Zaruky In dcZaruky.GetChangeSet.Inserts
            myFolder.Delete(ATTpath & EditDatabaze & "\" & dr.OwnID, False)
        Next
        PocetZmen = 0
        Me.Close()
    End Sub

#End Region

#Region " Hyperlink "

    Private Sub txtOptio_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs) Handles txtOptio2.MouseDoubleClick, txtOptio3.MouseDoubleClick, txtOptio5.MouseDoubleClick
        myLink.Start(Me, CType(sender, TextBox).Text)
    End Sub

    Private Sub cbxOptio_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs) Handles cbxOptio1.MouseDoubleClick, cbxOptio4.MouseDoubleClick
        myLink.Start(Me, CType(sender, ComboBox).Text)
    End Sub

    Private Sub txtOptio_TextChanged(sender As Object, e As TextChangedEventArgs) Handles txtOptio2.TextChanged, txtOptio3.TextChanged, txtOptio5.TextChanged
        Dim txtOpt As TextBox = CType(sender, TextBox)
        txtOpt.TextDecorations = If(myLink.Address(txtOpt.Text), TextDecorations.Underline, Nothing)
    End Sub

    Private Sub ComboBox_TextChanged(sender As Object, e As TextChangedEventArgs)
        Dim cbxOpt As ComboBox = CType(sender, ComboBox)
        Dim Exist = myLink.Address(cbxOpt.Text)
        EditableComboBox.SetTextDecorations(cbxOpt, If(Exist, TextDecorations.Underline, Nothing))
        'cbxOpt.Foreground = myColorConverter.ColorToBrush(If(Exist, Colors.Black, Colors.DarkRed))
    End Sub

#End Region

End Class
