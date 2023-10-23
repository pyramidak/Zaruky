Imports System.Collections.ObjectModel
Imports System.ComponentModel

Partial Public Class FileBrowser

#Region " Properties "

    Public Property SelectedFile As String
    Public Property Filter() As String
    Public Property Pripona As Boolean = True
    Public Property MenuAdd As Boolean = True
    Public Property MenuRename As Boolean = True

    Private sSlozka As String
    Public Property Slozka() As String
        Get
            Return sSlozka
        End Get
        Set(ByVal value As String)
            sSlozka = value
            RaiseEvent FolderChanged(value)
            Reload()
        End Set
    End Property

    Public Property Sloupec() As String
        Get
            Return CType(Me.FindResource("FileGridView"), GridView).Columns(0).Header.ToString
        End Get
        Set(ByVal value As String)
            CType(Me.FindResource("FileGridView"), GridView).Columns(0).Header = " " + value + " "
        End Set
    End Property

    Public Property ItemFontSize() As Integer
        Get
            Return CInt(lvwFiles.FontSize)
        End Get
        Set(ByVal value As Integer)
            lvwFiles.FontSize = value
        End Set
    End Property

    Public Property BackColor() As Brush
        Get
            Return lvwFiles.Background
        End Get
        Set(ByVal value As Brush)
            lvwFiles.Background = value
        End Set
    End Property

    Public ReadOnly Property FilesCount() As Integer
        Get
            Return lvwFiles.Items.Count
        End Get
    End Property

    Private Soubory As New ObservableCollection(Of clsSoubor)
    Public Event SelectedItemChanged(ByVal Filename As String)
    Public Event FolderChanged(ByVal Path As String)

#End Region

#Region " Loading "

    Sub New()
        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        Reload()
    End Sub

    Public Sub Reload()
        Soubory.Clear()
        If myFolder.Exist(sSlozka) Then
            Dim filenames() As String
            If Filter = "" Then
                filenames = System.IO.Directory.GetFiles(sSlozka)
            Else
                filenames = System.IO.Directory.GetFiles(sSlozka, Filter)
            End If
            For Each oneFile As String In filenames
                If CheckFile(New System.IO.FileInfo(oneFile)) Then Soubory.Add(New clsSoubor(oneFile, Pripona))
            Next
        End If
        lvwFiles.ItemsSource = Soubory
        lvwFiles.ContextMenu = CType(Me.FindResource("myMenu"), ContextMenu)
        CType(lvwFiles.ContextMenu.Items(0), MenuItem).Visibility = If(MenuAdd, Visibility.Visible, Visibility.Collapsed)
        CType(lvwFiles.ContextMenu.Items(1), MenuItem).Visibility = If(MenuRename, Visibility.Visible, Visibility.Collapsed)
    End Sub

    Private Function CheckFile(ByRef File As IO.FileInfo) As Boolean
        If File.Exists = False Then Return False
        If (File.Attributes And IO.FileAttributes.System) = IO.FileAttributes.System Then Return False
        If (File.Attributes And IO.FileAttributes.Hidden) = IO.FileAttributes.Hidden Then Return False
        If File.Length > 4 * 10 ^ 8 Then Return False
        If File.Name = "Thumbs.db" Then Return False
        Return True
    End Function

    Private Class clsSoubor
        Implements INotifyPropertyChanged

        Public Event PropertyChanged(sender As Object, e As PropertyChangedEventArgs) Implements INotifyPropertyChanged.PropertyChanged
        Protected Sub OnPropertyChanged(ByVal name As String)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(name))
        End Sub

#Region " Properties "
        Public Property Ikona() As ImageSource
            Get
                Return imgIkona
            End Get
            Set(ByVal value As ImageSource)
                imgIkona = value
                OnPropertyChanged("Ikona")
            End Set
        End Property

        Public Property Jmeno() As String
            Get
                Return sJmeno
            End Get
            Set(ByVal value As String)
                sJmeno = value
                OnPropertyChanged("Jmeno")
            End Set
        End Property

        Public Property Cesta() As String
            Get
                Return sCesta
            End Get
            Set(ByVal value As String)
                sCesta = value
                OnPropertyChanged("Cesta")
            End Set
        End Property
#End Region

        Private imgIkona As ImageSource
        Private sCesta As String
        Private sJmeno As String

        Sub New(ByVal FileName As String, Pripona As Boolean)
            sCesta = FileName
            sJmeno = myFile.Name(FileName, Pripona)
            Dim myIcon As New clsExtractIcon(FileName)
            imgIkona = myIcon.GetImageSource()
            myIcon.Dispose()
        End Sub
    End Class

#End Region

#Region " Selection "

    Public Sub SelectFirstImage()
        For Each oneFile As clsSoubor In lvwFiles.Items
            If oneFile.Cesta.ToLower.EndsWith("jpg") Or oneFile.Cesta.ToLower.EndsWith("jpeg") Or oneFile.Cesta.ToLower.EndsWith("png") Or oneFile.Cesta.ToLower.EndsWith("bmp") Then
                lvwFiles.SelectedItem = oneFile
                Exit For
            End If
        Next
    End Sub

    Public Sub SelectItem(Index As Integer)
        lvwFiles.SelectedIndex = Index
    End Sub

    Public Sub LaunchFiles()
        For Each oneFile As clsSoubor In lvwFiles.SelectedItems
            myFile.Launch(Nothing, oneFile.Cesta)
        Next
    End Sub

    Private Sub lvwFiles_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs) Handles lvwFiles.MouseDoubleClick
        LaunchFiles()
    End Sub

    Private Sub lvwFiles_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles lvwFiles.SelectionChanged
        If lvwFiles.SelectedItem Is Nothing Then
            SelectedFile = ""
            RaiseEvent SelectedItemChanged("")
        Else
            SelectedFile = CType(lvwFiles.SelectedItem, clsSoubor).Cesta
            RaiseEvent SelectedItemChanged(SelectedFile)
        End If
    End Sub

#End Region

#Region " Sorting "

    Private _lastHeaderClicked As GridViewColumnHeader = Nothing
    Private _lastDirection As ListSortDirection = ListSortDirection.Ascending

    Private Sub GridViewColumnHeaderClickedHandler(ByVal sender As Object, ByVal e As RoutedEventArgs)
        Dim headerClicked As GridViewColumnHeader = TryCast(e.OriginalSource, GridViewColumnHeader)
        Dim direction As ListSortDirection

        If headerClicked IsNot Nothing Then
            If headerClicked.Role <> GridViewColumnHeaderRole.Padding Then
                If headerClicked IsNot _lastHeaderClicked Then
                    direction = ListSortDirection.Ascending
                Else
                    If _lastDirection = ListSortDirection.Ascending Then
                        direction = ListSortDirection.Descending
                    Else
                        direction = ListSortDirection.Ascending
                    End If
                End If

                Dim sData As String = ""
                Select Case headerClicked.Content.ToString
                    Case " Příloha "
                        sData = "Jmeno"
                End Select
                Sort(sData, direction)

                If direction = ListSortDirection.Ascending Then
                    headerClicked.Column.HeaderTemplate = TryCast(lvwFiles.Resources("HeaderTemplateArrowUp"), DataTemplate)
                Else
                    headerClicked.Column.HeaderTemplate = TryCast(lvwFiles.Resources("HeaderTemplateArrowDown"), DataTemplate)
                End If

                ' Remove arrow from previously sorted header
                If _lastHeaderClicked IsNot Nothing AndAlso _lastHeaderClicked IsNot headerClicked Then
                    _lastHeaderClicked.Column.HeaderTemplate = Nothing
                End If

                _lastHeaderClicked = headerClicked
                _lastDirection = direction
            End If
        End If
    End Sub

    Private Sub Sort(ByVal sortBy As String, ByVal direction As ListSortDirection)
        Dim dataView As ICollectionView = CollectionViewSource.GetDefaultView(lvwFiles.ItemsSource)

        dataView.SortDescriptions.Clear()
        Dim sd As New SortDescription("Jmeno", direction)
        dataView.SortDescriptions.Add(sd)
        dataView.Refresh()
    End Sub


#End Region

#Region " Managing "

    Private Sub lvwFiles_KeyUp(sender As Object, e As KeyEventArgs) Handles lvwFiles.KeyUp
        If e.Key = Key.Delete Then
            Dim FormDialog = New wpfDialog(Nothing, "Chcete opravdu smazat vybrané přílohy?", "Mazání příloh", wpfDialog.Ikona.dotaz, "Ano", "Ne")
            If FormDialog.ShowDialog() Then Delete()
        End If
    End Sub

    Private Sub Delete()
        For Each Soubor As clsSoubor In lvwFiles.SelectedItems
            If myFile.Delete(Soubor.Cesta, True) Then
                myFolder.Delete(myFolder.Join(myFile.Path(Soubor.Cesta), myFile.Name(Soubor.Cesta, False)), True)
                Soubor.Cesta = ""
            End If        
        Next
        Soubory.Where(Function(a) a.Cesta = "").ToList.ForEach(Function(b) Soubory.Remove(b))
    End Sub

    Public Sub DeleteAllFiles()
        For Each Soubor As clsSoubor In lvwFiles.Items
            myFile.Delete(Soubor.Cesta, True)
            Soubor.Cesta = ""
        Next
        Soubory.Where(Function(a) a.Cesta = "").ToList.ForEach(Function(b) Soubory.Remove(b))
    End Sub

    Private Sub lvwFiles_ContextMenuOpening(sender As Object, e As ContextMenuEventArgs) Handles lvwFiles.ContextMenuOpening
        Dim bActive As Boolean = If(lvwFiles.SelectedItems.Count = 0, False, True)
        Dim FileMenu As ContextMenu = CType(Me.FindResource("myMenu"), ContextMenu)
        CType(FileMenu.Items(1), MenuItem).IsEnabled = bActive
        CType(FileMenu.Items(2), MenuItem).IsEnabled = bActive
    End Sub

    Private Sub Pridat_click(sender As Object, e As RoutedEventArgs)
        Dim dlg As New Microsoft.Win32.OpenFileDialog()
        dlg.Title = "Vyberte přílohy"
        dlg.Filter = "Všechny soubory (*.*)|*.*|Portable Document Format (*.PDF)|*.pdf|Joint Photographic Experts Group (*.JPEG)|*.jpg;*.jpeg"
        dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        dlg.Multiselect = True
        dlg.CheckFileExists = True
        If dlg.ShowDialog = False Then Exit Sub
        For Each oneFile In dlg.FileNames
            Pridat(oneFile)
        Next
    End Sub

    Private Sub Pridat(Filename As String)
        Dim newFile As String = myFile.Join(sSlozka, myFile.Name(Filename))
        If myFile.Copy(Filename, newFile) Then Soubory.Add(New clsSoubor(newFile, Pripona))
    End Sub

    Private Sub Prejmenovat_click(sender As Object, e As RoutedEventArgs)
        Dim Soubor As clsSoubor = CType(lvwFiles.SelectedItem, clsSoubor)
        Dim Vzkaz As String = ""
        Dim sNew As String = myFile.Name(Soubor.Jmeno, False)
        Do
            Dim FormDialog = New wpfDialog(Nothing, "Vložte nový název přílohy bez přípony:" + NR + NR + Vzkaz, "Název přílohy", wpfDialog.Ikona.tuzka, "OK", "Storno", True)
            FormDialog.Input = sNew
            If FormDialog.ShowDialog() = False Then Exit Sub
            sNew = FormDialog.Input
            If myFile.isNameSafe(sNew) Then
                Exit Do
            Else
                Vzkaz = "! Nepovolené znaky v názvu !"
            End If
        Loop
        If myFile.Move(Soubor.Cesta, myFile.Path(Soubor.Cesta) + "\" + sNew + myFile.Extension(Soubor.Jmeno)) Then
            Soubor.Jmeno = sNew + myFile.Extension(Soubor.Jmeno)
            Soubor.Cesta = myFile.Path(Soubor.Cesta) + "\" + Soubor.Jmeno
        End If
    End Sub

    Private Sub Odebrat_click(sender As Object, e As RoutedEventArgs)
        Delete()
    End Sub

#End Region

#Region " Drag and Drop "

    Private Sub lvwFiles_DragEnter(sender As Object, e As DragEventArgs) Handles lvwFiles.DragEnter
        If e.Data.GetDataPresent("FileDrop") Then
            e.Effects = DragDropEffects.Copy
        Else
            e.Effects = DragDropEffects.None
        End If
    End Sub

    Private Sub lvwFiles_Drop(sender As Object, e As DragEventArgs) Handles lvwFiles.Drop
        If e.Data.GetDataPresent("FileDrop") Then
            For Each oneFile As String In CType(e.Data.GetData(DataFormats.FileDrop), String())
                Pridat(oneFile)
            Next
        End If
    End Sub

#End Region

End Class


