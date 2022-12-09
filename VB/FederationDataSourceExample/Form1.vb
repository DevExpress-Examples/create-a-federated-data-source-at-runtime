Imports DevExpress.DataAccess.ConnectionParameters
Imports DevExpress.DataAccess.DataFederation
Imports DevExpress.DataAccess.Excel
Imports DevExpress.DataAccess.Sql
Imports DevExpress.XtraEditors
Imports System
Imports System.Reflection
Imports System.Windows.Forms

Namespace FederationDataSourceExample
	Partial Public Class Form1
		Inherits DevExpress.XtraEditors.XtraForm

		Public Sub New()
			InitializeComponent()

			For Each method In GetType(FederationDataSourceHelper).GetMethods(BindingFlags.DeclaredOnly Or BindingFlags.Static Or BindingFlags.Public)
				If Not method.IsSpecialName Then
					comboBoxEdit1.Properties.Items.Add(method.Name)
				End If
			Next method
			comboBoxEdit1.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor
			AddHandler comboBoxEdit1.Properties.SelectedValueChanged, AddressOf Properties_SelectedValueChanged

		End Sub

		Private Sub Properties_SelectedValueChanged(ByVal sender As Object, ByVal e As EventArgs)
			gridView1.BeginUpdate()
			gridView1.Columns.Clear()
			gridControl1.DataSource = Nothing
			Dim editor As ComboBoxEdit = TryCast(sender, ComboBoxEdit)
			GetType(FederationDataSourceHelper).InvokeMember(editor.SelectedItem.ToString(), BindingFlags.InvokeMethod Or BindingFlags.Public Or BindingFlags.Static, Nothing, Nothing, New Object() { })
			gridControl1.DataSource = FederationDataSourceHelper.DataSource
			gridView1.EndUpdate()
			gridView1.ExpandMasterRow(0)
		End Sub
	End Class
End Namespace
