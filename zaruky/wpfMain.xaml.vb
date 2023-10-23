Imports System.Windows.Threading
Imports System.Data.SqlServerCe

'http://msdn.microsoft.com/en-us/library/bb386915(v=vs.110).aspx filtering datacontext

Class wpfMain

#Region " Properties "
    Private Const NewBuild As Integer = 15
    Private ZJShareMem As New clsSharedMemory
    Private WithEvents timZJS, timFiltr, timStart As DispatcherTimer
    Private dcZaruky As zarukyContext
    Private MainPass, MenaDB, editDatabaze As String
    Private DataGridLoading, CheckBoxTargetUpdated As Boolean
    Private SelectedID, BackupID As Integer
    Private WithEvents ThreadWorker As New System.ComponentModel.BackgroundWorker
    Private myTask As New clsTaskScheduler

    Structure CenyCelkem
        Dim Nakup As Decimal
        Dim Prodej As Decimal
    End Structure
    Private Celkem As CenyCelkem

    Structure ColumnsPosition
        Dim Current As String
        Dim Last As String
    End Structure
    Private PoradiSloupcu As ColumnsPosition
#End Region


#Region " Window "

#Region " Info Umístění "

    Private Sub sbDatabaze_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles sbDatabaze.MouseDown
        ShowUmisteni()
    End Sub

    Private Sub wpfMain_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If Keyboard.IsKeyDown(Key.LeftCtrl) And e.Key = Key.U Then ShowUmisteni()
    End Sub

    Private Sub ShowUmisteni()
        Dim FormDialog = New wpfDialog(Me, "Jste připojeni k této databázi:" + NR + SDFpath _
            + NR + NR + "Umístění příloh:" + NR + ATTpath _
            + NR + NR + "Umístění záloh:" + NR + Nastaveni.CestaZaloh _
            + NR + NR + "Logující soubor:" + NR + myLogFile.FilePath _
            + NR + NR + "Soubor s nastavením:" + NR + INIpath, "Umístění souborů", wpfDialog.Ikona.ok, "OK")
        FormDialog.ShowDialog()
    End Sub

    Private Sub sbToday_MouseEnter(sender As Object, e As MouseEventArgs) Handles sbToday.MouseEnter
        sbToday.ToolTip = Globalization.DateTimeFormatInfo.CurrentInfo.GetDayName(Today.DayOfWeek)
    End Sub

#End Region

#Region " SharedMemory "

    Private Sub timZJS_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles timZJS.Tick
        Try
            If ZJShareMem.DataExists Then
                If ZJShareMem.Peek() = "ZJS:Zaruky:EXIT" Then Me.Close()
            End If
        Catch
            timZJS.Stop()
        End Try
    End Sub
#End Region

#Region " ContextMenu "

    Private Sub sbZoom_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles sbZoom.MouseDown
        sbZoom.ContextMenu.IsOpen = True
    End Sub

    Private Sub sbZoom_TouchDown(sender As Object, e As TouchEventArgs) Handles sbZoom.TouchDown
        sbZoom.ContextMenu.IsOpen = True
    End Sub

    Private Function GetImgOK() As Image
        Dim img As New Image
        img.Source = CType(Me.FindResource("imgOK"), ImageSource)
        img.Height = 20
        Return img
    End Function

    Private Sub cmiDatabaze_Click(sender As Object, e As RoutedEventArgs)
        Dim mItem As MenuItem = CType(sender, MenuItem)
        For Each oneItem As MenuItem In btnDatabaze.ContextMenu.Items
            oneItem.Icon = Nothing : oneItem.FontSize = 14
        Next
        mItem.Icon = GetImgOK() : mItem.FontSize = 16
        JmenoLoad = mItem.Header.ToString
        proLoadDatabaze()
    End Sub

    Private Sub btnDatabaze_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles btnDatabaze.MouseDown
        If e.RightButton = MouseButtonState.Pressed Then
            Dim cmuDatabaze As ContextMenu = btnDatabaze.ContextMenu
            cmuDatabaze.PlacementTarget = btnDatabaze
            cmuDatabaze.Placement = Primitives.PlacementMode.Bottom
            cmuDatabaze.IsOpen = True
        End If
    End Sub

    Private Sub btnUkonce_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles btnUkonce.MouseDown
        If e.RightButton = MouseButtonState.Pressed Then
            Dim cmuUkonce As ContextMenu = btnUkonce.ContextMenu
            cmuUkonce.PlacementTarget = btnUkonce
            cmuUkonce.Placement = Primitives.PlacementMode.Bottom
            cmuUkonce.IsOpen = True
        End If
    End Sub

    Private Sub txtDay_LostFocus(sender As Object, e As RoutedEventArgs) Handles txtDay.LostFocus
        If IsNumeric(txtDay.Text) = False Then txtDay.Text = Nastaveni.DnuDoKonce.ToString
        If Not CInt(txtDay.Text) = Nastaveni.DnuDoKonce Then
            Nastaveni.DnuDoKonce = CInt(txtDay.Text)
            proReadyDataGrid()
        End If
    End Sub

    Private Sub btnSeradit_Click(sender As Object, e As RoutedEventArgs) Handles btnSeradit.Click
        SeraditContextMenu()
    End Sub

    Private Sub TextBlock_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles btnSeradit.MouseDown
        If e.RightButton = MouseButtonState.Pressed Then SeraditContextMenu()
    End Sub

    Private Sub SeraditContextMenu()
        Dim cmuSeradit As ContextMenu = btnSeradit.ContextMenu
        cmuSeradit.PlacementTarget = btnSeradit
        cmuSeradit.Placement = Primitives.PlacementMode.Bottom
        cmuSeradit.IsOpen = True
    End Sub

    Private Sub cmiSeradit_Click(sender As Object, e As RoutedEventArgs)
        proSeradit(CType(sender, MenuItem))
        proReadyDataGrid()
    End Sub

    Private Sub proSeradit(ByVal cmi As MenuItem)
        For Each oneItem As MenuItem In btnSeradit.ContextMenu.Items
            oneItem.Icon = Nothing
        Next
        cmi.Icon = GetImgOK()
        Nastaveni.RaditPodleDoby = btnSeradit.ContextMenu.Items.IndexOf(cmi)
    End Sub

    Private Sub cmiEdit_Click(sender As Object, e As RoutedEventArgs)
        For Each oneItem As MenuItem In btnUpravit.ContextMenu.Items
            oneItem.Icon = Nothing
        Next
        Dim Item As MenuItem = CType(sender, MenuItem)
        Item.Icon = GetImgOK()
        Nastaveni.TableEdit = If(btnUpravit.ContextMenu.Items.IndexOf(Item) = 0, False, True)
        proOpenUdaje()
    End Sub

    Private Sub btnUpravit_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles btnUpravit.MouseDown
        If e.RightButton = MouseButtonState.Pressed Then
            CType(btnUpravit.ContextMenu.Items(1), MenuItem).IsEnabled = If(Verze = 1, False, True)
            Dim cmuUpravit As ContextMenu = btnUpravit.ContextMenu
            cmuUpravit.PlacementTarget = btnUpravit
            cmuUpravit.Placement = Primitives.PlacementMode.Bottom
            cmuUpravit.IsOpen = True
        End If
    End Sub

    Private Sub btnTisk_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles btnTisk.MouseDown
        If e.RightButton = MouseButtonState.Pressed Then
            Dim cmuTisk As ContextMenu = btnTisk.ContextMenu
            cmuTisk.PlacementTarget = btnTisk
            cmuTisk.Placement = Primitives.PlacementMode.Bottom
            cmuTisk.IsOpen = True
        End If
    End Sub

#End Region

#Region " Image "

    Private Sub mImage_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles mImage.MouseUp
        mFileBrowser.LaunchFiles()
    End Sub

    Private Sub mImage_TouchDown(sender As Object, e As TouchEventArgs) Handles mImage.TouchDown
        mFileBrowser.LaunchFiles()
    End Sub

    Private Sub ImageChanged(Filename As String) Handles mFileBrowser.SelectedItemChanged
        If Filename = "" Then Filename = "nopic"
        Select Case Filename.ToLower.Substring(Filename.Length - 4, 4)
            Case ".jpg", "jpeg", ".bmp", ".png", ".gif"
                mFileBrowser.SetValue(Grid.RowSpanProperty, 1)
                mImage.Visibility = Windows.Visibility.Visible
                Dim Bitmap As New BitmapImage
                Bitmap.BeginInit()
                Bitmap.CacheOption = BitmapCacheOption.OnLoad
                Bitmap.UriSource = New Uri(Filename, UriKind.Absolute)
                Bitmap.EndInit()
                mImage.Source = Bitmap
            Case Else
                mImage.Source = Nothing
                mImage.Visibility = Windows.Visibility.Collapsed
                mFileBrowser.SetValue(Grid.RowSpanProperty, 3)
        End Select
    End Sub
#End Region

#Region " Resize "

    Private Sub wpfMain_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles Me.SizeChanged
        'sbCelkem.Margin = New Thickness(Me.ActualWidth - 500, 0, 0, 0)
    End Sub

    Private Sub GetVisual()
        For Each oneItem As Object In mToolBar.Items
            Dim Elem As UIElement = CType(CType(oneItem, DependencyObject), UIElement)
            Do
                Try
                    Elem = DirectCast(VisualTreeHelper.GetChild(Elem, 0), UIElement)
                Catch
                    Exit Sub
                End Try
                If TypeOf Elem Is TextBlock Then
                    CType(Elem, TextBlock).Visibility = Windows.Visibility.Hidden   '.Margin = New Thickness(Mezera, 0, Mezera, 0)
                    Exit Do
                End If
            Loop
        Next
    End Sub

#End Region

#Region " StatusBar Celkem "

    Private Sub txtLimit_KeyUp(sender As Object, e As KeyEventArgs) Handles txtLimit.KeyUp
        If e.Key = Key.Enter And IsNumeric(txtLimit.Text) Then
            Nastaveni.LimitPolozek = CInt(txtLimit.Text)
            proFiltrZaruky()
        End If
    End Sub

    Private Sub sbDatabazeText(ByVal Jmeno As String)
        editDatabaze = Jmeno
        sbDatabaze.Text = String.Format("Databáze: {0}", Jmeno)
    End Sub
    Private Sub sbPolozekText(Polozek As Integer)
        sbPolozek.Text = String.Format("Položek: {0}", Polozek)
    End Sub
    Private Sub sbZobrazenoText()
        sbZobrazeno.Text = String.Format("Zobrazeno: {0}", mDataGrid.Items.Count)
    End Sub
    Private Sub sbCelkemText(Castka As Double)
        sbCelkem.Text = String.Format("{0} celkem: {1:N2} {2}", GetMenuText, Castka, MenaDB)
    End Sub

    Private Sub sbCelkem_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles sbCelkem.MouseDown
        If e.LeftButton = MouseButtonState.Pressed Then MoveChecked()
    End Sub

    Private Sub sbCelkem_TouchDown(sender As Object, e As TouchEventArgs) Handles sbCelkem.TouchDown
        MoveChecked()
    End Sub

    Private Sub cmiCelkem_Click(sender As Object, e As RoutedEventArgs)
        For Each Item As MenuItem In sbCelkem.ContextMenu.Items
            Item.IsChecked = False
        Next
        Dim miItem As MenuItem = CType(sender, MenuItem)
        miItem.IsChecked = True
        sbCelkemText(Calculate(miItem.Header.ToString))
    End Sub

    Private Function GetMenuText() As String
        For Each Item As MenuItem In sbCelkem.ContextMenu.Items
            If Item.IsChecked Then Return Item.Header.ToString
        Next
        Return "Nákup"
    End Function

    Private Sub MoveChecked()
        For a As Integer = 0 To 2
            If CType(sbCelkem.ContextMenu.Items(a), MenuItem).IsChecked Then
                CType(sbCelkem.ContextMenu.Items(a), MenuItem).IsChecked = False
                Dim NewIndex As Integer = If(a + 1 = 3, 0, a + 1)
                CType(sbCelkem.ContextMenu.Items(NewIndex), MenuItem).IsChecked = True
                sbCelkemText(Calculate(NewIndex))
                Exit For
            End If
        Next
    End Sub

    Private Function Calculate(ByVal iIndex As Integer) As Decimal
        Select Case iIndex
            Case 0
                Return Celkem.Nakup
            Case 1
                Return Celkem.Prodej
            Case Else
                Return Celkem.Prodej - Celkem.Nakup
        End Select
    End Function

    Private Function Calculate(ByVal sName As String) As Decimal
        Select Case sName
            Case "Nákup"
                Return Celkem.Nakup
            Case "Prodej"
                Return Celkem.Prodej
            Case Else
                Return Celkem.Prodej - Celkem.Nakup
        End Select
    End Function
#End Region

#Region " ToolBar "

    Private Sub btnUmisteni_Click(sender As Object, e As RoutedEventArgs) Handles btnUmisteni.Click
        Dim wnd As New wpfLocation
        wnd.Owner = Me
        wnd.ShowDialog()
        If wnd.ReloadNeeded Then timStart.Start()
    End Sub

    Private Sub btnDatabaze_Click(sender As Object, e As RoutedEventArgs) Handles btnDatabaze.Click
        If myLogFile.CheckAccess(Me, True) = False Then Exit Sub

        Dim wnd As New wpfDatabase
        wnd.Owner = Me
        wnd.ShowDialog()

        myLogFile.CheckAccess(Me, False)
        If wnd.ReloadNeeded Then proLoadDatabaze()
    End Sub

    Private Sub btnRegistrace_Click(sender As Object, e As RoutedEventArgs) Handles btnRegistrace.Click
        OpenSetting(If(Verze = 4, "About", "Registr"))
    End Sub

    Private Sub OpenSetting(ByVal PageName As String)
        Dim wnd As New wpfSetting
        wnd.Owner = Application.Current.MainWindow
        wnd.StartPageName = PageName
        wnd.ShowDialog()
        If wnd.ReloadNeeded Then
            proUpdateSecurity(0, 0, True)
            SwitchToVerze1()
            proLoadDatabaze()
        End If
    End Sub

    Private Sub btnNeprosle_Click(sender As Object, e As RoutedEventArgs) Handles btnNeprosle.Click, btnProsle.Click, btnUkonce.Click, btnVsechny.Click
        Nastaveni.ProsleNeprosle = mToolBar.Items.IndexOf(sender)
        CheckButton(Nastaveni.ProsleNeprosle)
        proReadyDataGrid()
    End Sub

    Private Sub CheckButton(ByVal ButtonIndex As Integer)
        For a As Integer = 1 To 4
            CType(mToolBar.Items(a), Primitives.ToggleButton).IsChecked = False
        Next
        CType(mToolBar.Items(ButtonIndex), Primitives.ToggleButton).IsChecked = True
    End Sub

    Private Sub btnHledat_Click(sender As Object, e As RoutedEventArgs) Handles btnHledat.Click
        Dim FormDialog As New wpfDialog(Me, "Hledat text: ", "Hledání v seznamu", wpfDialog.Ikona.lupa, "Najít", "Zavřít", True)
        If FormDialog.ShowDialog = False Then Exit Sub

        For Each dr As Zaruky In CType(mDataGrid.ItemsSource, IEnumerable(Of Zaruky))
            If FindTextInRow(dr, FormDialog.Input) Then
                mDataGrid.SelectedItem = dr
                mDataGrid.ScrollIntoView(dr)
                Dim sHledat As String = FormDialog.Input
                FormDialog = New wpfDialog(Me, "Nalezená věc:" & NR & dr.Vec & NR & NR & "Pokračovat v prohledávání?", "Hledání v seznamu", wpfDialog.Ikona.lupa, "Najít další", "Zavřít", True)
                FormDialog.Input = sHledat
                If FormDialog.ShowDialog = False Then Exit For
            End If
        Next
    End Sub

    Private Sub btnUpravit_Click(sender As Object, e As RoutedEventArgs) Handles btnUpravit.Click
        proOpenUdaje()
    End Sub

    Private Sub RowDoubleClick()
        If mToolBar.IsEnabled Then proOpenUdaje()
    End Sub

#End Region

#Region " Editovat data "

    Private Sub proOpenUdaje()
        If editDatabaze = Nothing Then editDatabaze = If(JmenoLoad = coAll, JmenoDef, JmenoLoad)

        If Not Verze = 1 AndAlso myLogFile.CheckAccess(Me, True, editDatabaze) = False Then Exit Sub

        Dim bPocetZmen As Integer
        If Nastaveni.TableEdit And Not Verze = 1 Then
            Dim wEdit As New wpfTableEdit
            wEdit.EditDatabaze = editDatabaze
            wEdit.SelectedID = SelectedID
            wEdit.Owner = Me
            wEdit.ShowDialog()
            SelectedID = wEdit.SelectedID
            bPocetZmen = wEdit.PocetZmen
        Else
            Dim wEdit As New wpfRowEdit
            wEdit.EditDatabaze = editDatabaze
            wEdit.SelectedID = SelectedID
            wEdit.Owner = Me
            wEdit.ShowDialog()
            SelectedID = wEdit.SelectedID
            bPocetZmen = wEdit.PocetZmen
        End If

        myLogFile.CheckAccess(Me, False)

        If bPocetZmen > 0 Then
            BackupID = SelectedID
            If mDataGrid.Items.Count = 0 Then CheckButton(4)
            If proUpdateSecurity(bPocetZmen) Then proReadyDataGrid()
        Else
            If Not mDataGrid.ItemsSource Is Nothing Then
                Dim dr As Zaruky = CType(mDataGrid.ItemsSource, IEnumerable(Of Zaruky)).FirstOrDefault(Function(x) x.ID = If(BackupID = 0, SelectedID, BackupID))
                If dr IsNot Nothing Then mDataGrid.SelectedItem = dr
            End If
        End If
        mDataGrid.Focus()
        mFileBrowser.Reload()
    End Sub
#End Region

#Region " Najít text "

    Private Function FindTextInRow(ByVal row As Zaruky, ByVal sText As String) As Boolean
        If If(row.Vec Is Nothing, False, row.Vec.ToLower.Contains(sText.ToLower)) Or
            If(row.Faktura Is Nothing, False, row.Faktura.ToLower.Contains(sText.ToLower)) Or
            If(row.SerNum Is Nothing, False, row.SerNum.ToLower.Contains(sText.ToLower)) Or
            If(row.Dodavatel Is Nothing, False, row.Dodavatel.ToLower.Contains(sText.ToLower)) Or
             row.Cena.ToString.ToLower.Contains(sText.ToLower) Or
             row.CenaOpt.ToString.ToLower.Contains(sText.ToLower) Or
            If(row.DatPoc Is Nothing, False, row.DatPoc.ToString.ToLower.Contains(sText.ToLower)) Or
            If(row.DatKon Is Nothing, False, row.DatKon.ToString.ToLower.Contains(sText.ToLower)) Or
            If(row.DatOpt Is Nothing, False, row.DatOpt.ToString.ToLower.Contains(sText.ToLower)) Or
            If(row.Optio1 Is Nothing, False, row.Optio1.ToLower.Contains(sText.ToLower)) Or
            If(row.Optio2 Is Nothing, False, row.Optio2.ToLower.Contains(sText.ToLower)) Or
            If(row.Optio3 Is Nothing, False, row.Optio3.ToLower.Contains(sText.ToLower)) Or
            If(row.Optio4 Is Nothing, False, row.Optio4.ToLower.Contains(sText.ToLower)) Or
            If(row.Optio5 Is Nothing, False, row.Optio5.ToLower.Contains(sText.ToLower)) Then Return True
        Return False

    End Function

#End Region

#Region " Load "

    Private Sub wMain_Loaded(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
        Me.Icon = Application.Icon
        PoradiSloupcu.Last = "avoid nothing"
        ImageChanged("nopic")
        sbProgress.Visibility = Windows.Visibility.Collapsed
        sbToday.Text = "Dnes " + Today.ToShortDateString
        txtLimit.Text = Nastaveni.LimitPolozek.ToString
        CheckButton(If(ShowUkonce, 2, Nastaveni.ProsleNeprosle))
        Me.Top = Nastaveni.MainTop
        Me.Left = Nastaveni.MainLeft
        Me.Height = Nastaveni.MainHeight
        Me.Width = Nastaveni.MainWidth
        If Nastaveni.MainSpliter > 80 Then Nastaveni.MainSpliter = 20
        mGrid.ColumnDefinitions(0).Width = New GridLength(100 - Nastaveni.MainSpliter, GridUnitType.Star)
        mGrid.ColumnDefinitions(2).Width = New GridLength(Nastaveni.MainSpliter, GridUnitType.Star)
        proSeradit(CType(btnSeradit.ContextMenu.Items(Nastaveni.RaditPodleDoby), MenuItem))
        txtDay.Text = Nastaveni.DnuDoKonce.ToString
        DataGridCellResize()
        'LoadRegisterAndCheckDisk()
        If Verze = 1 Then Nastaveni.TableEdit = False
        CType(btnUpravit.ContextMenu.Items(If(Nastaveni.TableEdit, 1, 0)), MenuItem).Icon = GetImgOK()
        btnHlidat.IsChecked = myTask.Exist
        ZJShareMem.Open("ZJS")
        timZJS = New DispatcherTimer
        timZJS.Interval = TimeSpan.FromSeconds(1)
        timZJS.Start()
        timFiltr = New DispatcherTimer
        timFiltr.Interval = TimeSpan.FromMilliseconds(1)
        timStart = New DispatcherTimer
        timStart.Interval = TimeSpan.FromMilliseconds(1)
        timStart.Start()
    End Sub

    Private Sub timStart_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles timStart.Tick
        timStart.Stop()
        myLogFile.CloseAccess()

        If Application.VersionNo = 434 Then
            If Not Nastaveni.AppVerze = 434 And Application.winStore = False Then
                Nastaveni.AppVerze = 434
                Dim wDialog = New wpfDialog(Me, "Vítejte v nové verzi Záruk" + NR + NR +
                    "Získání aplikace bylo přesunuto do Windows Storu." + NR +
                    "Nové verze budou dostupné pouze přes Store." + NR +
                    "To eliminuje problémy kvůli nepodepsanému kódu." + NR + NR +
                    "Kdykoliv můžete začít používat aplikaci ze Storu." + NR +
                    "Aplikaci pak hledejte ve START nabídce Windows." + NR +
                    "Získáné Pro verze zůstávají samozřejmě aktivní." + NR, Me.Title, wpfDialog.Ikona.ok, "Store")
                If wDialog.ShowDialog() = True Then
                    myLink.Start(Me, "ms-windows-store://pdp/?productid=9NM49C3HVVZ5")
                End If
            End If
        End If

        If myFile.Exist(SDFpath) = False Then
            proCreateSDFDatabase(SDFpath)
        End If

        If checkSDFConn(SDFpath, True) Then
            proLoadDatabaze()
            proBackup(False)
        Else
            If Verze = 4 Then
                Dim wnd As New wpfLocation
                wnd.Owner = Me
                wnd.ShowDialog()
                If wnd.ReloadNeeded Then proLoadDatabaze()
            Else
                mToolBar.IsEnabled = False
                Me.Close()
            End If
        End If
    End Sub

#End Region

#Region " Closing "

    Private Sub wpfMain_Closed(sender As Object, e As EventArgs) Handles Me.Closed
        If Me.WindowState = Windows.WindowState.Normal Then
            Nastaveni.MainTop = CInt(Me.Top)
            Nastaveni.MainLeft = CInt(Me.Left)
            Nastaveni.MainWidth = CInt(Me.ActualWidth)
            Nastaveni.MainHeight = CInt(Me.ActualHeight)
        End If
        Nastaveni.MainSpliter = CInt(mGrid.ColumnDefinitions(2).ActualWidth / mGrid.ActualWidth * 100)
        Call (New clsSerialization(Nastaveni, Me)).WriteXml(INIpath)
        CloseAllConnections()
        myLogFile.CloseAccess()
        myFolder.DeleteEmpty(ATTpath, True)
    End Sub

    Private Sub wpfMain_Closing(sender As Object, e As ComponentModel.CancelEventArgs) Handles Me.Closing
        If ThreadWorker.IsBusy Then e.Cancel = True : Exit Sub
    End Sub

#End Region

#End Region


#Region " Database "

#Region " Security "

    Public Function proUpdateSecurity(iZmen As Integer, Optional iBuild As Integer = 0, Optional bZmenitVerze As Boolean = False, Optional drSecurity As Security = Nothing) As Boolean
        dcZaruky = New zarukyContext(SdfConnection)
        Dim query As IQueryable(Of Security) = From a As Security In dcZaruky.Securities Select a
        Dim dr As Security = query.First
        'Aktualizace počtu změn
        If drSecurity IsNot Nothing Then
            iZmen += drSecurity.Mods
            If dr.Vytvoreno > drSecurity.Vytvoreno Then dr.Vytvoreno = drSecurity.Vytvoreno
        End If
        If dr.Mods + iZmen > 1000000 Then dr.Mods = coMods + 1
        dr.Mods += iZmen
        If iZmen > 0 Then dr.Upraveno = Now
        'Aktualizace build
        If Not iBuild = 0 Then dr.Build = iBuild
        'Kontrola prošlé zkušební verze
        Dim SwitchTo1 As Boolean
        If dr.Mods > coMods Or dr.Vytvoreno.AddMonths(2) < dr.Upraveno Or dr.Vytvoreno.AddDays(-1) > dr.Upraveno Or dr.Vytvoreno.AddMonths(2) < Now Then
            If dr.Verze = 2 Then
                Dim FormDialog = New wpfDialog(Me, "Vypršela zkušební doba tohoto programu." _
                    & NR & NR & "Pokud by jste měli zájem o plnou verzi," _
                    & NR & "klikněte v hlavním menu na Registrace." _
                    & NR & NR & "Jinak můžete pokračovat ve freeware verzi," _
                    & NR & "na kterou teď bude automaticky přepnuto.", "Záruky - zkušební doba", wpfDialog.Ikona.varovani, "Pokračovat")
                FormDialog.ShowDialog()
            End If
            If TrialRunOut = False Then SwitchTo1 = True
            TrialRunOut = True
            dr.Verze = 1
        End If
        'Určení verze
        If Not Verze = 4 Then
            If dr.Mods = 0 Then
                'První určení verze je Trial
                Verze = 2 : iZmen += 1
            Else
                If bZmenitVerze Then
                    dr.Verze = Verze
                Else
                    Verze = If(Verze = 4, 1, dr.Verze)
                End If
            End If
        End If
        'Heslo pro zápis
        If dr.Pass IsNot Nothing Then myLogFile.wrAccess = dr.Pass
        dcZaruky.SubmitChanges()

        If SwitchTo1 Then
            SwitchToVerze1()
            proLoadDatabaze()
        End If
        Return Not SwitchTo1
    End Function

#Region " Switch To Verze 1 "
    'Vypršení zkušební verze - nastavení výchozího adresáře v Documents
    Private Sub SwitchToVerze1()
        If Not Verze = 1 Then Exit Sub
        If myFolder.DiskType(mySystem.Path.Documents) = DiskTypes.Server_4 Then
            Dim FormDialog = New wpfDialog(Me, "Ve freeware verzi můžete používat pouze adresář:" + NR + mySystem.Path.Documents + NR + NR +
                            "Tento adresář nemůže být umístěn na vzdáleném disku." + NR + "Program bude ukončen.", Application.Title, wpfDialog.Ikona.chyba, "Ukončit")
            FormDialog.ShowDialog()
            End
        End If
        If myFolder.Path(SDFpath).ToLower = mySystem.Path.Documents.ToLower Then Exit Sub
        Dim sAddText As String = "Program není registrován na tento počítač." + NR + NR
        Dim SourcePath As String = ""
        If myFile.Exist(mySystem.Path.Documents & "\zaruky.sdf") Then
            Dim FormDialog = New wpfDialog(Me, sAddText + "Ve freeware verzi můžete používat pouze adresář:" + NR + mySystem.Path.Documents + NR + NR +
                "Tento adresář již obsahuje databázi." + NR + "Chcete ji nahradit nebo ji načíst?", Application.Title, wpfDialog.Ikona.dotaz, "Nahradit", "Načíst")
            If FormDialog.ShowDialog() Then
                myFile.Copy(SDFpath, mySystem.Path.Documents & "\zaruky.sdf")
                SourcePath = myFile.Path(SDFpath)
            Else
                Nastaveni.Zalohovat = False
            End If
        Else
            Dim FormDialog = New wpfDialog(Me, sAddText + "Ve freeware verzi můžete používat pouze adresář:" + NR + mySystem.Path.Documents + NR + NR +
            "Vaše databáze se tam nyní zkopíruje.", Application.Title, wpfDialog.Ikona.varovani, "Pokračovat")
            FormDialog.ShowDialog()
            myFile.Copy(SDFpath, mySystem.Path.Documents & "\zaruky.sdf")
            SourcePath = myFile.Path(SDFpath)
        End If
        Application.CreatePaths(mySystem.Path.Documents & "\zaruky.sdf")
        If checkSDFConn(SDFpath, True) = False Then Me.Close() : Exit Sub
        If Not SourcePath = "" Then
            myFolder.Delete(mySystem.Path.Documents + "\prilohy zaruk", True)
            myFolder.Copy(SourcePath + "\prilohy zaruk", mySystem.Path.Documents + "\prilohy zaruk", True, sbProgress)
        End If
    End Sub

#End Region
#End Region

#Region " Connections "

#Region " Create SDF Connection "

    Private Function checkSDFConn(ByVal SDFfrom As String, ByVal SDFmain As Boolean, Optional ByVal dbPass As String = "") As Boolean
        If mySQL.EnsureVersion40(SDFfrom) = False Then Return False
        SdfConn2 = New SqlCeConnection(mySQL.CreateSDFConnString(SDFfrom, dbPass))
        Try
            Using dcCheckZaruky As New zarukyContext(SdfConn2)
                Dim query As IQueryable(Of Security) = From a As Security In dcCheckZaruky.Securities Select a
                Dim dr As Security = query.First
                If SDFmain Then
                    MainPass = dbPass
                    SdfConnection = SdfConn2
                    Select Case dr.Build
                        Case Is < NewBuild
                            If myLogFile.GetFullAccess(Me) = False Then Return False
                            proUpdateDatabase(dr.Build)
                        Case Is > NewBuild
                            Return UpdateProgram("Tato verze programu je starší než již existující databáze.")
                        Case Else
                            proUpdateSecurity(0)
                    End Select
                Else
                    Return proUpdateSecurity(0, 0, False, dr)
                End If
            End Using

        Catch Ex1 As Exception
            If Not SdfConn2.State = ConnectionState.Closed Then SdfConn2.Close()
            If Err.Number = 5 Then
                If Err.Description.Contains("SqlCeEngine.Upgrade") Then
                    Dim CeEng As SqlCeEngine = New SqlCeEngine(SdfConn2.ConnectionString)
                    Try
                        CeEng.Upgrade()
                        Return checkSDFConn(SDFfrom, SDFmain, dbPass)
                    Catch
                        dbPass = ZadatHeslo(dbPass)
                        If Not dbPass = "" Then Return checkSDFConn(SDFfrom, SDFmain, dbPass)
                    End Try
                ElseIf Err.Description.Contains("Db version = 4000000,Requested version = 3505053") Then
                    Dim FormDialog As New wpfDialog(Me, "Databáze je novější verze." + NR + NR + "Program bude ukončen.", "Přístup k databázi", wpfDialog.Ikona.chyba, "Ukončit")
                    FormDialog.ShowDialog()
                    End
                Else
                    dbPass = ZadatHeslo(dbPass)
                    If Not dbPass = "" Then Return checkSDFConn(SDFfrom, SDFmain, dbPass)
                End If
            End If

            Dim sHead As String = "Error num." & Err.Number & " Záruky " & Application.Version
            If SDFmain Then
                Select Case Err.Number
                    Case 5
                        Dim FormDialog As New wpfDialog(Me, "Špatně zadané vstupní heslo." + NR + NR + "Program bude ukončen.", "Přístup k databázi", wpfDialog.Ikona.chyba, "Ukončit")
                        FormDialog.ShowDialog()
                    Case 55
                        Dim FormDialog As New wpfDialog(Me, "Přístup k databází selhal." + NR + NR + "Zkusit to znovu?", "Přístup k databázi", wpfDialog.Ikona.dotaz, "Ano", "Ukončit")
                        If FormDialog.ShowDialog Then
                            Return checkSDFConn(SDFfrom, SDFmain, dbPass)
                            Exit Function
                        End If
                    Case Else
                        Dim FormDialog As New wpfDialog(Me, Ex1.Message & NR & NR & "Program bude ukončen.", sHead, wpfDialog.Ikona.chyba, "Ukončit")
                        FormDialog.ShowDialog()
                End Select
                End
            Else
                Dim FormDialog As New wpfDialog(Me, Ex1.Message & NR & NR & "Databáze nebude načtena.", sHead, wpfDialog.Ikona.chyba, "Zavřít")
                FormDialog.ShowDialog()
            End If
            Return False
        End Try
        Return True
    End Function

    Private Function ZadatHeslo(ByVal dbPass As String) As String
        If Nastaveni.HesloDatabaze Is Nothing Or Not MainPass = "" Then
            Dim FormDialog As New wpfDialog(Me, "Zadejte heslo:", "Vstupní heslo", wpfDialog.Ikona.heslo, "OK", "Zavřít", True, True, "Pamatovat si heslo", False, 30, True)
            If FormDialog.ShowDialog Then
                If FormDialog.Zatrzeno Then
                    If FormDialog.Input = "" Then
                        Nastaveni.HesloDatabaze = Nothing
                    Else
                        Nastaveni.HesloDatabaze = myString.Encrypt(FormDialog.Input, PasswordXML)
                    End If
                Else
                    Nastaveni.HesloDatabaze = Nothing
                End If
                dbPass = FormDialog.Input
            Else
                Return ""
            End If
        Else
            dbPass = myString.Decrypt(Nastaveni.HesloDatabaze, PasswordXML)
            MainPass = dbPass
        End If
        Return dbPass
    End Function

    Private Function UpdateProgram(ByVal sMessage As String) As Boolean
        Dim sDodatek As String = "Nainstalujte z Windows Storu nejnovější verzi."
        Dim FormDialog As New wpfDialog(Me, sMessage + NR + sDodatek, Application.Title, wpfDialog.Ikona.varovani, "Zavřít")
        FormDialog.ShowDialog()
        Return False
    End Function
#End Region

#Region " Create XML Connection "

    Private Function checkXMLConn(ByVal XMLfrom As String, Optional ByVal Conn12 As Integer = 1) As Boolean
        Try
            Using ds As New DataSet
                ds.ReadXml(XMLfrom)
                If ds.Tables(0).TableName = "Zaruky" And ds.Tables(1).TableName = "Databaze" Then Return True
            End Using
        Catch Ex1 As Exception
            Dim FormDialog As New wpfDialog(Me, "Error-" & Err.Number & "- " & Ex1.Message & NR & NR & "Toto není databáze programu Záruky nebo je poškozena.", Application.Title, wpfDialog.Ikona.chyba, "Zavřít")
            FormDialog.ShowDialog()
        End Try
        Return False
    End Function
#End Region

    Private Sub CloseAllConnections()
        If SdfConnection IsNot Nothing Then
            If SdfConnection.State = ConnectionState.Open Then SdfConnection.Close()
        End If
        If SdfConn2 IsNot Nothing Then
            If SdfConn2.State = ConnectionState.Open Then SdfConn2.Close()
        End If
    End Sub

#End Region

#Region " Create "

    Private Sub proCreateSDFDatabase(ByVal dbPath As String)
        If myFile.Delete(dbPath, True, True) = False Then Exit Sub
        CloseAllConnections()
        Dim sAsk As String = "Chcete-li zamezit přístup do vytvářené databáze" & NR _
                           & "komukoliv, máte možnost nyní nastavit heslo." & NR & NR _
                           & "Mějte prosím na paměťi, že pokud ho zapomenete," & NR _
                           & "k datům už se nikdo nedostane, tedy ani Vy."
        Dim FormDialog As New wpfDialog(Me, sAsk, "Vstupní heslo", wpfDialog.Ikona.heslo, "Nastavit", "Bez hesla", True, False, "", False, 30, True)
        FormDialog.ShowDialog()
        Dim sPass As String = FormDialog.Input

        Dim strConn As String = mySQL.CreateSDFConnString(dbPath, sPass)

        Dim CeEng As SqlCeEngine = New SqlCeEngine(strConn)
        CeEng.CreateDatabase()

        Dim Conn As New SqlCeConnection(strConn)
        If Conn.State = ConnectionState.Closed Then Conn.Open()

        proSDFCmdExecute(Conn, "CREATE TABLE Security (" _
                & "ID int IDENTITY NOT NULL PRIMARY KEY, " _
                & "Vytvoreno datetime NOT NULL, Upraveno datetime NOT NULL, " _
                & "Build int NOT NULL, Mods int NOT NULL, Verze int NOT NULL, " _
                & "Pass nvarchar(5) NULL, UNIQUE (ID) )")

        proSDFCmdExecute(Conn, "INSERT INTO Security (" _
               & "Vytvoreno, Upraveno, Build, Mods, Verze) VALUES (" _
               & "GETDATE(), GETDATE(), " & NewBuild & ", 0, " & Verze & ")")

        proSDFCmdExecute(Conn, "CREATE TABLE Databaze (" _
            & "Jmeno nvarchar(20) NOT NULL PRIMARY KEY, " _
            & "Vec nvarchar(15) DEFAULT 'Položka', SerNum nvarchar(15) DEFAULT 'Sériové číslo', " _
            & "DatPoc nvarchar(15) DEFAULT 'Koupeno dne', DatKon nvarchar(15) DEFAULT 'Záruka končí', " _
            & "DatOpt nvarchar(15) DEFAULT 'Prodáno dne', Dodavatel nvarchar(15) DEFAULT 'Dodavatel', " _
            & "Faktura nvarchar(15) DEFAULT 'Doklad pořízení', Cena nvarchar(15) DEFAULT 'Cena nákupní', " _
            & "Optio2 nvarchar(15) DEFAULT '2.volitelné', Optio3 nvarchar(15) DEFAULT '3.volitelné', " _
            & "Optio1 nvarchar(15) DEFAULT '1.volitelné', Positions nvarchar(40), " _
            & "CenaOpt nvarchar(15) DEFAULT 'Prodejní cena', Mena nvarchar(3) DEFAULT 'Kč', " _
            & "Optio4 nvarchar(15) DEFAULT '4.volitelné', Optio5 nvarchar(15) DEFAULT '5.volitelné', " _
            & "ownCheck nvarchar(15) DEFAULT 'Vyřízeno', Active bit DEFAULT 'True' NOT NULL, " _
            & "Optio1b bit DEFAULT 'True' NOT NULL, Optio2b bit DEFAULT 'True' NOT NULL, " _
            & "Optio3b bit DEFAULT 'True' NOT NULL, Optio4b bit DEFAULT 'True' NOT NULL, " _
            & "Optio5b bit DEFAULT 'True' NOT NULL, UNIQUE (Jmeno) )")

        proSDFCmdExecute(Conn, "INSERT INTO Databaze (Jmeno)" _
            & "VALUES ('" & JmenoDef & "')")

        proSDFCmdExecute(Conn, "CREATE TABLE Zaruky (" _
            & "ID int IDENTITY NOT NULL PRIMARY KEY, ownID int DEFAULT '0' NOT NULL, newID int DEFAULT '0' NOT NULL, " _
            & "Oznacil bit DEFAULT 'False' NOT NULL, Smazano bit DEFAULT 'False' NOT NULL, ownCheck bit DEFAULT 'False' NOT NULL, " _
            & "Vec nvarchar(80) NULL, SerNum nvarchar(30) NULL, Dodavatel nvarchar(80) NULL, Faktura nvarchar(30) NULL, " _
            & "DatPoc datetime NULL, DatKon datetime NULL, DatOpt datetime NULL, Upraveno datetime NULL, " _
            & "Cena money DEFAULT '0' NOT NULL, CenaOpt money DEFAULT '0' NOT NULL, " _
            & "Optio1 nvarchar(100) NULL, Optio2 nvarchar(30) NULL, Optio3 nvarchar(30) NULL, Optio4 nvarchar(30) NULL, Optio5 nvarchar(30) NULL, " _
            & "Databaze nvarchar(20) NULL, UNIQUE (ID) )")

        Conn.Close() : Conn.Dispose() : CeEng.Dispose()
    End Sub

    Private Sub proSDFCmdExecute(ByVal SqlConn As SqlCeConnection, ByVal SqlString As String)
        Dim Cmd As New SqlCeCommand(SqlString, SqlConn)
        Try
            Cmd.ExecuteNonQuery()
        Catch sqlexception As SqlCeException
            Dim FormDialog As New wpfDialog(Me, sqlexception.Message & NR & NR & "Program bude ukončen.", "SQL Server Compact Database Error", wpfDialog.Ikona.chyba, "Ukončit")
            FormDialog.ShowDialog()
            End
        Catch ex As Exception
            Dim FormDialog As New wpfDialog(Me, "Error:" & Err.Number & ": " & ex.Message & NR & NR & "Program bude ukončen.", Application.Title, wpfDialog.Ikona.chyba, "Ukončit")
            FormDialog.ShowDialog()
            End
        End Try
        Cmd.Dispose()
    End Sub
#End Region

#Region " Update "

    Private Sub proUpdateDatabase(ByVal iBuild As Integer, Optional ByVal bMain As Boolean = True)
        mToolBar.IsEnabled = False : mDataGrid.IsEnabled = False
        CloseAllConnections()
        If SdfConn2.State = ConnectionState.Closed Then SdfConn2.Open()

        If bMain Then
            Dim SDFbak As String = myFolder.Path(SDFpath) + "\zaruky.bak.sdf"
            Dim FormDialog As New wpfDialog(Me, "Nyní proběhne záloha databáze do:" + NR + SDFbak + NR + "a proběhne aktualizace databáze na verzi č. " + NewBuild.ToString, Application.Title, wpfDialog.Ikona.varovani, "Pokračovat")
            FormDialog.ShowDialog()
            sbInfo.Text = "Probíhá zálohování databáze."
            myFile.Copy(SDFpath, SDFbak)
        End If

        If iBuild < 3 Then
            showInfoUpdate(3)
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Zaruky ADD CenaOpt money NULL")
            proSDFCmdExecute(SdfConn2, "UPDATE Zaruky SET CenaOpt = 0")
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Databaze ADD CenaOpt nvarchar(15) DEFAULT 'Prodejní cena' NULL")
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Databaze ADD Mena nvarchar(3) NULL")
            proSDFCmdExecute(SdfConn2, "UPDATE Databaze SET CenaOpt = 'Prodejní cena'")
            proSDFCmdExecute(SdfConn2, "UPDATE Databaze SET Mena = 'Kč'")
        End If
        If iBuild < 4 Then
            showInfoUpdate(4)
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Zaruky ADD ownID int NULL")
            proSDFCmdExecute(SdfConn2, "UPDATE Zaruky SET ownID = ID")
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Zaruky ALTER COLUMN Vec nvarchar(80) NULL")
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Zaruky ALTER COLUMN DatPoc datetime NULL")
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Zaruky ALTER COLUMN DatKon datetime NULL")
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Zaruky ALTER COLUMN Cena money NULL")
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Zaruky ALTER COLUMN Upraveno datetime NULL")
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Zaruky ALTER COLUMN Databaze nvarchar(20) NULL")
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Zaruky ALTER COLUMN CenaOpt money NULL")
        End If
        If iBuild < 5 Then
            showInfoUpdate(5)
            proSDFCmdExecute(SdfConn2, "UPDATE Zaruky SET PrilohyMod = NULL")
        End If
        If iBuild < 6 Then
            showInfoUpdate(6)
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Zaruky ADD Optio4 nvarchar(30) NULL")
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Zaruky ADD Optio5 nvarchar(30) NULL")
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Databaze ADD Optio4 nvarchar(15) DEFAULT '4.volitelné' NULL")
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Databaze ADD Optio5 nvarchar(15) DEFAULT '5.volitelné' NULL")
        End If
        If iBuild < 7 Then
            showInfoUpdate(7)
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Zaruky ADD ownCheck bit DEFAULT 'False'")
            proSDFCmdExecute(SdfConn2, "UPDATE Zaruky SET ownCheck = 'False'")
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Databaze ADD ownCheck nvarchar(15) DEFAULT 'Vyřízeno' NULL")
        End If
        If iBuild < 8 Then
            showInfoUpdate(8)
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Security ADD Pass nvarchar(5) NULL")
        End If
        If iBuild < 9 Then
            showInfoUpdate(9)
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Zaruky ADD newID int NULL")
            proSDFCmdExecute(SdfConn2, "UPDATE Zaruky SET newID = ownID")
            If SdfConn2.State = ConnectionState.Open Then SdfConn2.Close()
            Dim dsOld As New DataSet
            Dim cmd As New SqlCeCommand("SEL", SdfConn2)
            cmd.CommandText = "SELECT * FROM Zaruky"
            Dim adapter As New SqlCeDataAdapter(cmd)
            adapter.Fill(dsOld)
            For Each oneRow As DataRow In dsOld.Tables(0).Rows
                TransferPriloha(oneRow, ATTpath)
            Next
            adapter.Dispose() : cmd.Dispose() : dsOld.Dispose()
            If SdfConn2.State = ConnectionState.Closed Then SdfConn2.Open()
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Zaruky DROP COLUMN Prilohy")
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Zaruky DROP COLUMN PrilohyMod")
        End If
        If iBuild < 10 Then
            showInfoUpdate(10)
            proSDFCmdExecute(SdfConn2, "UPDATE Databaze SET Positions = ''")
        End If
        If iBuild < 11 Then
            showInfoUpdate(11)
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Zaruky ALTER COLUMN Optio1 nvarchar(100) NULL")
        End If
        If iBuild < 12 Then
            showInfoUpdate(12)
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Databaze ADD Active bit DEFAULT 'True'")
            proSDFCmdExecute(SdfConn2, "UPDATE Databaze SET Active = 'True'")
        End If
        If iBuild < 13 Then
            showInfoUpdate(13)
            Dim dsOld As New DataSet
            Dim cmd As New SqlCeCommand("SEL", SdfConn2)
            cmd.CommandText = "SELECT * FROM Databaze"
            Dim adapter As New SqlCeDataAdapter(cmd)
            adapter.Fill(dsOld)
            adapter.Dispose() : cmd.Dispose()
            If dsOld.Tables(0).Columns.Contains("Active") = False Then
                proSDFCmdExecute(SdfConn2, "ALTER TABLE Databaze ADD Active bit DEFAULT 'True'")
                proSDFCmdExecute(SdfConn2, "UPDATE Databaze SET Active = 'True'")
            End If
        End If
        If iBuild < 14 Then
            showInfoUpdate(14)
            proSDFCmdExecute(SdfConn2, "UPDATE Zaruky SET ownCheck = 'False' WHERE ownCheck IS NULL")
        End If
        If iBuild < 15 Then
            showInfoUpdate(15)
            proSDFCmdExecute(SdfConn2, "UPDATE Zaruky SET Oznacil = 'False' WHERE Oznacil IS NULL")
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Zaruky ALTER COLUMN Oznacil bit NOT NULL")
            proSDFCmdExecute(SdfConn2, "UPDATE Zaruky SET Smazano = 'False' WHERE Smazano IS NULL")
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Zaruky ALTER COLUMN Smazano bit NOT NULL")
            proSDFCmdExecute(SdfConn2, "UPDATE Zaruky SET ownCheck = 'False' WHERE ownCheck IS NULL")
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Zaruky ALTER COLUMN ownCheck bit NOT NULL")

            proSDFCmdExecute(SdfConn2, "UPDATE Zaruky SET Cena = '0' WHERE Cena IS NULL")
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Zaruky ALTER COLUMN Cena SET DEFAULT '0'")
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Zaruky ALTER COLUMN Cena money NOT NULL")
            proSDFCmdExecute(SdfConn2, "UPDATE Zaruky SET CenaOpt = '0' WHERE CenaOpt IS NULL")
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Zaruky ALTER COLUMN CenaOpt SET DEFAULT '0'")
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Zaruky ALTER COLUMN CenaOpt money NOT NULL")

            proSDFCmdExecute(SdfConn2, "UPDATE Zaruky SET ownID = '0' WHERE ownID IS NULL")
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Zaruky ALTER COLUMN ownID SET DEFAULT '0'")
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Zaruky ALTER COLUMN ownID int NOT NULL")
            proSDFCmdExecute(SdfConn2, "UPDATE Zaruky SET newID = '0' WHERE newID IS NULL")
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Zaruky ALTER COLUMN newID SET DEFAULT '0'")
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Zaruky ALTER COLUMN newID int NOT NULL")

            proSDFCmdExecute(SdfConn2, "UPDATE Databaze SET Active = 'True' WHERE Active IS NULL")
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Databaze ALTER COLUMN Active bit NOT NULL")
            proSDFCmdExecute(SdfConn2, "ALTER TABLE Databaze ADD Optio1b bit DEFAULT 'True' NOT NULL, " +
            "Optio2b bit DEFAULT 'True' NOT NULL, Optio3b bit DEFAULT 'True' NOT NULL, " +
            "Optio4b bit DEFAULT 'True' NOT NULL, Optio5b bit DEFAULT 'True' NOT NULL")
        End If

        SdfConn2.Close()
        proUpdateSecurity(0, NewBuild)
        mToolBar.IsEnabled = True : mDataGrid.IsEnabled = True
        sbInfo.Text = ""
    End Sub

#Region " Sub Updaters "

    Private Sub TransferPriloha(ByVal dr As DataRow, ByVal SourcePath As String, Optional ownID As Integer = 0)
        If IsDBNull(dr("Prilohy")) Then Exit Sub
        If dr("Prilohy").ToString = "" Then Exit Sub
        If ownID = 0 Then ownID = CInt(dr("ownID"))
        Dim FLSpath As String = myFolder.Join(ATTpath, dr("Databaze").ToString, ownID.ToString)
        For Each onePriloha As String In Split(dr("Prilohy").ToString, "|")
            If Not onePriloha = "" Then
                Dim OldAttFile As String = myFile.Join(SourcePath, dr("ID").ToString & onePriloha)
                If myFile.Copy(OldAttFile, myFile.Join(FLSpath, onePriloha)) Then
                    If SourcePath = ATTpath Then myFile.Delete(OldAttFile, False)
                End If
            End If
        Next
    End Sub

    Private Sub showInfoUpdate(ByVal iBuild As Integer)
        sbInfo.Text = "Aktualizace databáze verze " + iBuild.ToString + " na verzi " + NewBuild.ToString
        myWindow.DoEvents()
    End Sub
#End Region

#End Region

#Region " Import "

#Region " Open File Dialog "

    Private Sub btnImport_Click(sender As Object, e As RoutedEventArgs) Handles btnImport.Click
        ThreadVal.Clear()
        Dim ofdMain As New Microsoft.Win32.OpenFileDialog
        ofdMain.Title = "Otevřete databázi záruk"
        ofdMain.Filter = "Databáze Záruk (SDF;XML)|zaruky.xml;zaruky.sdf|Všechny soubory (*.*)|*.*"
        ofdMain.InitialDirectory = If(myFolder.Exist(myFile.Path(SDFpath)), myFile.Path(SDFpath), Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
        ofdMain.CheckFileExists = True
        If ofdMain.ShowDialog = False Then Exit Sub
        ThreadVal.Text = ofdMain.FileName

        If LCase(ofdMain.FileName) = LCase(SDFpath) Then
            Dim FormDialog = New wpfDialog(Me, "Do této databáze se budou přidávat data." & NR & SDFpath, Application.Title, wpfDialog.Ikona.varovani, "Zavřít")
            FormDialog.ShowDialog()
        Else
            Select Case Strings.Right(ofdMain.FileName, 3).ToLower
                Case "xml"
                    If checkXMLConn(ofdMain.FileName) = False Then Exit Sub
                Case "sdf"
                    If checkSDFConn(ofdMain.FileName, False, MainPass) = False Then Exit Sub
            End Select
            sbProgress.Minimum = 0
            sbProgress.Value = 0
            sbInfo.Visibility = Windows.Visibility.Visible
            sbProgress.Visibility = Windows.Visibility.Visible
            sbInfo.Text = "Importování záruk"
            Me.Cursor = Cursors.Wait
            ThreadWorker.WorkerReportsProgress = True
            ThreadWorker.RunWorkerAsync()
        End If
    End Sub

#End Region

#Region " Update thread "

    Private Sub ThreadWorker_ProgressChanged(sender As Object, e As ComponentModel.ProgressChangedEventArgs) Handles ThreadWorker.ProgressChanged
        sbProgress.Maximum = e.ProgressPercentage
        sbProgress.Value += 1
    End Sub

    Private Sub ThreadWorker_DoWork(ByVal sender As System.Object, ByVal e As System.ComponentModel.DoWorkEventArgs) Handles ThreadWorker.DoWork
        Dim dsOld As New DataSet
        Dim Total As Integer = dcZaruky.Zarukies.Count
        'connect to old database
        Select Case Strings.Right(ThreadVal.Text, 3).ToLower
            Case "xml"
                dsOld.ReadXml(ThreadVal.Text)
                proUpdatetTableDatabase(dsOld)
            Case "sdf"
                Dim dsDatabaze As New DataSet
                Dim cmd As New SqlCeCommand("SEL", SdfConn2)
                cmd.CommandText = "SELECT * FROM Databaze"
                Dim adapter As New SqlCeDataAdapter(cmd)
                adapter.Fill(dsDatabaze)
                dsDatabaze.Tables(0).TableName = "Databaze"
                proUpdatetTableDatabase(dsDatabaze)
                dsDatabaze.Dispose()
                cmd.CommandText = "SELECT * FROM Zaruky"
                adapter.SelectCommand = cmd
                adapter.Fill(dsOld)
                adapter.Dispose() : cmd.Dispose()
                dsOld.Tables(0).TableName = "Zaruky"
        End Select
        'pokud XML databáze obsahuje pouze tabulku Setting
        If Not dsOld.Tables(0).Columns.Contains("Vec") Then
            dsOld.Dispose() : ThreadVal.Number = -1 : Exit Sub
        End If
        'transfering data
        For Each row As DataRow In dsOld.Tables(0).Rows
            ThreadWorker.ReportProgress(dsOld.Tables(0).Rows.Count)
            Dim bSmazano As Boolean = False
            If dsOld.Tables(0).Columns.Contains("Smazano") Then
                If CBool(row("Smazano")) Then bSmazano = True
            End If
            If bSmazano = False Then
                If existRow(row) = False Then
                    Total = +1
                    If Verze = 2 And Total > coRows Then
                        Dim FormDialog = New wpfDialog(Me, "Zkušební verze je omezena na " & coRows & " položek všech databází dohromady. Nyní můžete v hlavním okně přes tlačítko Registrace přepnout na Freeware licenci nebo si pořídit Pro verzi.", "Záruky - zkušební verze", wpfDialog.Ikona.heslo, "Zavřít")
                        FormDialog.ShowDialog()
                        Exit For
                    End If
                    Dim dr As New Zaruky
                    dr.Vec = CStr(row("Vec"))
                    If row.IsNull("SerNum") = False Then dr.SerNum = CStr(row("SerNum"))
                    dr.DatPoc = CDate(row("DatPoc")) : dr.DatKon = CDate(row("DatKon"))
                    dr.Cena = If(IsNumeric(row("Cena")), CDec(row("Cena")), CDec(row("Cena").ToString.Replace(".0000", "")))
                    If dsOld.Tables(0).Columns.Contains("Optio1") Then
                        If row.IsNull("Optio1") = False And Not Verze = 1 Then dr.Optio1 = CStr(row("Optio1"))
                        If row.IsNull("Optio2") = False Then dr.Optio2 = CStr(row("Optio2"))
                        If row.IsNull("Optio3") = False Then dr.Optio3 = CStr(row("Optio3"))
                    End If
                    If dsOld.Tables(0).Columns.Contains("Dodavatel") And Not Verze = 1 Then
                        If row.IsNull("Dodavatel") = False Then dr.Dodavatel = CStr(row("Dodavatel"))
                        If row.IsNull("Faktura") = False Then dr.Faktura = CStr(row("Faktura"))
                    End If
                    If dsOld.Tables(0).Columns.Contains("Databaze") Then
                        dr.Databaze = CStr(row("Databaze")) : dr.Oznacil = CBool(row("Oznacil"))
                        dr.Smazano = CBool(row("Smazano"))
                        If row.IsNull("DatOpt") = False And Not Verze = 1 Then dr.DatOpt = CDate(row("DatOpt"))
                    Else
                        dr.Databaze = If(JmenoLoad = coAll, JmenoDef, JmenoLoad)
                    End If
                    dr.Upraveno = Now : dr.CenaOpt = 0
                    If dsOld.Tables(0).Columns.Contains("CenaOpt") And Not Verze = 1 Then
                        dr.CenaOpt = CDec(row("CenaOpt"))
                    End If
                    If dsOld.Tables(0).Columns.Contains("Optio4") And Not Verze = 1 Then
                        If row.IsNull("Optio4") = False Then dr.Optio4 = CStr(row("Optio4"))
                        If row.IsNull("Optio5") = False Then dr.Optio5 = CStr(row("Optio5"))
                    End If
                    dr.OwnCheck = False
                    dr.OwnID = 0 : dr.NewID = 0
                    If dsOld.Tables(0).Columns.Contains("ownID") Then
                        dr.OwnID = CInt(row("ownID"))
                        dr.NewID = dr.OwnID
                    End If
                    dr.NewID = mySQL.GetFreeOwnID(dcZaruky, dr.Databaze, CInt(dr.NewID))
                    If dsOld.Tables(0).Columns.Contains("Prilohy") Then
                        TransferPriloha(row, myFolder.Join(myFile.Path(ThreadVal.Text), "prilohy zaruk"), CInt(dr.NewID))
                    Else
                        Dim oldPath As String = myFolder.Join(myFile.Path(ThreadVal.Text), "prilohy zaruk", dr.Databaze, dr.OwnID.ToString)
                        Dim newPath As String = myFolder.Join(ATTpath, dr.Databaze, dr.NewID.ToString)
                        myFolder.Copy(oldPath, newPath, False, sbProgress)
                    End If
                    dr.OwnID = dr.NewID
                    dcZaruky.Zarukies.InsertOnSubmit(dr)
                    ThreadVal.Number += 1
                End If
            End If
        Next
        dsOld.Dispose() : SdfConn2 = Nothing
        ThreadVal.OK = True
        dcZaruky.SubmitChanges()
    End Sub

    Private Function existRow(ByVal FindRow As DataRow) As Boolean
        If FindRow.Table.Columns.Contains("ownID") Then
            Dim query As IQueryable(Of Zaruky) =
                From a As Zaruky In dcZaruky.Zarukies
                Where a.Databaze = CStr(FindRow("Databaze")) And a.OwnID = CInt(FindRow("ownID"))
                Select a
            Return If(query.Count = 0, False, True)
        End If

        Dim query2 As IQueryable(Of Zaruky) =
            From a As Zaruky In dcZaruky.Zarukies
            Where a.SerNum = CStr(FindRow("SerNum"))
            Select a
        Return If(query2.Count = 0, False, True)
    End Function

    Private Sub ThreadWorker_RunWorkerCompleted(ByVal sender As Object, ByVal e As System.ComponentModel.RunWorkerCompletedEventArgs) Handles ThreadWorker.RunWorkerCompleted
        If ThreadVal.OK And ThreadVal.Number > 0 Then proReadyDataGrid()
        Me.Cursor = Cursors.Arrow
        sbInfo.Visibility = Windows.Visibility.Collapsed
        sbProgress.Visibility = Windows.Visibility.Collapsed
        Dim FormDialog = New wpfDialog(Me, "Bylo přidáno " + ThreadVal.Number.ToString + " záznamů.", Application.Title, wpfDialog.Ikona.ok, "Zavřít")
        FormDialog.ShowDialog()
    End Sub
#End Region

#Region " Update table database "

    Private Sub proUpdatetTableDatabase(ByVal dsOld As DataSet)
        For Each row As DataRow In dsOld.Tables("Databaze").Rows
            Dim CenaOpt As String = "Prodáno"
            If dsOld.Tables(0).Columns.Contains("CenaOpt") Then CenaOpt = row("CenaOpt").ToString
            Dim Mena As String = "Kč"
            If dsOld.Tables(0).Columns.Contains("Mena") Then Mena = row("Mena").ToString
            Dim Optio4 As String = "4.volitelné"
            If dsOld.Tables(0).Columns.Contains("Optio4") Then Optio4 = row("Optio4").ToString
            Dim Optio5 As String = "5.volitelné"
            If dsOld.Tables(0).Columns.Contains("Optio5") Then Optio5 = row("Optio5").ToString
            Dim ownCheck As String = "Vyřízeno"
            If dsOld.Tables(0).Columns.Contains("ownCheck") Then ownCheck = row("ownCheck").ToString

            Dim query As IQueryable(Of Databaze) =
                From a As Databaze In dcZaruky.Databazes
                Where a.Jmeno = CStr(row("Jmeno"))
                Select a

            Dim ceRow As Databaze
            If query.Count = 0 Then
                ceRow = New Databaze
                ceRow.Jmeno = row("Jmeno").ToString : ceRow.Active = True
                dcZaruky.Databazes.InsertOnSubmit(ceRow)
            Else
                ceRow = query.First
            End If
            ceRow.Vec = row("Vec").ToString : ceRow.SerNum = row("SerNum").ToString : ceRow.DatPoc = row("DatPoc").ToString : ceRow.DatKon = row("DatKon").ToString : ceRow.DatOpt = row("DatOpt").ToString
            ceRow.Dodavatel = row("Dodavatel").ToString : ceRow.Faktura = row("Faktura").ToString : ceRow.Cena = row("Cena").ToString : ceRow.CenaOpt = CenaOpt
            ceRow.Optio1 = row("Optio1").ToString : ceRow.Optio2 = row("Optio2").ToString : ceRow.Optio3 = row("Optio3").ToString
            ceRow.Positions = row("Positions").ToString : ceRow.Mena = Mena : ceRow.Optio4 = Optio4 : ceRow.Optio5 = Optio5 : ceRow.OwnCheck = ownCheck
            If dsOld.Tables(0).Columns.Contains("Optio1b") Then
                ceRow.Optio1b = CBool(row("Optio1b")) : ceRow.Optio2b = CBool(row("Optio2b")) : ceRow.Optio3b = CBool(row("Optio3b")) : ceRow.Optio4b = CBool(row("Optio4b")) : ceRow.Optio5b = CBool(row("Optio5b"))
            End If
        Next
    End Sub
#End Region

#End Region

#Region " Back-up "

    Public Sub proBackup(Force As Boolean)
        If Verze = 1 Or Nastaveni.CestaZaloh = "" Then Exit Sub
        If Force OrElse Nastaveni.Zalohovat Then
            If myFolder.Exist(Nastaveni.CestaZaloh, True) Then
                Dim zDate As Date = (From a As Security In dcZaruky.Securities Select a).First.Upraveno
                Dim zName As String = Today.Year.ToString + "-" + myString.FromNumber(Today.Month, 2) + "-" + myString.FromNumber(Today.Day, 2) + " " + myString.FromNumber(Now.Hour, 2) + "h" + myString.FromNumber(Now.Minute, 2)
                Dim Filename As String = myFile.Join(Nastaveni.CestaZaloh, zName + ".sdf")
                If Force OrElse zDate.Date > Nastaveni.ZalohaDne.Date Then
                    Nastaveni.ZalohaDne = zDate
                    sbInfo.Visibility = Windows.Visibility.Visible
                    sbInfo.Text = "Zálohování"
                    If myFile.Delete(Filename, True) Then myFile.Copy(SDFpath, Filename)
                    If myFolder.Delete(myFolder.Join(Nastaveni.CestaZaloh, zName), True) Then
                        myFolder.Copy(ATTpath, myFolder.Join(Nastaveni.CestaZaloh, zName), True, sbProgress)
                    End If
                    sbInfo.Visibility = Windows.Visibility.Collapsed
                End If
            End If
        End If
    End Sub

#End Region

#Region " Load Databaze "

    Private Sub proLoadDatabaze()
        Me.Cursor = Cursors.Wait

        DataGridLoading = True
        Me.Title = Application.Title()
        dcZaruky = New zarukyContext(SdfConnection)
        Dim query As IQueryable(Of Databaze) = From a As Databaze In dcZaruky.Databazes Select a
        JmenoDef = query.First.Jmeno
        If Verze = 1 Then JmenoLoad = JmenoDef
        sbDatabaze.Text = "Databáze: " + JmenoLoad
        JmenoLast = JmenoLoad

        Dim cMenu As ContextMenu = btnDatabaze.ContextMenu
        cMenu.Items.Clear()
        Dim mItem As New MenuItem
        mItem.Header = coAll
        mItem.FontSize = 16
        If JmenoLoad = mItem.Header.ToString Then mItem.Icon = GetImgOK() : mItem.FontSize = 18
        cMenu.Items.Add(mItem)
        AddHandler mItem.Click, AddressOf cmiDatabaze_Click
        For Each oneRow As Databaze In query
            mItem = New MenuItem
            mItem.Header = oneRow.Jmeno
            mItem.FontSize = 16
            If JmenoLoad = mItem.Header.ToString Then mItem.Icon = GetImgOK() : mItem.FontSize = 18
            cMenu.Items.Add(mItem)
            AddHandler mItem.Click, AddressOf cmiDatabaze_Click
        Next

        Dim isFull As Boolean = If(Verze = 1, False, True)
        Dim isVisible As Visibility = If(isFull, Visibility.Visible, Visibility.Collapsed)
        mDataGrid.SetValue(Grid.RowSpanProperty, If(isFull, 1, 2))
        btnDatabaze.Visibility = isVisible
        btnUmisteni.Visibility = isVisible
        btnFiltr.Visibility = isVisible
        proLoadDataGrid()

        Me.Cursor = Cursors.Arrow
    End Sub
#End Region

#Region " Load Zaruky "

    Private colZaruky As ICollection(Of Zaruky)

    Private Sub proLoadZaruky()
        Me.Cursor = Cursors.Wait

        Using dcZarukyView As New zarukyContext(SdfConnection)
            Dim queryZaruky As IQueryable(Of Zaruky)

            If Verze = 1 Then
                queryZaruky = From a As Zaruky In dcZarukyView.Zarukies Where a.Smazano = False Select a
            ElseIf JmenoLoad = coAll Then
                queryZaruky = From a As Zaruky In dcZarukyView.Zarukies
                              Join b As Databaze In dcZarukyView.Databazes On b.Jmeno Equals a.Databaze
                              Where b.Active = True And a.Smazano = False Select a
            Else
                queryZaruky = From a As Zaruky In dcZarukyView.Zarukies Where a.Databaze = JmenoLoad And a.Smazano = False Select a
            End If

            colZaruky = queryZaruky.ToList
            sbPolozekText(colZaruky.Count) 'Počet položek v databázi
        End Using

        proFiltrZaruky()

        Me.Cursor = Cursors.Arrow
    End Sub

    Private Sub proFiltrZaruky()
        If btnFiltr.IsChecked = True Then RecheckFilters()
        colZaruky.ToList.ForEach(Sub(x) x.Smazano = MatchFilter(x))
        Try
            mDataGrid.ItemsSource = colZaruky.Where(Function(x) CBool(x.Smazano) = True).Take(Nastaveni.LimitPolozek).OrderBy(Function(x) x.NewID).ToList
        Catch Err As Exception
            If Err.Message = "Sorting není povoleno během transakce AddNew nebo EditItem." Then
                mDataGrid.ItemsSource = colZaruky.Where(Function(x) CBool(x.Smazano) = True).Take(Nastaveni.LimitPolozek).OrderBy(Function(x) x.NewID).ToList
            Else
                Throw Err
            End If
        End Try
        sbZobrazenoText() 'Počet zobrazených položek

        Celkem.Nakup = 0 : Celkem.Prodej = 0
        For Each Item As Zaruky In mDataGrid.Items
            Celkem.Nakup += CInt(Item.Cena)
            Celkem.Prodej += CInt(Item.CenaOpt)
        Next
        sbCelkemText(Calculate(GetMenuText))
    End Sub

#End Region

#End Region


#Region " DataGrid "

#Region " Load "

    Private Sub proLoadDataGrid()
        Me.Cursor = Cursors.Wait
        DataGridLoading = True

        mDataGrid.ItemsSource = Nothing
        AddDataGridViewColumns()
        PoradiSloupcu.Current = ReorderCoumns()

        proReadyDataGrid()
    End Sub

    Private Sub proReadyDataGrid()
        HeaderNewID() 'aktualizovat sloupeček Zbývá/Prošlé
        sbCelkemText(0) : sbPolozekText(0)
        proLoadZaruky()

        sbDatabazeText(If(JmenoLoad = coAll, JmenoDef, JmenoLoad))
        Dim areItems As Boolean = If(mDataGrid.Items.Count = 0, False, True)
        btnTisk.IsEnabled = areItems : btnHledat.IsEnabled = areItems
        If areItems Then
            DataGridLoading = False
            Dim dr As Zaruky = CType(mDataGrid.ItemsSource, IEnumerable(Of Zaruky)).FirstOrDefault(Function(x) x.ID = If(BackupID = 0, SelectedID, BackupID))
            If dr Is Nothing Then
                mDataGrid.SelectedItem = mDataGrid.Items(0)
            Else
                mDataGrid.SelectedItem = dr
            End If
            If Verze = 1 Then
                mWrap.Children.Clear()
            Else
                timFiltr.Start()
            End If
        Else
            mDataGrid.SetValue(Grid.ColumnSpanProperty, 3)
            mWrap.SetValue(Grid.ColumnSpanProperty, 3)
        End If

        Me.Cursor = Cursors.Arrow
    End Sub

    Private Sub timFiltr_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles timFiltr.Tick
        timFiltr.Stop()
        If PoradiSloupcu.Last <> PoradiSloupcu.Current Then
            PoradiSloupcu.Last = PoradiSloupcu.Current
            LoadFilters()
        Else
            ResizeFilters()
        End If
    End Sub

#End Region

#Region " Selection "

    Private Sub mDataGrid_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles mDataGrid.SelectionChanged
        If DataGridLoading Then Exit Sub

        If mDataGrid.SelectedItem Is Nothing Then
            sbDatabazeText(If(JmenoLoad = coAll, JmenoDef, JmenoLoad))
            mFileBrowser.Slozka = ""
            mDataGrid.SetValue(Grid.ColumnSpanProperty, 3)
            mWrap.SetValue(Grid.ColumnSpanProperty, 3)
        Else
            Dim dr As Zaruky = CType(mDataGrid.SelectedItem, Zaruky)
            SelectedID = dr.ID
            editDatabaze = dr.Databaze
            sbDatabazeText(editDatabaze)
            mFileBrowser.Slozka = myFolder.Join(ATTpath, dr.Databaze, dr.OwnID.ToString)
            If mFileBrowser.FilesCount = 0 Then
                mDataGrid.SetValue(Grid.ColumnSpanProperty, 3)
                mWrap.SetValue(Grid.ColumnSpanProperty, 3)
            Else
                mDataGrid.SetValue(Grid.ColumnSpanProperty, 1)
                mWrap.SetValue(Grid.ColumnSpanProperty, 1)
                mFileBrowser.SelectFirstImage()
            End If
        End If
    End Sub
#End Region

#Region " Add Columns "

    Private Sub HeaderNewID() 'aktualizovat sloupeček Zbývá/Prošlé
        Dim sHeader As String = ""
        Select Case Nastaveni.RaditPodleDoby
            Case 0
                If btnProsle.IsChecked Then
                    sHeader = "Prošlé"
                ElseIf btnNeprosle.IsChecked Then
                    sHeader = "Zbývá"
                Else
                    sHeader = If(Nastaveni.DnuDoKonce > 0, "Zbývá", "Prošlé")
                End If
            Case 1
                sHeader = "Uběhlo"
        End Select
        myDataGrid.ColumnNewID.Header = sHeader
    End Sub

    Private Sub AddDataGridViewColumns()
        mDataGrid.Columns.Clear()
        myDataGrid.AddColumn(mDataGrid, dcZaruky, True, "Oznacil", False, True, "")
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "OwnID", True, True, "Č.")
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "Vec", True, True)
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "Dodavatel", True, True)
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "NewID", True, True)
        HeaderNewID()
        myDataGrid.GetColumnBinding(mDataGrid, "NewID").Converter = New clsDaysConverter
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "Cena", True)
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "CenaOpt", True)
        myDataGrid.AddColumn(mDataGrid, dcZaruky, True, "OwnCheck", True)
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "Faktura", True)
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "SerNum", True)
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "DatPoc", True)
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "DatKon", True)
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "DatOpt", True)
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "Optio1", True)
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "Optio2", True)
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "Optio3", True)
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "Optio4", True)
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "Optio5", True)

        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "ID", True, False)
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "Databaze", True, False)
        myDataGrid.AddColumn(mDataGrid, dcZaruky, False, "Upraveno", True, False)
        myDataGrid.AddColumn(mDataGrid, dcZaruky, True, "Smazano", True, False)
    End Sub

#End Region

#Region " Reoder Columns "

    Private Sub mDataGrid_ColumnDisplayIndexChanged(sender As Object, e As DataGridColumnEventArgs) Handles mDataGrid.ColumnDisplayIndexChanged
        If DataGridLoading Then Exit Sub
        UpdateDatabazePositions()
    End Sub

    Private LastPositions As String

    Private Sub UpdateDatabazePositions()
        If Not JmenoLast = "" Then
            Dim sPositions As String = ""
            For Each oneColumn As DataGridColumn In mDataGrid.Columns
                If oneColumn.Visibility = Windows.Visibility.Visible Then
                    sPositions &= If(oneColumn.DisplayIndex > 9, "", "0") & CStr(oneColumn.DisplayIndex)
                End If
            Next
            If sPositions = LastPositions Then Exit Sub
            LastPositions = sPositions

            Dim query As IQueryable(Of Databaze)
            If JmenoLoad = coAll Then
                query = From a As Databaze In dcZaruky.Databazes Select a
            Else
                query = From a As Databaze In dcZaruky.Databazes Where a.Jmeno = JmenoLast Select a
            End If
            For Each row As Databaze In query
                row.Positions = sPositions
            Next
            dcZaruky.SubmitChanges()
        End If
    End Sub

    Private Function ReorderCoumns() As String
        Dim query As IQueryable(Of Databaze) =
            From a As Databaze In dcZaruky.Databazes
            Where a.Jmeno = If(JmenoLoad = coAll, JmenoDef, JmenoLoad)
            Select a

        Dim dr As Databaze = query.First
        MenaDB = dr.Mena

        If dr.Positions IsNot Nothing Then
            Dim iPos As Integer
            Dim arrRazeni As New ArrayList
            For Each oneColumn As DataGridColumn In mDataGrid.Columns
                If oneColumn.Visibility = Windows.Visibility.Visible Then
                    If dr.Positions.Length / 2 = iPos Then Exit For
                    Dim Index As Integer = mDataGrid.Columns.IndexOf(oneColumn)
                    arrRazeni.Add(dr.Positions.Substring(2 * iPos, 2) & If(Index > 9, "", "0") & CStr(Index))
                    iPos += 1
                End If
            Next
            arrRazeni.Sort()
            For Each oneItem As String In arrRazeni
                Dim iPozice As Integer = CInt(oneItem.Substring(0, 2))
                Dim iIndex As Integer = CInt(oneItem.Substring(2, 2))
                mDataGrid.Columns(iIndex).DisplayIndex = iPozice
            Next
            Return dr.Positions
        End If

        Return ""
    End Function
#End Region

#Region " Filter TextBoxes "

    Private Sub LoadFilters()
        mWrap.Children.Clear()
        For ColumnIndex As Integer = 0 To mDataGrid.Columns.Count - 1
            For Each Col As DataGridColumn In mDataGrid.Columns
                If Col.DisplayIndex = ColumnIndex Then
                    If Col.Visibility = Windows.Visibility.Visible Then
                        If TryCast(Col, DataGridTextColumn) IsNot Nothing Then
                            Dim sPath As String = CType(CType(Col, DataGridBoundColumn).Binding, Binding).Path.Path
                            Dim txtBox As New TextBox
                            txtBox.HorizontalAlignment = Windows.HorizontalAlignment.Left
                            txtBox.VerticalContentAlignment = Windows.VerticalAlignment.Center
                            txtBox.Tag = CType(CType(Col, DataGridBoundColumn).Binding, Binding).Path.Path
                            txtBox.Background = Brushes.FloralWhite   'myColorConverter.StringToBrush("#3FF3DFDF")
                            txtBox.Width = Col.ActualWidth + 1
                            txtBox.Height = 25
                            txtBox.FontSize = Nastaveni.ZoomFontSize
                            Dim sTip As String = " (nic)"
                            Select Case myMaxList.GetTyp(sPath)
                                Case "Int32"
                                    sTip = " <> číslo"
                                Case "Decimal"
                                    sTip = " <> částka"
                                Case "DateTime"
                                    sTip = " <> datum"
                            End Select
                            txtBox.ToolTip = Col.Header.ToString + sTip
                            mWrap.Children.Add(txtBox)
                            AddHandler txtBox.KeyUp, AddressOf WrapBox_KeyUp
                            AddHandler txtBox.MouseDoubleClick, AddressOf WrapTextBox_MouseDoubleClick
                        Else
                            Dim chkBox As New CheckBox
                            chkBox.HorizontalAlignment = Windows.HorizontalAlignment.Left
                            chkBox.VerticalContentAlignment = Windows.VerticalAlignment.Center
                            chkBox.Tag = CType(CType(Col, DataGridBoundColumn).Binding, Binding).Path.Path
                            chkBox.Width = Col.ActualWidth / 2 + 11
                            chkBox.Height = 25
                            chkBox.ToolTip = Col.Header
                            chkBox.IsThreeState = True
                            chkBox.IsChecked = Nothing
                            chkBox.VerticalContentAlignment = Windows.VerticalAlignment.Center
                            chkBox.Margin = New Thickness(Col.ActualWidth / 2 - 10, 0, 0, 0)
                            mWrap.Children.Add(chkBox)
                            AddHandler chkBox.KeyUp, AddressOf WrapBox_KeyUp
                        End If
                    End If
                End If
            Next
        Next
    End Sub

    Private Sub ResizeFilters()
        For Each Col As DataGridColumn In mDataGrid.Columns
            If Col.Visibility = Windows.Visibility.Visible Then
                Dim sPath As String = CType(CType(Col, DataGridBoundColumn).Binding, Binding).Path.Path
                For Each Item As Object In mWrap.Children
                    If TryCast(Item, TextBox) IsNot Nothing Then
                        If sPath = CType(Item, TextBox).Tag.ToString Then
                            CType(Item, TextBox).Width = Col.ActualWidth - 2
                        End If
                    Else
                        If sPath = CType(Item, CheckBox).Tag.ToString Then
                            CType(Item, CheckBox).Width = Col.ActualWidth - 2
                        End If
                    End If
                Next
            End If
        Next
    End Sub

    Private Sub FontResizeFilters()
        For Each Item As Object In mWrap.Children
            Dim txt As TextBox = TryCast(Item, TextBox)
            If txt IsNot Nothing Then txt.FontSize = Nastaveni.ZoomFontSize
        Next
    End Sub

    Private Sub WrapBox_KeyUp(sender As Object, e As KeyEventArgs)
        If e.Key = Key.Enter Then
            btnFiltr.IsChecked = True
            proFiltrZaruky()
        End If
    End Sub

    Private Sub WrapTextBox_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs)
        Dim txt As TextBox = CType(sender, TextBox)
        If txt.Text = "" Or txt.Text = "(nic)" Then
            If myMaxList.GetTyp(txt.Tag.ToString) = "String" Then
                txt.Text = "(nic)"
            Else
                txt.Text = ">"
            End If
        Else
            txt.Text = If(txt.Text.Substring(0, 1) = ">", "<", ">") + If(txt.Text.Substring(0, 1) = "<" Or txt.Text.Substring(0, 1) = ">", txt.Text.Substring(1, txt.Text.Length - 1), txt.Text)
        End If
    End Sub

#End Region

#Region " Resize "

    Private Sub cmiZoom_Click(sender As Object, e As RoutedEventArgs)
        Nastaveni.ZoomFontSize = CInt(CType(sender, MenuItem).Header.ToString.Substring(0, 2))
        DataGridCellResize()
    End Sub

    Private Sub mDataGrid_PreviewMouseWheel(sender As Object, e As MouseWheelEventArgs) Handles mDataGrid.PreviewMouseWheel
        If Keyboard.IsKeyDown(Key.LeftCtrl) Then
            Dim iFontSize As Double = If(e.Delta > 0, -2, 2)
            Dim cellDefault As DataGridCell = myDataGrid.GetCell(mDataGrid, 0, 0)
            If cellDefault IsNot Nothing Then
                iFontSize += cellDefault.FontSize
            Else
                iFontSize += Nastaveni.ZoomFontSize
            End If
            If iFontSize < 10 Then iFontSize = 10
            If iFontSize > 20 Then iFontSize = 20
            If Not Nastaveni.ZoomFontSize = CInt(iFontSize) Then
                Nastaveni.ZoomFontSize = CInt(iFontSize)
                DataGridCellResize()
            End If
        End If
    End Sub

    Private Sub DataGridCellResize()
        FontResizeFilters()
        For Each oneItem As MenuItem In sbZoom.ContextMenu.Items
            oneItem.IsChecked = False
            If CInt(oneItem.Header.ToString.Substring(0, 2)) = Nastaveni.ZoomFontSize Then oneItem.IsChecked = True
        Next
        sbZoom.Text = "Zoom: " + (Nastaveni.ZoomFontSize * 10).ToString + "%"
        mDataGrid.FontSize = Nastaveni.ZoomFontSize
        'Udpate Column Width
        For Each col As DataGridColumn In mDataGrid.Columns
            col.Width = 0
            mDataGrid.UpdateLayout()
            col.Width = New DataGridLength(1, DataGridLengthUnitType.Auto)
        Next
        mFileBrowser.ItemFontSize = Nastaveni.ZoomFontSize

        'For Each row As Integer In Enumerable.Range(0, mDataGrid.Items.Count)
        'Dim rowContainer As DataGridRow = GetRow(row)
        'For Each col As Integer In Enumerable.Range(0, mDataGrid.Columns.Count)
        'Dim cell As DataGridCell = GetCell(row, col)
        '
        'If cell IsNot Nothing Then
        'cell.FontSize = drSet.CellSize
        'cell.Margin = New Thickness(2)
        'If TryCast(cell.Content, CheckBox) IsNot Nothing Then
        'CType(cell.Content, CheckBox).VerticalAlignment = Windows.VerticalAlignment.Center
        'End If
        'End If
        'Next
        'Next
    End Sub

#End Region

#Region " CheckBox "

    Private Sub mDataGrid_BeginningEdit(sender As Object, e As DataGridBeginningEditEventArgs) Handles mDataGrid.BeginningEdit
        CheckBoxTargetUpdated = True
    End Sub

    Private Sub mDataGrid_TargetUpdated(sender As Object, e As DataTransferEventArgs) Handles mDataGrid.TargetUpdated
        Dim chb As CheckBox = TryCast(e.TargetObject, CheckBox)
        If chb IsNot Nothing And CheckBoxTargetUpdated Then
            Dim BindSource As BindingExpression = TryCast(e.TargetObject, CheckBox).GetBindingExpression(CheckBox.IsCheckedProperty)
            Dim Bind As Binding = BindingOperations.GetBinding(chb, CheckBox.IsCheckedProperty)
            Dim dr As Zaruky = CType(BindSource.DataItem, Zaruky)
            'Nelze použít, protože DataContext s DataGrid není propojen: dr.GetType.GetProperty(Bind.Path.Path).SetValue(dr, chb.IsChecked, Nothing)
            Dim query As IQueryable(Of Zaruky) = From a As Zaruky In dcZaruky.Zarukies Where a.ID = dr.ID Select a
            query.First.GetType.GetProperty(Bind.Path.Path).SetValue(query.First, chb.IsChecked, Nothing)
            dcZaruky.SubmitChanges()

            CheckBoxTargetUpdated = False
        End If
    End Sub

#End Region

#End Region



#Region " Filtering "

    Private Sub btnFiltr_Click(sender As Object, e As RoutedEventArgs) Handles btnFiltr.Click
        proReadyDataGrid()
    End Sub

    Private Sub RecheckFilters()
        If btnFiltr.IsChecked Then
            Dim isFilter As Boolean = False
            For Each Item As Object In mWrap.Children
                If TryCast(Item, TextBox) IsNot Nothing Then
                    If Not CType(Item, TextBox).Text = "" Then Exit Sub
                Else
                    If Not CType(Item, CheckBox).IsChecked Is Nothing Then Exit Sub
                End If
            Next
            btnFiltr.IsChecked = False
        End If
    End Sub

    Private Function MatchFilter(ByVal dr As Zaruky) As Boolean
        Select Case Nastaveni.RaditPodleDoby
            Case 0
                dr.NewID = CInt(System.Math.Abs(DateDiff(Microsoft.VisualBasic.DateInterval.Day, Today, CDate(dr.DatKon))) + 1)
            Case 1
                dr.NewID = CInt(System.Math.Abs(DateDiff(Microsoft.VisualBasic.DateInterval.Day, CDate(dr.DatPoc), CDate(If(dr.DatOpt Is Nothing, Today, dr.DatOpt)))) + 1)
        End Select

        MatchFilter = True

        If btnNeprosle.IsChecked Then MatchFilter = CDate(dr.DatKon) >= Today
        If Nastaveni.DnuDoKonce > 0 Then
            If btnUkonce.IsChecked Then MatchFilter = CDate(dr.DatKon) <= Today.AddDays(Nastaveni.DnuDoKonce) And CDate(dr.DatKon) >= Today
        Else
            If btnUkonce.IsChecked Then MatchFilter = CDate(dr.DatKon) < Today And CDate(dr.DatKon) >= Today.AddDays(Nastaveni.DnuDoKonce)
        End If
        If btnProsle.IsChecked Then MatchFilter = CDate(dr.DatKon) < Today

        If MatchFilter And btnFiltr.IsChecked Then

            For Each Item As Object In mWrap.Children
                Dim sPath, sText As String
                Dim bChangeColor As Boolean = False
                Dim bTextBox As Boolean = TryCast(Item, TextBox) IsNot Nothing
                If bTextBox Then
                    sPath = CType(Item, TextBox).Tag.ToString
                    sText = CType(Item, TextBox).Text.ToLower
                    CType(Item, TextBox).Background = Brushes.FloralWhite
                Else
                    sPath = CType(Item, CheckBox).Tag.ToString
                    sText = If(CType(Item, CheckBox).IsChecked Is Nothing, "", "filtrovat")
                End If
                If Not sText = "" Then
                    If dr.GetType.GetProperty(sPath).GetValue(dr, Nothing) Is Nothing Then
                        Select Case sText
                            Case "nic", "(nic)"

                            Case Else
                                MatchFilter = False

                        End Select
                    Else
                        Select Case dr.GetType.GetProperty(sPath).GetValue(dr, Nothing).GetType.ToString
                            Case "System.Boolean"
                                If CType(Item, CheckBox).IsChecked Then
                                    If Not CBool(dr.GetType.GetProperty(sPath).GetValue(dr, Nothing)) = True Then MatchFilter = False
                                Else
                                    If Not CBool(dr.GetType.GetProperty(sPath).GetValue(dr, Nothing)) = False Then MatchFilter = False
                                End If

                            Case "System.Int32", "System.Decimal"
                                Dim Znak As String = sText.Substring(0, 1)
                                Dim Promena As String
                                If Znak = "<" Or Znak = ">" Then
                                    Promena = sText.Substring(1, sText.Length - 1)
                                Else
                                    Znak = "="
                                    Promena = sText
                                End If
                                If IsNumeric(Promena) Then
                                    Select Case Znak
                                        Case "<"
                                            If Not CDec(dr.GetType.GetProperty(sPath).GetValue(dr, Nothing)) < CDec(Promena) Then MatchFilter = False

                                        Case ">"
                                            If Not CDec(dr.GetType.GetProperty(sPath).GetValue(dr, Nothing)) > CDec(Promena) Then MatchFilter = False

                                        Case Else
                                            If Not CDec(dr.GetType.GetProperty(sPath).GetValue(dr, Nothing)) = CDec(Promena) Then MatchFilter = False

                                    End Select
                                Else
                                    bChangeColor = True
                                End If

                            Case "System.DateTime"
                                Dim Znak As String = sText.Substring(0, 1)
                                Dim Promena As String
                                If Znak = "<" Or Znak = ">" Then
                                    Promena = sText.Substring(1, sText.Length - 1)
                                Else
                                    Znak = "="
                                    Promena = sText
                                End If
                                If IsDate(Promena) Then
                                    Select Case Znak
                                        Case "<"
                                            If Not CDate(dr.GetType.GetProperty(sPath).GetValue(dr, Nothing)) < CDate(Promena) Then MatchFilter = False

                                        Case ">"
                                            If Not CDate(dr.GetType.GetProperty(sPath).GetValue(dr, Nothing)) > CDate(Promena) Then MatchFilter = False

                                        Case Else
                                            If Not CDate(dr.GetType.GetProperty(sPath).GetValue(dr, Nothing)) = CDate(Promena) Then MatchFilter = False

                                    End Select
                                Else
                                    bChangeColor = True
                                End If

                            Case "System.String"
                                MatchFilter = dr.GetType.GetProperty(sPath).GetValue(dr, Nothing).ToString.ToLower.Contains(sText)

                        End Select
                    End If
                End If

                If bTextBox Then CType(Item, TextBox).Background = If(bChangeColor, Brushes.Salmon, Brushes.FloralWhite)
                If MatchFilter = False Then Exit For
            Next

        End If
    End Function
#End Region

#Region " Tisk "

    Private PrintLines As Integer = 3

    Private Sub cmiTisk_Click(sender As Object, e As RoutedEventArgs)
        Dim mi As MenuItem = CType(sender, MenuItem)
        PrintLines = CInt(mi.Tag)
        OpenPrintDialog()
    End Sub

    Private Sub cmiTisk2_Click(sender As Object, e As RoutedEventArgs)

    End Sub

    Private Sub cmiTisk3_Click(sender As Object, e As RoutedEventArgs)

    End Sub

    Private Sub btnTisk_Click(sender As Object, e As RoutedEventArgs) Handles btnTisk.Click
        OpenPrintDialog()
    End Sub

    Private Sub OpenPrintDialog()
        Dim pd As New PrintDialog()
        If pd.ShowDialog() = True Then
            Dim Paginator As IDocumentPaginatorSource = CreateFlowDocument(pd)
            pd.PrintDocument(Paginator.DocumentPaginator, "Záruky - " + JmenoLoad)
        End If
    End Sub

    Private Function CreateFlowDocument(ByVal Print As PrintDialog) As FlowDocument
        Dim query As IQueryable(Of Databaze) = From b As Databaze In dcZaruky.Databazes Where b.Jmeno = If(JmenoLoad = coAll, JmenoDef, JmenoLoad) Select b
        Dim drDatabaze As Databaze = query.First
        Dim coZaruky As IEnumerable(Of Zaruky) = CType(mDataGrid.ItemsSource, IEnumerable(Of Zaruky))

        Dim doc = New FlowDocument
        doc.PagePadding = New Thickness(16, 32, 16, 32)
        doc.PageHeight = Print.PrintableAreaHeight '1123 / 16 (6+64) 
        doc.PageWidth = Print.PrintableAreaWidth
        doc.ColumnGap = 0
        doc.ColumnWidth = Print.PrintableAreaWidth
        doc.FontSize = 11
        Dim LinesPerPage As Integer
        Select Case PrintLines
            Case 1
                LinesPerPage = CInt(((Print.PrintableAreaHeight / 16) - 10)) - 4
            Case 2
                LinesPerPage = CInt(((Print.PrintableAreaHeight / 16) - 10) / 3)
            Case 3
                LinesPerPage = CInt(((Print.PrintableAreaHeight / 16) - 10) / 4)
        End Select

        Dim tabMain As New Table()
        doc.Blocks.Add(tabMain)
        tabMain.Columns.Add(NewColumn(60))
        tabMain.Columns.Add(NewColumn(250))
        tabMain.Columns.Add(NewColumn(125))
        tabMain.Columns.Add(NewColumn(60))
        tabMain.Columns.Add(NewColumn(185))
        tabMain.Columns.Add(NewColumn(70))

        Dim grpPolozky As TableRowGroup = Nothing
        Dim row As TableRow = Nothing
        Dim Lines, Pages As Integer
        For Each dr As Zaruky In coZaruky
            If Lines Mod LinesPerPage = 0 Then
                Pages += 1
                tabMain.RowGroups.Add(NewHeaderGroup(drDatabaze, Pages))
                grpPolozky = New TableRowGroup
                tabMain.RowGroups.Add(grpPolozky)
            End If
            Lines += 1
            row = New TableRow
            grpPolozky.Rows.Add(row)
            Dim sp As New StackPanel
            sp.Orientation = Orientation.Horizontal
            Dim chb As New CheckBox
            chb.IsChecked = dr.OwnCheck
            Dim txt As New TextBlock
            txt.Text = " " + dr.OwnID.ToString
            sp.Children.Add(chb)
            sp.Children.Add(txt)
            Dim cel As New TableCell
            cel.Blocks.Add(New BlockUIContainer(sp))
            row.Cells.Add(cel)
            row.Cells.Add(NewCell(myString.Left(dr.Vec, myMaxList.GetLength("Vec", False))))
            row.Cells.Add(NewCell(myString.Left(dr.SerNum, myMaxList.GetLength("SerNum", False))))
            row.Cells.Add(NewCell(CDate(dr.DatKon).ToShortDateString))
            row.Cells.Add(NewCell(myString.Left(dr.Optio3, myMaxList.GetLength("Optio3", False))))
            row.Cells.Add(NewCell(dr.Cena.ToString("N2"), True, True))
            If PrintLines = 2 Or PrintLines = 3 Then
                row = New TableRow
                grpPolozky.Rows.Add(row)
                Dim wDatum As Date = DateSerial(1, 1, CInt(dr.NewID))
                Dim sDays As String = String.Format("d{2},m{1},r{0}", wDatum.Year - 2001, wDatum.Month - 1, wDatum.Day - 1)
                row.Cells.Add(NewCell(sDays))
                row.Cells.Add(NewCell(myString.Left(dr.Dodavatel, myMaxList.GetLength("Dodavatel", False))))
                row.Cells.Add(NewCell(myString.Left(dr.Faktura, myMaxList.GetLength("Faktura", False))))
                row.Cells.Add(NewCell(CDate(dr.DatPoc).ToShortDateString))
                row.Cells.Add(NewCell(myString.Left(dr.Optio4, myMaxList.GetLength("Optio4", False))))
                row.Cells.Add(NewCell(dr.CenaOpt.ToString("N2"), False, True))
            End If
            If PrintLines = 3 Then
                row = New TableRow
                grpPolozky.Rows.Add(row)
                cel = NewCell(myString.Left(dr.Optio1, myMaxList.GetLength("Optio1", False)), False, False, 2)
                cel.TextAlignment = TextAlignment.Center
                row.Cells.Add(cel)
                row.Cells.Add(NewCell(myString.Left(dr.Optio2, myMaxList.GetLength("Optio2", False))))
                If dr.DatOpt Is Nothing Then
                    row.Cells.Add(NewCell(""))
                Else
                    row.Cells.Add(NewCell(CDate(dr.DatOpt).ToShortDateString))
                End If
                row.Cells.Add(NewCell(myString.Left(dr.Optio5, myMaxList.GetLength("Optio5", False))))
                row.Cells.Add(NewCell((dr.CenaOpt - dr.Cena).ToString("N2"), False, True))
            End If
            If PrintLines = 2 Or PrintLines = 3 Then
                row = New TableRow
                grpPolozky.Rows.Add(row)
                row.Cells.Add(NewLine(1, Brushes.Gray))
            End If
        Next
        If PrintLines = 2 Or PrintLines = 3 Then row.Cells.RemoveAt(row.Cells.Count - 1)
        row = New TableRow
        grpPolozky.Rows.Add(row)
        row.Cells.Add(NewLine(2, Brushes.Black))
        row = New TableRow
        grpPolozky.Rows.Add(row)
        row.Cells.Add(NewCell(Celkem.Nakup.ToString("N2"), True, True, 6, 13))
        If PrintLines = 2 Or PrintLines = 3 Then
            row = New TableRow
            grpPolozky.Rows.Add(row)
            row.Cells.Add(NewCell(Celkem.Prodej.ToString("N2"), False, True, 6, 13))
        End If
        If PrintLines = 3 Then
            row = New TableRow
            grpPolozky.Rows.Add(row)
            row.Cells.Add(NewCell((Celkem.Prodej - Celkem.Nakup).ToString("N2"), False, True, 6, 13))
        End If

        Return doc
    End Function

    Private Function NewHeaderGroup(ByVal drDatabaze As Databaze, ByVal Pages As Integer) As TableRowGroup
        Dim grpHead As New TableRowGroup
        Dim row As New TableRow
        grpHead.Rows.Add(row)
        Dim img As New Image
        img.Source = CType(Me.FindResource("imgZaruky"), ImageSource)
        img.Height = 55
        Dim cel As New TableCell
        cel.Blocks.Add(New BlockUIContainer(img))
        cel.RowSpan = 2
        row.Cells.Add(cel)
        row.Cells.Add(NewCell("  " + Application.ProductName, True, False, 1, 22, Nothing, Brushes.DarkRed))
        For a = 1 To 2
            row.Cells.Add(NewCell(""))
        Next
        row.Cells.Add(NewCell("databáze", True, False, 1, 0, New Thickness(0, 12, 0, 0)))
        row.Cells.Add(NewCell("strana", True, False, 1, 0, New Thickness(0, 12, 0, 0)))
        row = New TableRow
        grpHead.Rows.Add(row)
        row.Cells.Add(NewCell("    vb.jantac.net", True, False, 1, 12, Nothing, Brushes.DarkRed))
        For a = 1 To 2
            row.Cells.Add(NewCell(""))
        Next
        cel = NewCell(JmenoLoad, False, False, 1, 14)
        row.Cells.Add(cel)
        cel.BorderThickness = New Thickness(0, 2, 0, 0)
        cel.BorderBrush = Brushes.Black
        cel = NewCell(Pages.ToString, False, False, 1, 14)
        row.Cells.Add(cel)
        cel.BorderThickness = New Thickness(0, 2, 0, 0)
        cel.BorderBrush = Brushes.Black

        row = New TableRow
        grpHead.Rows.Add(row)
        row.Cells.Add(NewCell(myString.FirstWord(drDatabaze.OwnCheck), True))
        Dim Vec As String = ""
        If btnNeprosle.IsChecked Then Vec = "Položky v záruce"
        If btnUkonce.IsChecked Then Vec = "Položky u konce záruky"
        If btnProsle.IsChecked Then Vec = "Položky s prošlou zárukou"
        If btnVsechny.IsChecked Then Vec = "Prošlé neprošlé dohromady"
        row.Cells.Add(NewCell(Vec, True))
        row.Cells.Add(NewCell(drDatabaze.SerNum, True))
        row.Cells.Add(NewCell(myString.FirstWord(drDatabaze.DatKon), True))
        row.Cells.Add(NewCell(drDatabaze.Optio3, True))
        row.Cells.Add(NewCell(myString.FirstWord(drDatabaze.Cena), True, True))
        If PrintLines = 2 Or PrintLines = 3 Then
            row = New TableRow
            grpHead.Rows.Add(row)
            row.Cells.Add(NewCell("Zbývá", True))
            row.Cells.Add(NewCell(drDatabaze.Dodavatel, True))
            row.Cells.Add(NewCell(drDatabaze.Faktura, True))
            row.Cells.Add(NewCell(myString.FirstWord(drDatabaze.DatPoc), True))
            row.Cells.Add(NewCell(drDatabaze.Optio4, True))
            row.Cells.Add(NewCell(myString.FirstWord(drDatabaze.CenaOpt), True, True))
        End If
        If PrintLines = 3 Then
            row = New TableRow
            grpHead.Rows.Add(row)
            row.Cells.Add(NewCell(""))
            row.Cells.Add(NewCell(drDatabaze.Optio1, True, False, 1))
            row.Cells.Add(NewCell(drDatabaze.Optio2, True))
            row.Cells.Add(NewCell(myString.FirstWord(drDatabaze.DatOpt), True))
            row.Cells.Add(NewCell(drDatabaze.Optio5, True))
            row.Cells.Add(NewCell("Rozdíl", True, True))
        End If
        'Čára
        row = New TableRow
        grpHead.Rows.Add(row)
        row.Cells.Add(NewLine(2, Brushes.Black))
        Return grpHead
    End Function

    Private Function NewLine(ByVal Thick As Integer, ByVal BrushColor As Brush) As TableCell
        Dim cel As New TableCell
        cel.BorderThickness = New Thickness(0, Thick, 0, 0)
        cel.BorderBrush = BrushColor
        cel.ColumnSpan = 6
        Return cel
    End Function

    Private Function NewColumn(ByVal iWidth As Integer) As TableColumn
        Dim col As New TableColumn
        col.Width = New GridLength(iWidth)
        Return col
    End Function

    Private Function NewParagraph(ByVal Obsah As String, Margin As Thickness) As Paragraph
        Dim prg As New Paragraph
        prg.Margin = Margin
        prg.Inlines.Add(New Run(Obsah))
        Return prg
    End Function

    Private Function NewCell(ByVal Obsah As String, Optional BoldFont As Boolean = False, Optional RightAlignment As Boolean = False, Optional ByVal Span As Integer = 1, Optional SizeFont As Integer = 0, Optional Margin As Thickness = Nothing, Optional BrushFont As Brush = Nothing) As TableCell
        Dim tc As New TableCell
        tc.FontFamily = New FontFamily("Tahoma")
        If BoldFont Then tc.FontWeight = FontWeights.Bold
        If RightAlignment Then tc.TextAlignment = TextAlignment.Right
        If Not SizeFont = 0 Then tc.FontSize = SizeFont
        If BrushFont IsNot Nothing Then tc.Foreground = BrushFont
        tc.ColumnSpan = Span
        tc.Blocks.Add(NewParagraph(Obsah, Margin))
        Return tc
    End Function
#End Region

#Region " Autostart "

    Private Sub btnHlidat_Click(sender As Object, e As RoutedEventArgs) Handles btnHlidat.Click
        If Application.winStore Then
            Dim msg As String = "Plánovač úloh Windows neumí spouštět aplikace z Windows Store s parametrem." + NR + NR +
                    "Pokud používáte můj program STARTzjs, tak si jednoduše v TAPs nastavte: " + NR +
                    "     Když nastane čas [10:00], tak spustit program [" + Chr(34) + "Zaruky" + Chr(34) + " -win]" + NR + NR +
                    "V nastavený čas pak dojde ke kontrole položek před koncem záruky u aktivních databází." + NR + NR +
                    "Pokud používáte vstupní heslo do databáze a chcete používat hlídání, musíte zvolit při přihlášení pamatovat si heslo, aby hlídání mohlo být prováděno."
            Call New wpfDialog(Me, msg, "Hlídání záruk", wpfDialog.Ikona.ok, "Zavřít").ShowDialog()
            btnHlidat.IsChecked = False
        Else
            If btnHlidat.IsChecked Then
                Dim iResult As Integer = myTask.Create()
                Dim FormDialog As wpfDialog
                If iResult = 0 Then
                    btnHlidat.IsChecked = False
                    FormDialog = New wpfDialog(Me, "Automatické hlídání položek před koncem záruky nemohlo být aktivováno. Selhala jak aktivace Plánovače úloh Windows, tak snaha o zápis do registru.", "Hlídání záruk", wpfDialog.Ikona.chyba, "Zavřít")
                Else
                    FormDialog = New wpfDialog(Me, "Automatické hlídání položek před koncem záruky bylo aktivováno. " + If(iResult = 1, "Plánovač úloh Windows každý den", "Windows po spuštění") + " nechá provést program Záruky kontrolu u aktivních databází. " +
                                                   "Pokud budou položky u konce záruky a nezaškrtnuté v prvním sloupci, zobrazí se okno programu Záruky." + NR + NR +
                                                   "Pokud používáte vstupní heslo do databáze a chcete používat hlídání, musíte zvolit při přihlášení pamatovat si heslo, aby hlídání mohlo být prováděno.", "Hlídání záruk", wpfDialog.Ikona.ok, "Zavřít")
                End If
                FormDialog.ShowDialog()
            Else
                myTask.Delete()
            End If
        End If
    End Sub

#End Region


#Region " Disk Register "

#Region " Check Register Disk "

    Private Sub LoadRegisterAndCheckDisk()
        Dim Subjekty As New clsSubjekty
        If Subjekty.Count = 0 Then Exit Sub

        Dim DBfolder = myFile.Path(SDFpath) 'kontrola zda cesta k databázi je na registrovaném disku
        If Not DBfolder = mySystem.Path.Documents Then 'disk C v Documents je povolen (bez kontroly)
            Dim bFolderAllowed As Boolean
            DBfolder = myFolder.Path(DBfolder)
            Dim myCloud As New clsCloud
            If myCloud.DropBoxExist AndAlso myCloud.DropBoxFolder = DBfolder Then bFolderAllowed = True
            If myCloud.GoogleDriveExist AndAlso myCloud.GoogleDriveFolder = DBfolder Then bFolderAllowed = True
            If myCloud.OneDriveExist AndAlso myCloud.OneDriveFolder = DBfolder Then bFolderAllowed = True
            If myCloud.SyncExist AndAlso myCloud.SyncFolder = DBfolder Then bFolderAllowed = True
            If bFolderAllowed = False Then
                If Subjekty.FindSubject(DBfolder) Then
                    Exit Sub
                Else
                    For Each Drive As clsSystem.clsHarddisk In mySystem.HardDisks 'kontrola výrobních čísel disků
                        If Drive.Letter = DBfolder.Substring(0, 1) Then
                            If Subjekty.FindSubject(Drive.SerialNumber, Drive.Type) Then Exit Sub
                        End If
                    Next
                    SwitchToDocuments()
                End If
            End If
        End If

        Verze = 4 '2
        If Subjekty.FindSubject(mySystem.GetProductID.Replace("-", ""), DiskTypes.System_0) Then Exit Sub
        If Subjekty.FindSubject(mySystem.GetDigitalProductID.Replace("-", ""), DiskTypes.System_0) Then Exit Sub

        For Each Drive As clsSystem.clsHarddisk In mySystem.HardDisks  'kontrola výrobních čísel disků
            If Subjekty.FindSubject(Drive.SerialNumber, Drive.Type) Then Exit Sub
        Next

        Dim DefaultPath As String = If(Nastaveni.CestaDatabaze = "", SDFpath, Nastaveni.CestaDatabaze & "\zaruky.sdf")
        If Subjekty.FindSubject(DefaultPath) = False Then
            For Each oneDrive As String In IO.Directory.GetLogicalDrives
                If Subjekty.FindSubject(oneDrive) Then Exit Sub
            Next
        End If
    End Sub

#End Region

#Region " Switch To Documents "
    'Vypršení zkušební verze - nastavení výchozího adresáře v Documents
    Private Sub SwitchToDocuments()
        If myFile.Exist(SDFpath) Then
            Dim sAddText As String = "Umístění databáze není na registrovaném disku:" + NR + SDFpath + NR + NR + "Databáze bude přesunuta do složky Documents."
            Dim SourcePath As String = ""
            If myFile.Exist(mySystem.Path.Documents & "\zaruky.sdf") Then
                Dim FormDialog = New wpfDialog(Me, sAddText + NR + "Složka Documents již databázi obsahuje." + NR + "Chcete ji nahradit nebo ji načíst?", Application.Title, wpfDialog.Ikona.dotaz, "Nahradit", "Načíst")
                If FormDialog.ShowDialog() Then
                    myFile.Copy(SDFpath, mySystem.Path.Documents & "\zaruky.sdf")
                    SourcePath = myFile.Path(SDFpath)
                Else
                    Nastaveni.Zalohovat = False
                End If
            Else
                Dim FormDialog = New wpfDialog(Me, sAddText, Application.Title, wpfDialog.Ikona.varovani, "Pokračovat")
                FormDialog.ShowDialog()
                myFile.Copy(SDFpath, mySystem.Path.Documents & "\zaruky.sdf")
                SourcePath = myFile.Path(SDFpath)
            End If
            Application.CreatePaths(mySystem.Path.Documents & "\zaruky.sdf")
            If Not SourcePath = "" Then
                myFolder.Delete(mySystem.Path.Documents + "\prilohy zaruk", True)
                myFolder.Copy(SourcePath + "\prilohy zaruk", mySystem.Path.Documents + "\prilohy zaruk", True, sbProgress)
            End If
        Else
            Application.CreatePaths(mySystem.Path.Documents & "\zaruky.sdf")
        End If
    End Sub


#End Region

#End Region

End Class
