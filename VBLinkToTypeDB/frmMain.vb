Imports GrpcServer
Imports TypeDBCustom

Public Class frmMain

    Private client As CoreClient = Nothing

    Private Sub AddValuesToTextBox(msg As String)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() AddValuesToTextBox(msg))
            Return
        End If

        RichTextBox1.AppendText(msg)
        RichTextBox1.AppendText(vbCrLf)

    End Sub

    Private Sub frmMain_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

        client = New CoreClient(TextBox1.Text, CInt(TextBox2.Text))
        RichTextBox1.Text = $"Successfully connected to server on {TextBox1.Text}:{TextBox2.Text}"
        RichTextBox1.AppendText(vbCrLf)

    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click

        RichTextBox1.AppendText(vbCrLf)
        ComboBox1.Items.AddRange(client.GetAllDatabases())
        If ComboBox1.Items.Count >= 1 Then
            ComboBox1.SelectedIndex = 0
        End If
        For Each dbName In ComboBox1.Items
            RichTextBox1.AppendText($"found databse with name ""{dbName}""")
            RichTextBox1.AppendText(vbCrLf)
        Next

    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click

        Call client.OpenDatabase(ComboBox1.SelectedItem.ToString())
        RichTextBox1.AppendText($"session successfully opened for database ""{ComboBox1.SelectedItem}""")
        RichTextBox1.AppendText(vbCrLf)
        RichTextBox1.AppendText(vbCrLf)

    End Sub

    Private Async Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click

        Dim query As New QueryManager.Types.Req() With {
            .MatchReq = New QueryManager.Types.Match.Types.Req() With {.Query = TextBox3.Text},
            .Options = New Options() With {.Parallel = True}
        }

        client.transactionClient.Reqs.Clear()
        client.transactionClient.Reqs.Add(New Transaction.Types.Req() With {.QueryManagerReq = query, .ReqId = client.SessionID})
        Await client.Transactions.RequestStream.WriteAsync(client.transactionClient)

        Dim ServerResp As Transaction.Types.Server = Nothing
        Do While Await client.Transactions.ResponseStream.MoveNext(Threading.CancellationToken.None)
            ServerResp = client.Transactions.ResponseStream.Current

            If ServerResp.ResPart.ResCase = Transaction.Types.ResPart.ResOneofCase.StreamResPart AndAlso
                ServerResp.ResPart.StreamResPart.State = Transaction.Types.Stream.Types.State.Done Then
                Exit Do
            End If

            For Each itm In ServerResp.ResPart.QueryManagerResPart.MatchResPart.Answers.ToArray()
                Dim cncpt As Concept = Nothing
                itm.Map.TryGetValue("fn", cncpt)
                AddValuesToTextBox($"[{DateTime.Now}] received response: {cncpt.Thing.Value.String}")
            Next

        Loop

    End Sub
End Class
