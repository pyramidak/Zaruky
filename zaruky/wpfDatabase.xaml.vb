Imports System.Data.SqlServerCe

Public Class wpfDatabase

    Public Property ReloadNeeded As Boolean
    Private wMain As wpfMain = CType(Application.Current.MainWindow, wpfMain)
    Private dcZaruky As zarukyContext
    Private DatabaseViewSource As CollectionViewSource
    Private WithEvents DatabaseView As BindingListCollectionView

#Region " Load "

    Private Sub wpfDatabase_Closed(sender As Object, e As EventArgs) Handles Me.Closed
        JmenoLoad = CType(DatabaseView.CurrentItem, Databaze).Jmeno
        dcZaruky.SubmitChanges()
        Me.ReloadNeeded = True
    End Sub

    Private Sub wpfDatabase_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        btnEditPass.IsEnabled = If(Verze = 1, False, True)
        dcZaruky = New zarukyContext(SdfConnection)
        AddDataGridColumns()
        Dim query As IQueryable(Of Databaze) = From a As Databaze In dcZaruky.Databazes Select a
        DatabaseViewSource = CType(Me.FindResource("DatabaseView"), CollectionViewSource)
        DatabaseViewSource.Source = query
        DatabaseView = CType(DatabaseViewSource.View, BindingListCollectionView)
        Dim Databaze = query.First(Function(x) x.Jmeno = If(JmenoLoad = coAll, JmenoDef, JmenoLoad))
        DatabaseView.MoveCurrentTo(Databaze)
        RenameColumns()
        myLogFile.CheckAccess(Me, False)
        FreezeButtons(myLogFile.CheckAccess(Me, True, Databaze.Jmeno))
    End Sub

    Private Sub Reload(sJmeno As String, Save As Boolean)
        If Save Then dcZaruky.SubmitChanges()
        dcZaruky = New zarukyContext(SdfConnection)
        Dim query As IQueryable(Of Databaze) = From a As Databaze In dcZaruky.Databazes Select a
        DatabaseViewSource.Source = query
        DatabaseView = CType(DatabaseViewSource.View, BindingListCollectionView)
        DatabaseView.MoveCurrentTo(query.First(Function(x) x.Jmeno = sJmeno))
    End Sub

#Region " Binding "

    Private Sub DatabaseView_CurrentChanged(sender As Object, e As EventArgs) Handles DatabaseView.CurrentChanged
        If DatabaseView.CurrentItem Is Nothing Then Exit Sub
        dgvSloupce.ScrollIntoView(dgvSloupce.SelectedItem)
        dgvHide.ScrollIntoView(dgvHide.SelectedItem)
        Dim dr As Databaze = CType(DatabaseView.CurrentItem, Databaze)
        btnRemove.IsEnabled = If(DatabaseView.Count = 1, False, True)
        RenameColumns()

        myLogFile.CheckAccess(Me, False)
        FreezeButtons(myLogFile.CheckAccess(Me, True, dr.Jmeno))
    End Sub

#End Region

#Region " Freeze "

    Private Sub FreezeButtons(ByVal bEnabled As Boolean)
        btnClear.IsEnabled = bEnabled : btnMena.IsEnabled = bEnabled : btnRemove.IsEnabled = bEnabled : btnRename.IsEnabled = bEnabled
        btnRemove.IsEnabled = If(DatabaseView.Count = 1, False, bEnabled)
    End Sub

    Private Sub FreezeWindow(ByVal Activate As Boolean)
        mToolBar.IsEnabled = Not Activate
        dgvJmeno.IsEnabled = Not Activate
        dgvSloupce.IsEnabled = Not Activate
        dgvHide.IsEnabled = Not Activate
        Me.Cursor = If(Activate, Cursors.Wait, Cursors.Arrow)
    End Sub

#End Region

#Region " DataGrid Columns "

    Private Sub AddDataGridColumns()
        dgvJmeno.Columns.Clear()
        myDataGrid.AddColumn(dgvJmeno, dcZaruky, True, "Active", False, True, "Aktivní")
        myDataGrid.AddColumn(dgvJmeno, dcZaruky, False, "Jmeno", True, True, "Databáze")
        myDataGrid.AddColumn(dgvJmeno, dcZaruky, False, "Mena", True, True, "Měna")

        dgvSloupce.Columns.Clear()
        myDataGrid.AddColumn(dgvSloupce, dcZaruky, False, "Vec", False, True, "Položka")
        myDataGrid.AddColumn(dgvSloupce, dcZaruky, False, "Cena", False, True, "Nákupka")
        myDataGrid.AddColumn(dgvSloupce, dcZaruky, False, "CenaOpt", False, True, "Prodejka")
        myDataGrid.AddColumn(dgvSloupce, dcZaruky, False, "OwnCheck", False, True, "Vyřízeno")
        myDataGrid.AddColumn(dgvSloupce, dcZaruky, False, "Dodavatel", False, True, "Dodavatel")
        myDataGrid.AddColumn(dgvSloupce, dcZaruky, False, "Faktura", False, True, "Doklad")
        myDataGrid.AddColumn(dgvSloupce, dcZaruky, False, "SerNum", False, True, "Sériové č.")
        myDataGrid.AddColumn(dgvSloupce, dcZaruky, False, "DatPoc", False, True, "Koupeno")
        myDataGrid.AddColumn(dgvSloupce, dcZaruky, False, "DatKon", False, True, "Záruka")
        myDataGrid.AddColumn(dgvSloupce, dcZaruky, False, "DatOpt", False, True, "Prodáno")
        myDataGrid.AddColumn(dgvSloupce, dcZaruky, False, "Optio1", False, True, "1.volitelné")
        myDataGrid.AddColumn(dgvSloupce, dcZaruky, False, "Optio2", False, True, "2.volitelné")
        myDataGrid.AddColumn(dgvSloupce, dcZaruky, False, "Optio3", False, True, "3.volitelné")
        myDataGrid.AddColumn(dgvSloupce, dcZaruky, False, "Optio4", False, True, "4.volitelné")
        myDataGrid.AddColumn(dgvSloupce, dcZaruky, False, "Optio5", False, True, "5.volitelné")

        dgvHide.Columns.Clear()
        myDataGrid.AddColumn(dgvHide, dcZaruky, True, "Optio1b", False)
        myDataGrid.AddColumn(dgvHide, dcZaruky, True, "Optio2b", False)
        myDataGrid.AddColumn(dgvHide, dcZaruky, True, "Optio3b", False)
        myDataGrid.AddColumn(dgvHide, dcZaruky, True, "Optio4b", False)
        myDataGrid.AddColumn(dgvHide, dcZaruky, True, "Optio5b", False)
    End Sub

#End Region

#Region " Columns Names "

    Private Sub RenameColumns()
        For Each one As DataGridColumn In dgvHide.Columns
            Dim Bind As Binding = CType(CType(one, DataGridBoundColumn).Binding, Binding)
            one.Header = GetColumnName(Bind.Path.Path)
        Next
    End Sub

    Private Function GetColumnName(ByVal PropertyName As String) As String
        Dim dr As Databaze = CType(DatabaseView.CurrentItem, Databaze)
        Dim sSloupec As String = PropertyName
        If sSloupec.StartsWith("Optio") And sSloupec.EndsWith("b") Then sSloupec = sSloupec.Substring(0, sSloupec.Length - 1)
        Dim columnNames = From a In dcZaruky.Mapping.MappingSource.GetModel(GetType(System.Data.Linq.DataContext)).GetMetaType(GetType(Databaze)).DataMembers Where a.Name = sSloupec Select a
        If Not columnNames.Count = 0 Then
            If dr.GetType.GetProperty(sSloupec).GetValue(dr, Nothing) IsNot Nothing Then
                Return myString.FirstWord(dr.GetType.GetProperty(sSloupec).GetValue(dr, Nothing).ToString)
            End If
        End If
        Return PropertyName
    End Function

#End Region

#End Region

#Region " Buttons "

#Region " Active "
    Private Sub btnActive_Click(sender As Object, e As RoutedEventArgs) Handles btnActive.Click
        Dim dr As Databaze = CType(DatabaseView.CurrentItem, Databaze)
        dr.Active = Not dr.Active
    End Sub

    Private Sub btnClose_Click(sender As Object, e As RoutedEventArgs) Handles btnClose.Click
        Me.Close()
    End Sub
#End Region

#Region " Rename "
    Private Sub btnRename_Click(sender As Object, e As RoutedEventArgs) Handles btnRename.Click
        Dim dr As Databaze = CType(DatabaseView.CurrentItem, Databaze)
        Dim sPromt As String = "Vložte nové jméno databáze"
        Dim sJmeno As String = dr.Jmeno
        Do
            Dim myInputBox As New wpfDialog(Me, sPromt, "Přejmenování databáze", wpfDialog.Ikona.tuzka, "Přejmenovat", "Zrušit", True, False, "", False, 20, True)
            myInputBox.Input = sJmeno
            If myInputBox.ShowDialog = False Then Exit Sub
            sJmeno = myInputBox.Input
            Dim query As IQueryable(Of Databaze) = From a As Databaze In dcZaruky.Databazes Where a.Jmeno = sJmeno Select a
            If query.Count = 0 Then
                Exit Do
            Else
                sPromt = "Databáze s jménem " & sJmeno.ToUpper & " již existuje." & NR & NR & sPromt
            End If
        Loop
        FreezeWindow(True)
        Dim oldPath As String = ATTpath & dr.Jmeno
        Dim newPath As String = ATTpath & sJmeno
        If myFolder.Rename(oldPath, newPath, True) Then
            If JmenoLoad = dr.Jmeno Then JmenoLoad = sJmeno
            Using cmd As New SqlCeCommand("UPDATE Zaruky SET Databaze = @NewJmeno WHERE (Databaze = @Jmeno)", SdfConnection)
                cmd.Parameters.AddWithValue("@NewJmeno", sJmeno)
                cmd.Parameters.AddWithValue("@Jmeno", dr.Jmeno)
                SdfConnection.Open()
                cmd.ExecuteNonQuery()
                SdfConnection.Close()
            End Using
            Using cmd As New SqlCeCommand("UPDATE Databaze SET Jmeno = @NewJmeno WHERE (Jmeno = @Jmeno)", SdfConnection)
                cmd.Parameters.AddWithValue("@NewJmeno", sJmeno)
                cmd.Parameters.AddWithValue("@Jmeno", dr.Jmeno)
                SdfConnection.Open()
                cmd.ExecuteNonQuery()
                SdfConnection.Close()
            End Using
            Reload(sJmeno, True)
        End If
        FreezeWindow(False)
    End Sub
#End Region

#Region " Mena "
    Private Sub btnMena_Click(sender As Object, e As RoutedEventArgs) Handles btnMena.Click
        Dim dr As Databaze = CType(DatabaseView.CurrentItem, Databaze)
        Dim myInputBox As New wpfDialog(Me, "Vložte zkratku nové měny", "Nová měna", wpfDialog.Ikona.tuzka, "OK", "Zrušit", True, False, "", False, 3, True)
        myInputBox.Input = dr.Mena
        If myInputBox.ShowDialog = False Then Exit Sub
        Dim sMena As String = myInputBox.Input
        myInputBox = New wpfDialog(Me, "Pokud chcete přepočítat ceny, vložte číslo, kterým budou vynásobeny všechny ceny ve zvolené databázi například 0,04", "Nová měna", wpfDialog.Ikona.tuzka, "OK", "Zrušit", True, False, "", False, 6)
        If myInputBox.ShowDialog = False Or IsNumeric(myInputBox.Input) = False Then Exit Sub
        If myInputBox.Input = "1" Then
            dr.Mena = sMena
            Exit Sub
        End If
        FreezeWindow(True)
        Using cmd As New SqlCeCommand("UPDATE Zaruky SET Cena = Cena * @Prepocet, CenaOpt = CenaOpt * @Prepocet WHERE (Databaze = @Jmeno)", SdfConnection)
            cmd.Parameters.AddWithValue("@Prepocet", CDec(myInputBox.Input))
            cmd.Parameters.AddWithValue("@Jmeno", dr.Jmeno)
            SdfConnection.Open()
            cmd.ExecuteNonQuery()
            SdfConnection.Close()
        End Using
        FreezeWindow(False)
        dr.Mena = sMena
        Dim myMsgBox As New wpfDialog(Me, "Ceny v databázi nabyly nových hodnot.", "Přepočítání cen", wpfDialog.Ikona.ok, "Zavřít")
        myMsgBox.ShowDialog()
    End Sub
#End Region

#Region " Add "

    Private Sub btnAdd_Click(sender As Object, e As RoutedEventArgs) Handles btnAdd.Click
        Dim dr As Databaze = CType(DatabaseView.CurrentItem, Databaze)
        Dim sJmeno As String = NewName()
        If sJmeno = "" Then Exit Sub
        Dim myMsgBox As New wpfDialog(Me, "Chcete použít nastavení z aktuálně vybrané databáze " + dr.Jmeno.ToUpper + " ?" + NR + NR + dr.Jmeno, "Přidání databáze", wpfDialog.Ikona.dotaz, "Ano", "Ne")
        If myMsgBox.ShowDialog Then
            CreateDatase(sJmeno, "selected")
        Else
            CreateDatase(sJmeno, "default")
        End If
    End Sub

    Private Sub CreateDatase(sJmeno As String, DBtype As String)
        Dim rDB As New Databaze
        rDB.Jmeno = sJmeno
        rDB.Active = True
        Select Case DBtype
            Case "selected"
                Dim dr As Databaze = CType(DatabaseView.CurrentItem, Databaze)
                Dim query As IQueryable(Of Databaze) = From a As Databaze In dcZaruky.Databazes Where a.Jmeno = dr.Jmeno Select a
                Dim rDBlast As Databaze = query.First
                rDB.Cena = rDBlast.Cena : rDB.CenaOpt = rDBlast.CenaOpt : rDB.DatPoc = rDBlast.DatPoc : rDB.DatKon = rDBlast.DatKon : rDB.DatOpt = rDBlast.DatOpt
                rDB.Vec = rDBlast.Vec : rDB.Dodavatel = rDBlast.Dodavatel : rDB.Faktura = rDBlast.Faktura : rDB.Mena = rDBlast.Mena : rDB.SerNum = rDBlast.SerNum
                rDB.Optio1 = rDBlast.Optio1 : rDB.Optio2 = rDBlast.Optio2 : rDB.Optio3 = rDBlast.Optio3 : rDB.Optio4 = rDBlast.Optio4 : rDB.Optio5 = rDBlast.Optio5
                rDB.OwnCheck = rDBlast.OwnCheck : rDB.Positions = rDBlast.Positions

            Case "default"
                rDB.Cena = "Nákupka" : rDB.CenaOpt = "Prodejka" : rDB.DatPoc = "Koupeno dne" : rDB.DatKon = "Záruka končí" : rDB.DatOpt = "Prodáno dne"
                rDB.Vec = "Položka" : rDB.Dodavatel = "Dodavatel" : rDB.Faktura = "Doklad pořízení" : rDB.SerNum = "Sériové číslo" : rDB.OwnCheck = "Vyřízeno"
                rDB.Optio1 = "Webová adresa" : rDB.Optio2 = "2.volitelné" : rDB.Optio3 = "3.volitelné" : rDB.Optio4 = "4.volitelné" : rDB.Optio5 = "5.volitelné"
                rDB.Mena = "Kč"
                rDB.Positions = "000102060409101207030805111314151617"

            Case "průkazy"
                rDB.Cena = "Cena pořizovací" : rDB.CenaOpt = "Pokuta" : rDB.DatPoc = "Vydáno" : rDB.DatKon = "Platnost" : rDB.DatOpt = "Zrušeno"
                rDB.Vec = "Průkaz" : rDB.Dodavatel = "Vydavatel" : rDB.Faktura = "Doklad pořízení" : rDB.SerNum = "Číslo průkazu" : rDB.OwnCheck = "Vyřízeno"
                rDB.Optio1 = "1.volitelné" : rDB.Optio2 = "2.volitelné" : rDB.Optio3 = "3.volitelné" : rDB.Optio4 = "4.volitelné" : rDB.Optio5 = "5.volitelné"
                rDB.Mena = "Kč"
                rDB.Optio1b = False : rDB.Optio2b = False : rDB.Optio3b = False : rDB.Optio4b = False : rDB.Optio5b = False
                rDB.Positions = "00010204050912081003060711"

        End Select
        dcZaruky.Databazes.InsertOnSubmit(rDB)
        Reload(sJmeno, True)
    End Sub

    Private Function NewName(Optional DefName As String = "") As String
        Dim sPromt As String = "Vložte jméno nové databáze"
        Do
            Dim myInputBox As New wpfDialog(Me, sPromt, "Přidání databáze", wpfDialog.Ikona.tuzka, "Přidat", "Zrušit", True, False, "", False, 20, True)
            myInputBox.Input = DefName
            If myInputBox.ShowDialog = False Then Return ""
            DefName = myInputBox.Input
            Dim query As IQueryable(Of Databaze) = From a As Databaze In dcZaruky.Databazes Where a.Jmeno = DefName Select a
            If query.Count = 0 Then
                Return DefName
            Else
                sPromt = "Databáze s jménem " & DefName.ToUpper & " již existuje." & NR & NR & sPromt
            End If
        Loop
    End Function

#End Region

#Region " Add +menu "

    Private Sub btnUkonce_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles btnAdd.MouseDown
        If e.RightButton = MouseButtonState.Pressed Then
            Dim cmuUkonce As ContextMenu = btnAdd.ContextMenu
            cmuUkonce.PlacementTarget = btnAdd
            cmuUkonce.Placement = Primitives.PlacementMode.Bottom
            cmuUkonce.IsOpen = True
        End If
    End Sub

    Private Sub cmiAdd_Click(sender As Object, e As RoutedEventArgs)
        Dim mItem As MenuItem = CType(sender, MenuItem)
        If mItem.Header.ToString = "Export" Then
            Export()
            Exit Sub
        End If
        Dim sJmeno As String = NewName(mItem.Header.ToString)
        If sJmeno = "" Then Exit Sub
        CreateDatase(sJmeno, mItem.Header.ToString)
    End Sub

    Private Sub Export()
        Dim dr As Databaze = CType(DatabaseView.CurrentItem, Databaze)
        Dim Text As String = "Jméno databáze: " + dr.Jmeno + NR + NR
        Text += "rDB.Vec = " + dr.Vec + " : rDB.SerNum = " + dr.SerNum + " : rDB.DatPoc = " + dr.DatPoc + " : rDB.DatKon = " + dr.DatKon + " : rDB.DatOpt = " + dr.DatOpt + NR
        Text += "rDB.Cena = " + dr.Cena + " : rDB.CenaOpt = " + dr.CenaOpt + " : rDB.Dodavatel = " + dr.Dodavatel + " : rDB.Faktura = " + dr.Faktura + " : rDB.OwnCheck = " + dr.OwnCheck + NR
        Text += "rDB.Optio1 = " + dr.Optio1 + " : rDB.Optio2 = " + dr.Optio2 + " : rDB.Optio3 = " + dr.Optio3 + " : rDB.Optio4 = " + dr.Optio4 + " : rDB.Optio5 = " + dr.Optio5 + NR
        Text += "rDB.Optio1b = " + dr.Optio1b.ToString + " : rDB.Optio2b = " + dr.Optio2b.ToString + " : rDB.Optio3b = " + dr.Optio3b.ToString + " : rDB.Optio4b = " + dr.Optio4b.ToString + " : rDB.Optio5b = " + dr.Optio5b.ToString + NR
        Text += "rDB.Positions = " + dr.Positions
        Clipboard.SetText(Text)
        Dim FormDialog = New wpfDialog(Me, "Pojmenování, viditelnost a řazení sloupců vybrané databáze bylo uloženo do schránky Windows." + NR +
                                       "Pokud chcete přidat parametry této databáze do další verze Záruk pod Přidat, " +
                                       "pošlete mi email, do kterého vložíte parametry databáze pomocí klávesové zkratky CTRL + V.", Me.Title, wpfDialog.Ikona.ok, "Zavřít")
        FormDialog.ShowDialog()
    End Sub

#End Region

#Region " Clear "
    Private Sub btnClear_Click(sender As Object, e As RoutedEventArgs) Handles btnClear.Click
        Dim dr As Databaze = CType(DatabaseView.CurrentItem, Databaze)
        Dim myMsgBox As New wpfDialog(Me, "Skutečně chcete odstranit všechny záznamy z databáze " & dr.Jmeno.ToUpper & " ?", "Vyprázdnění databáze", wpfDialog.Ikona.varovani, "Vyprázdnit", "Zrušit")
        If myMsgBox.ShowDialog = False Then Exit Sub
        EmptyDatabaze(dr.Jmeno)
        FreezeWindow(False)
        myMsgBox = New wpfDialog(Me, "Záznamy databáze " & dr.Jmeno.ToUpper & " jsou minulostí.", "Vyprázdnění databáze", wpfDialog.Ikona.ok, "Zavřít")
        myMsgBox.ShowDialog()
    End Sub

    Private Sub EmptyDatabaze(ByVal sJmeno As String)
        DiscardAttachment(sJmeno)
        FreezeWindow(True)
        Using cmd As New SqlCeCommand("DELETE FROM Zaruky WHERE (Databaze = @Jmeno)", SdfConnection)
            cmd.Parameters.AddWithValue("@Jmeno", sJmeno)
            SdfConnection.Open()
            cmd.ExecuteNonQuery()
            SdfConnection.Close()
        End Using
    End Sub

    Private Sub DiscardAttachment(ByVal JmenoDatabaze As String)
        If myFolder.Exist(ATTpath & JmenoDatabaze) Then
            Dim myMsgBox As New wpfDialog(Me, "Chcete odstranit také přílohy databáze?", "Vyprázdnění databáze", wpfDialog.Ikona.dotaz, "Ano", "Ne")
            If myMsgBox.ShowDialog Then
                myFolder.Delete(ATTpath & JmenoDatabaze, True)
            End If
        End If
    End Sub
#End Region

#Region " Remove "
    Private Sub btnRemove_Click(sender As Object, e As RoutedEventArgs) Handles btnRemove.Click
        Dim dr As Databaze = CType(DatabaseView.CurrentItem, Databaze)
        Dim myMsgBox As New wpfDialog(Me, "Skutečně chcete smazat databázi " & dr.Jmeno.ToUpper & " ?", "Smazání databáze", wpfDialog.Ikona.varovani, "Smazat", "Zrušit")
        If myMsgBox.ShowDialog = False Then Exit Sub
        EmptyDatabaze(dr.Jmeno)
        DatabaseView.Remove(dr)
        FreezeWindow(False)
    End Sub
#End Region

#Region " Database Password "
    Private Sub btnDBpass_Click(sender As Object, e As RoutedEventArgs) Handles btnDBpass.Click
        Dim query As IQueryable(Of Databaze) = From a As Databaze In dcZaruky.Databazes Select a
        For Each row As Databaze In query
            If myLogFile.OpenAccess(row.Jmeno, Me) = False Then Exit Sub
        Next

        Dim sVzkaz As String = "Vložte aktuální hlavní vstupní heslo databáze." + NR +
                               "Pokud je databáze bez hesla, ponechte prázdné." + NR + NR +
                               "Se změnou hesla proběhne kompletní reindexace" + NR +
                               "a bude vypnuto autozadávání hesla při startu."
        Dim myInputBox As New wpfDialog(Me, sVzkaz, "Vstupní heslo", wpfDialog.Ikona.tuzka, "OK", "Zrušit", True, False, "", False, 30, True)
        If myInputBox.ShowDialog() = False Then Exit Sub
        Dim sOldPass As String = myInputBox.Input

        sVzkaz = "Vložte nové hlavní heslo pro přístup Do databáze:" + NR +
                 "Nezadáním nového hesla vypnete vstupní heslo." + NR + NR +
                 "Mějte prosím na paměťi, že pokud ho zapomenete," + NR +
                 "k datům už se nikdo nikdy nedostane, zejména Vy."
        myInputBox = New wpfDialog(Me, sVzkaz, "Vstupní heslo", wpfDialog.Ikona.tuzka, "OK", "Zrušit", True, False, "", False, 30, True)
        If myInputBox.ShowDialog() = False Then Exit Sub
        Dim sNewPass As String = myInputBox.Input

        Dim SDFcompact As String = myFile.Path(SDFpath) + "\before_compact.sdf"
        If myFile.Copy(SDFpath, SDFcompact) = False Then Exit Sub
        dcZaruky.SubmitChanges()
        If Not SdfConnection.State = ConnectionState.Closed Then SdfConnection.Close()
        Dim myMsgBox As wpfDialog
        Try
            Dim CeEng As New SqlCeEngine(mySQL.CreateSDFConnString(SDFpath, sOldPass))
            CeEng.Compact(String.Format("DataSource=; Password=""{0}""", PasswordSDF & sNewPass))
        Catch ex As Exception
            If Err.Number = 5 Then
                myMsgBox = New wpfDialog(Me, "Špatně zadané aktuální vstupní heslo." & NR & "Heslo databáze nebylo změněno.", "Vstupní heslo", wpfDialog.Ikona.varovani, "Zavřít")
            Else
                myMsgBox = New wpfDialog(Me, ex.Message, "Vstupní heslo", wpfDialog.Ikona.chyba, "Zavřít")
            End If
            myMsgBox.ShowDialog()
            Exit Sub
        End Try
        SdfConnection = New SqlCeConnection(mySQL.CreateSDFConnString(SDFpath, sNewPass))
        Reload(CType(DatabaseView.CurrentItem, Databaze).Jmeno, False)
        Nastaveni.HesloDatabaze = Nothing
        myMsgBox = New wpfDialog(Me, "Vstupní heslo úspěšně změněno.", "Vstupní heslo", wpfDialog.Ikona.ok, "Zavřít")
        myMsgBox.ShowDialog()
    End Sub
#End Region

#Region " Edit Password "
    Private Sub btnEditPass_Click(sender As Object, e As RoutedEventArgs) Handles btnEditPass.Click
        Dim sVzkaz As String = "Vložte heslo pro přístup k úpravám databází maximálně 5 znaků dlouhé." + NR + NR +
                               "Toto heslo bude sloužit k omezení přístupu ostatním uživatelům k editaci a ke změně nastavení."
        Dim myInputBox As New wpfDialog(Me, sVzkaz, "Editační heslo", wpfDialog.Ikona.tuzka, "OK", "Zrušit", True, False, "", False, 5, True)
        If myInputBox.ShowDialog() Then
            myLogFile.wrAccess = myInputBox.Input
            Dim query As IQueryable(Of Security) = From a As Security In dcZaruky.Securities Select a
            query.First.Pass = myInputBox.Input
        End If
    End Sub
#End Region

#End Region

End Class
