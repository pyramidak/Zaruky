Imports System.Collections.ObjectModel

Public Class DriveCombo
    Public Disks As New Collection(Of clsDisk)
    Public Event SelectionChanged(ByVal Disk As clsDisk)
    Public Property SelectedDisk As clsDisk
    Public Property NoCdrom As Boolean
    Public Property NoFloppy As Boolean
    Public Property NoRemovable As Boolean
    Public Property NoRemote As Boolean
    Public Property NoFixed As Boolean

    Private Sub uctDriveCombo_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Reload()
    End Sub

    Public Sub Reload()
        Disks.Clear()
        For Each Drive As String In System.IO.Directory.GetLogicalDrives
            Dim DiskInfo As New System.IO.DriveInfo(Drive)
            If DiskInfo.IsReady Then
                Dim Disk As New clsDisk
                Disk.Type = CType(DiskInfo.DriveType, DiskTypes)
                Disk.Format = DiskInfo.DriveFormat
                Disk.Letter = Drive.Substring(0, 1)
                Disk.Number = myFolder.VolumeSerialNumber(Drive)
                If Disk.Number.Length = 8 AndAlso Disk.Number.Substring(0, 5) <> "00000" Then
                    Disk.Label = DiskInfo.VolumeLabel

                    Select Case Disk.Type '0=unknown,1=norootdir/floppy,2=removable,3=fixed,4=remote,5=cdrom,6=ramdisk
                        Case DiskTypes.Floppy_1
                            If Disk.Label = "" Then Disk.Label = "Disketa"
                            If NoFloppy = False Then Disk.Icon = CType(Me.FindResource("disketa"), ImageSource)
                        Case DiskTypes.Removable_2
                            If Disk.Label = "" Then Disk.Label = "Vyměnitelný disk"
                            If NoRemovable = False Then Disk.Icon = CType(Me.FindResource("flashdisk"), ImageSource)
                        Case DiskTypes.Fixed_3
                            If Disk.Label = "" Then Disk.Label = "Místní disk"
                            If NoFixed = False Then Disk.Icon = CType(Me.FindResource("harddisk"), ImageSource)
                        Case DiskTypes.Server_4
                            If Disk.Label = "" Then Disk.Label = "Vzdálený disk"
                            If NoRemote = False Then Disk.Icon = CType(Me.FindResource("server"), ImageSource)
                        Case DiskTypes.Cdrom_5
                            If Disk.Label = "" Then Disk.Label = "Optický disk"
                            If NoCdrom = False Then Disk.Icon = CType(Me.FindResource("cdrom"), ImageSource)
                    End Select
                    Disk.Label += " (" + Disk.Letter + ":) "
                    Select Case CDbl(DiskInfo.TotalSize)
                        Case Is > 10 ^ 12
                            Disk.Label += (DiskInfo.TotalSize / 1024 / 1024 / 1024 / 1024).ToString("N2") + " TB"
                        Case Is > 10 ^ 9
                            Disk.Label += (DiskInfo.TotalSize / 1024 / 1024 / 1024).ToString("N1") + " GB"
                        Case Is > 10 ^ 6
                            Disk.Label += (DiskInfo.TotalSize / 1024 / 1024).ToString("N0") + " MB"
                    End Select
                    If Not Disk.Icon Is Nothing Then Disks.Add(Disk)
                End If
            End If
        Next
        cbxDisks.ItemsSource = Disks
    End Sub

    Private Sub cbxDisks_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles cbxDisks.SelectionChanged
        If cbxDisks.SelectedItem IsNot Nothing Then
            SelectedDisk = CType(cbxDisks.SelectedItem, clsDisk)
            RaiseEvent SelectionChanged(SelectedDisk)
        End If
    End Sub

    Class clsDisk
        Public Property Icon As ImageSource
        Public Property Type As DiskTypes
        Public Property Letter As String
        Public Property Label As String
        Public Property Number As String
        Public Property Format As String
        Public Property Size As Long
    End Class
End Class

