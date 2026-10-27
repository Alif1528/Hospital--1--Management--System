using System;
using System.Windows.Forms;

namespace HospitalManagement
{
    public class ChangePasswordForm : Form
    {
        private TextBox oldBox, newBox, confirmBox;

        public ChangePasswordForm()
        {
            UI.Setup(this, "Change password", 520, 420);

            oldBox = UI.Txt(true, false, 0);
            newBox = UI.Txt(true, false, 0);
            confirmBox = UI.Txt(true, false, 0);

            TableLayoutPanel f = UI.FieldTable();
            UI.AddField(f, "Current password", oldBox);
            UI.AddField(f, "New password", newBox);
            UI.AddField(f, "Confirm new", confirmBox);

            FlowLayoutPanel buttons = UI.Flow(
                UI.Btn("Update password", Save),
                UI.Btn("Close", delegate { Close(); }, 1));

            UI.Compose(this, "Change password", "Use at least 6 characters", UI.FormLayout(f, buttons));
        }

        private void Save(object sender, EventArgs e)
        {
            if (oldBox.Text.Length == 0) { UI.Warn("Enter your current password."); return; }
            if (newBox.Text.Length < 6) { UI.Warn("The new password must be at least 6 characters."); return; }
            if (newBox.Text != confirmBox.Text) { UI.Warn("The new passwords do not match."); return; }

            UI.Run(delegate
            {
                int n = DatabaseConnection.Execute(
                    "UPDATE Users SET PasswordHash = @new WHERE UserId = @id AND PasswordHash = @old",
                    "@new", Security.Hash(newBox.Text), "@id", Session.UserId, "@old", Security.Hash(oldBox.Text));
                if (n == 0)
                {
                    UI.Error("Your current password is not correct.");
                    return;
                }
                UI.Info("Password updated.");
                Close();
            });
        }
    }
}
