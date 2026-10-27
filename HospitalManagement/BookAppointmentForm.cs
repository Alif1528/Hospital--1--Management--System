using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Windows.Forms;

namespace HospitalManagement
{
    /// <summary>Patient picks a department, doctor, date and a free time slot.</summary>
    public class BookAppointmentForm : Form
    {
        private ComboBox deptBox, doctorBox, slotBox;
        private DateTimePicker datePicker;
        private TextBox reasonBox;
        private Label infoLabel;
        private bool loading;

        private static readonly string[] AllSlots = new string[]
        {
            "09:00", "09:30", "10:00", "10:30", "11:00", "11:30", "12:00", "12:30",
            "14:00", "14:30", "15:00", "15:30", "16:00", "16:30", "17:00", "17:30"
        };

        public BookAppointmentForm()
        {
            UI.Setup(this, "Book appointment", 620, 640);

            deptBox = UI.Combo();
            deptBox.DisplayMember = "Name";
            deptBox.ValueMember = "DepartmentId";
            doctorBox = UI.Combo();
            doctorBox.DisplayMember = "Label";
            doctorBox.ValueMember = "DoctorId";
            slotBox = UI.Combo();
            datePicker = new DateTimePicker();
            datePicker.Format = DateTimePickerFormat.Long;
            datePicker.MinDate = DateTime.Today;
            datePicker.MaxDate = DateTime.Today.AddDays(60);
            reasonBox = UI.Txt(false, true, 80);
            infoLabel = UI.MutedLabel("");

            deptBox.SelectedIndexChanged += delegate { LoadDoctors(); };
            doctorBox.SelectedIndexChanged += delegate { LoadSlots(); };
            datePicker.ValueChanged += delegate { LoadSlots(); };

            TableLayoutPanel f = UI.FieldTable();
            UI.AddField(f, "Department", deptBox);
            UI.AddField(f, "Doctor", doctorBox);
            UI.AddField(f, "", infoLabel);
            UI.AddField(f, "Date", datePicker);
            UI.AddField(f, "Time slot", slotBox);
            UI.AddField(f, "Reason", reasonBox);

            FlowLayoutPanel buttons = UI.Flow(
                UI.Btn("Book appointment", Book),
                UI.Btn("Close", delegate { Close(); }, 1));

            UI.Compose(this, "Book an appointment", "Choose a doctor and a free time", UI.FormLayout(f, buttons));
            Load += delegate { LoadDepartments(); };
        }

        private void LoadDepartments()
        {
            UI.Run(delegate
            {
                deptBox.DataSource = DatabaseConnection.Query(
                    "SELECT DepartmentId, Name FROM Departments ORDER BY Name");
                LoadDoctors();
            });
        }

        private void LoadDoctors()
        {
            if (deptBox.SelectedValue == null || deptBox.SelectedValue is DataRowView) return;
            UI.Run(delegate
            {
                loading = true;
                doctorBox.DataSource = DatabaseConnection.Query(
                    "SELECT doc.DoctorId, " +
                    "u.FullName + ' - ' + doc.Specialization + '  (Tk ' + CAST(CAST(doc.ConsultationFee AS INT) AS VARCHAR(10)) + ')' AS Label, " +
                    "doc.AvailableDays, doc.Room " +
                    "FROM Doctors doc JOIN Users u ON u.UserId = doc.DoctorId " +
                    "WHERE doc.DepartmentId = @d ORDER BY u.FullName",
                    "@d", Convert.ToInt32(deptBox.SelectedValue));
                loading = false;
                LoadSlots();
            });
        }

        private int CurrentDoctorId()
        {
            object v = doctorBox.SelectedValue;
            if (v == null || v is DataRowView) return -1;
            return Convert.ToInt32(v);
        }

        private void LoadSlots()
        {
            if (loading) return;
            slotBox.Items.Clear();

            int doctorId = CurrentDoctorId();
            DataRowView doctor = doctorBox.SelectedItem as DataRowView;
            if (doctorId < 0 || doctor == null)
            {
                infoLabel.Text = "No doctors in this department yet.";
                return;
            }

            DateTime date = datePicker.Value.Date;
            string day = date.ToString("ddd", CultureInfo.InvariantCulture);
            string room = Convert.ToString(doctor["Room"]);
            string days = Convert.ToString(doctor["AvailableDays"]);
            infoLabel.Text = "Sees patients: " + days + (room.Length > 0 ? "   |   Room " + room : "");

            bool worksThatDay = false;
            foreach (string d in days.Split(','))
            {
                if (string.Equals(d.Trim(), day, StringComparison.OrdinalIgnoreCase)) worksThatDay = true;
            }
            if (!worksThatDay)
            {
                infoLabel.Text += "\r\nNot available on " + day + " - please pick another date.";
                return;
            }

            UI.Run(delegate
            {
                HashSet<string> taken = new HashSet<string>();
                DataTable booked = DatabaseConnection.Query(
                    "SELECT TimeSlot FROM Appointments WHERE DoctorId = @d AND AppointmentDate = @dt AND Status <> 'Cancelled'",
                    "@d", doctorId, "@dt", date);
                foreach (DataRow r in booked.Rows) taken.Add(Convert.ToString(r["TimeSlot"]).Trim());

                string now = DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture);
                foreach (string slot in AllSlots)
                {
                    if (taken.Contains(slot)) continue;
                    if (date == DateTime.Today && string.CompareOrdinal(slot, now) <= 0) continue;
                    slotBox.Items.Add(slot);
                }
                if (slotBox.Items.Count > 0) slotBox.SelectedIndex = 0;
                else infoLabel.Text += "\r\nNo free slots on this date.";
            });
        }

        private void Book(object sender, EventArgs e)
        {
            int doctorId = CurrentDoctorId();
            if (doctorId < 0) { UI.Warn("Please choose a doctor."); return; }
            if (slotBox.SelectedItem == null) { UI.Warn("Please choose a free time slot."); return; }
            if (reasonBox.Text.Trim().Length == 0) { UI.Warn("Please tell the doctor the reason for your visit."); return; }

            DateTime date = datePicker.Value.Date;
            string slot = slotBox.SelectedItem.ToString();

            UI.Run(delegate
            {
                if (DatabaseConnection.Scalar(
                    "SELECT 1 FROM Appointments WHERE PatientId = @p AND AppointmentDate = @d AND TimeSlot = @t AND Status <> 'Cancelled'",
                    "@p", Session.UserId, "@d", date, "@t", slot) != null)
                {
                    UI.Warn("You already have an appointment at this date and time.");
                    return;
                }

                try
                {
                    DatabaseConnection.Execute(
                        "INSERT INTO Appointments (PatientId, DoctorId, AppointmentDate, TimeSlot, Reason) VALUES (@p, @doc, @d, @t, @r)",
                        "@p", Session.UserId, "@doc", doctorId, "@d", date, "@t", slot, "@r", reasonBox.Text.Trim());
                }
                catch (SqlException ex)
                {
                    if (ex.Number == 2601 || ex.Number == 2627)
                    {
                        UI.Warn("Someone just booked that slot. Please pick another one.");
                        LoadSlots();
                        return;
                    }
                    throw;
                }

                UI.Info("Appointment booked for " + date.ToString("dddd, d MMMM yyyy", CultureInfo.InvariantCulture) + " at " + slot +
                    ".\nStatus: Pending - the doctor will confirm it.");
                reasonBox.Clear();
                LoadSlots();
            });
        }
    }
}
