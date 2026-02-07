using System.ComponentModel;

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
    }

    /// <summary>
    /// Родительское приложение, куда присылать события
    /// </summary>
    public readonly IRenderEngine ParentApp;

    private readonly BindingList<ModelOnDisk> _models = new();

    private void LoadModelFileButton_Click(object sender, EventArgs e)
    {
        if (_openModelFileDialog.ShowDialog() == DialogResult.Cancel)
            return;

        _modelPathTextBox.Text = _openModelFileDialog.SafeFileName;
        var model = new ModelOnDisk(_openModelFileDialog.FileName);

        ParentApp.AddModel(model);

        _objectsBinding.Add(model);
    }

    private void _objectListDataGridView_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
    {
        _objectListDataGridView.Columns[nameof(ModelOnDisk.FilePath)]?.Visible = false;
    }

    private void EditorForm_Load(object sender, EventArgs e)
    {
        _objectsBinding.DataSource = _models;
        _objectListDataGridView.DataSource = _objectsBinding;

        var modelsFolderName = "Models";

        _openModelFileDialog.InitialDirectory = Path.Combine(Directory.GetCurrentDirectory(), modelsFolderName);
        _modelPathTextBox.PlaceholderText = "Example.gltf";
    }
}

