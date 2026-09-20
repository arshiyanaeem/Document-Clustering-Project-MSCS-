using DSFS.App.Session;

namespace DSFS.App.Forms;

/// <summary>User Profile Module (section 5.4.2.2): a scholar's own profile and the
/// clustering outcome for their publications, once the admin has run the pipeline.</summary>
public class ScholarProfileForm : Form
{
    private readonly TextBox _txtFullName = new();
    private readonly TextBox _txtEmail = new();
    private readonly TextBox _txtDomain = new();
    private readonly DataGridView _grid = new();
    private readonly Label _lblStatus = new() { ForeColor = Color.SeaGreen, AutoSize = true };

    public ScholarProfileForm()
    {
        Text = $"DSFS - Scholar Profile - {SessionContext.CurrentUser?.FullName}";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(760, 520);
        MinimumSize = new Size(650, 420);

        BuildMenu();
        BuildUi();
        LoadProfile();
        LoadDocuments();
    }

    private void BuildMenu()
    {
        var menu = new MenuStrip();
        var fileMenu = new ToolStripMenuItem("File");
        var logout = new ToolStripMenuItem("Log Out", null, (_, _) => Close());
        var exit = new ToolStripMenuItem("Exit", null, (_, _) => Application.Exit());
        fileMenu.DropDownItems.Add(logout);
        fileMenu.DropDownItems.Add(exit);
        menu.Items.Add(fileMenu);
        MainMenuStrip = menu;
        Controls.Add(menu);
    }

    private void BuildUi()
    {
        var groupProfile = new GroupBox { Text = "My Profile", Location = new Point(15, 32), Size = new Size(720, 170) };

        var lblUsername = new Label { Text = $"Username: {SessionContext.CurrentUser?.Username}", Location = new Point(15, 25), AutoSize = true };

        var lblName = new Label { Text = "Full Name", Location = new Point(15, 55), AutoSize = true };
        _txtFullName.Location = new Point(15, 75);
        _txtFullName.Size = new Size(320, 24);

        var lblEmail = new Label { Text = "Email", Location = new Point(360, 55), AutoSize = true };
        _txtEmail.Location = new Point(360, 75);
        _txtEmail.Size = new Size(320, 24);

        var lblDomain = new Label { Text = "Research Domain", Location = new Point(15, 105), AutoSize = true };
        _txtDomain.Location = new Point(15, 125);
        _txtDomain.Size = new Size(320, 24);

        var btnSave = new Button { Text = "Save Profile", Location = new Point(360, 124), Size = new Size(140, 28) };
        btnSave.Click += BtnSave_Click;

        _lblStatus.Location = new Point(510, 130);

        groupProfile.Controls.AddRange(new Control[] { lblUsername, lblName, _txtFullName, lblEmail, _txtEmail, lblDomain, _txtDomain, btnSave, _lblStatus });

        var groupDocs = new GroupBox { Text = "My Publications & Cluster Assignment", Location = new Point(15, 212), Size = new Size(720, 275), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom };

        _grid.Location = new Point(10, 25);
        _grid.Size = new Size(700, 235);
        _grid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.Columns.Add("FileName", "File");
        _grid.Columns.Add("Domain", "Domain");
        _grid.Columns.Add("Cluster", "Cluster");

        groupDocs.Controls.Add(_grid);

        Controls.Add(groupProfile);
        Controls.Add(groupDocs);
    }

    private void LoadProfile()
    {
        var user = SessionContext.CurrentUser!;
        _txtFullName.Text = user.FullName;
        _txtEmail.Text = user.Email;
        _txtDomain.Text = user.ResearchDomain;
    }

    private void LoadDocuments()
    {
        _grid.Rows.Clear();
        var user = SessionContext.CurrentUser!;
        var myDocs = SessionContext.Documents.GetAll()
            .Where(d => d.ScholarName.Equals(user.FullName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var doc in myDocs)
        {
            string clusterText = doc.ClusterId >= 0 ? $"Cluster {doc.ClusterId + 1}" : "Not clustered yet";
            _grid.Rows.Add(doc.FileName, doc.Domain, clusterText);
        }

        if (myDocs.Count == 0)
            _grid.Rows.Add("(no publications on file yet - ask the admin to add them)", "", "");
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        var user = SessionContext.CurrentUser!;
        user.FullName = _txtFullName.Text.Trim();
        user.Email = _txtEmail.Text.Trim();
        user.ResearchDomain = _txtDomain.Text.Trim();
        SessionContext.Users.Update(user);
        _lblStatus.Text = "Saved.";
        Text = $"DSFS - Scholar Profile - {user.FullName}";
        LoadDocuments();
    }
}
