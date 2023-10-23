Public Class wpfLocation

    Public Property ReloadNeeded() As Boolean
    Private wMain As wpfMain = CType(Application.Current.MainWindow, wpfMain)

#Region " Load "

    Private Sub wpfLocation_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        btnDatabaze.IsEnabled = False : btnZaloha.IsEnabled = False : btnObnovit.IsEnabled = False
        Dim DBpath As String = GetDatabazeSubFolder(Nastaveni.CestaDatabaze)
        txtDatabaze.Text = ShortName(myFolder.Name(DBpath))
        imgDatabaze.Source = mTreeBrowser.GetFolderIcon(DBpath)
        txtObnovit.Text = txtDatabaze.Text
        imgObnovit.Source = imgDatabaze.Source

        If Nastaveni.CestaZaloh = "" Then
            txtZaloha.Text = "nenastavena"
            imgZaloha.Source = Nothing
            ckbZaloha.IsChecked = False
            btnZalohovat.IsEnabled = False
        Else
            Dim ZalohaSubFolder As String = GetZalohaSubFolder(Nastaveni.CestaZaloh)
            txtZaloha.Text = myFolder.Name(ZalohaSubFolder)
            imgZaloha.Source = mTreeBrowser.GetFolderIcon(ZalohaSubFolder)
            mFileBrowser.Slozka = Nastaveni.CestaZaloh
            ckbZaloha.IsChecked = Nastaveni.Zalohovat
            btnZalohovat.IsEnabled = True
        End If

    End Sub

    Private Function GetDatabazeSubFolder(Fullname As String) As String
        If myFile.Name(Fullname).ToLower = "zaruky.sdf" Then Fullname = myFolder.Path(Fullname)
        If myFolder.Name(Fullname).ToLower = "pyramidak" Then Fullname = myFolder.Path(Fullname)
        Return Fullname
    End Function

    Private Function GetZalohaSubFolder(Fullname As String) As String
        Dim sZaloha As String = Nastaveni.CestaZaloh
        Dim a As Integer = sZaloha.LastIndexOf("pyramidak")
        If Not a = -1 Then sZaloha = sZaloha.Substring(0, a - 1)
        If sZaloha.Length = 2 Then sZaloha += "\"
        Return sZaloha
    End Function

#End Region

#Region " Select "

    Private Sub mTreeBrowser_SelectedItemChanged(Folder As TreeBrowser.clsFolder) Handles mTreeBrowser.SelectedItemChanged
        Dim OK As Boolean = Folder IsNot Nothing
        If OK Then OK = If(Folder.FullName = "", False, True)
        Dim DBpath As String = GetDatabazeSubFolder(Nastaveni.CestaDatabaze)
        btnDatabaze.IsEnabled = If(DBpath = Folder.FullName, False, OK)
        btnZaloha.IsEnabled = If(GetZalohaSubFolder(Nastaveni.CestaZaloh) = Folder.FullName, False, OK)
    End Sub

    Private Sub mFileBrowser_SelectedItemChanged(Filename As String) Handles mFileBrowser.SelectedItemChanged
        btnObnovit.IsEnabled = If(Filename = "", False, True)
    End Sub

#End Region

#Region " Button "

    Private Sub btnDatabaze_Click(sender As Object, e As RoutedEventArgs) Handles btnDatabaze.Click
        btnDatabaze.IsEnabled = False : btnObnovit.IsEnabled = False
        Dim Folder As TreeBrowser.clsFolder = mTreeBrowser.SelectedFolder
        Dim NewPath As String = If(Folder.FullName.ToLower.Contains("pyramidak"), Folder.FullName, myFolder.Join(Folder.FullName, "pyramidak"))
        NewPath = If(NewPath.ToLower.Contains("users\pyramidak") And Not NewPath.ToLower.Contains("users\pyramidak\pyramidak"), myFolder.Join(NewPath, "pyramidak"), NewPath)
        Dim NewFullName As String = myFolder.Join(NewPath, "zaruky.sdf")
        Dim DBpath As String = If(myFile.Name(Nastaveni.CestaDatabaze).ToLower = "zaruky.sdf", myFolder.Path(Nastaveni.CestaDatabaze), Nastaveni.CestaDatabaze)
        Dim SourceATT As String = myFolder.Join(DBpath, "prilohy zaruk")
        Dim DestinATT As String = myFolder.Join(NewPath, "prilohy zaruk")
        If myFile.Exist(NewFullName) Then
            Dim wDialog As wpfDialog
            Dim LogFile As New clsLogFile(myFolder.Join(NewPath, "zarukyLog.xml"))
            If LogFile.GetFullAccess(Me) = False Then
                wDialog = New wpfDialog(Me, "V novém umístění již databáze záruk existuje a není přístupná, protože je právě používána.", Me.Title, wpfDialog.Ikona.varovani, "Zavřít")
                wDialog.ShowDialog()
                btnDatabaze.IsEnabled = True : btnObnovit.IsEnabled = True
                Exit Sub
            End If

            wDialog = New wpfDialog(Me, "V novém umístění již databáze záruk existuje." + NR + NR + "Chcete použít nalezenou databázi nebo ji nahradit aktuální databází?", Me.Title, wpfDialog.Ikona.dotaz, "Použít", "Nahradit")
            If wDialog.ShowDialog() = False Then
                If myFile.Copy(SDFpath, NewFullName) Then
                    myFolder.Delete(DestinATT, True, False)
                    myFolder.Copy(SourceATT, DestinATT, True)
                End If
            Else
                btnZalohovat.IsEnabled = True
                If Nastaveni.Zalohovat Then
                    ckbZaloha.IsChecked = False
                    wDialog = New wpfDialog(Me, "Bylo vypnuto automatické zálohování pro případ, že by jste chtěli novou databázi zálohovat jinam.", Me.Title, wpfDialog.Ikona.varovani, "Zavřít")
                    wDialog.ShowDialog()
                End If
            End If
        Else
            If myFile.Copy(SDFpath, NewFullName) Then
                myFolder.Copy(SourceATT, DestinATT, True)
            Else
                btnDatabaze.IsEnabled = True : btnObnovit.IsEnabled = True
                Exit Sub
            End If
        End If
        txtDatabaze.Text = ShortName(Folder.Name)
        imgDatabaze.Source = Folder.Icon
        txtObnovit.Text = txtDatabaze.Text
        imgObnovit.Source = Folder.Icon
        Application.CreatePaths(NewPath)
        ReloadNeeded = True
        mTreeBrowser.lblLoading.Visibility = Windows.Visibility.Hidden
    End Sub

    Private Function ShortName(Name As String) As String
        Dim a As Integer = Name.IndexOf("(")
        If Not a = -1 Then
            If Name.Length > a Then Return Name.Substring(0, a - 1)
        End If
        If Name.Length = 2 Then Name += "\"
        Return Name
    End Function

    Private Sub btnZaloha_Click(sender As Object, e As RoutedEventArgs) Handles btnZaloha.Click
        btnZaloha.IsEnabled = False
        Dim Folder As TreeBrowser.clsFolder = mTreeBrowser.SelectedFolder
        Dim NewPath As String = If(Folder.FullName.ToLower.Contains("pyramidak"), Folder.FullName, myFolder.Join(Folder.FullName, "pyramidak"))
        NewPath = If(NewPath.ToLower.Contains("users\pyramidak") And Not NewPath.ToLower.Contains("users\pyramidak\pyramidak"), myFolder.Join(NewPath, "pyramidak"), NewPath)
        NewPath = If(NewPath.ToLower.Contains("zaloha zaruk"), NewPath, myFolder.Join(NewPath, "zaloha zaruk"))
        txtZaloha.Text = ShortName(Folder.Name)
        imgZaloha.Source = Folder.Icon
        Nastaveni.CestaZaloh = NewPath
        mTreeBrowser.lblLoading.Visibility = Windows.Visibility.Hidden
        mTabControl.SelectedIndex = 1
        mFileBrowser.Slozka = NewPath
        ckbZaloha.IsChecked = True
        btnZalohovat.IsEnabled = True
    End Sub

    Private Sub ckbZaloha_Checked(sender As Object, e As RoutedEventArgs) Handles ckbZaloha.Checked, ckbZaloha.Unchecked
        Nastaveni.Zalohovat = CBool(ckbZaloha.IsChecked)
        If lblZaloha IsNot Nothing Then lblZaloha.Text = "Záloha " + If(Nastaveni.Zalohovat, "zapnuta", "vypnuta")
    End Sub

    Private Sub btnObnovit_Click(sender As Object, e As RoutedEventArgs) Handles btnObnovit.Click
        btnObnovit.IsEnabled = False
        btnZalohovat.IsEnabled = False
        If myFile.Delete(SDFpath, True) Then myFile.Copy(mFileBrowser.SelectedFile, SDFpath)
        If myFolder.Delete(ATTpath, True) Then
            myFolder.Copy(myFolder.Join(Nastaveni.CestaZaloh, myFile.Name(mFileBrowser.SelectedFile, False)), ATTpath, True)
        End If
        ReloadNeeded = True
        Dim FormDialog = New wpfDialog(Me, "Obnovení databáze ze zálohy dokončeno.", Me.Title, wpfDialog.Ikona.ok, "Zavřít")
        FormDialog.ShowDialog()
    End Sub

    Private Sub btnZalohovat_Click(sender As Object, e As RoutedEventArgs) Handles btnZalohovat.Click
        btnZalohovat.IsEnabled = False
        wMain.proBackup(True)
        mFileBrowser.Reload()
    End Sub

#End Region

End Class
