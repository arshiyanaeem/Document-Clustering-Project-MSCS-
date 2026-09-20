using DSFS.App.Session;
using DSFS.Core.Models;

namespace DSFS.App.Forms;

/// <summary>
/// Entry point of the User Interface (Figure 7): authenticates either the Admin
/// or a registered Scholar and routes to the matching dashboard.
/// </summary>
public class LoginForm : Form
{
    private readonly TextBox _txtUsername = new();
    private readonly TextBox _txtPassword = new() { PasswordChar = '\u25CF' };
    private readonly Label _lblStatus = new() { ForeColor = Color.Firebrick, AutoSize = true };

    public LoginForm()
    {
        Text = "DSFS - Desktop Scholar Facilitator System - Sign In";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(420, 320);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        BuildUi();
    }

    private void BuildUi()
    {
        var lblTitle = new Label
        {
            Text = "Desktop Scholar Facilitator System",
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(20, 20),
            Size = new Size(380, 50)
        };

        var lblSubtitle = new Label
        {
            Text = "Hierarchical document clustering using an improved agglomerative algorithm",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.DimGray,
            Location = new Point(20, 68),
            Size = new Size(380, 40)
        };

        var lblUser = new Label { Text = "Username", Location = new Point(30, 125), AutoSize = true };
        _txtUsername.Location = new Point(30, 145);
        _txtUsername.Size = new Size(360, 24);

        var lblPass = new Label { Text = "Password", Location = new Point(30, 178), AutoSize = true };
        _txtPassword.Location = new Point(30, 198);
        _txtPassword.Size = new Size(360, 24);

        var btnLogin = new Button
        {
            Text = "Sign In",
            Location = new Point(30, 235),
            Size = new Size(170, 34),
            BackColor = Color.FromArgb(0, 120, 212),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnLogin.Click += BtnLogin_Click;

        var btnRegister = new Button
        {
            Text = "Register as Scholar",
            Location = new Point(220, 235),
            Size = new Size(170, 34)
        };
        btnRegister.Click += (_, _) =>
        {
            using var registerForm = new RegisterForm();
            registerForm.ShowDialog(this);
        };

        _lblStatus.Location = new Point(30, 278);
        _lblStatus.MaximumSize = new Size(360, 30);

        var lblHint = new Label
        {
            Text = "Default admin login: admin / Admin@123",
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 8f, FontStyle.Italic),
            AutoSize = true,
            Location = new Point(30, 300)
        };

        Controls.AddRange(new Control[]
        {
            lblTitle, lblSubtitle, lblUser, _txtUsername, lblPass, _txtPassword,
            btnLogin, btnRegister, _lblStatus, lblHint
        });

        AcceptButton = btnLogin;
    }

    private void BtnLogin_Click(object? sender, EventArgs e)
    {
        _lblStatus.Text = string.Empty;
        var user = SessionContext.Users.Authenticate(_txtUsername.Text.Trim(), _txtPassword.Text);
        if (user is null)
        {
            _lblStatus.Text = "Invalid username or password.";
            return;
        }

        SessionContext.CurrentUser = user;
        Hide();

        Form nextForm = user.Role == UserRole.Admin
            ? new AdminDashboardForm()
            : new ScholarProfileForm();

        nextForm.FormClosed += (_, _) =>
        {
            SessionContext.SignOut();
            _txtPassword.Clear();
            Show();
        };
        nextForm.Show();
    }
}
