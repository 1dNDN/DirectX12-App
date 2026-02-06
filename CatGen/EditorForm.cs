namespace CatGen;

/// <summary>
/// Редактор сцены, чтобы не пересобирать каждый раз всё заново
/// </summary>
public partial class EditorForm : Form
{
    /// <summary>
    /// Редактор сцены, чтобы не пересобирать каждый раз всё заново
    /// </summary>
    public EditorForm(DirectXApp parentApp)
    {
        ParentApp = parentApp;
        InitializeComponent();

        var modelsFolderName = "Models";

        openModelFileDialog.InitialDirectory = Path.Combine(Directory.GetCurrentDirectory(), modelsFolderName);
        modelPathTextBox.PlaceholderText = openModelFileDialog.InitialDirectory;
    }

    /// <summary>
    /// Родительское приложение, куда присылать события
    /// </summary>
    public readonly DirectXApp ParentApp;

    private void LoadModelFileButton_Click(object sender, EventArgs e)
    {
        if (openModelFileDialog.ShowDialog() == DialogResult.Cancel)
            return;

        modelPathTextBox.Text = openModelFileDialog.FileName;

        ParentApp.AddModel(openModelFileDialog.FileName);
    }
}

