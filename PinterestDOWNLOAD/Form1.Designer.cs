namespace PinterestDOWNLOAD
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            lblUrl = new Label();
            txtUrl = new TextBox();
            lblPasta = new Label();
            txtPasta = new TextBox();
            btnSelecionarPasta = new Button();
            chkImagens = new CheckBox();
            chkVideos = new CheckBox();
            lblSimultaneos = new Label();
            numSimultaneos = new NumericUpDown();
            btnBaixar = new Button();
            btnContinuar = new Button();
            btnCancelar = new Button();
            progressBar = new ProgressBar();
            lblStatus = new Label();
            txtLog = new TextBox();
            ((System.ComponentModel.ISupportInitialize)numSimultaneos).BeginInit();
            SuspendLayout();
            //
            // lblUrl
            //
            lblUrl.AutoSize = true;
            lblUrl.Location = new Point(12, 15);
            lblUrl.Name = "lblUrl";
            lblUrl.Size = new Size(79, 15);
            lblUrl.TabIndex = 0;
            lblUrl.Text = "URL do board:";
            //
            // txtUrl
            //
            txtUrl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtUrl.Location = new Point(110, 12);
            txtUrl.Name = "txtUrl";
            txtUrl.PlaceholderText = "https://www.pinterest.com/usuario/nome-do-board/";
            txtUrl.Size = new Size(598, 23);
            txtUrl.TabIndex = 1;
            //
            // lblPasta
            //
            lblPasta.AutoSize = true;
            lblPasta.Location = new Point(12, 47);
            lblPasta.Name = "lblPasta";
            lblPasta.Size = new Size(82, 15);
            lblPasta.TabIndex = 2;
            lblPasta.Text = "Pasta destino:";
            //
            // txtPasta
            //
            txtPasta.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtPasta.Location = new Point(110, 44);
            txtPasta.Name = "txtPasta";
            txtPasta.Size = new Size(520, 23);
            txtPasta.TabIndex = 3;
            //
            // btnSelecionarPasta
            //
            btnSelecionarPasta.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSelecionarPasta.Location = new Point(636, 43);
            btnSelecionarPasta.Name = "btnSelecionarPasta";
            btnSelecionarPasta.Size = new Size(72, 25);
            btnSelecionarPasta.TabIndex = 4;
            btnSelecionarPasta.Text = "Procurar";
            btnSelecionarPasta.UseVisualStyleBackColor = true;
            btnSelecionarPasta.Click += btnSelecionarPasta_Click;
            //
            // chkImagens
            //
            chkImagens.AutoSize = true;
            chkImagens.Checked = true;
            chkImagens.CheckState = CheckState.Checked;
            chkImagens.Location = new Point(110, 78);
            chkImagens.Name = "chkImagens";
            chkImagens.Size = new Size(74, 19);
            chkImagens.TabIndex = 5;
            chkImagens.Text = "Imagens";
            chkImagens.UseVisualStyleBackColor = true;
            //
            // chkVideos
            //
            chkVideos.AutoSize = true;
            chkVideos.Checked = true;
            chkVideos.CheckState = CheckState.Checked;
            chkVideos.Location = new Point(200, 78);
            chkVideos.Name = "chkVideos";
            chkVideos.Size = new Size(62, 19);
            chkVideos.TabIndex = 6;
            chkVideos.Text = "Videos";
            chkVideos.UseVisualStyleBackColor = true;
            //
            // lblSimultaneos
            //
            lblSimultaneos.AutoSize = true;
            lblSimultaneos.Location = new Point(300, 79);
            lblSimultaneos.Name = "lblSimultaneos";
            lblSimultaneos.Size = new Size(80, 15);
            lblSimultaneos.TabIndex = 7;
            lblSimultaneos.Text = "Simultaneos:";
            //
            // numSimultaneos
            //
            numSimultaneos.Location = new Point(386, 76);
            numSimultaneos.Maximum = new decimal(new int[] { 16, 0, 0, 0 });
            numSimultaneos.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numSimultaneos.Name = "numSimultaneos";
            numSimultaneos.Size = new Size(50, 23);
            numSimultaneos.TabIndex = 8;
            numSimultaneos.Value = new decimal(new int[] { 5, 0, 0, 0 });
            //
            // btnBaixar
            //
            btnBaixar.Location = new Point(110, 110);
            btnBaixar.Name = "btnBaixar";
            btnBaixar.Size = new Size(120, 32);
            btnBaixar.TabIndex = 9;
            btnBaixar.Text = "Baixar tudo";
            btnBaixar.UseVisualStyleBackColor = true;
            btnBaixar.Click += btnBaixar_Click;
            //
            // btnContinuar
            //
            btnContinuar.Enabled = false;
            btnContinuar.Location = new Point(240, 110);
            btnContinuar.Name = "btnContinuar";
            btnContinuar.Size = new Size(150, 32);
            btnContinuar.TabIndex = 10;
            btnContinuar.Text = "Ja fiz login";
            btnContinuar.UseVisualStyleBackColor = true;
            btnContinuar.Click += btnContinuar_Click;
            //
            // btnCancelar
            //
            btnCancelar.Enabled = false;
            btnCancelar.Location = new Point(400, 110);
            btnCancelar.Name = "btnCancelar";
            btnCancelar.Size = new Size(100, 32);
            btnCancelar.TabIndex = 11;
            btnCancelar.Text = "Cancelar";
            btnCancelar.UseVisualStyleBackColor = true;
            btnCancelar.Click += btnCancelar_Click;
            //
            // progressBar
            //
            progressBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            progressBar.Location = new Point(12, 155);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(696, 20);
            progressBar.TabIndex = 12;
            //
            // lblStatus
            //
            lblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblStatus.AutoEllipsis = true;
            lblStatus.Location = new Point(12, 185);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(696, 20);
            lblStatus.TabIndex = 13;
            lblStatus.Text = "Aguardando...";
            //
            // txtLog
            //
            txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtLog.BackColor = SystemColors.Window;
            txtLog.Location = new Point(12, 210);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(696, 238);
            txtLog.TabIndex = 14;
            txtLog.WordWrap = false;
            //
            // Form1
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(720, 460);
            Controls.Add(txtLog);
            Controls.Add(lblStatus);
            Controls.Add(progressBar);
            Controls.Add(btnCancelar);
            Controls.Add(btnContinuar);
            Controls.Add(btnBaixar);
            Controls.Add(numSimultaneos);
            Controls.Add(lblSimultaneos);
            Controls.Add(chkVideos);
            Controls.Add(chkImagens);
            Controls.Add(btnSelecionarPasta);
            Controls.Add(txtPasta);
            Controls.Add(lblPasta);
            Controls.Add(txtUrl);
            Controls.Add(lblUrl);
            MinimumSize = new Size(560, 400);
            Name = "Form1";
            Text = "Pinterest Downloader";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)numSimultaneos).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblUrl;
        private TextBox txtUrl;
        private Label lblPasta;
        private TextBox txtPasta;
        private Button btnSelecionarPasta;
        private CheckBox chkImagens;
        private CheckBox chkVideos;
        private Label lblSimultaneos;
        private NumericUpDown numSimultaneos;
        private Button btnBaixar;
        private Button btnContinuar;
        private Button btnCancelar;
        private ProgressBar progressBar;
        private Label lblStatus;
        private TextBox txtLog;
    }
}
