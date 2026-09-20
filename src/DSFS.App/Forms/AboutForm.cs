namespace DSFS.App.Forms;

public class AboutForm : Form
{
    public AboutForm()
    {
        Text = "About DSFS";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(460, 260);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var text = new Label
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            Text =
                "Desktop Scholar Facilitator System (DSFS)\n\n" +
                "A C# implementation of the thesis:\n" +
                "\"Development of an Efficient Hierarchical Clustering Analysis Using " +
                "Agglomerative Clustering Algorithm\"\n\n" +
                "Pipeline: Data Collection -> Preprocessing (Tokenization, Stop Word Removal, " +
                "Porter Stemming, TF-IDF) -> Agglomerative Clustering (Improved Single Linkage) " +
                "-> Cosine Similarity -> Evaluation (Precision / Recall / F-Measure).\n\n" +
                "Built with .NET 8 and Windows Forms."
        };

        var btnClose = new Button { Text = "Close", Dock = DockStyle.Bottom, Height = 36 };
        btnClose.Click += (_, _) => Close();

        Controls.Add(text);
        Controls.Add(btnClose);
    }
}
