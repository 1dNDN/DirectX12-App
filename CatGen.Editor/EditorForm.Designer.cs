using System.ComponentModel;

namespace CatGen;

partial class EditorForm
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        components = new Container();
        _loadModelFileButton = new Button();
        _openModelFileDialog = new OpenFileDialog();
        _modelPathTextBox = new TextBox();
        _objectListDataGridView = new DataGridView();
        _objectsBinding = new BindingSource(components);
        _saveButton = new Button();
        ((ISupportInitialize)_objectListDataGridView).BeginInit();
        ((ISupportInitialize)_objectsBinding).BeginInit();
        SuspendLayout();
        // 
        // _loadModelFileButton
        // 
        _loadModelFileButton.Location = new Point(384, 743);
        _loadModelFileButton.Name = "_loadModelFileButton";
        _loadModelFileButton.Size = new Size(88, 23);
        _loadModelFileButton.TabIndex = 0;
        _loadModelFileButton.Text = "Впендюрить";
        _loadModelFileButton.UseVisualStyleBackColor = true;
        _loadModelFileButton.Click += LoadModelFileButton_Click;
        // 
        // _openModelFileDialog
        // 
        _openModelFileDialog.FileName = "openFileDialog1";
        _openModelFileDialog.Filter = "Models (*.gltf;*.obj;*.stl)|*.gltf;*.obj;*.stl|All files (*.*)|*.*";
        _openModelFileDialog.InitialDirectory = "C:\\Users\\nikit\\RiderProjects\\DirectX12-App";
        // 
        // _modelPathTextBox
        // 
        _modelPathTextBox.Enabled = false;
        _modelPathTextBox.Location = new Point(12, 743);
        _modelPathTextBox.Name = "_modelPathTextBox";
        _modelPathTextBox.PlaceholderText = "C:\\Users\\nikit\\RiderProjects\\DirectX12-App";
        _modelPathTextBox.Size = new Size(366, 23);
        _modelPathTextBox.TabIndex = 1;
        // 
        // _objectListDataGridView
        // 
        _objectListDataGridView.AutoGenerateColumns = false;
        _objectListDataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        _objectListDataGridView.DataSource = _objectsBinding;
        _objectListDataGridView.Location = new Point(26, 13);
        _objectListDataGridView.Name = "_objectListDataGridView";
        _objectListDataGridView.Size = new Size(711, 356);
        _objectListDataGridView.TabIndex = 2;
        // 
        // _saveButton
        // 
        _saveButton.Location = new Point(704, 743);
        _saveButton.Name = "_saveButton";
        _saveButton.Size = new Size(91, 23);
        _saveButton.TabIndex = 3;
        _saveButton.Text = "Заебенить";
        _saveButton.UseVisualStyleBackColor = true;
        // 
        // EditorForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(807, 778);
        Controls.Add(_saveButton);
        Controls.Add(_objectListDataGridView);
        Controls.Add(_modelPathTextBox);
        Controls.Add(_loadModelFileButton);
        Name = "EditorForm";
        Text = "EditorForm";
        Load += EditorForm_Load;
        ((ISupportInitialize)_objectListDataGridView).EndInit();
        ((ISupportInitialize)_objectsBinding).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    private System.Windows.Forms.TextBox _modelPathTextBox;

    private System.Windows.Forms.Button _loadModelFileButton;
    private System.Windows.Forms.OpenFileDialog _openModelFileDialog;

    #endregion

    private DataGridView _objectListDataGridView;
    private BindingSource _objectsBinding;
    private Button _saveButton;
}

