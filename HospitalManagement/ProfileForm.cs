using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace HospitalManagement
{
    /// <summary>Everyone can edit their own details. Doctors also edit fee, days and room.</summary>
    public class ProfileForm : Form
    {
        private TextBox nameBox, emailBox, phoneBox, addressBox, specBox, qualBox, daysBox, roomBox;
        private ComboBox genderBox, bloodBox;
        private DateTimePicker dobPicker;
        private NumericUpDown feeBox;
        private CheckBox hasDob;

        public ProfileForm()
        {
            UI.Setup(this, "My profile", 560, 760);

            nameBox = UI.Txt();
            emailBox = UI.Txt();
            phoneBox = UI.Txt();
            genderBox = UI.Combo("Male", "Female", "Other");
            dobPicker = new DateTimePicker();
            dobPicker.Format = DateTimePickerFormat.Short;
            dobPicker.MaxDate = DateTime.Today;
            hasDob = new CheckBox();
            hasDob.Text = "Date of birth is set";
            hasDob.AutoSize = true;
            hasDob.CheckedChanged += delegate { dobPicker.Enabled = hasDob.Checked; };
            bloodBox = UI.Combo("", "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-");
            addressBox = UI.Txt(false, true, 60);

            TableLayoutPanel f = UI.FieldTable();
            UI.AddField(f, "Full name", nameBox);
            UI.AddField(f, "Email", emailBox);
            UI.AddField(f, "Phone", phoneBox);
            UI.AddField(f, "Gender", genderBox);
            UI.AddField(f, "", hasDob);
            UI.AddField(f, "Date of birth", dobPicker);
            UI.AddField(f, "Blood group", bloodBox);
            UI.AddField(f, "Address", addressBox);

            if (Session.Role == "Doctor")
            {
                specBox = UI.Txt();
                qualBox = UI.Txt();
                feeBox = new NumericUpDown();
                feeBox.Maximum = 100000;
                feeBox.Increment = 100;
                daysBox = UI.Txt();
                roomBox = UI.Txt();
                UI.AddField(f, "Specialization", specBox);
                UI.AddField(f, "Qualification", qualBox);
                UI.AddField(f, "Fee (Tk)", feeBox);
                UI.AddField(f, "Days (Sat,Sun..)", daysBox);
                UI.AddField(f, "Room", roomBox);
            }

            FlowLayoutPanel buttons = UI.Flow(
                UI.Btn("Save changes", Save),
                UI.Btn("Close", delegate { Close(); }, 1));

            UI.Compose(this, "My profile", "Login ID " + Session.UserId + "  |  " + Session.Role, UI.FormLayout(f, buttons));
            Load += delegate { LoadProfile(); };
        }

        private void LoadProfile()
        {
            UI.Run(delegate
            {
                DataRow u = DatabaseConnection.Row(
                    "SELECT FullName, Email, Phone, Gender, DateOfBirth, BloodGroup, Address FROM Users WHERE UserId = @id",
                    "@id", Session.UserId);
                nameBox.Text = Convert.ToString(u["FullName"]);
                emailBox.Text = Convert.ToString(u["Email"]);
                phoneBox.Text = Convert.ToString(u["Phone"]);
                string g = Convert.ToString(u["Gender"]);
                if (genderBox.Items.Contains(g)) genderBox.SelectedItem = g;
                hasDob.Checked = u["DateOfBirth"] != DBNull.Value;
                if (hasDob.Checked) dobPicker.Value = Convert.ToDateTime(u["DateOfBirth"]);
                dobPicker.Enabled = hasDob.Checked;
                string b = Convert.ToString(u["BloodGroup"]);
                bloodBox.SelectedItem = bloodBox.Items.Contains(b) ? b : "";
                addressBox.Text = Convert.ToString(u["Address"]);

                if (Session.Role == "Doctor")
                {
                    DataRow d = DatabaseConnection.Row(
                        "SELECT Specialization, Qualification, ConsultationFee, AvailableDays, Room FROM Doctors WHERE DoctorId = @id",
                        "@id", Session.UserId);
                    specBox.Text = Convert.ToString(d["Specialization"]);
                    qualBox.Text = Convert.ToString(d["Qualification"]);
                    feeBox.Value = Convert.ToDecimal(d["ConsultationFee"]);
                    daysBox.Text = Convert.ToString(d["AvailableDays"]);
                    roomBox.Text = Convert.ToString(d["Room"]);
                }
            });
        }

        private void Save(object sender, EventArgs e)
        {
            if (nameBox.Text.Trim().Length < 2) { UI.Warn("Please enter your name."); return; }
            if (emailBox.Text.IndexOf('@') < 1) { UI.Warn("Please enter a valid email."); return; }
            if (phoneBox.Text.Trim().Length < 7) { UI.Warn("Please enter a valid phone number."); return; }
            if (Session.Role == "Doctor" && (specBox.Text.Trim().Length == 0 || daysBox.Text.Trim().Length == 0))
            {
                UI.Warn("Specialization and available days are required.");
                return;
            }

            UI.Run(delegate
            {
                if (DatabaseConnection.Scalar("SELECT 1 FROM Users WHERE Email = @e AND UserId <> @id",
                    "@e", emailBox.Text.Trim(), "@id", Session.UserId) != null)
                {
                    UI.Warn("Another account already uses this email.");
                    return;
                }

                object dob = hasDob.Checked ? (object)dobPicker.Value.Date : null;
                string blood = Convert.ToString(bloodBox.SelectedItem);
                if (blood.Length == 0) blood = null;

                DatabaseConnection.Execute(
                    "UPDATE Users SET FullName = @n, Email = @e, Phone = @p, Gender = @g, DateOfBirth = @d, " +
                    "BloodGroup = @b, Address = @a WHERE UserId = @id",
                    "@n", nameBox.Text.Trim(), "@e", emailBox.Text.Trim(), "@p", phoneBox.Text.Trim(),
                    "@g", genderBox.SelectedItem.ToString(), "@d", dob, "@b", blood,
                    "@a", addressBox.Text.Trim(), "@id", Session.UserId);

                if (Session.Role == "Doctor")
                {
                    DatabaseConnection.Execute(
                        "UPDATE Doctors SET Specialization = @s, Qualification = @q, ConsultationFee = @f, " +
                        "AvailableDays = @days, Room = @r WHERE DoctorId = @id",
                        "@s", specBox.Text.Trim(), "@q", qualBox.Text.Trim(), "@f", feeBox.Value,
                        "@days", daysBox.Text.Trim(), "@r", roomBox.Text.Trim(), "@id", Session.UserId);
                }

                Session.FullName = nameBox.Text.Trim();
                UI.Info("Profile updated.");
            });
        }
    }
}
