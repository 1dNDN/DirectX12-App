using CatGen.Common;
using CatGen.DTOs;
using CatGen.Interfaces;
using CatGen.Saves;

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

    private readonly List<ModelOnDisk> _models = new();

    private void LoadModelFileButton_Click(object sender, EventArgs e)
    {
        if (_openModelFileDialog.ShowDialog() == DialogResult.Cancel)
            return;

        _modelPathTextBox.Text = _openModelFileDialog.SafeFileName;
        var model = new ModelOnDisk(_openModelFileDialog.FileName, IdGenerator.NewGuid());

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

        _models.AddRange(SaveService.GetModelsOnDisk());
    }

    private void _saveButton_Click(object sender, EventArgs e)
    {
        SaveService.Save(_models);
    }

    private void deleteButton_Click(object sender, EventArgs e)
    {
        foreach (DataGridViewRow item in _objectListDataGridView.SelectedRows)
        {
            if (item.DataBoundItem is ModelOnDisk boundItem)
                ParentApp.DeleteModel(boundItem);

            _objectListDataGridView.Rows.RemoveAt(item.Index);
        }
    }
}

