Imports System.Data.SqlServerCe

Public Class wpfTableEdit

#Region " Properties "

    Private WithEvents timStart As Windows.Threading.DispatcherTimer
    Private dcZaruky As zarukyContext
    Private WithEvents ZarukyView As BindingListCollectionView
    Private PocetRows As Integer
    Public Property EditDatabaze() As String
    Public Property SelectedID() As Integer
    Private cmuTool As ContextMenu
    Public Property PocetZmen As Integer

#End Region

#Region " Load "

    Private Sub wpfTableEdit_Closed(sender As Object, e As EventArgs) Handles Me.Closed
        If ZarukyView.CurrentItem IsNot Nothing Then SelectedID = CType(ZarukyView.CurrentItem, Zaruky).ID
    End Sub

    Private Sub ckbCislo_Checked(sender As Object, e As RoutedEventArgs) Handles ckbCislo.Checked, ckbCislo.Unchecked
        Nastaveni.CislovaniDokladu = CBool(ckbCislo.IsChecked)
    End Sub

    Private Sub wpfTableEdit_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        ckbCislo.IsChecked = Nastaveni.CislovaniDokladu
        mDataGrid.ClipboardCopyMode = If(Verze = 4, DataGridClipboardCopyMode.ExcludeHeader, DataGridClipboardCopyMode.None)
        If Not Verze = 4 Then btnExport.ContextMenu.IsEnabled = False
        cmuTool = CType(Me.FindResource("ToolMenu"), ContextMenu)
        timStart = New Windows.Threading.DispatcherTimer
        timStart.Interval = TimeSpan.FromMilliseconds(1)
        timStart.Start()
        dcZaruky = New zarukyContext(SdfConnection)
        PocetRows = (From a As Zaruky In dcZaruky.Zarukies Where a.Databaze <> EditDatabaze Select a.ID).Count
        timStart.Start()
    End Sub

    Private Sub timStart_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles timStart.Tick
        timStart.Stop()
        AddDataGridColumns()
        Dim queryZaruky As IQueryable(Of Zaruky) = From a As Zaruky In dcZaruky.Zarukies Where a.Databaze = EditDatabaze Select a Order By a.OwnID
        If Verze = 2 Then
            mDataGrid.DataContext = queryZaruky.Take(50)
        Else
            mDataGrid.DataContext = queryZaruky
        End If
        ZarukyView = CType(CollectionViewSource.GetDefaultView(mDataGrid.DataContext), BindingListCollectionView)
        If Not ZarukyView.Count = 0 Then
            Dim dr As Zaruky = queryZaruky.FirstOrDefault(Function(x) x.ID = SelectedID)
            If dr IsNot Nothing Then ZarukyView.MoveCurrentTo(dr)
        End If
        UpdateButtons()
    End Sub

    Private Function FullVerze() As Boolean
        Return If(Verze = 1, False, If(Verze = 4, True, If(PocetRows + ZarukyView.Count - 1 < coRows, True, False)))
    End Function

#Region " Add Columns "

    Private Sub AddDataGridColumns()
        mDataGrid.Columns.Clear()
        myDataGrid.AddColumn(mDataGrid, dcZaruky, True, "Oznacil", False, True, "")
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "NewID", False, True, "Č.")
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "Vec", False)
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "SerNum", False)
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "Cena", False)
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "CenaOpt", False)
        myDataGrid.AddColumn(mDataGrid, dcZaruky, True, "OwnCheck", False)
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "Faktura", False)
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "Dodavatel", False)
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "DatPoc", False)
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "DatKon", False)
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "DatOpt", False)
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "Optio1", False)
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "Optio2", False)
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "Optio3", False)
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "Optio4", False)
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "Optio5", False)

        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "OwnID", True, False)
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "ID", True, False)
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "Databaze", True, False)
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "Upraveno", True, False)
        myDataGrid.AddColumn(mDataGrid, dcZaruky, True, "Smazano", True, False)
    End Sub

#End Region

    Private Sub UpdateButtons()
        Dim OK = If(ZarukyView.Count = 0, False, True)
        btnSmazat.IsEnabled = OK : btnExport.IsEnabled = OK
        For Each one As MenuItem In cmuTool.Items
            one.IsEnabled = OK
        Next
    End Sub

#End Region

#Region " ContextMenu "

    Private Sub btnExport_Click(sender As Object, e As RoutedEventArgs) Handles btnExport.Click
        If Verze = 4 Then
            OpenContextMenu(btnExport.ContextMenu, btnExport)
        Else
            Dim FormDialog = New wpfDialog(Me, "Exportovat databázi do excelu a xml souboru lze pouze ve verzi Donationware.", "Export", Nothing, "Zavřít")
            FormDialog.ShowDialog()
        End If
    End Sub

    Private Sub btnHelp_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles btnHelp.MouseDown
        If e.RightButton = MouseButtonState.Pressed Then
            OpenContextMenu(cmuTool, btnHelp)
        End If
    End Sub

    Private Sub btnExport_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles btnExport.MouseDown
        If e.RightButton = MouseButtonState.Pressed Then
            OpenContextMenu(btnExport.ContextMenu, btnExport)
        End If
    End Sub

    Private Sub OpenContextMenu(ByVal cmu As ContextMenu, control As UIElement)
        cmu.PlacementTarget = control
        cmu.Placement = Primitives.PlacementMode.Right
        cmu.IsOpen = True
    End Sub

    Private Sub miRemove_Click(sender As Object, e As RoutedEventArgs)
        DeleteRows()
    End Sub

    Private Sub miNum_Click(sender As Object, e As RoutedEventArgs)
        If mDataGrid.SelectedItems.Count = 0 Then
            Dim FormDialog = New wpfDialog(Me, "Vyberte řádky pro přečíslování.", "Nápověda", Nothing, "Zavřít")
            FormDialog.ShowDialog()
        Else
            Dim bSerazeno As Boolean = If(mDataGrid.Items.IndexOf(mDataGrid.SelectedItems(0)) > mDataGrid.Items.IndexOf(mDataGrid.SelectedItems(mDataGrid.SelectedItems.Count - 1)), False, True)
            Dim iNum As Integer
            If bSerazeno Then
                iNum = CType(mDataGrid.SelectedItems(0), Zaruky).NewID
            Else
                iNum = CType(mDataGrid.SelectedItems(mDataGrid.SelectedItems.Count - 1), Zaruky).NewID + mDataGrid.SelectedItems.Count - 1
            End If
            For Each dr As Zaruky In mDataGrid.SelectedItems
                dr.NewID = iNum
                iNum = If(bSerazeno, iNum + 1, iNum - 1)
            Next
        End If
    End Sub

    Private Sub miCheck_Click(sender As Object, e As RoutedEventArgs)
        PocetZmen += 1
        Dim sPath As String = myDataGrid.GetColumnBinding(mDataGrid.CurrentCell.Column).Path.Path
        Dim drFirst As Zaruky = CType(mDataGrid.SelectedItems(0), Zaruky)
        Dim Objekt As Object = drFirst.GetType.GetProperty(sPath).GetValue(drFirst, Nothing)
        If Objekt.GetType Is GetType(Boolean) Then
            Dim bSet As Boolean = Not CBool(Objekt)
            For Each dr As Zaruky In mDataGrid.SelectedItems
                dr.GetType.GetProperty(sPath).SetValue(dr, bSet, Nothing)
            Next
        Else
            Dim FormDialog = New wpfDialog(Me, "Skončete výběr na sloupci, kde chcete změnit hodnoty zatrženo / nezatrženo.", "Nápověda", Nothing, "Zavřít")
            FormDialog.ShowDialog()
        End If
    End Sub

#End Region

#Region " Binding +New "

    Private Sub ZarukyView_CurrentChanged(sender As Object, e As EventArgs) Handles ZarukyView.CurrentChanged
        UpdateButtons()
    End Sub

    Private Sub ZarukyAddNew(Optional ByVal iNewID As Integer = 0)
        If Verze = 2 And PocetRows + ZarukyView.Count + 1 > coRows Then
            If iNewID = 0 Then
                Dim FormDialog = New wpfDialog(Me, "Zkušební verze je omezena na " & coRows & " položek všech databází dohromady. Nyní můžete v hlavním okně přes tlačítko Registrace přepnout na Freeware licenci nebo si pořídit Pro verzi.", "Záruky - zkušební verze", wpfDialog.Ikona.heslo, "Zavřít")
                FormDialog.ShowDialog()
            End If
            btnPridat.IsEnabled = False
            Exit Sub
        End If

        Dim drNew = CType(ZarukyView.AddNew, Zaruky)
        drNew.Oznacil = False : drNew.OwnCheck = False : drNew.Smazano = False
        drNew.Vec = "<nová>" : drNew.Databaze = EditDatabaze
        drNew.DatPoc = Today : drNew.DatKon = Today.AddYears(2) : drNew.Upraveno = Now
        drNew.Cena = 0 : drNew.CenaOpt = 0
        If iNewID = 0 Then
            drNew.NewID = mySQL.GetFreeOwnID(dcZaruky, If(Nastaveni.CislovaniDokladu, "", EditDatabaze))
        Else
            drNew.NewID = iNewID
        End If
        drNew.OwnID = drNew.OwnID
        ZarukyView.CommitNew()
        UpdateButtons()
        PocetZmen += 1
    End Sub

    Private Sub CheckCells()
        For Each one As Zaruky In ZarukyView
            If one.OwnID = 0 Or Not one.OwnID = one.NewID Then
                one.NewID = mySQL.GetFreeOwnID(dcZaruky, If(Nastaveni.CislovaniDokladu, "", EditDatabaze), CInt(one.NewID))
                If one.OwnID = 0 Then one.OwnID = one.NewID
            End If
        Next
    End Sub

#End Region

#Region " Buttons "

    Private Sub btnPridat_Click(sender As Object, e As RoutedEventArgs) Handles btnPridat.Click
        ZarukyAddNew()
        mDataGrid.ScrollIntoView(mDataGrid.Items(mDataGrid.Items.Count - 1))
    End Sub

    Private Sub btnSmazat_Click(sender As Object, e As RoutedEventArgs) Handles btnSmazat.Click
        DeleteRows()
    End Sub

    Private Sub DeleteRows()
        PocetZmen += 1
        Dim itemList As List(Of Zaruky) = mDataGrid.SelectedItems.Cast(Of Zaruky)().ToList()
        itemList.ForEach(Sub(x) ZarukyView.Remove(x))
    End Sub

    Private Sub btnHelp_Click(sender As Object, e As RoutedEventArgs) Handles btnHelp.Click
        Dim msg As String = "Pravým kliknutím v tabulce můžete z kontextové nabídky vybrat další funkce." + NR + NR +
                          "Můžete kopírovat vybrané řádky do paměťi nebo naopak vkládat řádky či tabulku směrem od vybrané buňky." + NR + NR +
                          "Funkce přečíslovat vybrané řádky čísluje sestupně od vybraného čísla řádku." + NR + NR +
                          "Pro hromadnou změnu hodnot (ne)zatrženo vyberte řádky tažením v požadovaném sloupci." + NR + NR +
                          "Zatržením číslovat bude nové pořadové číslo položky v rámci všech databází."

        Dim FormDialog = New wpfDialog(Me, msg, "Nápověda", Nothing, "Zavřít")
        FormDialog.ShowDialog()
    End Sub

    Private Sub btnNeukladat_Click(sender As Object, e As RoutedEventArgs) Handles btnNeukladat.Click
        PocetZmen = 0
        Me.Close()
    End Sub

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

#End Region

#Region " Export database "

    Private Sub cmiXLS_Click(sender As Object, e As RoutedEventArgs)
        Dim ofdMain As New Microsoft.Win32.SaveFileDialog
        ofdMain.Title = "Zvolte místo a název souboru"
        ofdMain.Filter = "Comma-separated values (UTF-8)|*.csv"
        ofdMain.InitialDirectory = If(myFolder.Exist(myFile.Path(SDFpath)), myFile.Path(SDFpath), Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
        ofdMain.FileName = "zarukyUTF8"
        If ofdMain.ShowDialog Then
            If myFile.Delete(ofdMain.FileName, True) Then
                mDataGrid.SelectAllCells()
                mDataGrid.ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader
                ApplicationCommands.Copy.Execute(Nothing, mDataGrid)
                mDataGrid.UnselectAllCells()
                Dim sText As String = DirectCast(Clipboard.GetData(DataFormats.CommaSeparatedValue), String)
                Clipboard.Clear()
                Dim FileStream As New System.IO.StreamWriter(ofdMain.FileName)
                FileStream.WriteLine(sText)
                FileStream.Close()
                System.Diagnostics.Process.Start(myFile.Path(ofdMain.FileName))
            End If
        End If
    End Sub

    Private Sub cmiXML_Click(sender As Object, e As RoutedEventArgs)
        Dim ofdMain As New Microsoft.Win32.SaveFileDialog
        ofdMain.Title = "Zvolte místo a název souboru"
        ofdMain.Filter = "Extensible Markup Language (UTF-8)|*.xml"
        ofdMain.InitialDirectory = If(myFolder.Exist(myFile.Path(SDFpath)), myFile.Path(SDFpath), Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
        ofdMain.FileName = "zarukyUTF8"
        If ofdMain.ShowDialog Then
            If myFile.Delete(ofdMain.FileName, True) Then
                SdfConnection.Open()
                Dim cmd As New SqlCeCommand("SEL", SdfConnection)
                cmd.CommandText = "SELECT * FROM Zaruky WHERE Databaze = '" + EditDatabaze + "'"
                Dim ZarukyTable As New DataTable("Zaruky")
                ZarukyTable.Load(cmd.ExecuteReader)
                cmd.Dispose()
                SdfConnection.Close()
                Dim dsFull As New DataSet("dsZaruky")
                dsFull.Tables.Add(ZarukyTable)
                dsFull.WriteXml(ofdMain.FileName)
                dsFull.Dispose()
                System.Diagnostics.Process.Start(myFile.Path(ofdMain.FileName))
            End If
        End If
    End Sub

#End Region

#Region " Copy a Paste "

    Private Sub mDataGrid_KeyDown(sender As Object, e As KeyEventArgs) Handles mDataGrid.KeyDown
        If mDataGrid.Items.Count = 0 Then Exit Sub
        If Keyboard.IsKeyDown(Key.LeftCtrl) Then
            If Keyboard.IsKeyDown(Key.C) Then
                If Not Verze = 4 Then
                    Dim FormDialog = New wpfDialog(Me, "Kopírování do schránky Windows je povoleno pouze ve verzi Donationware.", "CTRL + C", Nothing, "Zavřít")
                    FormDialog.ShowDialog()
                End If
            ElseIf Keyboard.IsKeyDown(Key.V) Then
                proPaste()
            End If
        End If
    End Sub

    Private Sub miPaste_Click(sender As Object, e As RoutedEventArgs)
        proPaste()
    End Sub

    Private Sub miCopy_Click(sender As Object, e As RoutedEventArgs)
        If Verze = 4 Then
            ApplicationCommands.Copy.Execute(Nothing, mDataGrid)
        Else
            Dim FormDialog = New wpfDialog(Me, "Kopírování do schránky Windows je povoleno pouze ve verzi Donationware.", "CTRL + C", Nothing, "Zavřít")
            FormDialog.ShowDialog()
        End If
    End Sub

    Private Sub proPaste()
        PocetZmen += 1
        Me.Cursor = Cursors.Wait
        Dim rowSplitter As Char() = {CChar(vbCr), CChar(vbLf)}
        Dim columnSplitter As Char() = {CChar(vbTab)}

        'get the text from clipboard
        Dim dataInClipboard As IDataObject = Clipboard.GetDataObject()
        Dim stringInClipboard As String = CStr(dataInClipboard.GetData(DataFormats.Text))

        'split it into lines
        Dim rowsInClipboard As String() = stringInClipboard.Split(rowSplitter, StringSplitOptions.RemoveEmptyEntries)

        'get the row and column of selected cell in grid
        Dim r, c As Integer
        r = mDataGrid.Items.IndexOf(mDataGrid.CurrentItem) : c = mDataGrid.CurrentColumn.DisplayIndex
        'add row into grid to fit clipboard lines
        Dim iLines As Integer = rowsInClipboard.Length - (mDataGrid.Items.Count - r)
        Dim iNewID As Integer = mySQL.GetFreeOwnID(dcZaruky, If(Nastaveni.CislovaniDokladu, "", EditDatabaze))
        For a As Integer = 0 To iLines - 1
            ZarukyAddNew(iNewID + a)
        Next

        ' loop through the lines, split them into cells and place the values in the corresponding cell.
        For iRow As Integer = 0 To rowsInClipboard.Length - 1
            If r + iRow > mDataGrid.Items.Count - 1 Then Exit For
            Dim dr As Zaruky = CType(mDataGrid.Items(r + iRow), Zaruky)
            'split row into cell values
            Dim valuesInRow As New ArrayList
            valuesInRow.AddRange(rowsInClipboard(iRow).Split(columnSplitter))
            'Dim valuesInRow As  = rowsInClipboard(iRow).Split(columnSplitter)
            If valuesInRow(0).ToString = "" Then valuesInRow.RemoveAt(0)
            'cycle through cell values
            For iCol As Integer = 0 To valuesInRow.Count - 1
                'assign cell value, only if it within columns of the grid
                If (mDataGrid.Columns.Count - 1 >= c + iCol) Then
                    Dim sVal As String = valuesInRow(iCol).ToString
                    Dim MaxList As clsMaxList.clsDataColumn = myMaxList.Item(myDataGrid.GetColumnBinding(mDataGrid.Columns(c + iCol)).Path.Path)
                    Select Case MaxList.Type
                        Case "Boolean"
                            If sVal.ToLower = "true" Or sVal.ToLower = "false" Or sVal = "1" Or sVal = "0" Then dr.GetType.GetProperty(MaxList.Name).SetValue(dr, CBool(sVal), Nothing)
                        Case "DateTime"
                            If IsDate(sVal) Then dr.GetType.GetProperty(MaxList.Name).SetValue(dr, CDate(sVal), Nothing)
                        Case "Decimal"
                            If IsNumeric(sVal) And sVal.Length < 10 Then dr.GetType.GetProperty(MaxList.Name).SetValue(dr, CDec(sVal), Nothing)
                        Case "Int32"
                            If IsNumeric(sVal) And sVal.Length < 7 Then dr.GetType.GetProperty(MaxList.Name).SetValue(dr, CInt(sVal), Nothing)
                        Case "String"
                            If sVal.Length > MaxList.MaxLength Then sVal = sVal.Substring(0, MaxList.MaxLength)
                            dr.GetType.GetProperty(MaxList.Name).SetValue(dr, sVal, Nothing)
                    End Select
                End If
            Next
        Next
        Me.Cursor = Cursors.Arrow
    End Sub

#End Region

End Class
