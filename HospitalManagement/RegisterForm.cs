using System;
using System.Windows.Forms;

namespace HospitalManagement
{
    /// <summary>Self-service sign up. New accounts are always Patients.</summary>
    public class RegisterForm : Form
    {
        public int NewUserId;

        private TextBox nameBox, emailBox, phoneBox, addressBox, pwBox, pw2Box;
        private ComboBox genderBox, bloodBox;
        private DateTimePicker dobPicker;

        public RegisterForm()
        {
            UI.Setup(this, "Create account", 520, 700);

            nameBox = UI.Txt();
            emailBox = UI.Txt();
            phoneBox = UI.Txt();
            genderBox = UI.Combo("Male", "Female", "Other");
            dobPicker = new DateTimePicker();
            dobPicker.Format = DateTimePickerFormat.Short;
            dobPicker.MaxDate = DateTime.Today;
            dobPicker.Value = new DateTime(1995, 1, 1);
            bloodBox = UI.Combo("", "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-");
            addressBox = UI.Txt(false, true, 60);
            pwBox = UI.Txt(true, false, 0);
            pw2Box = UI.Txt(true, false, 0);

            TableLayoutPanel f = UI.FieldTable();
            UI.AddField(f, "Full name", nameBox);
            UI.AddField(f, "Email", emailBox);
            UI.AddField(f, "Phone", phoneBox);
            UI.AddField(f, "Gender", genderBox);
            UI.AddField(f, "Date of birth", dobPicker);
            UI.AddField(f, "Blood group", bloodBox);
            UI.AddField(f, "Address", addressBox);
            UI.AddField(f, "Password", pwBox);
            UI.AddField(f, "Confirm password", pw2Box);

            FlowLayoutPanel buttons = UI.Flow(
                UI.Btn("Create account", Save),
                UI.Btn("Cancel", delegate { DialogResult = DialogResult.Cancel; Close(); }, 1));

            UI.Compose(this, "Create patient account", "Register to book appointments online", UI.FormLayout(f, buttons));
        }

        private void Save(object sender, EventArgs e)
        {
            string name = nameBox.Text.Trim();
            string email = emailBox.Text.Trim();
            string phone = phoneBox.Text.Trim();

            if (name.Length < 2) { UI.Warn("Please enter your full name."); return; }
            if (email.IndexOf('@') < 1 || email.IndexOf('.') < 0) { UI.Warn("Please enter a valid email address."); return; }
            if (phone.Length < 7) { UI.Warn("Please enter a valid phone number."); return; }
            if (pwBox.Text.Length < 6) { UI.Warn("Password must be at least 6 characters."); return; }
            if (pwBox.Text != pw2Box.Text) { UI.Warn("The two passwords do not match."); return; }

            UI.Run(delegate
            {
                if (DatabaseConnection.Scalar("SELECT 1 FROM Users WHERE Email = @e", "@e", email) != null)
                {
                    UI.Warn("An account with this email already exists.");
                    return;
                }

                string blood = Convert.ToString(bloodBox.SelectedItem);
                if (blood.Length == 0) blood = null;

                object id = DatabaseConnection.Scalar(
                    "INSERT INTO Users (FullName, Email, Phone, PasswordHash, Role, Gender, DateOfBirth, BloodGroup, Address) " +
                    "OUTPUT INSERTED.UserId " +
                    "VALUES (@n, @e, @p, @h, 'Patient', @g, @d, @b, @a)",
                    "@n", name, "@e", email, "@p", phone, "@h", Security.Hash(pwBox.Text),
                    "@g", genderBox.SelectedItem.ToString(), "@d", dobPicker.Value.Date,
                    "@b", blood, "@a", addressBox.Text.Trim());

                NewUserId = Convert.ToInt32(id);
                UI.Info("Account created!\n\nYour login ID is " + NewUserId + ".\nPlease remember it - you need it to sign in.");
                DialogResult = DialogResult.OK;
                Close();
            });
        }
    }
}
