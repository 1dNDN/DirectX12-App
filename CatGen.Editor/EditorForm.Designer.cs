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
        _modelsListDataGridView = new DataGridView();
        _modelsBinding = new BindingSource(components);
        _saveButton = new Button();
        deleteButton = new Button();
        _spawnedObjectsDataGridView = new DataGridView();
        _spawnedObjectBinding = new BindingSource(components);
        label1 = new Label();
        label2 = new Label();
        spawnButton = new Button();
        despawnButton = new Button();
        xAxisUpDown = new NumericUpDown();
        label3 = new Label();
        label4 = new Label();
        yAxisUpDown = new NumericUpDown();
        label5 = new Label();
        zAxisUpDown = new NumericUpDown();
        label6 = new Label();
        numericUpDown4 = new NumericUpDown();
        label7 = new Label();
        numericUpDown5 = new NumericUpDown();
        label8 = new Label();
        numericUpDown6 = new NumericUpDown();
        cloneButton = new Button();
        ((ISupportInitialize)_modelsListDataGridView).BeginInit();
        ((ISupportInitialize)_modelsBinding).BeginInit();
        ((ISupportInitialize)_spawnedObjectsDataGridView).BeginInit();
        ((ISupportInitialize)_spawnedObjectBinding).BeginInit();
        ((ISupportInitialize)xAxisUpDown).BeginInit();
        ((ISupportInitialize)yAxisUpDown).BeginInit();
        ((ISupportInitialize)zAxisUpDown).BeginInit();
        ((ISupportInitialize)numericUpDown4).BeginInit();
        ((ISupportInitialize)numericUpDown5).BeginInit();
        ((ISupportInitialize)numericUpDown6).BeginInit();
        SuspendLayout();
        // 
        // _loadModelFileButton
        // 
        _loadModelFileButton.Location = new Point(26, 419);
        _loadModelFileButton.Name = "_loadModelFileButton";
        _loadModelFileButton.Size = new Size(154, 23);
        _loadModelFileButton.TabIndex = 0;
        _loadModelFileButton.Text = "Добавить новую модель";
        _loadModelFileButton.UseVisualStyleBackColor = true;
        _loadModelFileButton.Click += LoadModelFileButton_Click;
        // 
        // _openModelFileDialog
        // 
        _openModelFileDialog.FileName = "openFileDialog1";
        _openModelFileDialog.Filter = "Models (*.gltf;*.obj;*.stl)|*.gltf;*.obj;*.stl|All files (*.*)|*.*";
        _openModelFileDialog.InitialDirectory = "C:\\Users\\nikit\\RiderProjects\\DirectX12-App";
        // 
        // _modelsListDataGridView
        // 
        _modelsListDataGridView.AllowUserToAddRows = false;
        _modelsListDataGridView.AllowUserToDeleteRows = false;
        _modelsListDataGridView.AllowUserToResizeRows = false;
        _modelsListDataGridView.AutoGenerateColumns = false;
        _modelsListDataGridView.ClipboardCopyMode = DataGridViewClipboardCopyMode.Disable;
        _modelsListDataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        _modelsListDataGridView.DataSource = _modelsBinding;
        _modelsListDataGridView.EditMode = DataGridViewEditMode.EditProgrammatically;
        _modelsListDataGridView.Location = new Point(26, 13);
        _modelsListDataGridView.Name = "_modelsListDataGridView";
        _modelsListDataGridView.ReadOnly = true;
        _modelsListDataGridView.RowHeadersVisible = false;
        _modelsListDataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _modelsListDataGridView.Size = new Size(409, 356);
        _modelsListDataGridView.TabIndex = 2;
        _modelsListDataGridView.DataBindingComplete += _objectListDataGridView_DataBindingComplete;
        // 
        // _saveButton
        // 
        _saveButton.Location = new Point(704, 743);
        _saveButton.Name = "_saveButton";
        _saveButton.Size = new Size(91, 23);
        _saveButton.TabIndex = 3;
        _saveButton.Text = "Сохранить";
        _saveButton.UseVisualStyleBackColor = true;
        _saveButton.Click += _saveButton_Click;
        // 
        // deleteButton
        // 
        deleteButton.Location = new Point(186, 419);
        deleteButton.Name = "deleteButton";
        deleteButton.Size = new Size(75, 23);
        deleteButton.TabIndex = 4;
        deleteButton.Text = "Забыть";
        deleteButton.UseVisualStyleBackColor = true;
        deleteButton.Click += deleteButton_Click;
        // 
        // _spawnedObjectsDataGridView
        // 
        _spawnedObjectsDataGridView.AllowUserToAddRows = false;
        _spawnedObjectsDataGridView.AllowUserToDeleteRows = false;
        _spawnedObjectsDataGridView.AllowUserToResizeRows = false;
        _spawnedObjectsDataGridView.AutoGenerateColumns = false;
        _spawnedObjectsDataGridView.ClipboardCopyMode = DataGridViewClipboardCopyMode.Disable;
        _spawnedObjectsDataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        _spawnedObjectsDataGridView.DataSource = _spawnedObjectBinding;
        _spawnedObjectsDataGridView.Location = new Point(441, 12);
        _spawnedObjectsDataGridView.MultiSelect = false;
        _spawnedObjectsDataGridView.Name = "_spawnedObjectsDataGridView";
        _spawnedObjectsDataGridView.ReadOnly = true;
        _spawnedObjectsDataGridView.RowHeadersVisible = false;
        _spawnedObjectsDataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _spawnedObjectsDataGridView.Size = new Size(502, 357);
        _spawnedObjectsDataGridView.TabIndex = 5;
        _spawnedObjectsDataGridView.SelectionChanged += _spawnedObjectsDataGridView_SelectionChanged;
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new Point(26, 401);
        label1.Name = "label1";
        label1.Size = new Size(106, 15);
        label1.TabIndex = 6;
        label1.Text = "Загрузка моделей";
        // 
        // label2
        // 
        label2.AutoSize = true;
        label2.Location = new Point(441, 401);
        label2.Name = "label2";
        label2.Size = new Size(167, 15);
        label2.TabIndex = 7;
        label2.Text = "Управление моделям в мире";
        // 
        // spawnButton
        // 
        spawnButton.Location = new Point(441, 419);
        spawnButton.Name = "spawnButton";
        spawnButton.Size = new Size(91, 23);
        spawnButton.TabIndex = 8;
        spawnButton.Text = "Заспавнить";
        spawnButton.UseVisualStyleBackColor = true;
        spawnButton.Click += spawnButton_Click;
        // 
        // despawnButton
        // 
        despawnButton.Location = new Point(538, 419);
        despawnButton.Name = "despawnButton";
        despawnButton.Size = new Size(138, 23);
        despawnButton.TabIndex = 9;
        despawnButton.Text = "Спавн говно";
        despawnButton.UseVisualStyleBackColor = true;
        despawnButton.Click += despawnButton_Click;
        // 
        // xAxisUpDown
        // 
        xAxisUpDown.DecimalPlaces = 2;
        xAxisUpDown.Location = new Point(469, 448);
        xAxisUpDown.Name = "xAxisUpDown";
        xAxisUpDown.Size = new Size(78, 23);
        xAxisUpDown.TabIndex = 10;
        xAxisUpDown.ValueChanged += xAxisUpDown_ValueChanged;
        // 
        // label3
        // 
        label3.AutoSize = true;
        label3.Location = new Point(449, 450);
        label3.Name = "label3";
        label3.Size = new Size(17, 15);
        label3.TabIndex = 11;
        label3.Text = "X:";
        // 
        // label4
        // 
        label4.AutoSize = true;
        label4.Location = new Point(449, 479);
        label4.Name = "label4";
        label4.Size = new Size(17, 15);
        label4.TabIndex = 13;
        label4.Text = "Y:";
        // 
        // yAxisUpDown
        // 
        yAxisUpDown.DecimalPlaces = 2;
        yAxisUpDown.Location = new Point(469, 477);
        yAxisUpDown.Name = "yAxisUpDown";
        yAxisUpDown.Size = new Size(78, 23);
        yAxisUpDown.TabIndex = 12;
        yAxisUpDown.ValueChanged += yAxisUpDown_ValueChanged;
        // 
        // label5
        // 
        label5.AutoSize = true;
        label5.Location = new Point(449, 508);
        label5.Name = "label5";
        label5.Size = new Size(19, 15);
        label5.TabIndex = 15;
        label5.Text = "Й:";
        // 
        // zAxisUpDown
        // 
        zAxisUpDown.DecimalPlaces = 2;
        zAxisUpDown.Location = new Point(469, 506);
        zAxisUpDown.Name = "zAxisUpDown";
        zAxisUpDown.Size = new Size(78, 23);
        zAxisUpDown.TabIndex = 14;
        zAxisUpDown.ValueChanged += zAxisUpDown_ValueChanged;
        // 
        // label6
        // 
        label6.AutoSize = true;
        label6.Location = new Point(578, 450);
        label6.Name = "label6";
        label6.Size = new Size(14, 15);
        label6.TabIndex = 17;
        label6.Text = "X";
        // 
        // numericUpDown4
        // 
        numericUpDown4.Enabled = false;
        numericUpDown4.Location = new Point(598, 448);
        numericUpDown4.Name = "numericUpDown4";
        numericUpDown4.Size = new Size(78, 23);
        numericUpDown4.TabIndex = 16;
        // 
        // label7
        // 
        label7.AutoSize = true;
        label7.Location = new Point(578, 479);
        label7.Name = "label7";
        label7.Size = new Size(14, 15);
        label7.TabIndex = 19;
        label7.Text = "X";
        // 
        // numericUpDown5
        // 
        numericUpDown5.Enabled = false;
        numericUpDown5.Location = new Point(598, 477);
        numericUpDown5.Name = "numericUpDown5";
        numericUpDown5.Size = new Size(78, 23);
        numericUpDown5.TabIndex = 18;
        // 
        // label8
        // 
        label8.AutoSize = true;
        label8.Location = new Point(578, 510);
        label8.Name = "label8";
        label8.Size = new Size(14, 15);
        label8.TabIndex = 21;
        label8.Text = "X";
        // 
        // numericUpDown6
        // 
        numericUpDown6.Enabled = false;
        numericUpDown6.Location = new Point(598, 508);
        numericUpDown6.Name = "numericUpDown6";
        numericUpDown6.Size = new Size(78, 23);
        numericUpDown6.TabIndex = 20;
        // 
        // cloneButton
        // 
        cloneButton.Location = new Point(441, 535);
        cloneButton.Name = "cloneButton";
        cloneButton.Size = new Size(235, 23);
        cloneButton.TabIndex = 22;
        cloneButton.Text = "Клонировать";
        cloneButton.UseVisualStyleBackColor = true;
        cloneButton.Click += cloneButton_Click;
        // 
        // EditorForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1222, 778);
        Controls.Add(cloneButton);
        Controls.Add(label8);
        Controls.Add(numericUpDown6);
        Controls.Add(label7);
        Controls.Add(numericUpDown5);
        Controls.Add(label6);
        Controls.Add(numericUpDown4);
        Controls.Add(label5);
        Controls.Add(zAxisUpDown);
        Controls.Add(label4);
        Controls.Add(yAxisUpDown);
        Controls.Add(label3);
        Controls.Add(xAxisUpDown);
        Controls.Add(despawnButton);
        Controls.Add(spawnButton);
        Controls.Add(label2);
        Controls.Add(label1);
        Controls.Add(_spawnedObjectsDataGridView);
        Controls.Add(deleteButton);
        Controls.Add(_saveButton);
        Controls.Add(_modelsListDataGridView);
        Controls.Add(_loadModelFileButton);
        Name = "EditorForm";
        Text = "Editor";
        Load += EditorForm_Load;
        ((ISupportInitialize)_modelsListDataGridView).EndInit();
        ((ISupportInitialize)_modelsBinding).EndInit();
        ((ISupportInitialize)_spawnedObjectsDataGridView).EndInit();
        ((ISupportInitialize)_spawnedObjectBinding).EndInit();
        ((ISupportInitialize)xAxisUpDown).EndInit();
        ((ISupportInitialize)yAxisUpDown).EndInit();
        ((ISupportInitialize)zAxisUpDown).EndInit();
        ((ISupportInitialize)numericUpDown4).EndInit();
        ((ISupportInitialize)numericUpDown5).EndInit();
        ((ISupportInitialize)numericUpDown6).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    private System.Windows.Forms.Button _loadModelFileButton;
    private System.Windows.Forms.OpenFileDialog _openModelFileDialog;

    #endregion

    private DataGridView _modelsListDataGridView;
    private BindingSource _modelsBinding;
    private Button _saveButton;
    private Button deleteButton;
    private DataGridView _spawnedObjectsDataGridView;
    private BindingSource _spawnedObjectBinding;
    private Label label1;
    private Label label2;
    private Button spawnButton;
    private Button despawnButton;
    private NumericUpDown xAxisUpDown;
    private Label label3;
    private Label label4;
    private NumericUpDown yAxisUpDown;
    private Label label5;
    private NumericUpDown zAxisUpDown;
    private Label label6;
    private NumericUpDown numericUpDown4;
    private Label label7;
    private NumericUpDown numericUpDown5;
    private Label label8;
    private NumericUpDown numericUpDown6;
    private Button cloneButton;
}

