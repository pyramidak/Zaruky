Imports Microsoft.Win32.TaskScheduler

#Region " TaskScheduler "

Class clsTaskScheduler

    Public Name, Description, File, Arguments, Directory As String
    Public Cas As Date

    Sub New()
        Name = "pyramidak " & Application.ExeName
        Description = "Každodenní automatické spouštění pyramidak " & Application.ProductName & " podle nastavení."
        File = Chr(34) & System.Reflection.Assembly.GetExecutingAssembly().Location & Chr(34)
        Arguments = "-win"
        Cas = Today.AddHours(9).AddMinutes(0)
    End Sub

    Public Function Create() As Integer
        Try
            ' Get the service on the local machine
            Using ts As New TaskService()
                ' Create a new task definition and assign properties
                Dim td As TaskDefinition = ts.NewTask()
                td.RegistrationInfo.Description = Description
                td.Settings.StartWhenAvailable = True

                Dim dt As New DailyTrigger()
                dt.DaysInterval = 1
                dt.StartBoundary = Cas
                'dt.Repetition.Duration = TimeSpan.FromDays(30) 'po dobu
                'dt.Repetition.Interval = TimeSpan.FromMinutes(60) 'každých 
                td.Triggers.Add(dt)
                'Dim lt As New LogonTrigger
                'lt.Delay = TimeSpan.FromMinutes(3)
                'lt.UserId = mySystem.User
                'td.Triggers.Add(lt)

                ' Create an action that will launch Notepad whenever the trigger fires
                td.Actions.Add(New ExecAction(File, Arguments, Directory))

                ' Register the task in the root folder
                ts.RootFolder.RegisterTaskDefinition(Name, td)
            End Using
            Return 1
        Catch ex As Exception
            Return CreateAutostart()
        End Try
    End Function

    Public Sub Delete()
        Try
            Using ts As New TaskService()
                ' Remove the task we just created
                ts.RootFolder.DeleteTask(Name)
            End Using
        Catch ex As Exception
            DeleteAutostart()
        End Try
    End Sub

    Public Function Exist() As Boolean
        Try
            Using ts As New TaskService()
                'Get an instance of an existing task
                Dim myTask As Task = ts.GetTask(Name)
                If myTask Is Nothing Then Return False

                ' Check to ensure you have a trigger and it is the one want
                If myTask.Definition.Triggers.Count > 0 Then
                    Cas = myTask.Definition.Triggers(0).StartBoundary
                    Return True
                Else
                    Return False
                End If
            End Using
        Catch ex As Exception
            Return ExistAutostart()
        End Try
    End Function

    Private appKey As String = "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"

    Private Function CreateAutostart() As Integer
        Try
            myRegister.CreateValue(HKEY.CURRENT_USER, appKey, Name, Chr(34) & File & Chr(34) & " -win")
            Return 2
        Catch Ex As Exception
            Return 0
        End Try
    End Function

    Private Sub DeleteAutostart()
        Try
            myRegister.DeleteValue(HKEY.CURRENT_USER, appKey, Name)
        Catch Ex As Exception
        End Try
    End Sub

    Private Function ExistAutostart() As Boolean
        Try
            Return If(myRegister.GetValue(HKEY.CURRENT_USER, appKey, Name, "").ToLower = Chr(34) & File & Chr(34) & " -win", True, False)
        Catch Ex As Exception
            Return False
        End Try
    End Function

End Class

#End Region

#Region " Rename Attachment´s Folders "

Class clsAttachment

    Sub New()
    End Sub

    Public Sub UpdateFolders(ByRef Context As zarukyContext, ByRef Binding As BindingListCollectionView, ByVal sDatabaze As String)
        Dim query As IEnumerable(Of Zaruky) = CType(Binding.SourceCollection, IEnumerable(Of Zaruky))
        For Each oRow As Zaruky In query
            If Not Context.GetChangeSet.Deletes.Contains(oRow) Then
                If oRow.Databaze <> sDatabaze Then
                    Dim oldPath As String = myFolder.Join(ATTpath, sDatabaze, oRow.OwnID.ToString)
                    Dim newPath As String = myFolder.Join(ATTpath, oRow.Databaze, oRow.NewID.ToString)
                    If myFolder.Rename(oldPath, newPath, True) Then
                        oRow.OwnID = oRow.NewID
                    Else
                        oRow.Databaze = sDatabaze
                    End If
                End If
            End If
        Next

        For Each oRow As Zaruky In query
            If Not Context.GetChangeSet.Deletes.Contains(oRow) Then
                If oRow.OwnID <> oRow.NewID Then
                    Dim oldPath As String = myFolder.Join(ATTpath, oRow.Databaze, oRow.OwnID.ToString)
                    Dim newPath As String = myFolder.Join(ATTpath, oRow.Databaze, oRow.NewID.ToString)
                    If doIhaveToRenameAll(Context, query, oRow) Then
                        proRenameAllFolders(Context, query)
                        Exit For
                    Else
                        myFolder.Rename(oldPath, newPath)
                    End If
                    oRow.OwnID = oRow.NewID
                End If
            End If
        Next
    End Sub

    Private Function doIhaveToRenameAll(ByRef Context As zarukyContext, ByVal query As IEnumerable(Of Zaruky), ByVal drZaruky As Zaruky) As Boolean
        For Each oRow As Zaruky In query
            If Not Context.GetChangeSet.Deletes.Contains(oRow) Then
                If oRow.OwnID = drZaruky.NewID Then Return True
            End If
        Next
        Return False
    End Function

    Private Sub proRenameAllFolders(ByRef Context As zarukyContext, ByVal query As IEnumerable(Of Zaruky))
        For Each oRow As Zaruky In query
            If Not Context.GetChangeSet.Deletes.Contains(oRow) Then
                Dim oldPath As String = myFolder.Join(ATTpath, oRow.Databaze, oRow.OwnID.ToString)
                myFolder.Rename(oldPath, oldPath & "bak")
            End If
        Next
        For Each oRow As Zaruky In query
            If Not Context.GetChangeSet.Deletes.Contains(oRow) Then
                Dim oldPath As String = myFolder.Join(ATTpath, oRow.Databaze, oRow.OwnID.ToString)
                Dim newPath As String = myFolder.Join(ATTpath, oRow.Databaze, oRow.NewID.ToString)
                If myFolder.Rename(oldPath & "bak", newPath, True) = False Then
                    myFolder.Rename(oldPath & "bak", oldPath)
                End If
                oRow.OwnID = oRow.NewID
            End If
        Next
    End Sub

End Class
#End Region



#Region " SQL "

Public NotInheritable Class mySQL

#Region " Get Version "

    Public Shared Function EnsureVersion40(filename As String) As Boolean
        Dim fileversion As SQLCE = DetermineVersion(filename)

        Select Case fileversion
            Case SQLCE.V20
                Dim FormDialog = New wpfDialog(Nothing, "Databáze je příliš zastaralá." + NR + "Nelze ji aktualizovat.", filename, wpfDialog.Ikona.varovani, "Zavřít")
                FormDialog.ShowDialog()
                Return False
            Case SQLCE.V45
                Dim FormDialog = New wpfDialog(Nothing, "Databáze je novější verze než SQL server použitý v aplikaci." + NR + "Nelze se připojit.", filename, wpfDialog.Ikona.varovani, "Zavřít")
                FormDialog.ShowDialog()
                Return False
            Case SQLCE.V40
                Return True

            Case SQLCE.V30, SQLCE.V35
                '"Aktualizovat"
                '"Databáze bude během připojení aktualizována (v3.5 na v4.0)."
                Return True
            Case Else
                Dim FormDialog = New wpfDialog(Nothing, "Verze databáze nebyla identifikována." + NR + "Nelze ji načíst.", filename, wpfDialog.Ikona.varovani, "Zavřít")
                FormDialog.ShowDialog()
                Return False
        End Select
    End Function

    Private Enum SQLCE
        V20 = 0
        V30 = 1
        V35 = 2
        V40 = 3
        V45 = 4 'new now unknown version
    End Enum

    Private Shared Function DetermineVersion(filename As String) As SQLCE
        Dim versionDictionary = New Dictionary(Of Integer, SQLCE)() From {
            {&H73616261, SQLCE.V20},
            {&H2DD714, SQLCE.V30},
            {&H357B9D, SQLCE.V35},
            {&H3D0900, SQLCE.V40}
        }
        Dim versionLONGWORD As Integer = 0
        Try
            Using fs = New IO.FileStream(filename, IO.FileMode.Open)
                fs.Seek(16, IO.SeekOrigin.Begin)
                Using reader As New IO.BinaryReader(fs)
                    versionLONGWORD = reader.ReadInt32()
                End Using
            End Using
        Catch
            Throw
        End Try
        If versionDictionary.ContainsKey(versionLONGWORD) Then
            Return versionDictionary(versionLONGWORD)
        Else
            Return SQLCE.V45
        End If
    End Function

#End Region

    Public Shared Function CreateSDFConnString(dbPath As String, Optional dbPass As String = "") As String
        If dbPath = "" Then dbPath = SDFpath '"encryption mode=platform default;" použít jen při vytváření nové databáze, aby byla šifrována
        Return String.Format("Autoshrink Threshold=80; DataSource=""{0}""; Password=""{1}""", dbPath, PasswordSDF & dbPass)
    End Function

#Region " Get Free ID "

    Public Shared Function GetFreeOwnID(Context As zarukyContext, Optional sDatabaze As String = "", Optional iNewID As Integer = 0) As Integer
        Dim FreeID As Integer
        Dim Obsazene As Boolean
        Dim query As IQueryable(Of Zaruky)
        If Not iNewID = 0 Then
            For Each one As Object In Context.GetChangeSet.Inserts
                If one.GetType Is GetType(Zaruky) Then
                    Dim dr As Zaruky = CType(one, Zaruky)
                    If Not sDatabaze = "" Then
                        If dr.Databaze = sDatabaze And dr.NewID = iNewID Then Obsazene = True
                    Else
                        If dr.NewID = iNewID Then Obsazene = True
                    End If
                End If
            Next
            If Not sDatabaze = "" Then
                query = From a As Zaruky In Context.Zarukies Where a.Databaze = sDatabaze And a.NewID = iNewID Select a
            Else
                query = From a As Zaruky In Context.Zarukies Where a.NewID = iNewID Select a
            End If
            If Not query.Count = 0 Then Obsazene = True
            If Obsazene = False Then Return iNewID
        End If

        For Each one As Object In Context.GetChangeSet.Inserts
            If one.GetType Is GetType(Zaruky) Then
                Dim dr As Zaruky = CType(one, Zaruky)
                If Not sDatabaze = "" Then 'Kontrola v rámci jedné databáze 
                    If dr.Databaze = sDatabaze And dr.NewID > FreeID Then FreeID = CInt(dr.NewID)
                Else
                    If dr.NewID > FreeID Then FreeID = CInt(dr.NewID)
                End If
            End If
        Next

        If Not sDatabaze = "" Then
            query = From a As Zaruky In Context.Zarukies Where a.Databaze = sDatabaze Select a
        Else
            query = From a As Zaruky In Context.Zarukies Select a
        End If
        For Each one As Zaruky In query
            If one.NewID > FreeID Then FreeID = CInt(one.NewID)
        Next
        Return FreeID + 1
    End Function

#End Region

End Class

#End Region

#Region " MaxLength of ComboBox "

Class EditableComboBox
    Public Shared ReadOnly MaxLengthProperty As DependencyProperty = DependencyProperty.RegisterAttached("MaxLength", GetType(Integer), GetType(EditableComboBox), New PropertyMetadata(AddressOf OnMaxLengthChanged))

    Public Shared Function GetMaxLength(ByVal obj As DependencyObject) As Integer
        Return CInt(obj.GetValue(MaxLengthProperty))
    End Function

    Public Shared Sub SetMaxLength(ByVal obj As DependencyObject, ByVal value As Integer)
        obj.SetValue(MaxLengthProperty, value)
    End Sub

    Private Shared Sub OnMaxLengthChanged(ByVal obj As DependencyObject, ByVal e As DependencyPropertyChangedEventArgs)
        Dim CombBox = TryCast(obj, ComboBox)
        If CombBox Is Nothing Then
            Exit Sub
        End If
        CombBox.Dispatcher.BeginInvoke(Sub() FindChild(CombBox, e))
    End Sub

    Private Shared Sub FindChild(ByVal CombBox As ComboBox, ByVal e As DependencyPropertyChangedEventArgs)
        Dim rootElement = TryCast(VisualTreeHelper.GetChild(CombBox, 0), FrameworkElement)
        Dim textBox As TextBox = DirectCast(rootElement.FindName("PART_EditableTextBox"), TextBox)
        If textBox IsNot Nothing Then
            textBox.SetValue(textBox.MaxLengthProperty, e.NewValue)
        End If
    End Sub

    Public Shared Sub SetTextDecorations(ByVal CombBox As ComboBox, ByVal Decoration As TextDecorationCollection)
        Dim rootElement = TryCast(VisualTreeHelper.GetChild(CombBox, 0), FrameworkElement)
        Dim textBox As TextBox = DirectCast(rootElement.FindName("PART_EditableTextBox"), TextBox)
        If textBox IsNot Nothing Then
            textBox.TextDecorations = Decoration
        End If
    End Sub
End Class

#End Region

#Region " DataGrid "

Class myDataGrid

    Public Shared ColumnNewID As DataGridTextColumn

#Region " Get Cell "

    Public Shared Function GetCell(DG As DataGrid, row As Integer, column As Integer) As DataGridCell
        Dim rowContainer As DataGridRow = GetRow(DG, row)

        If rowContainer IsNot Nothing Then
            Dim presenter As Primitives.DataGridCellsPresenter = GetVisualChild(Of Primitives.DataGridCellsPresenter)(rowContainer)
            If presenter IsNot Nothing Then
                Dim cell As DataGridCell = DirectCast(presenter.ItemContainerGenerator.ContainerFromIndex(column), DataGridCell)
                If cell Is Nothing Then
                    DG.ScrollIntoView(rowContainer, DG.Columns(column))
                    cell = DirectCast(presenter.ItemContainerGenerator.ContainerFromIndex(column), DataGridCell)
                End If
                Return cell
            End If
        End If
        Return Nothing
    End Function

    Public Shared Function GetRow(DG As DataGrid, index As Integer) As DataGridRow
        Dim row As DataGridRow = DirectCast(DG.ItemContainerGenerator.ContainerFromIndex(index), DataGridRow)
        If row Is Nothing And DG.Items.Count > 0 Then
            DG.UpdateLayout()
            DG.ScrollIntoView(DG.Items(index))
            row = DirectCast(DG.ItemContainerGenerator.ContainerFromIndex(index), DataGridRow)
        End If
        Return row
    End Function

    Public Shared Function GetVisualChild(Of T As Visual)(parent As Visual) As T
        Dim child As T = Nothing
        Dim numVisuals As Integer = VisualTreeHelper.GetChildrenCount(parent)
        For i As Integer = 0 To numVisuals - 1
            Dim v As Visual = DirectCast(VisualTreeHelper.GetChild(parent, i), Visual)
            child = TryCast(v, T)
            If child Is Nothing Then
                child = GetVisualChild(Of T)(v)
            End If
            If child IsNot Nothing Then
                Exit For
            End If
        Next
        Return child
    End Function

#End Region

#Region " Get Binding "

    Public Shared Function GetColumnBinding(DG As DataGrid, sName As String) As Binding
        For Each oneColumn In DG.Columns
            Dim Bind As Binding = CType(CType(oneColumn, DataGridBoundColumn).Binding, Binding)
            If Bind.Path.Path = sName Then Return Bind
        Next
        Return Nothing
    End Function

    Public Shared Function GetColumnBinding(col As DataGridColumn) As Binding
        Return CType(CType(col, DataGridBoundColumn).Binding, Binding)
    End Function

#End Region

#Region " Add Column  "

    Public Shared Sub AddColumn(DG As DataGrid, Context As zarukyContext, bCheckedBox As Boolean, sPath As String, bReadOnly As Boolean, Optional bVisible As Boolean = True, Optional sHeader As String = "#")
        Dim bOptioVisible As Boolean = True
        If sHeader = "#" Then
            If Not Verze = 1 Then
                Dim query As IQueryable(Of Databaze) = Nothing
                Dim columnNames = From a In Context.Mapping.MappingSource.GetModel(GetType(System.Data.Linq.DataContext)).GetMetaType(GetType(Databaze)).DataMembers Where a.Name = sPath Select a
                If Not columnNames.Count = 0 Then
                    query = From a As Databaze In Context.Databazes Where a.Jmeno = If(JmenoLoad = coAll, JmenoDef, JmenoLoad) Select a
                    If query.First.GetType.GetProperty(sPath).GetValue(query.First, Nothing) IsNot Nothing Then
                        sHeader = myString.FirstWord(query.First.GetType.GetProperty(sPath).GetValue(query.First, Nothing).ToString)
                    End If
                End If
                columnNames = From a In Context.Mapping.MappingSource.GetModel(GetType(System.Data.Linq.DataContext)).GetMetaType(GetType(Databaze)).DataMembers Where a.Name = sPath + "b" Select a
                If Not columnNames.Count = 0 Then
                    bOptioVisible = CBool(query.First.GetType.GetProperty(sPath + "b").GetValue(query.First, Nothing))
                End If
            End If
            sHeader = If(sHeader = "#", sPath, sHeader)
        End If

        If bCheckedBox Then
            Dim dgItem As New DataGridCheckBoxColumn
            dgItem.Header = sHeader : dgItem.Visibility = If(bVisible, Visibility.Visible, Visibility.Collapsed)
            dgItem.DisplayIndex = DG.Columns.Count : dgItem.IsReadOnly = bReadOnly
            Dim Bind As New Binding(sPath)
            Bind.NotifyOnTargetUpdated = True
            Bind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            Bind.Mode = BindingMode.TwoWay
            dgItem.Binding = Bind
            DG.Columns.Add(dgItem)
        Else
            Dim dgItem As New DataGridTextColumn
            dgItem.Header = sHeader : dgItem.Visibility = If(bVisible And bOptioVisible, Visibility.Visible, Visibility.Collapsed)
            dgItem.DisplayIndex = DG.Columns.Count : dgItem.IsReadOnly = bReadOnly
            Dim Bind0 As New Binding(sPath)
            Bind0.Converter = New clsDataConverter
            'Zaruky Styles
            If DG.Tag Is Nothing Then DG.Tag = "nothing"
            If DG.Tag.ToString = "Database" Then
                Bind0.ConverterParameter = New clsMaxList.clsDataColumn(sPath, "String", False, 15, 15)
            ElseIf DG.Tag.ToString.StartsWith("Zaruky") Then
                Dim WS As New Style
                Select Case myMaxList.GetTyp(sPath)
                    Case "String"
                        Bind0.ConverterParameter = myMaxList.Item(sPath)
                    Case "Decimal"
                        Bind0.StringFormat = "N2"
                        WS.Setters.Add(New Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right))
                    Case "DateTime"
                        Bind0.ConverterParameter = myMaxList.Item(sPath)
                        Bind0.StringFormat = "d"
                        WS.Setters.Add(New Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right))
                    Case "Int32"
                        WS.Setters.Add(New Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right))
                End Select
                If DG.Tag.ToString = "ZarukyStyle" Then
                    Select Case sPath
                        Case "Vec"
                            Dim Bind1 As New Binding("DatKon")
                            Bind1.Converter = New clsDateToBooleanConverter
                            Dim Trigger As New DataTrigger
                            Trigger.Binding = Bind1
                            Trigger.Value = True
                            Trigger.Setters.Add(New Setter(DataGridCell.BackgroundProperty, myColorConverter.StringToBrush("#3FF3DFDF")))
                            Trigger.Setters.Add(New Setter(DataGridTextColumn.ForegroundProperty, Brushes.DarkRed))
                            WS.Triggers.Add(Trigger)
                            Dim Bind2 As New Binding("DatOpt")
                            Bind2.Converter = New clsNothingToBooleanConverter
                            Dim Trigger2 As New DataTrigger
                            Trigger2.Binding = Bind2
                            Trigger2.Value = True
                            Trigger2.Setters.Add(New Setter(DataGridCell.BackgroundProperty, myColorConverter.StringToBrush("#3FBDBBBB")))
                            Trigger2.Setters.Add(New Setter(DataGridTextColumn.ForegroundProperty, Brushes.Gray))
                            WS.Triggers.Add(Trigger2)
                        Case "DatKon"
                            Dim Bind2 As New Binding(sPath)
                            Bind2.Converter = New clsDateToBooleanConverter
                            Dim Trigger As New DataTrigger
                            Trigger.Binding = Bind2
                            Trigger.Value = True
                            Trigger.Setters.Add(New Setter(DataGridCell.BackgroundProperty, myColorConverter.StringToBrush("#3FF3DFDF")))
                            Trigger.Setters.Add(New Setter(DataGridTextColumn.ForegroundProperty, Brushes.DarkRed))
                            WS.Triggers.Add(Trigger)
                        Case "DatOpt"
                            Dim Bind2 As New Binding(sPath)
                            Bind2.Converter = New clsNothingToBooleanConverter
                            Dim Trigger2 As New DataTrigger
                            Trigger2.Binding = Bind2
                            Trigger2.Value = True
                            Trigger2.Setters.Add(New Setter(DataGridCell.BackgroundProperty, myColorConverter.StringToBrush("#3FBDBBBB")))
                            Trigger2.Setters.Add(New Setter(DataGridTextColumn.ForegroundProperty, Brushes.Gray))
                            WS.Triggers.Add(Trigger2)
                        Case "Dodavatel"
                            WS.Setters.Add(New Setter(DataGridCell.BackgroundProperty, myColorConverter.StringToBrush("#0C00BFFF")))
                            WS.Setters.Add(New Setter(DataGridTextColumn.ForegroundProperty, Brushes.DarkSlateBlue))
                        Case "Faktura"
                            WS.Setters.Add(New Setter(DataGridCell.BackgroundProperty, myColorConverter.StringToBrush("#0C00BFFF")))
                            WS.Setters.Add(New Setter(DataGridTextColumn.ForegroundProperty, Brushes.DarkSlateBlue))
                    End Select
                    If Not WS.Setters.Count = 0 Or Not WS.Triggers.Count = 0 Then dgItem.CellStyle = WS
                End If
            End If

            dgItem.Binding = Bind0
            DG.Columns.Add(dgItem)
            If sPath = "NewID" Then ColumnNewID = dgItem
        End If
    End Sub

#End Region

End Class

#End Region

#Region " Max Column List "

#Region " Data Converter for Max Column List "

Public Class clsDataConverter
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.Convert
        If parameter IsNot Nothing AndAlso Not parameter.GetType Is GetType(clsMaxList.clsDataColumn) Then
            Return Format(value, parameter.ToString)
        End If

        Return value
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.ConvertBack
        Dim Null As Boolean = True
        Dim MaxLength As Integer = 500
        If parameter IsNot Nothing AndAlso parameter.GetType Is GetType(clsMaxList.clsDataColumn) Then
            Dim DataColumn = CType(parameter, clsMaxList.clsDataColumn)
            Null = DataColumn.Null
            MaxLength = DataColumn.MaxLength
        End If
        If IsNothing(value) = False AndAlso value.ToString = "" Then value = Nothing

        If targetType Is GetType(Date) OrElse targetType Is GetType(Nullable(Of Date)) Then
            If IsDate(value) Then
                Return CDate(value)
            Else
                If IsNothing(value) And Null Then
                    Return value
                Else
                    Return DependencyProperty.UnsetValue
                    'Return Today
                End If
            End If

        ElseIf targetType Is GetType(Integer) OrElse targetType Is GetType(Nullable(Of Integer)) Then
            If IsNumeric(value) Then
                Return CInt(value)
            Else
                Return 0
            End If

        ElseIf targetType Is GetType(Decimal) OrElse targetType Is GetType(Nullable(Of Decimal)) Then
            If IsNumeric(value) Then
                Return CDec(value)
            Else
                Return 0
            End If

        ElseIf targetType Is GetType(Double) OrElse targetType Is GetType(Nullable(Of Double)) Then
            If IsNumeric(value) Then
                Return CDbl(value)
            Else
                Return 0
            End If

        End If

        If targetType Is GetType(String) And parameter IsNot Nothing Then
            If IsNothing(value) Then
                Return If(Null, value, DependencyProperty.UnsetValue)
            Else
                If value.ToString.Length > MaxLength Then
                    Return value.ToString.Substring(0, MaxLength)
                End If
            End If
        End If

        Return value
    End Function
End Class

#End Region

Public Class clsMaxList
    Inherits System.Collections.ObjectModel.Collection(Of clsDataColumn)

    Sub New()
        Me.Add(New clsDataColumn("Vec", "String", False, 40, 80))
        Me.Add(New clsDataColumn("SerNum", "String", True, 20, 30))
        Me.Add(New clsDataColumn("Dodavatel", "String", True, 40, 80))
        Me.Add(New clsDataColumn("Faktura", "String", True, 20, 30))
        Me.Add(New clsDataColumn("Optio1", "String", True, 50, 100))
        Me.Add(New clsDataColumn("Optio2", "String", True, 40, 20))
        Me.Add(New clsDataColumn("Optio3", "String", True, 40, 30))
        Me.Add(New clsDataColumn("Optio4", "String", True, 40, 30))
        Me.Add(New clsDataColumn("Optio5", "String", True, 40, 30))
        Me.Add(New clsDataColumn("Databaze", "String", True, 20, 20))
        Me.Add(New clsDataColumn("ID", "Int32", False))
        Me.Add(New clsDataColumn("OwnID", "Int32", False))
        Me.Add(New clsDataColumn("NewID", "Int32", False))
        Me.Add(New clsDataColumn("Oznacil", "Boolean", False))
        Me.Add(New clsDataColumn("OwnCheck", "Boolean", False))
        Me.Add(New clsDataColumn("Smazano", "Boolean", False))
        Me.Add(New clsDataColumn("DatPoc", "DateTime", False))
        Me.Add(New clsDataColumn("DatKon", "DateTime", False))
        Me.Add(New clsDataColumn("DatOpt", "DateTime", True))
        Me.Add(New clsDataColumn("Upraveno", "DateTime", False))
        Me.Add(New clsDataColumn("Cena", "Decimal", False))
        Me.Add(New clsDataColumn("CenaOpt", "Decimal", False))
    End Sub

    Public Function GetLength(ByVal sName As String, Optional ByVal bMax As Boolean = False) As Integer
        For Each one As clsDataColumn In Me
            If one.Name.ToLower = sName.ToLower Then
                If bMax Then
                    Return one.MaxLength
                Else
                    Return one.Length
                End If
            End If
        Next
        Return 1
    End Function

    Public Function GetMaxLength(ByVal sName As String) As Integer
        For Each one As clsDataColumn In Me
            If one.Name.ToLower = sName.ToLower Then Return one.MaxLength
        Next
        Return 1
    End Function

    Public Function GetTyp(ByVal sName As String) As String
        For Each one As clsDataColumn In Me
            If one.Name.ToLower = sName.ToLower Then Return one.Type
        Next
        Return "String"
    End Function

    Public Shadows Function Item(sName As String) As clsDataColumn
        Return (From a In Me Where a.Name = sName Select a).FirstOrDefault
    End Function

    Public Class clsDataColumn
        Sub New(sName As String, sType As String, bNull As Boolean, Optional iLength As Integer = 0, Optional iMaxLength As Integer = 0)
            Name = sName : Type = sType : Length = iLength : MaxLength = iMaxLength : Null = bNull
        End Sub

        Public Name As String
        Public Length As Integer
        Public MaxLength As Integer
        Public Type As String
        Public Null As Boolean
    End Class
End Class

#End Region
