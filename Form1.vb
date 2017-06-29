Imports System.Text.RegularExpressions
Imports System.Net
Imports System.IO
Imports System.Threading

Public Class Form1

    Dim working As List(Of String) = New List(Of String)
    Dim scraped As List(Of String) = New List(Of String)
    Dim bufferS As List(Of String) = New List(Of String)
    Dim sources As List(Of String) = New List(Of String)
    Dim uiSc As List(Of String) = sources
    Dim failed As Integer = 0
    Dim src As Integer = 0
    Dim mtch As Integer = 0
    Dim listLock As Object = New Object
    Dim thrdLock As Object = New Object
    Dim lb2Lock As Object = New Object
    Dim isRunning As Boolean = False
    Dim isBox As Boolean = False
    Dim isDone As Boolean = False
    Dim thrdCnt As Integer = 50
    Private uiCtrl As Thread = New Thread(AddressOf uiControler)
    Private lb2CtrlThrd As Thread = New Thread(AddressOf lb2Ctrl)
    Dim d As New Dictionary(Of String, Thread)()

    Private Function scrapeLink(link As String) As List(Of String)
        Dim proxies As List(Of String) = New List(Of String)
        Dim Temp As String
        Try
            Dim r As HttpWebRequest = HttpWebRequest.Create(link)
            r.UserAgent = "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/30.0.1599.69 Safari/537.36"
            r.Timeout = 15000
            Dim re As HttpWebResponse = r.GetResponse()
            Dim rs As Stream = re.GetResponseStream
            Using sr As New StreamReader(rs)
                Temp = sr.ReadToEnd()
            End Using
            Dim src = Temp
            rs.Dispose()
            rs.Close()
            r.Abort()

            proxies = extractProx(src)

            If proxies.Count() >= 1 Then
                working.Add(link)
            Else
                failed += 1
            End If

        Catch ex As Exception

        End Try

        Return proxies
    End Function

    Private Sub Button5_Click(sender As Object, e As EventArgs) Handles Button5.Click
        If isRunning Then
            MessageBox.Show("Program is already running!")
            Exit Sub
        End If
        isBox = False
        isRunning = True
        isDone = False
        thrdCnt = 50

        uiCtrl.IsBackground = True
        uiCtrl.Start()
        lb2CtrlThrd.IsBackground = True
        lb2CtrlThrd.Start()

        For int As Integer = 1 To thrdCnt Step 1
            d(int.ToString) = New Thread(AddressOf threadedProxyScraper)
            d(int.ToString).IsBackground = True
            d(int.ToString).Start()
        Next
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        Dim clip As String = String.Empty
        If scraped.Count() > 0 Then
            clip = String.Join(vbNewLine, scraped.ToArray())
            Clipboard.SetText(clip)
            MessageBox.Show(scraped.Count() & " proxies copied to clipboard!")
        Else
            MessageBox.Show("No proxies to copy!")
        End If
    End Sub

    Private Sub threadedProxyScraper()
        Dim source As String
        While isRunning
            If sources.Count() = 0 Then
                Exit While
            End If
            SyncLock listLock
                source = sources.Item(0)
                sources.RemoveAt(0)
            End SyncLock
            'do work
            Dim tmp = (scraped.Count() - 1)

            For Each str As String In scrapeLink(source)
                scraped.Add(str)
                bufferS.Add(str)
            Next


        End While
        SyncLock thrdLock
            thrdCnt = thrdCnt - 1
        End SyncLock
        'check for job completion
        If thrdCnt = 0 And Not isBox Then
            MessageBox.Show("Done scraping!" & vbNewLine & scraped.Count() & " scraped proxies")
            isBox = True
            isRunning = False
            While bufferS.Count() > 0
                isDone = False
            End While
            isDone = True
            If CheckBox1.Checked Then
                MessageBox.Show("There are " & working.Count() & " working sources to save!")
                saveWorking()
            End If
        End If
    End Sub

    Private Sub saveWorking()
        If (working.Count() > 0) Then
            Dim tempL As List(Of String)
            SyncLock listLock
                tempL = working
            End SyncLock
            Dim fs As New SaveFileDialog
            fs.RestoreDirectory = True
            fs.Filter = "txt files (*.txt)|*.txt"
            fs.FilterIndex = 1
            fs.ShowDialog()
            If Not (fs.FileName = Nothing) Then
                Using sw As New StreamWriter(fs.FileName)
                    For Each line As String In tempL
                        sw.WriteLine(line)
                    Next
                End Using
            End If
        Else
            MessageBox.Show("No working proxy sources to save!")
        End If
    End Sub

    Private Sub Button6_Click(sender As Object, e As EventArgs) Handles Button6.Click
        If isRunning Then
            MessageBox.Show("Please stop the program first!")
            Exit Sub
        End If
        If clearFields() Then
            clearVars()
        End If
    End Sub

    Private Function clearFields() As Boolean
        Try
            If d(1).IsAlive Then
                MessageBox.Show("Please stop all threads first.")
                Return False
            End If
        Catch ex As Exception

        End Try

        Label1.Text = "Proxy Sources: 0"
        Label2.Text = "Scraped Proxies: 0"
        ListBox1.Items.Clear()
        ListBox2.Items.Clear()

        Label1.Update()
        Label2.Update()
        ListBox1.Update()
        ListBox2.Update()

        Return True
    End Function

    Private Function clearVars() As Boolean
        Try
            If d(1).IsAlive Then
                MessageBox.Show("Please stop all threads first.")
                Return False
            End If
        Catch ex As Exception

        End Try

        sources.Clear()
        working.Clear()
        bufferS.Clear()
        failed = 0
        mtch = 0
        src = 0
        d.Clear()
        Return True
    End Function

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        isRunning = False
        MessageBox.Show("Stopped!")
        While thrdCnt > 0 AndAlso bufferS.Count() > 0
            isDone = False
        End While
        isDone = True
    End Sub

    Private Sub lb2Ctrl()
        While Not isDone
            If bufferS.Count > 0 Then
                Try
                    If bufferS(0) IsNot Nothing Then
                        ListBox2.Invoke(Sub()
                                            ListBox2.Items.Add(bufferS(0))
                                        End Sub)
                        bufferS.RemoveAt(0)
                    End If
                Catch ex As Exception

                End Try
            End If
        End While
    End Sub

    Private Sub uiControler()
        While Not isDone
            ListBox2.Invoke(Sub()
                                ListBox2.TopIndex = ListBox2.Items.Count - 1
                                ListBox2.Update()
                            End Sub)
            Label2.Invoke(Sub()
                              Label2.Text = "Scraped Proxies: " & mtch
                              Label2.Update()
                          End Sub)
            Label3.Invoke(Sub()
                              Label3.Text = thrdCnt
                              Label3.Update()
                          End Sub)
            uiCtrl.Sleep(10)
        End While
    End Sub

    Private Function extractProx(http As String) As List(Of String)
        Dim output As List(Of String) = New List(Of String)

        For Each proxy As Match In Regex.Matches(http, "[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+:[0-9]+")
            output.Add(proxy.ToString())
            mtch += 1
        Next
        Return output
    End Function

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        If (scraped.Count() > 0) Then
            Dim tempL As List(Of String)
            SyncLock listLock
                tempL = scraped
            End SyncLock
            Dim fs As New SaveFileDialog
            fs.RestoreDirectory = True
            fs.Filter = "txt files (*.txt)|*.txt"
            fs.FilterIndex = 1
            fs.ShowDialog()
            If Not (fs.FileName = Nothing) Then
                Using sw As New StreamWriter(fs.FileName)
                    For Each line As String In tempL
                        sw.WriteLine(line)
                    Next
                End Using
            End If
        Else
            MessageBox.Show("No scraped proxies to save!")
        End If
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Dim fo As New OpenFileDialog
        fo.RestoreDirectory = True
        fo.Multiselect = False
        fo.Filter = "txt files (*.txt)|*.txt"
        fo.FilterIndex = 1
        fo.ShowDialog()
        If (Not fo.FileName = Nothing) Then
            Using sr As New StreamReader(fo.FileName)
                While sr.Peek <> -1
                    sources.Add(sr.ReadLine())
                    ListBox1.Items.Add(sources.Last())
                    ListBox1.TopIndex = ListBox1.Items.Count - 1
                    ListBox1.Update()
                    Label1.Text = "Proxy Sources: " & sources.Count()
                    Label1.Update()
                End While
            End Using
        End If
    End Sub
End Class
