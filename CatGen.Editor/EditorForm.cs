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

        var model = new ModelOnDisk(_openModelFileDialog.FileName, IdGenerator.NewGuid());

        ParentApp.AddModel(model);

        _modelsBinding.Add(model);

        Dirtyfy();
    }

    private void _objectListDataGridView_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
    {
        _modelsListDataGridView.Columns[nameof(ModelOnDisk.FilePath)]?.Visible = false;
    }

    private void EditorForm_Load(object sender, EventArgs e)
    {
        _modelsListDataGridView.AutoGenerateColumns = true;

        _modelsBinding.DataSource = _models;
        _modelsListDataGridView.DataSource = _modelsBinding;

        var modelsFolderName = "Models";

        _openModelFileDialog.InitialDirectory = Path.Combine(Directory.GetCurrentDirectory(), modelsFolderName);

        var modelsOnDisk = SaveService.GetModelsOnDisk();
        foreach (var model in modelsOnDisk)
        {
            _modelsBinding.Add(model);
        }
    }

    private void _saveButton_Click(object sender, EventArgs e)
    {
        SaveService.Save(_models);

        Undirtyfy();
    }

    private void deleteButton_Click(object sender, EventArgs e)
    {


        foreach (DataGridViewRow item in _modelsListDataGridView.SelectedRows)
        {
            if (item.DataBoundItem is ModelOnDisk boundItem)
                ParentApp.DeleteModel(boundItem);

            _modelsListDataGridView.Rows.RemoveAt(item.Index);
        }

        Dirtyfy();
    }

    private void Dirtyfy()
    {
        this.Text = "Editor [Dirty]";
    }

    private void Undirtyfy()
    {
        this.Text = "Editor";
    }
}

