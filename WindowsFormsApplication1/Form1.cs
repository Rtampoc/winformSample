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
using System.Text.RegularExpressions;
using Excel = Microsoft.Office.Interop.Excel;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        private string connectionString = "SERVER=(local)\\RAP;Database=dbData;USER=SA;PWD=1234";
        private bool isUpdating = false;
        private bool isNewRecord = false;
        public Form1()
        {
            InitializeComponent();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            enable();
        }

        private void btnAccept_Click(object sender, EventArgs e)
        {
            string badge = cmbxBadge.Text;
            string firstName = txtFname.Text;
            string lastName = txtLname.Text;
            DateTime birthday = dateTimePicker1.Value;
            string gender = cmbxGender.SelectedItem.ToString();

            if (!Regex.IsMatch(badge, @"^[SMP]\d{6}$"))
            {
                MessageBox.Show("BadgeNo must start with 'S', 'M', or 'P' followed by 6 digits.", "Invalid BadgeNo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd;

                if (isUpdating)
                {
                    cmd = new SqlCommand("UPDATE tblData SET FirstName=@FirstName, LastName=@LastName, Birthday=@Birthday, Gender=@Gender WHERE BadgNo=@BadgNo", conn);
                }
                else
                {
                    cmd = new SqlCommand("INSERT INTO tblData (BadgNo, FirstName, LastName, Birthday, Gender) VALUES (@BadgNo, @FirstName, @LastName, @Birthday, @Gender)", conn);
                }

                cmd.Parameters.AddWithValue("@BadgNo", badge);
                cmd.Parameters.AddWithValue("@FirstName", firstName);
                cmd.Parameters.AddWithValue("@LastName", lastName);
                cmd.Parameters.AddWithValue("@Birthday", birthday);
                cmd.Parameters.AddWithValue("@Gender", gender);


                //SqlCommand cmd = new SqlCommand("INSERT INTO Persons (BadgNo, FirstName, LastName, Birthday, Gender) VALUES (@BadgNo, @FirstName, @LastName, @Birthday, @Gender)", conn);
                //cmd.Parameters.AddWithValue("@BadgNo", badge);
                //cmd.Parameters.AddWithValue("@FirstName", firstName);
                //cmd.Parameters.AddWithValue("@LastName", lastName);
                //cmd.Parameters.AddWithValue("@Birhtday", birthday);
                //cmd.Parameters.AddWithValue("@Gender", gender);

                try
                {
                    cmd.ExecuteNonQuery();
                    MessageBox.Show(isUpdating ? "BadgeNo updated successfully!" : "BadgeNo added successfully!");
                    LoadData();
                    btnAccept.Enabled = false;
                    //btnAccept.Text = "Add";
                    isUpdating = false;
                }
                catch (SqlException ex)
                {
                    MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
            disable();
            LoadData();
            SetAcceptButtonState();
        }

        private void LoadData()
        {
            using (SqlConnection conn = new SqlConnection(connectionString)){
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT BadgNo FROM tblData", conn);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    cmbxBadge.Items.Add(reader["BadgNo"].ToString());
                }
                reader.Close();
            }
        }

        private void cmbxBadge_SelectedValueChanged(object sender, EventArgs e)
        {

        }

        private void cmbxBadge_SelectedIndexChanged(object sender, EventArgs e)
        {
            string badge = cmbxBadge.SelectedItem.ToString();
            LoadDetails(badge);
            isNewRecord = false;
            SetAcceptButtonState();
        }

        private void LoadDetails(string badge)
        {
            using (SqlConnection conn = new SqlConnection(connectionString)) {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT Firstname, Lastname, Birthday, Gender FROM tblData WHERE BadgNo = @BadgNo", conn);
                cmd.Parameters.AddWithValue("BadgNo", badge);

                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    txtFname.Text = reader["Firstname"].ToString();
                    txtLname.Text = reader["Lastname"].ToString();
                    dateTimePicker1.Value = Convert.ToDateTime(reader["Birthday"]);
                    cmbxGender.SelectedItem = reader["Gender"].ToString();
                }
                reader.Close();
            }
        }

        private void disable()
        {
            txtFname.Enabled = false;
            txtLname.Enabled = false;
            dateTimePicker1.Enabled = false;
            cmbxGender.Enabled = false;
        }
        private void enable()
        {
            txtFname.Enabled = true;
            txtLname.Enabled = true;
            dateTimePicker1.Enabled = true;
            cmbxGender.Enabled = true;
        }

        private void SetAcceptButtonState()
        {
            btnAccept.Enabled = isNewRecord;
        }

        private void txtFname_TextChanged(object sender, EventArgs e)
        {
            isNewRecord = true;
            SetAcceptButtonState();
        }

        private void txtLname_TextChanged(object sender, EventArgs e)
        {
            isNewRecord = true;
            SetAcceptButtonState();
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            isNewRecord = true;
            SetAcceptButtonState();
        }

        private void cmbxGender_TextChanged(object sender, EventArgs e)
        {
            isNewRecord = true;
            SetAcceptButtonState();
        }

        private void ExportToExcel(DataTable dt)
        {
            Excel.Application excelApp = new Excel.Application();
            Excel.Workbook workBook = excelApp.Workbooks.Add();
            Excel.Worksheet workSheet = (Excel.Worksheet)workBook.Sheets[1];

            for (int i = 0; i < dt.Columns.Count; i++)
            {
                workSheet.Cells[1, i + 1] = dt.Columns[i].ColumnName;
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                for (int j = 0; j < dt.Columns.Count; j++)
                {
                    workSheet.Cells[i + 2, j + 1] = dt.Rows[i][j];
                }
            }
            excelApp.Visible = true;
        }

        private void btnExtract_Click(object sender, EventArgs e)
        {
            string genderFilter = "";
            if (rbMale.Checked)
            {
                genderFilter = "Male";
            }
            else if (rbFemale.Checked)
            {
                genderFilter = "Female";
            }

            DataTable dataTable = new DataTable();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd;
                if (genderFilter == "")
                {
                    cmd = new SqlCommand("SELECT BadgNo, FirstName, LastName, Birthday, Gender FROM tblData", conn);
                }
                else
                {
                    cmd = new SqlCommand("SELECT BadgNo, FirstName, LastName, Birthday, Gender FROM tblData WHERE Gender = @Gender", conn);
                    cmd.Parameters.AddWithValue("@Gender", genderFilter);
                }

                SqlDataReader reader = cmd.ExecuteReader();
                dataTable.Load(reader);
                reader.Close();
            }

            ExportToExcel(dataTable);
        }
    }
}
