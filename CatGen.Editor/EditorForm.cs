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

    private void EditorForm_Load(object sender, EventArgs e)
    {
        _modelsListDataGridView.AutoGenerateColumns = true;
        _spawnedObjectsDataGridView.AutoGenerateColumns = true;

        _modelsBinding.DataSource = _models;
        _modelsListDataGridView.DataSource = _modelsBinding;

        _spawnedObjectBinding.DataSource = _spawnedObjects;
        _spawnedObjectsDataGridView.DataSource = _spawnedObjectBinding;

        var modelsFolderName = "Models";

        _openModelFileDialog.InitialDirectory = Path.Combine(Directory.GetCurrentDirectory(), modelsFolderName);

        var modelsOnDisk = SaveService.GetModelsOnDisk();
        foreach (var model in modelsOnDisk)
        {
            _modelsBinding.Add(model);
        }
    }

    /// <summary>
    /// Родительское приложение, куда присылать события
    /// </summary>
    public readonly IRenderEngine ParentApp;

    private readonly List<ModelOnDisk> _models = new();

    private readonly List<SpawnedObject> _spawnedObjects = new();

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

    private void _saveButton_Click(object sender, EventArgs e)
    {
        SaveService.Save(_models);
        SaveService.Save(_spawnedObjects);

        Undirtyfy();
    }

    private void deleteButton_Click(object sender, EventArgs e)
    {
        foreach (DataGridViewRow row in _modelsListDataGridView.SelectedRows)
        {
            if (row.DataBoundItem is ModelOnDisk boundItem)
                ParentApp.DeleteModel(boundItem);

            _modelsListDataGridView.Rows.RemoveAt(row.Index);
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

    private void spawnButton_Click(object sender, EventArgs e)
    {
        foreach (DataGridViewRow item in _modelsListDataGridView.SelectedRows)
            if (item.DataBoundItem is ModelOnDisk boundItem)
            {
                var spawnedObject = new SpawnedObject(IdGenerator.NewGuid(), boundItem.Id, 0.0F, 0.0F, 0.0F);
                ParentApp.SpawnObject(spawnedObject);
                _spawnedObjectBinding.Add(spawnedObject);
            }

        Dirtyfy();
    }

    private void despawnButton_Click(object sender, EventArgs e)
    {
        var row = _spawnedObjectsDataGridView.CurrentRow;
        if (row?.DataBoundItem is not SpawnedObject item)
            return;

        ParentApp.DespawnObject(item);
        _modelsListDataGridView.Rows.RemoveAt(row.Index);

        Dirtyfy();
    }

    private void cloneButton_Click(object sender, EventArgs e)
    {
        var row = _spawnedObjectsDataGridView.CurrentRow;
        if (row?.DataBoundItem is not SpawnedObject item)
            return;

        var spawnedObject = new SpawnedObject(IdGenerator.NewGuid(), item.Id, 0.0F, 0.0F, 0.0F);

        ParentApp.SpawnObject(spawnedObject);
        _spawnedObjectBinding.Add(spawnedObject);

        Dirtyfy();
    }

    private void xAxisUpDown_ValueChanged(object sender, EventArgs e)
    {
        if (!xAxisUpDown.Focused)
            return;

        var row = _spawnedObjectsDataGridView.CurrentRow;
        if (row?.DataBoundItem is not SpawnedObject item)
            return;

        ParentApp.UpdateObject(item);

        Dirtyfy();
    }

    private void yAxisUpDown_ValueChanged(object sender, EventArgs e)
    {
        if (!yAxisUpDown.Focused)
            return;

        var row = _spawnedObjectsDataGridView.CurrentRow;
        if (row?.DataBoundItem is not SpawnedObject item)
            return;

        ParentApp.UpdateObject(item);

        Dirtyfy();
    }

    private void zAxisUpDown_ValueChanged(object sender, EventArgs e)
    {
        if (!zAxisUpDown.Focused)
            return;

        var row = _spawnedObjectsDataGridView.CurrentRow;
        if (row?.DataBoundItem is not SpawnedObject item)
            return;

        ParentApp.UpdateObject(item);

        Dirtyfy();
    }

    private void _spawnedObjectsDataGridView_SelectionChanged(object sender, EventArgs e)
    {
        var row = _spawnedObjectsDataGridView.CurrentRow;
        if (row?.DataBoundItem is not SpawnedObject item)
            return;

        xAxisUpDown.Value = (decimal)item.X;
        yAxisUpDown.Value = (decimal)item.Y;
        zAxisUpDown.Value = (decimal)item.Z;
    }
}

