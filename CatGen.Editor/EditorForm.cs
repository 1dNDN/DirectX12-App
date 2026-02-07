using CatGen.DTOs;
using CatGen.Interfaces;

namespace CatGen;

/// <summary>
/// Редактор сцены, чтобы не пересобирать каждый раз всё заново
/// </summary>
public partial class EditorForm : Form
{
    /// <summary>
    /// Редактор сцены, чтобы не пересобирать каждый раз всё заново
    /// </summary>
    public EditorForm(IRenderEngine parentApp)
    {
        ParentApp = parentApp;
        InitializeComponent();

        var modelsFolderName = "Models";

        openModelFileDialog.InitialDirectory = Path.Combine(Directory.GetCurrentDirectory(), modelsFolderName);
        modelPathTextBox.PlaceholderText = "Example.gltf";
    }

    /// <summary>
    /// Родительское приложение, куда присылать события
    /// </summary>
    public readonly IRenderEngine ParentApp;

    private void LoadModelFileButton_Click(object sender, EventArgs e)
    {
        if (openModelFileDialog.ShowDialog() == DialogResult.Cancel)
            return;

        modelPathTextBox.Text = openModelFileDialog.SafeFileName;

        ParentApp.AddModel(new ModelOnDisk(openModelFileDialog.FileName));
    }
}

