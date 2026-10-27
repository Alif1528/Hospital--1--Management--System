using System;
using System.Data;
using System.Windows.Forms;

namespace HospitalManagement
{
    public class LoginForm : Form
    {
        private TextBox idBox;
        private TextBox pwBox;
        private CheckBox showBox;

        public LoginForm()
        {
            UI.Setup(this, "Login", 480, 520);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            idBox = UI.Txt();
            pwBox = UI.Txt(true, false, 0);
            showBox = new CheckBox();
            showBox.Text = "Show password";
            showBox.AutoSize = true;
            showBox.CheckedChanged += delegate { pwBox.UseSystemPasswordChar = !showBox.Checked; };

            TableLayoutPanel fields = UI.FieldTable();
            UI.AddField(fields, "User ID", idBox);
            UI.AddField(fields, "Password", pwBox);
            UI.AddField(fields, "", showBox);

            Button loginBtn = UI.Btn("Login", LoginClicked);
            FlowLayoutPanel buttons = UI.Flow(
                loginBtn,
                UI.Btn("Create patient account", RegisterClicked, 1),
                UI.MutedLabel("Demo logins (ID / password):\r\nAdmin  1 / admin123\r\nDoctor  1001 / doctor123\r\nPatient  1004 / patient123"));

            AcceptButton = loginBtn;
            UI.Compose(this, "Welcome", "Sign in to the Hospital Management System", UI.FormLayout(fields, buttons));
        }

        private void LoginClicked(object sender, EventArgs e)
        {
            int id;
            if (!int.TryParse(idBox.Text.Trim(), out id))
            {
                UI.Warn("Please enter your numeric User ID.");
                return;
            }
            if (pwBox.Text.Length == 0)
            {
                UI.Warn("Please enter your password.");
                return;
            }

            UI.Run(delegate
            {
                DataRow user = DatabaseConnection.Row(
                    "SELECT FullName, Role FROM Users WHERE UserId = @id AND PasswordHash = @hash",
                    "@id", id, "@hash", Security.Hash(pwBox.Text));

                if (user == null)
                {
                    UI.Error("Invalid User ID or password.");
                    return;
                }

                Session.UserId = id;
                Session.FullName = Convert.ToString(user["FullName"]);
                Session.Role = Convert.ToString(user["Role"]);
                OpenDashboard();
            });
        }

        private void OpenDashboard()
        {
            Hide();
            DashboardForm dash = new DashboardForm();
            dash.FormClosed += delegate
            {
                if (dash.LoggedOut)
                {
                    idBox.Clear();
                    pwBox.Clear();
                    Show();
                    idBox.Focus();
                }
                else
                {
                    Close();
                }
            };
            dash.Show();
        }

        private void RegisterClicked(object sender, EventArgs e)
        {
            RegisterForm reg = new RegisterForm();
            if (reg.ShowDialog(this) == DialogResult.OK)
            {
                idBox.Text = reg.NewUserId.ToString();
                pwBox.Clear();
                pwBox.Focus();
            }
        }
    }
}
