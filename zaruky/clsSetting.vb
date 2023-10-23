Imports System.Collections.ObjectModel

#Region " Register Zaruky "

Public Class clsRegZaruky
    Inherits Collection(Of clsSubjekt)

    Sub New()
    End Sub

    Public Class clsSubjekt
        Sub New()
        End Sub

        Sub New(Subject As String, Mail As String, VAT As String)
            sSubjekt = Subject : sEmail = Mail : sICO = VAT
        End Sub

#Region " Get/Set "

        Public Property Subjekt() As String
            Get
                Return sSubjekt
            End Get
            Set(ByVal value As String)
                sSubjekt = value
            End Set
        End Property

        Public Property Email() As String
            Get
                Return sEmail
            End Get
            Set(ByVal value As String)
                sEmail = value
            End Set
        End Property

        Public Property ICO() As String
            Get
                Return sICO
            End Get
            Set(ByVal value As String)
                sICO = value
            End Set
        End Property

        Public Property RegCisla() As clsRegCisla
            Get
                Return Cisla
            End Get
            Set(ByVal value As clsRegCisla)
                Cisla = value
            End Set
        End Property

#End Region

        Private sSubjekt As String
        Private sEmail As String
        Private sICO As String
        Private Cisla As New clsRegCisla

    End Class

    Public Class clsRegCislo
        Sub New()
        End Sub

#Region " Get/Set "

        Public Property Cislo() As String
            Get
                Return sCislo
            End Get
            Set(ByVal value As String)
                sCislo = value
            End Set
        End Property

        Public Property Typ() As Integer
            Get
                Return CType(iTyp, DiskTypes)
            End Get
            Set(ByVal value As Integer)
                iTyp = value
            End Set
        End Property

#End Region

        Private sCislo As String
        Private iTyp As Integer

        Sub New(Number As String, Type As Integer)
            sCislo = Number
            iTyp = Type
        End Sub
    End Class

    Public Class clsRegCisla
        Inherits Collection(Of clsRegCislo)

        Sub New()
        End Sub

        Overloads Sub Add(Cislo As String, Typ As Integer)
            Me.Add(New clsRegCislo(Cislo, Typ))
        End Sub
    End Class

End Class

#End Region

#Region " Subjekty "

Class clsSubjekty

    Private RegZaruky As New clsRegZaruky

    Sub New()
        Dim myAES As New clsEncryption(System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).Comments)
        Dim memStream = New IO.MemoryStream(myFile.ReadEmbeddedResource("RegZaruk.lock"))
        RegZaruky = CType(New clsSerialization(RegZaruky).ReadXml(myAES.DecryptStream(memStream)), clsRegZaruky)
    End Sub

    Public ReadOnly Property Count() As Integer
        Get
            Return RegZaruky.Count
        End Get
    End Property

    Public Function FindSubject(Cislo As String, Typ As DiskTypes) As Boolean
        For Each Subjekt In RegZaruky
            Dim RegCislo = Subjekt.RegCisla.FirstOrDefault(Function(x) (x.Cislo = Cislo) And x.Typ = Typ)
            If RegCislo IsNot Nothing Then
                RegSubjekt.Email = Subjekt.Email : RegSubjekt.ICO = Subjekt.ICO : RegSubjekt.Name = Subjekt.Subjekt
                Verze = 4
                Return True
            End If
        Next
        Return False
    End Function

    Public Function FindSubject(Cesta As String) As Boolean
        Dim Typ = myFolder.DiskType(Cesta)
        If Not Typ = DiskTypes.Removable_2 And Not Typ = DiskTypes.Fixed_3 And Not Typ = DiskTypes.Server_4 Then Return False
        Dim Cislo = myFolder.VolumeSerialNumber(Cesta)
        If Cislo = "" Then Return False
        For Each Subjekt In RegZaruky
            Dim RegCislo = Subjekt.RegCisla.Where(Function(y) y.Typ = DiskTypes.Removable_2 Or y.Typ = DiskTypes.Fixed_3 Or y.Typ = DiskTypes.Server_4).FirstOrDefault(Function(x) x.Cislo = Cislo)
            If RegCislo IsNot Nothing Then
                RegSubjekt.Email = Subjekt.Email : RegSubjekt.ICO = Subjekt.ICO : RegSubjekt.Name = Subjekt.Subjekt
                Verze = 4
                Return True
            End If
        Next
        Return False
    End Function
End Class

#End Region

#Region " Log File "

Public Class clsLogFile

    Public Class clsLogs
        Inherits Collection(Of clsLog)

        Sub New()
        End Sub
    End Class

    Public Class clsLog
        Sub New()
        End Sub

        Sub New(Database_ As String, User_ As String, Time_ As Date)
            sDatabase = Database_ : sUser = User_ : dTime = Time_
        End Sub

#Region " Get/Set "

        Public Property Database() As String
            Get
                Return sDatabase
            End Get
            Set(ByVal value As String)
                sDatabase = value
            End Set
        End Property

        Public Property User() As String
            Get
                Return sUser
            End Get
            Set(ByVal value As String)
                sUser = value
            End Set
        End Property

        Public Property Time() As Date
            Get
                Return dTime
            End Get
            Set(ByVal value As Date)
                dTime = value
            End Set
        End Property

#End Region

        Private sDatabase As String
        Private sUser As String
        Private dTime As Date
    End Class


    Private pcUser As String = My.User.Name.Substring(My.User.Name.LastIndexOf("\".ToCharArray) + 1)
    Private sHeader As String = "Ověření přístupu k editaci"
    Private Logs As New clsLogs
    Private sLogPath As String
    Public wrAccess, reAccess As String

    Sub New(LogFilePath As String)
        sLogPath = LogFilePath
    End Sub

    ReadOnly Property FilePath As String
        Get
            Return sLogPath
        End Get
    End Property

    Public Function CheckAccess(Okno As Window, Enter As Boolean, Optional sDatabaze As String = "") As Boolean
        If Enter Then
            If sDatabaze = "" Then
                Return IsWriteAccess(Okno)
            Else
                If OpenAccess(sDatabaze, Okno) Then
                    Return IsWriteAccess(Okno)
                Else
                    Return False
                End If
            End If
        Else
            Return CloseAccess()
        End If
    End Function

    Private Function IsWriteAccess(Okno As Window) As Boolean
        If reAccess = wrAccess Or wrAccess = "" Then Return True
        Dim FormDialog = New wpfDialog(Okno, "Zadejte heslo pro přístup k úpravám:", sHeader, wpfDialog.Ikona.heslo, "OK", "Zavřít", True, False, "", False, 5)
        FormDialog.ShowDialog()
        reAccess = FormDialog.Input
        Return reAccess = wrAccess
    End Function

    Public Function OpenAccess(DatabazeName As String, Okno As Window) As Boolean
        If IsFile(sLogPath) Then
            If LogFileRead() = False Then Return False
            Dim Log = Logs.FirstOrDefault(Function(x) x.Database = DatabazeName)
            If Log Is Nothing Then
                Logs.Add(New clsLog(DatabazeName, pcUser, Now))
                Return LogFileWrite()
            Else
                If Log.User = pcUser Then Return True
                Dim FormDialog = New wpfDialog(Okno, "Od " + Log.Time.ToShortTimeString + " uživatel " + Log.User.ToUpper + " upravuje databázi " + DatabazeName.ToUpper + NR +
                                "Přístup odepřen.", sHeader, wpfDialog.Ikona.varovani, "Zavřít")
                FormDialog.ShowDialog()
                Return False
            End If
        Else
            Logs.Add(New clsLog(DatabazeName, pcUser, Now))
            Return LogFileWrite()
        End If
    End Function

    Public Function CloseAccess() As Boolean
        If IsFile(sLogPath) = False Then Return True
        If LogFileRead() = False Then Return False
        Logs.Where(Function(x) x.User = pcUser).ToList.ForEach(Sub(y) Logs.Remove(y))
        Return LogFileWrite()
    End Function

    Public Function GetFullAccess(Okno As Window) As Boolean
        If IsFile(sLogPath) Then
            If CloseAccess() = False Then Return False
            If Not Logs.Count = 0 Then
                Dim ActiveUsers As String = ""
                Logs.ToList.ForEach(Sub(x) ActiveUsers += x.User + " (" + x.Time.ToShortTimeString + ") ")
                Dim FormDialog = New wpfDialog(Okno, "Aktuálně připojení uživatelé: " + NR + ActiveUsers + NR + NR +
                                "Přístup by měl být odepřen. Zrušit operaci?" + NR + NR + "[" + sLogPath + "]", sHeader, wpfDialog.Ikona.dotaz, "Přerušit", "Pokračovat")
                If FormDialog.ShowDialog() Then
                    Return False
                Else
                    myFile.Delete(sLogPath, False)
                End If
            End If
        End If
        Return True
    End Function

#Region " Private "

    Private Function LogFileRead() As Boolean
        Try
            Logs.Clear()
            Logs = CType(New clsSerialization(Logs).ReadXml(sLogPath), clsLogs)
            Return True
        Catch ex As Exception
            Dim FormDialog = New wpfDialog(Nothing, "Soubor zarukyLog.xml není přístupný nebo je poškozen." + NR +
                            "Zkuste to znovu a pokud problém přetrvává, smažte soubor.", sHeader, wpfDialog.Ikona.varovani, "Zavřít")
            FormDialog.ShowDialog()
        End Try
        Return False
    End Function

    Private Function LogFileWrite() As Boolean
        Do
            Try
                Call (New clsSerialization(Logs)).WriteXml(sLogPath)
                Return True
            Catch ex As Exception
                Dim FormDialog = New wpfDialog(Nothing, "Soubor zarukyLog.xml nebyl přístupný pro zápis. Zkusit znovu?", "Ověření přístupu k editaci", wpfDialog.Ikona.dotaz, "Opakovat", "Zavřít")
                If FormDialog.ShowDialog() = False Then Return False
            End Try
        Loop
    End Function

    Private Function IsFile(ByVal Soubor As String) As Boolean
        If Soubor = "" Then Return False
        Try
            Dim exFile As New System.IO.FileInfo(Soubor)
            If exFile.Exists = False Then Return False
        Catch
            Return False
        End Try
        Return True
    End Function

    Protected Overrides Sub Finalize()
        Logs.Clear()
        MyBase.Finalize()
    End Sub
#End Region

End Class

#End Region

#Region " Nastavení "

Public Class clsSetting
    Sub New()
    End Sub

    'Common
    Private iAppVerze As Integer
    Public Zalohovat As Boolean
    Public ZalohaDne As Date = Today.Date.AddMonths(-1)
    Public CestaZaloh As String = ""
    Public CestaDatabaze As String = ""
    Public HesloDatabaze As String = Nothing
    Public Aktualizovat As Boolean = True
    'Main Window
    Public MainTop As Integer = 200
    Public MainLeft As Integer = 200
    Public MainWidth As Integer = 1000
    Public MainHeight As Integer = 800
    Public MainSpliter As Integer = 20
    Public RowEditWidth As Integer = 650
    Public ZoomFontSize As Integer = 12
    Public DnuDoKonce As Integer = 30
    Public ProsleNeprosle As Integer = 2
    Public RaditPodleDoby As Integer
    Public LimitPolozek As Integer = 100
    'RowEdit Window
    Public TableEdit As Boolean
    Public KolonkyVypraznit As Boolean
    Public KolonkyDelsi As Boolean
    Public CombaPlnitVsemi As Boolean
    Public RowEditFontBold As Boolean
    Public CislovaniDokladu As Boolean

    Public Property AppVerze() As Integer
        Get
            iAppVerze = If(iAppVerze = 0, Application.VersionNo, iAppVerze)
            Return iAppVerze
        End Get
        Set(ByVal value As Integer)
            iAppVerze = value
        End Set
    End Property

End Class

#End Region