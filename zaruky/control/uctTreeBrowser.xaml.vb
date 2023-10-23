Imports System.ComponentModel

Public Class TreeBrowser

#Region " Properties "

    Private imgCesta As String
    Public Property StartUpFolder As String
    Public Property SelectedFolder As clsFolder
    Public Property MainWindow As Window
    Private myCloud As New clsCloud
    Event SelectedItemChanged(ByVal Folder As clsFolder)

    Public Property BackColor As Brush
        Get
            Return mTreeView.Background
        End Get
        Set(value As Brush)
            mTreeView.Background = value
        End Set
    End Property

    Class clsFolder
        Public Property Icon As ImageSource
        Public Property Type As DiskTypes
        Public Property Name As String
        Public Property FullName As String
        Public Property Letter As String
        Public Property SerialNumber As String
        Public NewOne As Boolean
        Public Special As Boolean
        Public Loaded As Boolean
    End Class

    Private Function NewItem(Folder As clsFolder) As TreeViewItem
        Dim img As New Image()
        img.Source = Folder.Icon
        img.Height = If(Folder.Special, 20, 20)
        img.Margin = New Thickness(0, 0, 5, 0)
        Dim txt As New TextBlock
        txt.Text = Folder.Name
        txt.FontSize = If(Folder.Special, 14, 14)
        txt.VerticalAlignment = Windows.VerticalAlignment.Center
        Dim panel As New StackPanel
        panel.Orientation = Orientation.Horizontal
        panel.Margin = New Thickness(2)
        panel.Children.Add(img)
        panel.Children.Add(txt)
        Dim Item As New TreeViewItem
        Item.Header = panel
        Item.Tag = Folder
        Return Item
    End Function

    Private Sub AddImagesToContextMenu() ' Error in xaml
        Dim img As New Image
        img.Source = CType(Me.FindResource("imgPridat"), ImageSource)
        img.Height = 16
        CType(mTreeView.ContextMenu.Items(0), MenuItem).Icon = img
        img = New Image
        img.Source = CType(Me.FindResource("imgPrejmenovat"), ImageSource)
        img.Height = 16
        CType(mTreeView.ContextMenu.Items(1), MenuItem).Icon = img
        img = New Image
        img.Source = CType(Me.FindResource("imgOdebrat"), ImageSource)
        img.Height = 16
        CType(mTreeView.ContextMenu.Items(2), MenuItem).Icon = img
    End Sub

#End Region

    Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        imgCesta = "/Zaruky;component/Images/"
        AddImagesToContextMenu()
        '
        Reload()
    End Sub

    Public Sub Reload()
        mTreeView.Items.Clear()
        LoadDisks()
        LoadUserFolder()
        SelectItemByFullName()
        mTreeView.Focus()
    End Sub

    Private Sub mTreeView_SelectedItemChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Object)) Handles mTreeView.SelectedItemChanged
        Dim Item As TreeViewItem = TryCast(e.NewValue, TreeViewItem)
        If Item IsNot Nothing Then
            SelectedFolder = CType(Item.Tag, clsFolder)
            StartUpFolder = SelectedFolder.FullName
            RaiseEvent SelectedItemChanged(SelectedFolder)
        End If
    End Sub

    Private Sub TreeViewItem_Expanded(sender As Object, e As RoutedEventArgs)
        Dim Item As TreeViewItem = TryCast(e.OriginalSource, TreeViewItem)
        If Item IsNot Nothing Then LoadSubFoldersByParent(Item)
    End Sub

    Private Sub LoadSubFoldersByParent(Item As TreeViewItem)
        For Each SubItem As TreeViewItem In Item.Items
            LoadSubFolders(SubItem)
        Next
    End Sub

#Region " Select Path "

    Private Sub SelectItemByFullName()
        If StartUpFolder = "" Then Exit Sub
        For Each one As TreeViewItem In mTreeView.Items
            If CheckItem(one) Then Exit Sub
        Next
    End Sub

    Private Function CheckItem(Item As TreeViewItem) As Boolean
        For Each one As TreeViewItem In Item.Items
            If CType(one.Tag, clsFolder).FullName = StartUpFolder Then
                one.IsExpanded = True
                one.IsSelected = True
                one.BringIntoView()
                LoadSubFoldersByParent(one)
                Return True
            End If
        Next
        Return False
    End Function

#End Region

#Region " Load UserFolder "

    Private Sub LoadUserFolder()
        Dim Folder As New clsFolder
        Folder.Special = True
        Folder.FullName = ""
        Folder.Name = myFolder.Name(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))
        Folder.Type = DiskTypes.Fixed_3
        Folder.Icon = New BitmapImage(New Uri(imgCesta + "User128.png", UriKind.Relative))
        Dim Item As TreeViewItem = NewItem(Folder)
        mTreeView.Items.Add(Item)

        Folder = New clsFolder
        Folder.Special = True
        Folder.FullName = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        Folder.Icon = GetFolderIcon(Folder.FullName)
        Folder.Name = myFolder.Name(Folder.FullName)
        Folder.Type = DiskTypes.Fixed_3
        Folder.FullName = If(myFolder.DiskType(Folder.FullName) = DiskTypes.Fixed_3, Folder.FullName, "")
        Dim SubItem As TreeViewItem = NewItem(Folder)
        Item.Items.Add(SubItem)

        LoadCloud("DropBox", Item)
        LoadCloud("GoogleDrive", Item)
        LoadCloud("OneDrive", Item)
        LoadCloud("Sync", Item)

        Item.ExpandSubtree()
    End Sub

    Private Sub LoadCloud(Cloud As String, Parent As TreeViewItem)
        Dim Folder As New clsFolder
        Select Case Cloud
            Case "DropBox"
                If myCloud.DropBoxExist = False Then Exit Sub
                Folder.FullName = myCloud.DropBoxFolder
            Case "GoogleDrive"
                If myCloud.GoogleDriveExist = False Then Exit Sub
                Folder.FullName = myCloud.GoogleDriveFolder
            Case "OneDrive"
                If myCloud.OneDriveExist = False Then Exit Sub
                Folder.FullName = myCloud.OneDriveFolder
            Case "Sync"
                If myCloud.SyncExist = False Then Exit Sub
                Folder.FullName = myCloud.SyncFolder
        End Select
        Folder.Special = True
        Folder.Name = myFolder.Name(Cloud)
        Folder.Type = DiskTypes.Fixed_3
        Folder.Icon = GetFolderIcon(Folder.FullName)
        Folder.FullName = If(myFolder.DiskType(Folder.FullName) = DiskTypes.Fixed_3, Folder.FullName, "")
        Dim Item As TreeViewItem = NewItem(Folder)
        Parent.Items.Add(Item)
    End Sub
#End Region

#Region " Load SubFolders "

    Private Function LoadSubFolders(ByVal Parent As TreeViewItem) As Boolean
        Dim Folder As clsFolder = CType(Parent.Tag, clsFolder)
        If Folder.Loaded Or Folder.Special Then Return False
        Folder.Loaded = True
        Try
            Dim Dir As New IO.DirectoryInfo(Folder.FullName)
            If Not Dir.GetDirectories.Length = 0 Then
                For Each oneDir In Dir.GetDirectories.OrderBy(Function(x) x.Name)
                    If myFolder.Exist(oneDir.FullName, False, False) Then
                        Dim newFolder As New clsFolder
                        newFolder.Name = oneDir.Name
                        newFolder.FullName = oneDir.FullName
                        newFolder.Type = Folder.Type
                        newFolder.Icon = GetFolderIcon(oneDir.FullName)
                        Dim Item As TreeViewItem = NewItem(newFolder)
                        Parent.Items.Add(Item)
                    End If
                Next

            End If
        Catch ex As Exception
            Return False
        End Try
        Return True
    End Function

#End Region

#Region " Load Disks "

    Public Sub LoadDisks()
        Dim Subjekty As New clsSubjekty
        For Each Drive As String In System.IO.Directory.GetLogicalDrives
            Dim DiskInfo As New System.IO.DriveInfo(Drive)
            If DiskInfo.IsReady Then
                Dim Disk As New clsFolder
                Disk.Type = CType(DiskInfo.DriveType, DiskTypes)
                Disk.Letter = Drive.Substring(0, 1).ToUpper
                Disk.SerialNumber = myFolder.VolumeSerialNumber(Drive)
                If Disk.SerialNumber.Length = 8 AndAlso Disk.SerialNumber.Substring(0, 5) <> "00000" AndAlso IsRegistered(Subjekty, Disk) Then
                    Disk.Name = DiskInfo.VolumeLabel
                    Disk.FullName = Drive
                    Select Case Disk.Type '0=unknown,1=norootdir/floppy,2=removable,3=fixed,4=remote,5=cdrom,6=ramdisk
                        Case DiskTypes.Floppy_1
                            If Disk.Name = "" Then Disk.Name = "Disketa"
                            Disk.Icon = CType(Me.FindResource("disketa"), ImageSource)
                        Case DiskTypes.Removable_2
                            If Disk.Name = "" Then Disk.Name = "Vyměnitelný disk"
                            Disk.Icon = CType(Me.FindResource("flashdisk"), ImageSource)
                        Case DiskTypes.Fixed_3
                            If Disk.Name = "" Then Disk.Name = "Místní disk"
                            Disk.Icon = CType(Me.FindResource("harddisk"), ImageSource)
                        Case DiskTypes.Server_4
                            If Disk.Name = "" Then Disk.Name = "Vzdálený disk"
                            Disk.Icon = CType(Me.FindResource("server"), ImageSource)
                        Case DiskTypes.Cdrom_5
                            If Disk.Name = "" Then Disk.Name = "Optický disk"
                            Disk.Icon = CType(Me.FindResource("cdrom"), ImageSource)
                    End Select
                    Disk.Name += " (" + Disk.Letter + ":) "
                    Select Case CDbl(DiskInfo.TotalSize)
                        Case Is > 10 ^ 12
                            Disk.Name += (DiskInfo.TotalSize / 1024 / 1024 / 1024 / 1024).ToString("N2") + " TB"
                        Case Is > 10 ^ 9
                            Disk.Name += (DiskInfo.TotalSize / 1024 / 1024 / 1024).ToString("N1") + " GB"
                        Case Is > 10 ^ 6
                            Disk.Name += (DiskInfo.TotalSize / 1024 / 1024).ToString("N0") + " MB"
                    End Select
                    If Disk.Type = DiskTypes.Fixed_3 Or Disk.Type = DiskTypes.Server_4 Or Disk.Type = DiskTypes.Removable_2 Or Disk.Type = DiskTypes.Ramdisk_6 Then
                        Dim Item As TreeViewItem = NewItem(Disk)
                        mTreeView.Items.Add(Item)
                        LoadSubFolders(Item)
                    End If
                End If
            End If
        Next
    End Sub

    Private Function IsRegistered(Subjekty As clsSubjekty, Disk As clsFolder) As Boolean
        If Subjekty.FindSubject(Disk.SerialNumber, Disk.Type) Then Return True
        For Each Drive In mySystem.Harddisks
            If Drive.Letter = Disk.Letter Then
                If Subjekty.FindSubject(Drive.SerialNumber, Drive.Type) Then Return True
            End If
        Next
        Return False
    End Function

#End Region

#Region " Get Icon Folder "

    Public Function GetFolderIcon(FullName As String) As ImageSource
        Select Case FullName
            Case myCloud.DropBoxFolder
                Return CType(Me.FindResource("imgDropBox"), ImageSource)
            Case myCloud.GoogleDriveFolder
                Return CType(Me.FindResource("imgGoogleDrive"), ImageSource)
            Case myCloud.OneDriveFolder
                Return CType(Me.FindResource("imgOneDrive"), ImageSource)
            Case myCloud.SyncFolder
                Return CType(Me.FindResource("imgSync"), ImageSource)
        End Select

        Dim sINIpath As String = myFile.Join(FullName, "desktop.ini")
        If myFile.Exist(sINIpath) Then
            Dim Result As String = myINI.GetSetting(sINIpath, ".ShellClassInfo", "IconResource", "")
            If Result = "" Then Result = myINI.GetSetting(sINIpath, ".ShellClassInfo", "IconFile", "")
            If Not Result = "" Then
                Dim sIcon As String
                Dim iIconIndex As Integer = 0
                If Result.Substring(0, 2) = ".." Then Return New BitmapImage(New Uri(imgCesta + "Folder128.png", UriKind.Relative))
                If Result.Contains(",") Then
                    Dim LastIndex As Integer = Result.LastIndexOf(",")
                    sIcon = Result.Substring(0, LastIndex)
                    iIconIndex = CInt(Result.Substring(LastIndex + 1, Result.Length - LastIndex - 1))
                Else
                    sIcon = Result
                    Result = myINI.GetSetting(sINIpath, ".ShellClassInfo", "IconIndex", "")
                    If IsNumeric(Result) Then iIconIndex = CInt(Result)
                End If
                If sIcon.StartsWith("%SystemRoot%") Then
                    sIcon = sIcon.Replace("%SystemRoot%", Environment.GetFolderPath(Environment.SpecialFolder.Windows))
                End If
                If sIcon.Length > 2 AndAlso Not sIcon.Substring(1, 1) = ":" Then
                    sIcon = myFile.Join(FullName, sIcon)
                End If
                If myFile.Exist(sIcon) Then
                    Dim IconExtractor As New clsExtractIcon(sIcon)
                    IconExtractor.ChangeIndexIconInFile(iIconIndex)
                    Return IconExtractor.GetImageSource
                End If
            End If
        End If
        Return New BitmapImage(New Uri(imgCesta + "Folder128.png", UriKind.Relative))
    End Function

#End Region

#Region " Přidat "
    Private Sub Pridat_click(sender As Object, e As RoutedEventArgs)
        Dim Item As TreeViewItem = TryCast(mTreeView.SelectedItem, TreeViewItem)
        If Item IsNot Nothing Then
            Dim InputBox As New wpfDialog(MainWindow, "Vložte jméno nového adresáře", "Nová složka", wpfDialog.Ikona.tuzka, "OK", "Zrušit", True, False, "", False, 99, True)
            If InputBox.ShowDialog() Then
                Dim Folder As clsFolder = CType(Item.Tag, clsFolder)
                Dim NewFullName As String = myFolder.Join(Folder.FullName, InputBox.Input)
                If myFolder.Exist(NewFullName) Then
                    Item.IsExpanded = True
                Else
                    If myFolder.Create(NewFullName) Then
                        Dim newFolder As New clsFolder
                        newFolder.Name = InputBox.Input
                        newFolder.FullName = NewFullName
                        newFolder.Type = Folder.Type
                        newFolder.Icon = GetFolderIcon(NewFullName)
                        newFolder.NewOne = True
                        Dim addItem As TreeViewItem = NewItem(newFolder)
                        Item.Items.Add(addItem)
                        addItem.IsSelected = True
                        addItem.BringIntoView()
                    End If
                End If
            End If
        End If
    End Sub
#End Region

#Region " Přejmenovat "
    Private Sub Prejmenovat_click(sender As Object, e As RoutedEventArgs)
        Dim Item As TreeViewItem = TryCast(mTreeView.SelectedItem, TreeViewItem)
        If Item IsNot Nothing Then
            Dim Folder As clsFolder = CType(Item.Tag, clsFolder)
            Dim InputBox As New wpfDialog(MainWindow, "Vložte nové jméno adresáře", "Přejmenování složky", wpfDialog.Ikona.tuzka, "OK", "Zrušit", True, False, "", False, 99, True)
            InputBox.Input = Folder.Name
            If InputBox.ShowDialog() Then
                Dim NewFullName As String = myFolder.Join(myFolder.Path(Folder.FullName), InputBox.Input)
                If myFolder.Exist(NewFullName) Then
                    Item.IsExpanded = True
                Else
                    If myFolder.Rename(Folder.FullName, NewFullName) Then
                        Folder.Name = InputBox.Input
                        Folder.FullName = NewFullName
                        Dim panel As StackPanel = CType(Item.Header, StackPanel)
                        Dim txt As TextBlock = CType(panel.Children(1), TextBlock)
                        txt.Text = Folder.Name
                        StartUpFolder = NewFullName
                        SelectedFolder = Folder
                    End If
                End If
            End If
        End If
    End Sub
#End Region

#Region " Odebrat "
    Private Sub Odebrat_click(sender As Object, e As RoutedEventArgs)
        Dim Item As TreeViewItem = TryCast(mTreeView.SelectedItem, TreeViewItem)
        If Item IsNot Nothing Then
            If myFolder.Delete(StartUpFolder, False) Then
                Dim Parent As TreeViewItem = CType(Item.Parent, TreeViewItem)
                If Parent IsNot Nothing Then
                    Parent.IsSelected = True
                    Parent.Items.Remove(Item)
                End If
            End If
        End If
    End Sub

    Private Sub mTreeView_ContextMenuOpening(sender As Object, e As ContextMenuEventArgs) Handles mTreeView.ContextMenuOpening
        mTreeView.ContextMenu.IsEnabled = If(mTreeView.SelectedItem Is Nothing, False, True)
        If mTreeView.SelectedItem IsNot Nothing Then
            Dim Folder As clsFolder = CType(CType(mTreeView.SelectedItem, TreeViewItem).Tag, clsFolder)
            CType(mTreeView.ContextMenu.Items(0), MenuItem).IsEnabled = Not Folder.Special
            CType(mTreeView.ContextMenu.Items(1), MenuItem).IsEnabled = Folder.NewOne
            CType(mTreeView.ContextMenu.Items(2), MenuItem).IsEnabled = Folder.NewOne
        End If
    End Sub
#End Region










End Class
