using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace HospitalManagement
{
    /// <summary>Admin: add doctors (creates their login too), view and remove them.</summary>
    public class ManageDoctorsForm : Form
    {
        private DataGridView grid;
        private TextBox nameBox, emailBox, phoneBox, specBox, qualBox, daysBox, roomBox, pwBox;
        private ComboBox genderBox, deptBox;
        private NumericUpDown feeBox;

        public ManageDoctorsForm()
        {
            UI.Setup(this, "Manage doctors", 1180, 700);

            grid = UI.Grid();
            FlowLayoutPanel footer = UI.Flow(
                UI.Btn("Refresh", delegate { LoadDoctors(); }, 1),
                UI.Btn("Reset password", ResetPassword, 1),
                UI.Btn("Delete selected", DeleteDoctor, 2));
            TableLayoutPanel left = UI.ListLayout(null, grid, footer);

            nameBox = UI.Txt();
            emailBox = UI.Txt();
            phoneBox = UI.Txt();
            genderBox = UI.Combo("Male", "Female", "Other");
            deptBox = UI.Combo();
            deptBox.DisplayMember = "Name";
            deptBox.ValueMember = "DepartmentId";
            specBox = UI.Txt();
            qualBox = UI.Txt();
            feeBox = new NumericUpDown();
            feeBox.Maximum = 100000;
            feeBox.Increment = 100;
            feeBox.Value = 800;
            daysBox = UI.Txt();
            daysBox.Text = "Sat,Sun,Mon,Tue,Wed,Thu";
            roomBox = UI.Txt();
            pwBox = UI.Txt(true, false, 0);

            TableLayoutPanel f = UI.FieldTable();
            UI.AddField(f, "Full name", nameBox);
            UI.AddField(f, "Email", emailBox);
            UI.AddField(f, "Phone", phoneBox);
            UI.AddField(f, "Gender", genderBox);
            UI.AddField(f, "Department", deptBox);
            UI.AddField(f, "Specialization", specBox);
            UI.AddField(f, "Qualification", qualBox);
            UI.AddField(f, "Fee (Tk)", feeBox);
            UI.AddField(f, "Days (Sat,Sun..)", daysBox);
            UI.AddField(f, "Room", roomBox);
            UI.AddField(f, "Login password", pwBox);

            TableLayoutPanel right = UI.FormLayout(f, UI.Flow(UI.Btn("Add doctor", AddDoctor)));
            right.BackColor = System.Drawing.Color.White;

            TableLayoutPanel root = new TableLayoutPanel();
            root.ColumnCount = 2;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.Controls.Add(left, 0, 0);
            root.Controls.Add(right, 1, 0);
            root.RowCount = 1;

            UI.Compose(this, "Doctors", "Add a doctor on the right - their login ID is shown after saving", root);
            Load += delegate { UI.Run(delegate { LoadDepartments(); LoadDoctors(); }); };
        }

        private void LoadDepartments()
        {
            deptBox.DataSource = DatabaseConnection.Query("SELECT DepartmentId, Name FROM Departments ORDER BY Name");
        }

        private void LoadDoctors()
        {
            UI.Run(delegate
            {
                grid.DataSource = DatabaseConnection.Query(
                    "SELECT u.UserId AS ID, u.FullName AS Name, d.Name AS Department, doc.Specialization, " +
                    "doc.ConsultationFee AS Fee, doc.AvailableDays AS Days, doc.Room, u.Phone " +
                    "FROM Doctors doc " +
                    "JOIN Users u ON u.UserId = doc.DoctorId " +
                    "JOIN Departments d ON d.DepartmentId = doc.DepartmentId " +
                    "ORDER BY u.FullName");
                UI.Money(grid, "Fee");
            });
        }

        private void AddDoctor(object sender, EventArgs e)
        {
            if (nameBox.Text.Trim().Length < 2) { UI.Warn("Enter the doctor's name."); return; }
            if (emailBox.Text.IndexOf('@') < 1) { UI.Warn("Enter a valid email."); return; }
            if (phoneBox.Text.Trim().Length < 7) { UI.Warn("Enter a valid phone number."); return; }
            if (deptBox.SelectedValue == null) { UI.Warn("Add a department first."); return; }
            if (specBox.Text.Trim().Length == 0) { UI.Warn("Enter the specialization."); return; }
            if (daysBox.Text.Trim().Length == 0) { UI.Warn("Enter the days the doctor is available."); return; }
            if (pwBox.Text.Length < 6) { UI.Warn("Password must be at least 6 characters."); return; }

            UI.Run(delegate
            {
                if (DatabaseConnection.Scalar("SELECT 1 FROM Users WHERE Email = @e", "@e", emailBox.Text.Trim()) != null)
                {
                    UI.Warn("Someone with this email already exists.");
                    return;
                }

                int newId;
                using (SqlConnection con = DatabaseConnection.GetConnection())
                {
                    con.Open();
                    using (SqlTransaction tx = con.BeginTransaction())
                    {
                        SqlCommand c1 = new SqlCommand(
                            "INSERT INTO Users (FullName, Email, Phone, PasswordHash, Role, Gender) OUTPUT INSERTED.UserId " +
                            "VALUES (@n, @e, @p, @h, 'Doctor', @g)", con, tx);
                        c1.Parameters.AddWithValue("@n", nameBox.Text.Trim());
                        c1.Parameters.AddWithValue("@e", emailBox.Text.Trim());
                        c1.Parameters.AddWithValue("@p", phoneBox.Text.Trim());
                        c1.Parameters.AddWithValue("@h", Security.Hash(pwBox.Text));
                        c1.Parameters.AddWithValue("@g", genderBox.SelectedItem.ToString());
                        newId = Convert.ToInt32(c1.ExecuteScalar());

                        SqlCommand c2 = new SqlCommand(
                            "INSERT INTO Doctors (DoctorId, DepartmentId, Specialization, Qualification, ConsultationFee, AvailableDays, Room) " +
                            "VALUES (@id, @d, @s, @q, @f, @days, @r)", con, tx);
                        c2.Parameters.AddWithValue("@id", newId);
                        c2.Parameters.AddWithValue("@d", Convert.ToInt32(deptBox.SelectedValue));
                        c2.Parameters.AddWithValue("@s", specBox.Text.Trim());
                        c2.Parameters.AddWithValue("@q", qualBox.Text.Trim());
                        c2.Parameters.AddWithValue("@f", feeBox.Value);
                        c2.Parameters.AddWithValue("@days", daysBox.Text.Trim());
                        c2.Parameters.AddWithValue("@r", roomBox.Text.Trim());
                        c2.ExecuteNonQuery();

                        tx.Commit();
                    }
                }

                UI.Info("Doctor added.\n\nLogin ID: " + newId + "\nPassword: the one you just typed.");
                nameBox.Clear(); emailBox.Clear(); phoneBox.Clear(); specBox.Clear();
                qualBox.Clear(); roomBox.Clear(); pwBox.Clear();
                LoadDoctors();
            });
        }

        private void DeleteDoctor(object sender, EventArgs e)
        {
            int id = UI.SelectedId(grid, "ID");
            if (id < 0) { UI.Warn("Select a doctor first."); return; }
            if (!UI.Confirm("Delete " + UI.Selected(grid, "Name") + "? This cannot be undone.")) return;

            UI.Run(delegate
            {
                try
                {
                    using (SqlConnection con = DatabaseConnection.GetConnection())
                    {
                        con.Open();
                        using (SqlTransaction tx = con.BeginTransaction())
                        {
                            SqlCommand c1 = new SqlCommand("DELETE FROM Doctors WHERE DoctorId = @id", con, tx);
                            c1.Parameters.AddWithValue("@id", id);
                            c1.ExecuteNonQuery();
                            SqlCommand c2 = new SqlCommand("DELETE FROM Users WHERE UserId = @id", con, tx);
                            c2.Parameters.AddWithValue("@id", id);
                            c2.ExecuteNonQuery();
                            tx.Commit();
                        }
                    }
                }
                catch (SqlException ex)
                {
                    if (ex.Number == 547)
                    {
                        UI.Warn("This doctor already has appointments or ratings, so they cannot be deleted.");
                        return;
                    }
                    throw;
                }
                LoadDoctors();
            });
        }

        private void ResetPassword(object sender, EventArgs e)
        {
            PasswordTools.Reset(UI.SelectedId(grid, "ID"), Convert.ToString(UI.Selected(grid, "Name")));
        }
    }
}
