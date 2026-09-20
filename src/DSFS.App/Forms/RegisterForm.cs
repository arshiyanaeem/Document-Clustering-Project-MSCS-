using DSFS.App.Session;

namespace DSFS.App.Forms;

/// <summary>Registration Module (section 5.4.2.1): lets a new research scholar create an account.</summary>
public class RegisterForm : Form
{
    private readonly TextBox _txtFullName = new();
    private readonly TextBox _txtUsername = new();
    private readonly TextBox _txtEmail = new();
    private readonly TextBox _txtDomain = new();
    private readonly TextBox _txtPassword = new() { PasswordChar = '\u25CF' };
    private readonly TextBox _txtConfirm = new() { PasswordChar = '\u25CF' };
    private readonly Label _lblStatus = new() { ForeColor = Color.Firebrick, AutoSize = false, MaximumSize = new Size(340, 40) };

    public RegisterForm()
    {
        Text = "Register New Scholar";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(400, 430);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        BuildUi();
    }

    private void BuildUi()
    {
        int y = 20;
        Control AddField(string label, TextBox box)
        {
            var lbl = new Label { Text = label, Location = new Point(20, y), AutoSize = true };
            box.Location = new Point(20, y + 20);
            box.Size = new Size(340, 24);
            Controls.Add(lbl);
            Controls.Add(box);
            y += 55;
            return box;
        }

        AddField("Full Name", _txtFullName);
        AddField("Username", _txtUsername);
        AddField("Email", _txtEmail);
        AddField("Research Domain (e.g. Data Mining, Networks)", _txtDomain);
        AddField("Password", _txtPassword);
        AddField("Confirm Password", _txtConfirm);

        var btnRegister = new Button
        {
            Text = "Create Account",
            Location = new Point(20, y + 5),
            Size = new Size(160, 34),
            BackColor = Color.FromArgb(0, 120, 212),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnRegister.Click += BtnRegister_Click;

        var btnCancel = new Button
        {
            Text = "Cancel",
            Location = new Point(200, y + 5),
            Size = new Size(160, 34)
        };
        btnCancel.Click += (_, _) => Close();

        _lblStatus.Location = new Point(20, y + 45);

        Controls.Add(btnRegister);
        Controls.Add(btnCancel);
        Controls.Add(_lblStatus);

        AcceptButton = btnRegister;
        CancelButton = btnCancel;
    }

    private void BtnRegister_Click(object? sender, EventArgs e)
    {
        _lblStatus.ForeColor = Color.Firebrick;

        if (string.IsNullOrWhiteSpace(_txtFullName.Text) || string.IsNullOrWhiteSpace(_txtUsername.Text) ||
            string.IsNullOrWhiteSpace(_txtPassword.Text))
        {
            _lblStatus.Text = "Full name, username and password are required.";
            return;
        }

        if (_txtPassword.Text != _txtConfirm.Text)
        {
            _lblStatus.Text = "Passwords do not match.";
            return;
        }

        if (SessionContext.Users.UsernameExists(_txtUsername.Text.Trim()))
        {
            _lblStatus.Text = "That username is already taken.";
            return;
        }

        SessionContext.Users.Register(
            _txtUsername.Text.Trim(),
            _txtFullName.Text.Trim(),
            _txtEmail.Text.Trim(),
            _txtDomain.Text.Trim(),
            _txtPassword.Text);

        _lblStatus.ForeColor = Color.SeaGreen;
        _lblStatus.Text = "Account created successfully. You can now sign in.";
        _txtPassword.Clear();
        _txtConfirm.Clear();
    }
}
