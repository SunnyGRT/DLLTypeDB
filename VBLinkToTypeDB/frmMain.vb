Imports GrpcServer
Imports TypeDBCustom

Public Class frmMain

    'this is the client we build in C# 
    Private client As CoreClient = Nothing

    Private Sub AddValuesToTextBox(Optional msg As String = "")
        If Me.InvokeRequired Then
            Me.Invoke(Sub() AddValuesToTextBox(msg))
            Return
        End If

        If msg = "" Then
            RichTextBox1.AppendText(vbCrLf)
        Else
            RichTextBox1.AppendText(msg)
            RichTextBox1.AppendText(vbCrLf)
        End If

    End Sub

    Private Sub frmMain_Load(sender As Object, e As EventArgs) Handles MyBase.Load

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
            Button3.Enabled = False
            Button4.Enabled = False
            Button5.Enabled = False
            client = Nothing

        Else

            'connect to selected database from combo box and create session + pulse automatically.
            Call client.OpenDatabase(ComboBox1.SelectedItem.ToString())
            RichTextBox1.AppendText($"session successfully opened for database ""{ComboBox1.SelectedItem}""")
            RichTextBox1.AppendText(vbCrLf)
            RichTextBox1.AppendText(vbCrLf)
            'RichTextBox1.AppendText(client.GetSchema)
            Button3.Text = "Close Session"
            Button1.Enabled = False
            Button2.Enabled = False
            Button4.Enabled = True
            Button5.Enabled = True

        End If

    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click

        Dim iid As Google.Protobuf.ByteString = Nothing
        Dim rsult = client.ExecuteQuery(TextBox3.Text)
        Try

            For Each conc In rsult

                Dim cncpts As Concept() = New Concept(conc.Map.Count - 1) {}
                For i = 0 To conc.Map.Count - 1
                    conc.Map.TryGetValue(conc.Map.Keys(i), cncpts(i)) 'you can get the maping key from your query

                    If Not IsNothing(cncpts(i).Thing?.Iid) Then
                        iid = cncpts(i).Thing.Iid
                    End If
                    CheckConceptData(cncpts(i), conc.Map.Keys(i))

                Next
                Erase cncpts
                AddValuesToTextBox()

            Next
            If rsult.Count() <= 0 Then
                AddValuesToTextBox($"[{DateTime.Now}] request Completed")
            End If
            AddValuesToTextBox()

        Catch ex As Exception
            MsgBox(ex.Message)
        End Try

    End Sub

    Private Sub CheckConceptData(cncpt As Concept, mapKey As String)

        Select Case cncpt.ConceptCase
            Case Concept.ConceptOneofCase.Type
                AddValuesToTextBox($"[{DateTime.Now}] {cncpt.Type.Label} - [{cncpt.Type.Encoding} - {cncpt.Type.ValueType}]")
                Exit Select

            Case Concept.ConceptOneofCase.Thing
                Select Case cncpt.Thing.Type.ValueType
                    Case AttributeType.Types.ValueType.Boolean
                        AddValuesToTextBox($"[{DateTime.Now}] {mapKey}: {cncpt.Thing.Value.Boolean} [{cncpt.Thing.Type.Label}]")
                        Exit Select
                    Case AttributeType.Types.ValueType.String
                        AddValuesToTextBox($"[{DateTime.Now}] {mapKey}: {cncpt.Thing.Value.String} [{cncpt.Thing.Type.Label}]")
                        Exit Select
                    Case AttributeType.Types.ValueType.Datetime
                        AddValuesToTextBox($"[{DateTime.Now}] {mapKey}: {cncpt.Thing.Value.DateTime} [{cncpt.Thing.Type.Label}]")
                        Exit Select
                    Case AttributeType.Types.ValueType.Double
                        AddValuesToTextBox($"[{DateTime.Now}] {mapKey}: {cncpt.Thing.Value.Double} [{cncpt.Thing.Type.Label}]")
                        Exit Select
                    Case AttributeType.Types.ValueType.Long
                        AddValuesToTextBox($"[{DateTime.Now}] {mapKey}: {cncpt.Thing.Value.Long} [{cncpt.Thing.Type.Label}]")
                        Exit Select
                    Case AttributeType.Types.ValueType.Object
                        'AddValuesToTextBox($"[{DateTime.Now}] {mapKey}: {If(cncpt.Thing.Value, cncpt.Thing.Iid.ToBase64)} [{cncpt.Thing.Type.Label}]")
                        Dim lsFieldValue As String = ""
                        Try
                            lsFieldValue = cncpt.Thing.Value.String
                        Catch ex As Exception
                            lsFieldValue = cncpt.Thing.ToString
                            'Not a biggie at this stage.
                        End Try
                        AddValuesToTextBox($"[{DateTime.Now}] {lsFieldValue}]")
                        Dim results = client.getHas(cncpt.Thing.Iid)
                        If Not IsNothing(results) Then
                            For Each result In results
                                AddValuesToTextBox($"{result.Type.Label}: {result.Value}")
                            Next
                            Erase results
                        End If
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
        Dim Entities = client.getEntities()
        For Each entity In Entities

            Dim nodeEntity = nodeEntities.Nodes.Add($"{entity.Label}")
            Try
                Dim attributes = client.getAttributes(entity.Label)
                For Each attribute In attributes
                    nodeEntity.Nodes.Add($"{attribute.Label} [{attribute.ValueType}]")
                Next

                attributes = client.getPlays(entity.Label)
                For Each attribute In attributes
                    nodeEntity.Nodes.Add($"plays {attribute.Scope}:{attribute.Label}")
                Next

                attributes = client.getAttributes(entity.Label, True)
                For Each attribute In attributes
                    nodeEntity.Nodes.Add($"key - {attribute.Label} [{attribute.ValueType}]")
                Next

            Catch ex As Exception
            End Try

        Next

        ' THIS WILL GET ALL THE RELATIONS
        Dim nodeRelations = TreeView1.Nodes.Add("Relations")
        Dim Rules = client.getRules.ToList()
        Dim Relations = client.getRelations()
        For Each Relation In Relations

            Dim relationtype = Array.FindAll(client.getSuperTypes(Relation.Label),
                                             Function(x) Not x.Label = Relation.Label AndAlso Not x.Root)
            Dim nodeRelation As TreeNode
            If IsNothing(relationtype) OrElse relationtype.Length < 1 Then
                nodeRelation = nodeRelations.Nodes.Add($"{Relation.Label}")
            Else
                nodeRelation = nodeRelations.Nodes.Add($"{Relation.Label} [{relationtype(0).Label}]")
            End If

            Try
                Dim larAttributes = client.getAttributes(Relation.Label)
                For Each lrAttribute In larAttributes
                    nodeRelation.Nodes.Add($"owns {lrAttribute.Label} [{lrAttribute.ValueType}]")
                Next

                Dim attributes = client.getPlays(Relation.Label)
                For Each attribute In attributes
                    nodeRelation.Nodes.Add($"plays {attribute.Scope}:{attribute.Label}")
                Next

                attributes = client.getRelates(Relation.Label)
                For Each attribute In attributes
                    nodeRelation.Nodes.Add($"relates {attribute.Label} [{attribute.Encoding}]")
                    For Each player In client.getPlayers(attribute.Label, attribute.Scope)
                        If Not player.Label = attribute.Label AndAlso Not player.Root Then
                            nodeRelation.Nodes.Add($"   player {player.Label} [{player.Encoding}]")
                        End If
                    Next
                    For Each superType In client.getSuperTypes(attribute.Label, attribute.Scope)
                        If Not superType.Label = attribute.Label AndAlso Not superType.Root Then
                            nodeRelation.Nodes.Add($"   as {superType.Label} [{superType.Encoding}]")
                        End If
                    Next
                Next
            Catch ex As Exception
            End Try

            Dim Rule = Rules.Find(Function(x) x.Then.Trim.EndsWith($"isa {Relation.Label}", StringComparison.InvariantCultureIgnoreCase))
            If Not IsNothing(Rule) Then
                Dim nodeRule = nodeRelation.Nodes.Add("Rule")
                nodeRule.Nodes.Add($"Label: {Rule.Label}")
                nodeRule.Nodes.Add($"When: {Rule.When}")
                nodeRule.Nodes.Add($"Then: {Rule.Then}")
            End If

        Next

        ' THIS WILL GET ALL THE ATTRIBUTES 
        Dim nodeAttributes = TreeView1.Nodes.Add("Attributes")
        Dim attributesTypes = client.getAllAttributes()
        For Each attribute In attributesTypes

            Dim lrAttributeNode = nodeAttributes.Nodes.Add($"{attribute.Label} [{attribute.ValueType}]")

            Dim larSubAttributes = client.getAttributes(attribute.Label)
            For Each lrSubAttribute In larSubAttributes
                lrAttributeNode.Nodes.Add($"{lrSubAttribute.Label} [{lrSubAttribute.ValueType}]")
            Next

            Dim plays = client.getPlays(attribute.Label)
            For Each playing In plays
                lrAttributeNode.Nodes.Add($"plays {playing.Scope}:{playing.Label}")
            Next

        Next

        ' THIS WILL GET ALL THE RULES
        Dim nodeRules = TreeView1.Nodes.Add("Rules")
        For Each Rule In Rules

            Dim nodeRule = nodeRules.Nodes.Add($"{Rule.Label}")
            Try
                nodeRule.Nodes.Add($"When: {Rule.When}")
                nodeRule.Nodes.Add($"Then: {Rule.Then}")
            Catch ex As Exception
            End Try

        Next

    End Sub

End Class
