using DSFS.App.Controls;
using DSFS.App.Session;
using DSFS.Core.Clustering;
using DSFS.Core.Evaluation;
using DSFS.Core.Models;
using DSFS.Core.Preprocessing;
using DSFS.Core.Similarity;
using DSFS.Core.TextExtraction;

namespace DSFS.App.Forms;

/// <summary>
/// The Admin Interface (section 5.2.1 / 5.4.1): a tabbed workspace that walks the admin
/// through the same four modules shown in Figure 7 - Data Collection, Preprocessing,
/// Clustering, and Similarity Measure - finishing with the Results Generation module.
/// </summary>
public class AdminDashboardForm : Form
{
    private readonly List<DocumentRecord> _documents;
    private readonly TextPreprocessor _preprocessor = new();
    private ClusterNode? _dendrogramRoot;

    // --- Tab 1: Data Collection -------------------------------------------------
    private readonly DataGridView _gridDocuments = new();

    // --- Tab 2: Preprocessing ----------------------------------------------------
    private readonly DataGridView _gridPreprocessing = new();
    private readonly TextBox _txtPreprocessLog = new();

    // --- Tab 3: Clustering --------------------------------------------------------
    private readonly ComboBox _cmbLinkage = new();
    private readonly NumericUpDown _numChainGuard = new();
    private readonly NumericUpDown _numClusterCount = new();
    private readonly DendrogramControl _dendrogramControl = new();
    private readonly DataGridView _gridClusters = new();

    // --- Tab 4: Similarity ---------------------------------------------------------
    private readonly DataGridView _gridSimilarity = new();
    private readonly Label _lblClosestPair = new() { AutoSize = true, ForeColor = Color.SeaGreen };

    // --- Tab 5: Evaluation / Results ------------------------------------------------
    private readonly BarChartControl _chart = new() { Title = "Evaluation Results (Precision / Recall / F-Measure)" };
    private readonly Label _lblCounts = new() { AutoSize = true, Font = new Font("Segoe UI", 10f) };

    public AdminDashboardForm()
    {
        _documents = SessionContext.Documents.GetAll();

        Text = "DSFS - Admin Interface";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(980, 680);
        MinimumSize = new Size(860, 560);

        BuildMenu();

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildDataCollectionTab());
        tabs.TabPages.Add(BuildPreprocessingTab());
        tabs.TabPages.Add(BuildClusteringTab());
        tabs.TabPages.Add(BuildSimilarityTab());
        tabs.TabPages.Add(BuildEvaluationTab());
        Controls.Add(tabs);
        tabs.BringToFront();

        RefreshDocumentsGrid();
    }

    // ============================================================================
    // Menu
    // ============================================================================
    private void BuildMenu()
    {
        var menu = new MenuStrip();

        var fileMenu = new ToolStripMenuItem("File");
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("Save Documents to Database", null, (_, _) => SessionContext.Documents.Save()));
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("Log Out", null, (_, _) => Close()));
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("Exit", null, (_, _) => Application.Exit()));

        var usersMenu = new ToolStripMenuItem("Scholars");
        usersMenu.DropDownItems.Add(new ToolStripMenuItem("Manage Registered Scholars", null, (_, _) => ShowScholarList()));

        var helpMenu = new ToolStripMenuItem("Help");
        helpMenu.DropDownItems.Add(new ToolStripMenuItem("About DSFS", null, (_, _) => new AboutForm().ShowDialog(this)));

        menu.Items.Add(fileMenu);
        menu.Items.Add(usersMenu);
        menu.Items.Add(helpMenu);

        MainMenuStrip = menu;
        Controls.Add(menu);
    }

    private void ShowScholarList()
    {
        var users = SessionContext.Users.GetAll()
            .Where(u => u.Role == UserRole.Scholar)
            .Select(u => $"{u.FullName}  <{u.Username}>  -  {u.ResearchDomain}")
            .ToArray();

        MessageBox.Show(
            users.Length == 0 ? "No scholars have registered yet." : string.Join(Environment.NewLine, users),
            "Registered Scholars", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // ============================================================================
    // Tab 1: Data Collection Module (Figure 8)
    // ============================================================================
    private TabPage BuildDataCollectionTab()
    {
        var page = new TabPage("1. Data Collection");

        var lblInfo = new Label
        {
            Text = "Import research publications (.pdf, .docx, .txt), then fill in the Scholar and Domain for each one - " +
                   "this is the ground truth the Evaluation tab will compare the clustering result against.",
            Dock = DockStyle.Top,
            Height = 40,
            Padding = new Padding(8)
        };

        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(5) };
        var btnAdd = new Button { Text = "Add File(s)...", Width = 130 };
        var btnSample = new Button { Text = "Load Sample Dataset", Width = 150 };
        var btnRemove = new Button { Text = "Remove Selected", Width = 130 };
        var btnSave = new Button { Text = "Save to Database", Width = 130 };
        btnAdd.Click += BtnAddFiles_Click;
        btnSample.Click += BtnLoadSample_Click;
        btnRemove.Click += BtnRemoveSelected_Click;
        btnSave.Click += (_, _) => { SessionContext.Documents.Save(); MessageBox.Show("Saved.", "DSFS"); };
        toolbar.Controls.AddRange(new Control[] { btnAdd, btnSample, btnRemove, btnSave });

        _gridDocuments.Dock = DockStyle.Fill;
        _gridDocuments.AutoGenerateColumns = false;
        _gridDocuments.AllowUserToAddRows = false;
        _gridDocuments.AllowUserToDeleteRows = false;
        _gridDocuments.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _gridDocuments.Columns.Add(new DataGridViewTextBoxColumn { Name = "FileName", HeaderText = "File", ReadOnly = true, FillWeight = 35 });
        _gridDocuments.Columns.Add(new DataGridViewTextBoxColumn { Name = "ScholarName", HeaderText = "Scholar", FillWeight = 25 });
        _gridDocuments.Columns.Add(new DataGridViewTextBoxColumn { Name = "Domain", HeaderText = "Domain", FillWeight = 25 });
        _gridDocuments.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cluster", HeaderText = "Cluster", ReadOnly = true, FillWeight = 15 });
        _gridDocuments.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _gridDocuments.CellEndEdit += GridDocuments_CellEndEdit;

        page.Controls.Add(_gridDocuments);
        page.Controls.Add(toolbar);
        page.Controls.Add(lblInfo);
        return page;
    }

    private void BtnAddFiles_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Filter = "Research publications (*.pdf;*.docx;*.txt)|*.pdf;*.docx;*.txt|All files (*.*)|*.*",
            Title = "Select research publication files"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        int nextId = _documents.Count == 0 ? 1 : _documents.Max(d => d.Id) + 1;
        foreach (var path in dialog.FileNames)
        {
            try
            {
                string text = DocumentTextExtractorFactory.ExtractText(path);
                _documents.Add(new DocumentRecord
                {
                    Id = nextId++,
                    FileName = Path.GetFileName(path),
                    RawText = text,
                    ScholarName = string.Empty,
                    Domain = string.Empty
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not read '{Path.GetFileName(path)}':{Environment.NewLine}{ex.Message}",
                    "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        RefreshDocumentsGrid();
    }

    private void BtnLoadSample_Click(object? sender, EventArgs e)
    {
        string sampleFolder = Path.Combine(AppContext.BaseDirectory, "SampleDocuments");
        if (!Directory.Exists(sampleFolder))
        {
            MessageBox.Show(
                "The bundled SampleDocuments folder was not found next to the executable.\n" +
                "You can still use 'Add File(s)...' with your own .txt/.docx/.pdf publications.",
                "Sample dataset not found", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        foreach (var path in Directory.GetFiles(sampleFolder, "*.txt"))
        {
            string content = File.ReadAllText(path);
            // Sample files are named like: DataMining_ScholarA_1.txt -> Domain_Scholar_Index
            string baseName = Path.GetFileNameWithoutExtension(path);
            var parts = baseName.Split('_');
            string domain = parts.Length > 0 ? parts[0] : "General";
            string scholar = parts.Length > 1 ? parts[1] : "Unknown";

            _documents.Add(new DocumentRecord
            {
                Id = _documents.Count == 0 ? 1 : _documents.Max(d => d.Id) + 1,
                FileName = Path.GetFileName(path),
                RawText = content,
                ScholarName = scholar,
                Domain = domain
            });
        }
        RefreshDocumentsGrid();
    }

    private void BtnRemoveSelected_Click(object? sender, EventArgs e)
    {
        var idsToRemove = _gridDocuments.SelectedRows.Cast<DataGridViewRow>()
            .Select(r => (int)r.Tag!)
            .ToList();
        _documents.RemoveAll(d => idsToRemove.Contains(d.Id));
        RefreshDocumentsGrid();
    }

    private void GridDocuments_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _gridDocuments.Rows.Count) return;
        var row = _gridDocuments.Rows[e.RowIndex];
        int id = (int)row.Tag!;
        var doc = _documents.FirstOrDefault(d => d.Id == id);
        if (doc is null) return;

        doc.ScholarName = row.Cells["ScholarName"].Value?.ToString() ?? string.Empty;
        doc.Domain = row.Cells["Domain"].Value?.ToString() ?? string.Empty;
    }

    private void RefreshDocumentsGrid()
    {
        _gridDocuments.Rows.Clear();
        foreach (var doc in _documents)
        {
            int rowIndex = _gridDocuments.Rows.Add(
                doc.FileName, doc.ScholarName, doc.Domain,
                doc.ClusterId >= 0 ? $"Cluster {doc.ClusterId + 1}" : "-");
            _gridDocuments.Rows[rowIndex].Tag = doc.Id;
        }
    }

    // ============================================================================
    // Tab 2: Preprocessing of Data Module (Figure 9)
    // ============================================================================
    private TabPage BuildPreprocessingTab()
    {
        var page = new TabPage("2. Preprocessing");

        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(5) };
        var btnRun = new Button { Text = "Run Preprocessing (Tokenize -> Stopwords -> Stem -> TF-IDF)", Width = 380 };
        btnRun.Click += BtnRunPreprocessing_Click;
        toolbar.Controls.Add(btnRun);

        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 300 };

        _gridPreprocessing.Dock = DockStyle.Fill;
        _gridPreprocessing.AutoGenerateColumns = false;
        _gridPreprocessing.AllowUserToAddRows = false;
        _gridPreprocessing.ReadOnly = true;
        _gridPreprocessing.Columns.Add(new DataGridViewTextBoxColumn { Name = "FileName", HeaderText = "File", FillWeight = 25 });
        _gridPreprocessing.Columns.Add(new DataGridViewTextBoxColumn { Name = "TokenCount", HeaderText = "Stemmed Term Count", FillWeight = 20 });
        _gridPreprocessing.Columns.Add(new DataGridViewTextBoxColumn { Name = "Sample", HeaderText = "Sample Terms After Preprocessing", FillWeight = 55 });
        _gridPreprocessing.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        _txtPreprocessLog.Multiline = true;
        _txtPreprocessLog.ReadOnly = true;
        _txtPreprocessLog.ScrollBars = ScrollBars.Vertical;
        _txtPreprocessLog.Dock = DockStyle.Fill;
        _txtPreprocessLog.Font = new Font("Consolas", 9f);

        split.Panel1.Controls.Add(_gridPreprocessing);
        split.Panel2.Controls.Add(_txtPreprocessLog);

        page.Controls.Add(split);
        page.Controls.Add(toolbar);
        return page;
    }

    private void BtnRunPreprocessing_Click(object? sender, EventArgs e)
    {
        if (_documents.Count == 0)
        {
            MessageBox.Show("Add some documents first (Data Collection tab).", "DSFS");
            return;
        }

        _preprocessor.ProcessCorpus(_documents);

        _gridPreprocessing.Rows.Clear();
        foreach (var doc in _documents)
        {
            string sample = string.Join(", ", doc.ProcessedTokens.Distinct().Take(12));
            _gridPreprocessing.Rows.Add(doc.FileName, doc.ProcessedTokens.Count, sample);
        }

        _txtPreprocessLog.Text =
            $"Preprocessing complete for {_documents.Count} document(s).{Environment.NewLine}" +
            $"Vocabulary size (after stop word removal + Porter stemming): {_preprocessor.Vectorizer.Vocabulary.Count} terms.{Environment.NewLine}" +
            $"Each document is now represented as a TF-IDF weighted vector in this {_preprocessor.Vectorizer.Vocabulary.Count}-dimensional space, " +
            "ready for the Clustering and Similarity Measure modules.";
    }

    // ============================================================================
    // Tab 3: Clustering Module (Figure 10) - Agglomerative Hierarchical Clustering
    // ============================================================================
    private TabPage BuildClusteringTab()
    {
        var page = new TabPage("3. Clustering");

        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 70, Padding = new Padding(5) };

        _cmbLinkage.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbLinkage.Items.AddRange(new object[] { "Improved Single Linkage (proposed - fixes chaining)", "Single Linkage", "Complete Linkage", "Average Linkage" });
        _cmbLinkage.SelectedIndex = 0;
        _cmbLinkage.Width = 300;
        _cmbLinkage.SelectedIndexChanged += (_, _) => _numChainGuard.Enabled = _cmbLinkage.SelectedIndex == 0;

        var lblGuard = new Label { Text = "Chain guard factor:", AutoSize = true, Margin = new Padding(15, 8, 3, 0) };
        _numChainGuard.DecimalPlaces = 2;
        _numChainGuard.Increment = 0.1M;
        _numChainGuard.Minimum = 1.0M;
        _numChainGuard.Maximum = 5.0M;
        _numChainGuard.Value = 1.5M;
        _numChainGuard.Width = 70;

        var lblK = new Label { Text = "Number of clusters:", AutoSize = true, Margin = new Padding(15, 8, 3, 0) };
        _numClusterCount.Minimum = 1;
        _numClusterCount.Maximum = 50;
        _numClusterCount.Value = 2;
        _numClusterCount.Width = 60;

        var btnRun = new Button { Text = "Run Clustering", Width = 150, Margin = new Padding(15, 3, 3, 3) };
        btnRun.Click += BtnRunClustering_Click;

        toolbar.Controls.AddRange(new Control[] { _cmbLinkage, lblGuard, _numChainGuard, lblK, _numClusterCount, btnRun });

        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 340 };

        _dendrogramControl.Dock = DockStyle.Fill;
        var dendroScroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        dendroScroll.Controls.Add(_dendrogramControl);
        _dendrogramControl.Size = new Size(2000, 320);

        _gridClusters.Dock = DockStyle.Fill;
        _gridClusters.AutoGenerateColumns = false;
        _gridClusters.AllowUserToAddRows = false;
        _gridClusters.ReadOnly = true;
        _gridClusters.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cluster", HeaderText = "Cluster", FillWeight = 15 });
        _gridClusters.Columns.Add(new DataGridViewTextBoxColumn { Name = "Count", HeaderText = "# Documents", FillWeight = 15 });
        _gridClusters.Columns.Add(new DataGridViewTextBoxColumn { Name = "Members", HeaderText = "Documents", FillWeight = 70 });
        _gridClusters.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        split.Panel1.Controls.Add(dendroScroll);
        split.Panel2.Controls.Add(_gridClusters);

        page.Controls.Add(split);
        page.Controls.Add(toolbar);
        return page;
    }

    private void BtnRunClustering_Click(object? sender, EventArgs e)
    {
        if (_documents.Count < 2)
        {
            MessageBox.Show("At least two documents are needed to build a dendrogram.", "DSFS");
            return;
        }
        if (_documents.Any(d => d.TfIdfVector.Count == 0))
        {
            _preprocessor.ProcessCorpus(_documents); // auto-run preprocessing if it hasn't been done yet
        }

        LinkageType linkage = _cmbLinkage.SelectedIndex switch
        {
            1 => LinkageType.Single,
            2 => LinkageType.Complete,
            3 => LinkageType.Average,
            _ => LinkageType.ImprovedSingleLinkage
        };

        var clustering = new AgglomerativeClustering(linkage, (double)_numChainGuard.Value);
        _dendrogramRoot = clustering.BuildDendrogram(_documents);
        var clusters = clustering.CutClusters(_dendrogramRoot, (int)_numClusterCount.Value, _documents);

        var leafLabels = _documents.ToDictionary(d => d.Id, d => d.FileName);
        _dendrogramControl.SetData(_dendrogramRoot, leafLabels);
        _dendrogramControl.Width = Math.Max(_dendrogramControl.Parent?.Width ?? 800, _documents.Count * 90);

        _gridClusters.Rows.Clear();
        foreach (var (clusterId, memberIds) in clusters.OrderBy(kv => kv.Key))
        {
            var names = memberIds.Select(id => _documents.First(d => d.Id == id).FileName);
            _gridClusters.Rows.Add($"Cluster {clusterId + 1}", memberIds.Count, string.Join(", ", names));
        }

        RefreshDocumentsGrid();
    }

    // ============================================================================
    // Tab 4: Similarity Measure Module (Figure 11) - Cosine Similarity
    // ============================================================================
    private TabPage BuildSimilarityTab()
    {
        var page = new TabPage("4. Similarity");

        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(5) };
        var btnRun = new Button { Text = "Compute Cosine Similarity Matrix", Width = 230 };
        btnRun.Click += BtnComputeSimilarity_Click;
        toolbar.Controls.Add(btnRun);

        _gridSimilarity.Dock = DockStyle.Fill;
        _gridSimilarity.ReadOnly = true;
        _gridSimilarity.AllowUserToAddRows = false;

        _lblClosestPair.Dock = DockStyle.Bottom;
        _lblClosestPair.Padding = new Padding(8);

        page.Controls.Add(_gridSimilarity);
        page.Controls.Add(_lblClosestPair);
        page.Controls.Add(toolbar);
        return page;
    }

    private void BtnComputeSimilarity_Click(object? sender, EventArgs e)
    {
        if (_documents.Count == 0) return;
        if (_documents.Any(d => d.TfIdfVector.Count == 0))
            _preprocessor.ProcessCorpus(_documents);

        _gridSimilarity.Columns.Clear();
        _gridSimilarity.Columns.Add("Doc", string.Empty);
        foreach (var doc in _documents)
            _gridSimilarity.Columns.Add(doc.FileName, doc.FileName);

        double best = -1;
        string bestPairText = string.Empty;

        for (int i = 0; i < _documents.Count; i++)
        {
            var rowValues = new List<object> { _documents[i].FileName };
            for (int j = 0; j < _documents.Count; j++)
            {
                double sim = i == j ? 1.0 : CosineSimilarity.Compute(_documents[i].TfIdfVector, _documents[j].TfIdfVector);
                rowValues.Add(sim.ToString("0.000"));

                if (i != j && sim > best)
                {
                    best = sim;
                    bestPairText = $"Most similar pair: \"{_documents[i].FileName}\" <-> \"{_documents[j].FileName}\"  (cosine similarity = {sim:0.000})";
                }
            }
            _gridSimilarity.Rows.Add(rowValues.ToArray());
        }
        _gridSimilarity.AutoResizeColumns();
        _lblClosestPair.Text = bestPairText;
    }

    // ============================================================================
    // Tab 5: Results Generation / Performance Evaluation (Figure 12, Chapter 4)
    // ============================================================================
    private TabPage BuildEvaluationTab()
    {
        var page = new TabPage("5. Evaluation & Results");

        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(5) };
        var btnRun = new Button { Text = "Evaluate Clustering (Precision / Recall / F-Measure)", Width = 320 };
        btnRun.Click += BtnEvaluate_Click;
        toolbar.Controls.Add(btnRun);

        _chart.Dock = DockStyle.Fill;
        _lblCounts.Dock = DockStyle.Bottom;
        _lblCounts.Padding = new Padding(8);

        page.Controls.Add(_chart);
        page.Controls.Add(_lblCounts);
        page.Controls.Add(toolbar);
        return page;
    }

    private void BtnEvaluate_Click(object? sender, EventArgs e)
    {
        if (_documents.Any(d => d.ClusterId < 0))
        {
            MessageBox.Show("Run Clustering first (tab 3) so every document has a cluster assignment.", "DSFS");
            return;
        }
        if (_documents.Any(d => string.IsNullOrWhiteSpace(d.ScholarName) || string.IsNullOrWhiteSpace(d.Domain)))
        {
            MessageBox.Show(
                "Every document needs a Scholar and Domain (tab 1) - these form the ground truth " +
                "that True Positive / False Positive / False Negative are measured against.", "DSFS");
            return;
        }

        var result = ClusterEvaluator.EvaluatePairwise(_documents);

        _chart.Data = new List<(string, double)>
        {
            ("Precision", result.Precision),
            ("Recall", result.Recall),
            ("F-Measure", result.FMeasure)
        };
        _chart.Invalidate();

        _lblCounts.Text = $"True Positive = {result.TruePositive}   False Positive = {result.FalsePositive}   False Negative = {result.FalseNegative}";
    }
}
