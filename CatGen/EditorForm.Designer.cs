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
        loadModelFileButton = new System.Windows.Forms.Button();
        openModelFileDialog = new System.Windows.Forms.OpenFileDialog();
        modelPathTextBox = new System.Windows.Forms.TextBox();
        SuspendLayout();
        //
        // loadModelFileButton
        //
        loadModelFileButton.Location = new System.Drawing.Point(12, 41);
        loadModelFileButton.Name = "loadModelFileButton";
        loadModelFileButton.Size = new System.Drawing.Size(88, 23);
        loadModelFileButton.TabIndex = 0;
        loadModelFileButton.Text = "Заебенить";
        loadModelFileButton.UseVisualStyleBackColor = true;
        loadModelFileButton.Click += LoadModelFileButton_Click;
        //
        // openModelFileDialog
        //
        openModelFileDialog.FileName = "openFileDialog1";
        openModelFileDialog.Filter = "Models (*.gltf;*.obj;*.stl)|*.gltf;*.obj;*.stl|All files (*.*)|*.*";
        openModelFileDialog.InitialDirectory = "C:\\Users\\nikit\\RiderProjects\\DirectX12-App";
        //
        // modelPathTextBox
        //
        modelPathTextBox.Enabled = false;
        modelPathTextBox.Location = new System.Drawing.Point(12, 12);
        modelPathTextBox.Name = "modelPathTextBox";
        modelPathTextBox.PlaceholderText = "C:\\Users\\nikit\\RiderProjects\\DirectX12-App";
        modelPathTextBox.Size = new System.Drawing.Size(776, 23);
        modelPathTextBox.TabIndex = 1;
        //
        // EditorForm
        //
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(800, 450);
        Controls.Add(modelPathTextBox);
        Controls.Add(loadModelFileButton);
        Text = "EditorForm";
        ResumeLayout(false);
        PerformLayout();
    }

    private System.Windows.Forms.TextBox modelPathTextBox;

    private System.Windows.Forms.Button loadModelFileButton;
    private System.Windows.Forms.OpenFileDialog openModelFileDialog;

    #endregion
}

