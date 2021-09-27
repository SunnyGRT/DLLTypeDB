Imports GrpcServer
Imports TypeDBCustom

Public Class frmMain

    'this is the client we build in C# 
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
        ComboBox2.SelectedIndex = 2
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

        'make connection to server
        client = New CoreClient(TextBox1.Text, CInt(TextBox2.Text))
        RichTextBox1.Text = $"Successfully connected to server on {TextBox1.Text}:{TextBox2.Text}"
        RichTextBox1.AppendText(vbCrLf)
        Button1.Enabled = False
        Button2.Enabled = True

    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click

        ComboBox1.Items.Clear()
        RichTextBox1.AppendText(vbCrLf)
        'get all databases and add it to combobox
        ComboBox1.Items.AddRange(client.GetAllDatabases())
        If ComboBox1.Items.Count >= 1 Then
            ComboBox1.SelectedIndex = 0
        End If
        For Each dbName In ComboBox1.Items
            RichTextBox1.AppendText($"found databse with name ""{dbName}""")
            RichTextBox1.AppendText(vbCrLf)
        Next
        Button3.Enabled = True

    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click

        If Button3.Text = "Close Session" Then

            client.CloseDatabase()
            Button3.Text = "Create Session With Database"
            Button1.Enabled = True
            Button2.Enabled = False
            Button4.Enabled = False
            Button5.Enabled = False
            client = Nothing

        Else

            'connect to selected database from combo box and create session + pulse automatically.
            Call client.OpenDatabase(ComboBox1.SelectedItem.ToString())
            RichTextBox1.AppendText($"session successfully opened for database ""{ComboBox1.SelectedItem}""")
            RichTextBox1.AppendText(vbCrLf)
            RichTextBox1.AppendText(vbCrLf)
            Button3.Text = "Close Session"
            Button1.Enabled = False
            Button2.Enabled = False
            Button4.Enabled = True
            Button5.Enabled = True

        End If

    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click

        Dim iid As Google.Protobuf.ByteString = Nothing
        Dim rsult = client.ExecuteQuery(TextBox3.Text, ComboBox2.SelectedIndex)
        For Each conc In rsult

            Dim cncpts As Concept() = New Concept(conc.Map.Count - 1) {}
            For i = 0 To conc.Map.Count - 1
                conc.Map.TryGetValue(conc.Map.Keys(i), cncpts(i)) 'you can get the maping key from your query
            Next

            For i = 0 To cncpts.Length - 1
                If Not IsNothing(cncpts(i).Thing?.Iid) Then
                    iid = cncpts(i).Thing.Iid
                End If
                CheckConceptData(cncpts(i))
            Next

        Next
        If rsult.Count() <= 0 Then
            AddValuesToTextBox($"[{DateTime.Now}] request Completed")
        End If

    End Sub

    Private Sub CheckConceptData(cncpt As Concept)

        Select Case cncpt.ConceptCase
            Case Concept.ConceptOneofCase.Type
                AddValuesToTextBox($"[{DateTime.Now}] {cncpt.Type.Label} - [{cncpt.Type.Encoding} - {cncpt.Type.ValueType}]")
                Exit Select

            Case Concept.ConceptOneofCase.Thing
                Select Case cncpt.Thing.Type.ValueType
                    Case AttributeType.Types.ValueType.Boolean
                        AddValuesToTextBox($"[{DateTime.Now}] received response: {cncpt.Thing.Type.Label} - {cncpt.Thing.Value.Boolean}")
                        Exit Select
                    Case AttributeType.Types.ValueType.String
                        AddValuesToTextBox($"[{DateTime.Now}] received response: {cncpt.Thing.Type.Label} - {cncpt.Thing.Value.String}")
                        Exit Select
                    Case AttributeType.Types.ValueType.Datetime
                        AddValuesToTextBox($"[{DateTime.Now}] received response: {cncpt.Thing.Type.Label} - {cncpt.Thing.Value.DateTime}")
                        Exit Select
                    Case AttributeType.Types.ValueType.Double
                        AddValuesToTextBox($"[{DateTime.Now}] received response: {cncpt.Thing.Type.Label} - {cncpt.Thing.Value.Double}")
                        Exit Select
                    Case AttributeType.Types.ValueType.Long
                        AddValuesToTextBox($"[{DateTime.Now}] received response: {cncpt.Thing.Type.Label} - {cncpt.Thing.Value.Long}")
                        Exit Select
                    Case AttributeType.Types.ValueType.Object
                        AddValuesToTextBox($"[{DateTime.Now}] received response: {cncpt.Thing.Type.Label} - {cncpt.Thing.Value}")
                        Exit Select
                End Select
                Exit Select

            Case Else
                AddValuesToTextBox($"[{DateTime.Now}] received response: {cncpt.Thing.Value.String}")
        End Select

    End Sub

    Private Sub Button5_Click(sender As Object, e As EventArgs) Handles Button5.Click

        TreeView1.Nodes.Clear()

        ' THIS WILL GET ALL THE ENTITIES
        Dim nodeEntities = TreeView1.Nodes.Add("Entities")
        Dim Entities = client.GetAllEntities()
        For Each entity In Entities

            Dim cncpts As Concept = Nothing
            entity.Map.TryGetValue(entity.Map.FirstOrDefault.Key, cncpts)

            Dim nodeEntity = nodeEntities.Nodes.Add($"{cncpts.Type.Label} [{cncpts.Type.Encoding}]")
            Try
                Dim attributes = client.GetAttributes(cncpts.Type.Label)
                For Each attribute In attributes.Keys.ToArray()
                    nodeEntity.Nodes.Add($"{attribute} [{attributes(attribute).Encoding}]")
                Next
            Catch ex As Exception
            End Try


        Next

        ' THIS WILL GET ALL THE RELATIONS
        Dim nodeRelations = TreeView1.Nodes.Add("Relations")
        Dim Relations = client.GetAllRelations()
        For Each Relation In Relations

            Dim cncpts As Concept = Nothing
            Relation.Map.TryGetValue(Relation.Map.FirstOrDefault.Key, cncpts)

            Dim nodeRelation = nodeRelations.Nodes.Add($"{cncpts.Type.Label} [{cncpts.Type.Encoding}]")
            Try
                Dim attributes = client.GetAttributes(cncpts.Type.Label)
                For Each attribute In attributes.Keys.ToArray()
                    nodeRelation.Nodes.Add($"{attribute} [{attributes(attribute).Encoding}]")
                Next
            Catch ex As Exception
            End Try

        Next

    End Sub

End Class
