using DevExpress.DataAccess.ConnectionParameters;
using DevExpress.DataAccess.DataFederation;
using DevExpress.DataAccess.Excel;
using DevExpress.DataAccess.Sql;
using DevExpress.XtraEditors;
using System;
using System.Reflection;
using System.Windows.Forms;

namespace FederationDataSourceExample
{
    public partial class Form1 : DevExpress.XtraEditors.XtraForm
    {
        public Form1()
        {
            InitializeComponent();
            
            foreach (var method in typeof(FederationDataSourceHelper).GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Static |
                BindingFlags.Public))
            {
                if (!method.IsSpecialName)
                    comboBoxEdit1.Properties.Items.Add(method.Name);
            }
            comboBoxEdit1.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            comboBoxEdit1.Properties.SelectedValueChanged += Properties_SelectedValueChanged;

        }

        private void Properties_SelectedValueChanged(object sender, EventArgs e)
        {
            gridView1.BeginUpdate();
            gridView1.Columns.Clear();
            gridControl1.DataSource = null;
            ComboBoxEdit editor = sender as ComboBoxEdit;
            typeof(FederationDataSourceHelper).InvokeMember(editor.SelectedItem.ToString(), BindingFlags.InvokeMethod | BindingFlags.Public |
                BindingFlags.Static, null, null, new object[] { });
            gridControl1.DataSource = FederationDataSourceHelper.DataSource;
            gridView1.EndUpdate();
            gridView1.ExpandMasterRow(0);
        }
    }
}
