namespace PinterestDOWNLOAD
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            label1 = new Label();
            txtUrl = new TextBox();
            txtPasta = new TextBox();
            btnSelecionarPasta = new Button();
            btnBaixar = new Button();
            lblStatus = new Label();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(49, 74);
            label1.Name = "label1";
            label1.Size = new Size(142, 15);
            label1.TabIndex = 0;
            label1.Text = "Link da Pasta do Pinterest";
            // 
            // txtUrl
            // 
            txtUrl.Location = new Point(208, 71);
            txtUrl.Name = "txtUrl";
            txtUrl.Size = new Size(498, 23);
            txtUrl.TabIndex = 1;
            // 
            // txtPasta
            // 
            txtPasta.Location = new Point(208, 194);
            txtPasta.Name = "txtPasta";
            txtPasta.Size = new Size(498, 23);
            txtPasta.TabIndex = 2;
            // 
            // btnSelecionarPasta
            // 
            btnSelecionarPasta.Location = new Point(387, 250);
            btnSelecionarPasta.Name = "btnSelecionarPasta";
            btnSelecionarPasta.Size = new Size(100, 38);
            btnSelecionarPasta.TabIndex = 3;
            btnSelecionarPasta.Text = "Escolher pasta";
            btnSelecionarPasta.UseVisualStyleBackColor = true;
            btnSelecionarPasta.Click += btnSelecionarPasta_Click;
            // 
            // btnBaixar
            // 
            btnBaixar.Location = new Point(387, 333);
            btnBaixar.Name = "btnBaixar";
            btnBaixar.Size = new Size(100, 37);
            btnBaixar.TabIndex = 4;
            btnBaixar.Text = "Baixar tudo";
            btnBaixar.UseVisualStyleBackColor = true;
            btnBaixar.Click += btnBaixar_Click;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(128, 355);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(82, 15);
            lblStatus.TabIndex = 5;
            lblStatus.Text = "Aguardando...";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(lblStatus);
            Controls.Add(btnBaixar);
            Controls.Add(btnSelecionarPasta);
            Controls.Add(txtPasta);
            Controls.Add(txtUrl);
            Controls.Add(label1);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox txtUrl;
        private TextBox txtPasta;
        private Button btnSelecionarPasta;
        private Button btnBaixar;
        private Label lblStatus;
    }
}
