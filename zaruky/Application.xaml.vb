Imports System.Windows.Threading

Class Application

    Public Shared StartUpLocation As String = myFolder.Path(System.Reflection.Assembly.GetExecutingAssembly().Location)
    Public Shared VersionNo As Integer = CInt(System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).FileMajorPart & System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).FileMinorPart & System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).FileBuildPart)
    Public Shared Version As String = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).FileMajorPart & "." & System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).FileMinorPart & "." & System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).FileBuildPart
    Public Shared LegalCopyright As String = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).LegalCopyright
    Public Shared CompanyName As String = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).CompanyName
    Public Shared ProductName As String = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).ProductName
    Public Shared ExeName As String = myFile.Name(System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).InternalName, False)
    Public Shared ProcessName As String = Diagnostics.Process.GetCurrentProcess.ProcessName
    Public Shared selType As Integer
    Public Shared winStore As Boolean = False

#Region " Global "

    Public Class myGlobal
        Public Shared NR As String = Chr(13) & Chr(10)
        Public Shared mySystem As New clsSystem
        Public Shared myMaxList As New clsMaxList
        Public Shared INIpath, SDFpath, ATTpath As String
        Public Shared Nastaveni As New clsSetting
        Public Shared coRows As Integer = 50
        Public Shared coMods As Integer = 100
        Public Shared coAll As String = "všechny"
        Public Shared JmenoLoad As String = coAll
        Public Shared JmenoDef As String = "hlavní"
        Public Shared SdfConnection, SdfConn2 As SqlServerCe.SqlCeConnection
        Public Shared JmenoLast As String
        Public Shared TrialRunOut, ShowUkonce As Boolean
        Public Shared myLogFile As clsLogFile
        '"InstZarukyTrial.exe"
        Public Shared PasswordXML As String = Chr(73) & Chr(110) & Chr(115) & Chr(116) & Chr(90) & Chr(97) & Chr(114) & Chr(117) & Chr(107) & Chr(121) & Chr(84) & Chr(114) & Chr(105) & Chr(97) & Chr(108) & Chr(46) & Chr(101) & Chr(120) & Chr(101)
        '"heslomusibytveslo"
        Public Shared PasswordSDF As String = Chr(104) & Chr(101) & Chr(115) & Chr(108) & Chr(111) & Chr(109) & Chr(117) & Chr(115) & Chr(105) & Chr(98) & Chr(121) & Chr(116) & Chr(118) & Chr(101) & Chr(115) & Chr(108) & Chr(111)

        Public Shared PasswordMail As String = ""
        'Free=1, Trial=2, Full=4
        Public Shared Verze As Integer = 4 '2
        Public Shared Lge As Boolean = mySystem.LgeCzech

        Structure RegisteredSubject
            Dim Name As String
            Dim ICO As String
            Dim Email As String
        End Structure
        Public Shared RegSubjekt As RegisteredSubject

        Structure ThreadValues
            Dim Number As Integer
            Dim Text As String
            Dim OK As Boolean

            Public Sub Clear()
                Number = 0 : Text = "" : OK = False
            End Sub
        End Structure
        Public Shared ThreadVal As ThreadValues

    End Class

#End Region

#Region " Start "

    Private Sub App_StartUp(ByVal sender As Object, ByVal e As StartupEventArgs) Handles MyClass.Startup
        If mySystem.isAppRunning(ProcessName, mySystem.User) Then End
        FrameworkElement.LanguageProperty.OverrideMetadata(GetType(FrameworkElement), New FrameworkPropertyMetadata(Markup.XmlLanguage.GetLanguage(Globalization.CultureInfo.CurrentCulture.IetfLanguageTag)))

        LoadSetting()
        Dim Arg As String = ""
        Dim Args() As String = Environment.GetCommandLineArgs
        If UBound(Args) > 0 Then Arg = Args(1)
        If Arg = "-win" Or Arg = "/win" Then
            If JsouUkonce() = False Then End
            ShowUkonce = True
        End If
        Dim mainWindow As New wpfMain
        mainWindow.Show()
    End Sub

    Private Function JsouUkonce() As Boolean
        If myFile.Exist(SDFpath) = False Then Return False
        If mySQL.EnsureVersion40(SDFpath) = False Then Return False
        Dim Heslo As String
        If Nastaveni.HesloDatabaze Is Nothing Then
            Heslo = ""
        Else
            Heslo = myString.Decrypt(Nastaveni.HesloDatabaze, PasswordXML)
        End If
        SdfConn2 = New SqlServerCe.SqlCeConnection(mySQL.CreateSDFConnString(SDFpath, Heslo))
        Try
            Using dcZaruky As New zarukyContext(SdfConn2)
                Dim queryZaruky As IQueryable(Of Zaruky)

                If Nastaveni.DnuDoKonce > 0 Then
                    queryZaruky = From a As Zaruky In dcZaruky.Zarukies
                                  Join b As Databaze In dcZaruky.Databazes On b.Jmeno Equals a.Databaze
                                  Where b.Active = True And a.Smazano = False And a.Oznacil = False And
                        CDate(a.DatKon) <= Today.AddDays(Nastaveni.DnuDoKonce) And CDate(a.DatKon) >= Today
                                  Select a
                Else
                    queryZaruky = From a As Zaruky In dcZaruky.Zarukies
                                  Join b As Databaze In dcZaruky.Databazes On b.Jmeno Equals a.Databaze
                                  Where b.Active = True And a.Smazano = False And a.Oznacil = False And
                        CDate(a.DatKon) < Today And CDate(a.DatKon) >= Today.AddDays(Nastaveni.DnuDoKonce)
                                  Select a
                End If

                Return Not queryZaruky.Count = 0
            End Using
        Catch
            Return False
        End Try
    End Function

#End Region

#Region " Load Setting "

    Private Sub LoadSetting()
        'SETTING
        INIpath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\pyramidak\zaruky.xml"
        myFolder.Exist(myFolder.Path(INIpath), True)
        If myFile.Exist(INIpath) Then
            Nastaveni = CType(New clsSerialization(Nastaveni).ReadXml(INIpath), clsSetting)
        End If
        CreatePaths(Nastaveni.CestaDatabaze)
    End Sub

#End Region

#Region " Create Paths "

    Public Shared Sub CreatePaths(ByVal DBpathToSet As String)
        Dim Folder As String
        If DBpathToSet.ToLower.EndsWith("zaruky.sdf") Then
            Folder = myFolder.Path(DBpathToSet)
        Else
            Folder = DBpathToSet
        End If
        If myFolder.Exist(Folder, True) = False Then
            Folder = mySystem.Path.Documents
            myFolder.Exist(Folder, True)
        End If
        SDFpath = myFile.Join(Folder, "zaruky.sdf")
        myLogFile = New clsLogFile(myFile.Join(Folder, "zaruky.log"))
        ATTpath = myFolder.Join(Folder, "prilohy zaruk")
        Nastaveni.CestaDatabaze = SDFpath
    End Sub

#End Region

#Region " Window "

    Public Shared ReadOnly Property Icon As ImageSource
        Get
            Return myBitmap.UriToImageSource(New Uri("/" + ExeName + ";component/" + ExeName + ".ico", UriKind.Relative))
        End Get
    End Property

    Public Shared Function SettingWindow() As wpfSetting
        For Each wOne As Window In Application.Current.Windows
            If wOne.Name = "wSetting" Then Return CType(wOne, wpfSetting)
        Next
        Return Nothing
    End Function

    Public Shared Function Title() As String
        Return If(Lge, "Záruky", ProductName) + " " + If(Verze = 4, "Pro", If(Verze = 2, "Trial", "")) + " " + Version
    End Function

#End Region

#Region " Exception "

    ' Application-level events, such as Startup, Exit, and DispatcherUnhandledException
    ' can be handled in this file.
    Private bError As Boolean

    Private Sub App_DispatcherUnhandledException(ByVal sender As Object, ByVal e As DispatcherUnhandledExceptionEventArgs) Handles MyClass.DispatcherUnhandledException
        ' Process unhandled exception
        If bError Then Exit Sub
        bError = True
        e.Handled = True

        If e.Exception.Message.Contains("System.Data.SqlServerCe") Then
            Dim wDialog As New wpfDialog(Nothing, "Aplikace vyžaduje knihovny" + NR + "Microsoft SQL Server Compact 4.0." + NR +
                                                  "Klikněte na Ukončit a otevře se" + NR + "Microsoft Download Center.", Application.Title, wpfDialog.Ikona.chyba, "Ukončit")
            wDialog.ShowDialog()
            myFile.Launch(Nothing, "http://www.microsoft.com/cs-cz/download/details.aspx?id=30709")
        Else
            Dim Form As New wpfError
            Form.myError = e
            Form.ShowDialog()
        End If

        End
    End Sub

#End Region

End Class
