using System;
using System.Windows.Forms;

namespace HospitalManagement
{
    /// <summary>
    /// One list for everybody: admins see all appointments, doctors see their own,
    /// patients see theirs. The buttons shown depend on the role.
    /// </summary>
    public class AppointmentsForm : Form
    {
        private DataGridView grid;
        private ComboBox statusBox;
        private ComboBox whenBox;

        public AppointmentsForm()
        {
            UI.Setup(this, Session.Role == "Admin" ? "All appointments" : "My appointments", 1180, 660);

            grid = UI.Grid();
            statusBox = UI.Combo("All", "Pending", "Confirmed", "Completed", "Cancelled");
            whenBox = UI.Combo("All dates", "Today", "Upcoming");
            statusBox.SelectedIndexChanged += delegate { LoadAppointments(); };
            whenBox.SelectedIndexChanged += delegate { LoadAppointments(); };

            FlowLayoutPanel toolbar = UI.Flow(
                UI.Lbl("Status:"), statusBox,
                UI.Lbl("When:"), whenBox,
                UI.Btn("Refresh", delegate { LoadAppointments(); }, 1));

            FlowLayoutPanel footer = UI.Flow(UI.Btn("View details", ShowDetails, 1));
            if (Session.Role == "Admin" || Session.Role == "Doctor")
                footer.Controls.Add(UI.Btn("Confirm", Confirm));
            if (Session.Role == "Doctor")
                footer.Controls.Add(UI.Btn("Complete visit...", Complete));
            footer.Controls.Add(UI.Btn("Cancel appointment", CancelAppointment, 2));

            string subtitle = Session.Role == "Admin" ? "Every appointment in the hospital"
                : Session.Role == "Doctor" ? "Patients booked with you"
                : "Your booked visits";
            UI.Compose(this, "Appointments", subtitle, UI.ListLayout(toolbar, grid, footer));
            Load += delegate { LoadAppointments(); };
        }

        private void LoadAppointments()
        {
            UI.Run(delegate
            {
                grid.DataSource = DatabaseConnection.Query(
                    "SELECT a.AppointmentId AS ID, CONVERT(VARCHAR(10), a.AppointmentDate, 23) AS [Date], " +
                    "a.TimeSlot AS [Time], p.FullName AS Patient, du.FullName AS Doctor, dep.Name AS Department, " +
                    "a.Status, a.Reason " +
                    "FROM Appointments a " +
                    "JOIN Users p ON p.UserId = a.PatientId " +
                    "JOIN Users du ON du.UserId = a.DoctorId " +
                    "JOIN Doctors d ON d.DoctorId = a.DoctorId " +
                    "JOIN Departments dep ON dep.DepartmentId = d.DepartmentId " +
                    "WHERE (@status = 'All' OR a.Status = @status) " +
                    "AND (@when = 'All dates' " +
                    "     OR (@when = 'Today' AND a.AppointmentDate = CAST(GETDATE() AS DATE)) " +
                    "     OR (@when = 'Upcoming' AND a.AppointmentDate >= CAST(GETDATE() AS DATE))) " +
                    "AND (@role = 'Admin' " +
                    "     OR (@role = 'Doctor' AND a.DoctorId = @uid) " +
                    "     OR (@role = 'Patient' AND a.PatientId = @uid)) " +
                    "ORDER BY a.AppointmentDate DESC, a.TimeSlot",
                    "@status", statusBox.SelectedItem.ToString(),
                    "@when", whenBox.SelectedItem.ToString(),
                    "@role", Session.Role,
                    "@uid", Session.UserId);

                if (Session.Role == "Patient") grid.Columns["Patient"].Visible = false;
                if (Session.Role == "Doctor") grid.Columns["Doctor"].Visible = false;
                grid.Columns["ID"].FillWeight = 40;
                grid.Columns["Time"].FillWeight = 50;
                grid.Columns["Reason"].FillWeight = 140;
            });
        }

        private bool PickAppointment(out int id, out string status)
        {
            id = UI.SelectedId(grid, "ID");
            status = Convert.ToString(UI.Selected(grid, "Status"));
            if (id < 0)
            {
                UI.Warn("Select an appointment first.");
                return false;
            }
            return true;
        }

        private void ShowDetails(object sender, EventArgs e)
        {
            int id;
            string status;
            if (!PickAppointment(out id, out status)) return;

            UI.Run(delegate
            {
                System.Data.DataRow r = DatabaseConnection.Row(
                    "SELECT a.Reason, a.Diagnosis, a.Prescription, p.FullName AS Patient, du.FullName AS Doctor " +
                    "FROM Appointments a JOIN Users p ON p.UserId = a.PatientId JOIN Users du ON du.UserId = a.DoctorId " +
                    "WHERE a.AppointmentId = @id AND (@role = 'Admin' OR a.DoctorId = @uid OR a.PatientId = @uid)",
                    "@id", id, "@role", Session.Role, "@uid", Session.UserId);
                if (r == null) return;

                UI.Info("Patient: " + r["Patient"] + "\nDoctor: " + r["Doctor"] + "\nStatus: " + status +
                    "\n\nReason:\n" + Show(r["Reason"]) +
                    "\n\nDiagnosis:\n" + Show(r["Diagnosis"]) +
                    "\n\nPrescription:\n" + Show(r["Prescription"]));
            });
        }

        private static string Show(object value)
        {
            string s = Convert.ToString(value);
            return s.Length == 0 ? "-" : s;
        }

        private void Confirm(object sender, EventArgs e)
        {
            int id;
            string status;
            if (!PickAppointment(out id, out status)) return;
            if (status != "Pending") { UI.Warn("Only pending appointments can be confirmed."); return; }

            UI.Run(delegate
            {
                int n = DatabaseConnection.Execute(
                    "UPDATE Appointments SET Status = 'Confirmed' " +
                    "WHERE AppointmentId = @id AND Status = 'Pending' AND (@role = 'Admin' OR DoctorId = @uid)",
                    "@id", id, "@role", Session.Role, "@uid", Session.UserId);
                if (n == 0) UI.Warn("This appointment could not be confirmed.");
                LoadAppointments();
            });
        }

        private void Complete(object sender, EventArgs e)
        {
            int id;
            string status;
            if (!PickAppointment(out id, out status)) return;
            if (status != "Pending" && status != "Confirmed")
            {
                UI.Warn("Only pending or confirmed appointments can be completed.");
                return;
            }

            ConsultationForm form = new ConsultationForm(id,
                Convert.ToString(UI.Selected(grid, "Patient")),
                Convert.ToString(UI.Selected(grid, "Reason")));
            form.ShowDialog(this);
            form.Dispose();
            LoadAppointments();
        }

        private void CancelAppointment(object sender, EventArgs e)
        {
            int id;
            string status;
            if (!PickAppointment(out id, out status)) return;
            if (status != "Pending" && status != "Confirmed")
            {
                UI.Warn("Only pending or confirmed appointments can be cancelled.");
                return;
            }
            if (!UI.Confirm("Cancel this appointment?")) return;

            UI.Run(delegate
            {
                int n = DatabaseConnection.Execute(
                    "UPDATE Appointments SET Status = 'Cancelled' " +
                    "WHERE AppointmentId = @id AND Status IN ('Pending','Confirmed') " +
                    "AND (@role = 'Admin' OR DoctorId = @uid OR PatientId = @uid)",
                    "@id", id, "@role", Session.Role, "@uid", Session.UserId);
                if (n == 0) UI.Warn("This appointment could not be cancelled.");
                LoadAppointments();
            });
        }
    }
}
