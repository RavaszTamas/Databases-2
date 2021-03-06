using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Configuration;
using System.Xml;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        string connectionString = "Data Source=TAMAS-PC\\SQLEXPRESS; Initial Catalog=laboratory;" +
        "Integrated Security=true";
        DataSet dset;
        SqlDataAdapter daEmployees, daConstructionProjects;

        SqlConnection conn;
        String captionText = "";
        String parent = "";
        String child = "";
        String columnParent = "";
        String columnChild = "";
        String relation;
        public Form1()
        {
            InitializeComponent();
            InitializeBasedOnConfiguration();

            if (parent == "" || child == "" || columnParent == "" || columnChild == "" || relation == "")
            {
                string message = "Invalid configuration, choosen configuration not available";
                string caption = "Error Detected in configration";
                MessageBoxButtons buttons = MessageBoxButtons.OK;

                MessageBox.Show(message, caption, buttons);
                this.updateButton.Enabled = false;
                return;
            }

            this.Text = captionText;
            this.childLabel.Text = child;
            this.parentLabel.Text = parent;
            //SQLConnection


            conn = new SqlConnection(connectionString);

            conn.Open();
            //SQLCommand

            string queryConstructionSelect = "SELECT * FROM " + this.parent;
            string queryEmployeesSelect = "SELECT * FROM " + this.child;

            //SqlCommand cmd = new SqlCommand(queryConstructionSelect, conn);

            daConstructionProjects = new SqlDataAdapter(queryConstructionSelect, conn);
            daEmployees = new SqlDataAdapter(queryEmployeesSelect, conn);

            dset = new DataSet();

            daConstructionProjects.Fill(dset, this.parent.ToLower());
            daEmployees.Fill(dset, this.child.ToLower());

            DataRelation dataRelationProjectsEmployees = new DataRelation("FK_" + this.relation,
            dset.Tables[this.parent.ToLower()].Columns[this.columnParent],
            dset.Tables[this.child.ToLower()].Columns[this.columnChild]);

            dset.Relations.Add(dataRelationProjectsEmployees);

            BindingSource bsProjects = new BindingSource();
            bsProjects.DataSource = dset;
            bsProjects.DataMember = this.parent.ToLower();

            BindingSource bsEmployees = new BindingSource();
            bsEmployees.DataSource = bsProjects;
            bsEmployees.DataMember = "FK_" + this.relation;

            parentTable.DataSource = bsProjects;
            childTable.DataSource = bsEmployees;

            parentTable.Columns[this.columnParent].Visible = false;

            childTable.Columns[this.columnChild].Visible = false;

            childTable.Columns[0].Visible = false;


            parentTable.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            childTable.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.childTable.DataError += new DataGridViewDataErrorEventHandler(childGridView_DataError);

            conn.Close();


        }
        private void InitializeBasedOnConfiguration()
        {
            Boolean isMaximized = Convert.ToBoolean(ConfigurationManager.AppSettings["maximizedOnStart"]);
            if(isMaximized)
                this.WindowState = FormWindowState.Maximized;

            string relationToUse = ConfigurationManager.AppSettings["relation"];
            this.captionText = ConfigurationManager.AppSettings["formCaption"];
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load("relationQueries.xml");
            XmlNodeList xmlNodeList = xmlDocument.SelectNodes("/data/queries/query");

            foreach (XmlNode node in xmlNodeList)
            {
                if (node.Attributes.GetNamedItem("relation").Value == relationToUse)
                {
                    this.relation = relationToUse;
                    this.captionText = node.SelectSingleNode("captionText").InnerText;
                    this.parent = node.SelectSingleNode("parent").InnerText;
                    this.child = node.SelectSingleNode("child").InnerText;
                    this.columnParent = node.SelectSingleNode("column_parent").InnerText;
                    this.columnChild = node.SelectSingleNode("column_child").InnerText;
                }
            }
            Debug.WriteLine(this.captionText);
            Debug.WriteLine(this.parent);
            Debug.WriteLine(this.child);
            Debug.WriteLine(this.columnParent);
            Debug.WriteLine(this.columnChild);

        }


        private void updateButtonClick(object sender, EventArgs e)
        {
            conn.Open();

            string queryConstructionSelect = "SELECT * FROM " + this.parent;
            string queryEmployeesSelect = "SELECT * FROM " + this.child;


            daConstructionProjects = new SqlDataAdapter(queryConstructionSelect, conn);
            daEmployees = new SqlDataAdapter(queryEmployeesSelect, conn);


            SqlCommandBuilder cb = new SqlCommandBuilder(daEmployees);
            daEmployees.UpdateCommand = cb.GetUpdateCommand();

            try
            {
                daEmployees.Update(dset, this.child);
            }
            catch (DBConcurrencyException)
            {
                DialogResult response = MessageBox.Show("Concurrency Exception","Error when updating",MessageBoxButtons.OK);
            }
            catch (Exception)
            {
                MessageBox.Show("An error was thrown while attempting to update the database.", "Error when updating", MessageBoxButtons.OK);
            }

            conn.Close();

        }

        private void childGridView_DataError(object sender,
            DataGridViewDataErrorEventArgs anError)
        {
            MessageBox.Show("Error happened " + "\nPossible case of invalid input for cell\nPlease remove the data before continuing with any other task",
                "Error when inputing data", MessageBoxButtons.OK);

            if (anError.Context == DataGridViewDataErrorContexts.Commit)
            {
                MessageBox.Show("Commit error");
            }
            if (anError.Context == DataGridViewDataErrorContexts.CurrentCellChange)
            {
                MessageBox.Show("Cell change");
            }
            if (anError.Context == DataGridViewDataErrorContexts.Parsing)
            {
                MessageBox.Show("parsing error");
            }
            if (anError.Context == DataGridViewDataErrorContexts.LeaveControl)
            {
                MessageBox.Show("leave control error");
            }

            if ((anError.Exception) is ConstraintException)
            {
                DataGridView view = (DataGridView)sender;
                view.Rows[anError.RowIndex].ErrorText = "an error";
                view.Rows[anError.RowIndex].Cells[anError.ColumnIndex].ErrorText = "an error";

                anError.ThrowException = false;
            }

        }
    }
}
