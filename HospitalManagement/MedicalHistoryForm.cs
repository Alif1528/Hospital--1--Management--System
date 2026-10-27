using System;
using System.Windows.Forms;

namespace HospitalManagement
{
    /// <summary>Patient: past completed visits with the doctor's diagnosis and prescription.</summary>
    public class MedicalHistoryForm : Form
    {
        private DataGridView grid;
        private TextBox detailBox;

        public MedicalHistoryForm()
        {
            UI.Setup(this, "Medical history", 1100, 700);

            grid = UI.Grid();
            grid.SelectionChanged += delegate { ShowDetail(); };
            detailBox = UI.Txt(false, true, 170);
            detailBox.ReadOnly = true;
            detailBox.BackColor = System.Drawing.Color.White;

            UI.Compose(this, "Medical history", "Diagnosis and prescriptions from your completed visits",
                UI.ListLayout(null, grid, detailBox));
            Load += delegate { LoadHistory(); };
        }

        private void LoadHistory()
        {
            UI.Run(delegate
            {
                grid.DataSource = DatabaseConnection.Query(
                    "SELECT a.AppointmentId AS ID, CONVERT(VARCHAR(10), a.AppointmentDate, 23) AS [Date], " +
                    "du.FullName AS Doctor, dep.Name AS Department, a.Reason, a.Diagnosis, a.Prescription " +
                    "FROM Appointments a " +
                    "JOIN Users du ON du.UserId = a.DoctorId " +
                    "JOIN Doctors d ON d.DoctorId = a.DoctorId " +
                    "JOIN Departments dep ON dep.DepartmentId = d.DepartmentId " +
                    "WHERE a.PatientId = @uid AND a.Status = 'Completed' " +
                    "ORDER BY a.AppointmentDate DESC",
                    "@uid", Session.UserId);
                grid.Columns["Diagnosis"].Visible = false;
                grid.Columns["Prescription"].Visible = false;
                grid.Columns["ID"].FillWeight = 30;
                ShowDetail();
            });
        }

        private void ShowDetail()
        {
            if (grid.CurrentRow == null)
            {
                detailBox.Text = "No completed visits yet.";
                return;
            }
            detailBox.Text =
                "Visit on " + UI.Selected(grid, "Date") + " with " + UI.Selected(grid, "Doctor") + "\r\n\r\n" +
                "Diagnosis:\r\n" + UI.Selected(grid, "Diagnosis") + "\r\n\r\n" +
                "Prescription:\r\n" + UI.Selected(grid, "Prescription");
        }
    }
}
