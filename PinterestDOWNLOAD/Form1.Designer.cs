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
            lblModo = new Label();
            rbPasta = new RadioButton();
            rbPin = new RadioButton();
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
            // lblModo
            //
            lblModo.AutoSize = true;
            lblModo.Location = new Point(12, 16);
            lblModo.Name = "lblModo";
            lblModo.Size = new Size(45, 15);
            lblModo.TabIndex = 0;
            lblModo.Text = "Baixar:";
            //
            // rbPasta
            //
            rbPasta.AutoSize = true;
            rbPasta.Checked = true;
            rbPasta.Location = new Point(150, 14);
            rbPasta.Name = "rbPasta";
            rbPasta.Size = new Size(103, 19);
            rbPasta.TabIndex = 1;
            rbPasta.TabStop = true;
            rbPasta.Text = "Pasta (board)";
            rbPasta.UseVisualStyleBackColor = true;
            rbPasta.CheckedChanged += ModoChanged;
            //
            // rbPin
            //
            rbPin.AutoSize = true;
            rbPin.Location = new Point(280, 14);
            rbPin.Name = "rbPin";
            rbPin.Size = new Size(93, 19);
            rbPin.TabIndex = 2;
            rbPin.Text = "Pin unico";
            rbPin.UseVisualStyleBackColor = true;
            rbPin.CheckedChanged += ModoChanged;
            //
            // lblUrl
            //
            lblUrl.AutoSize = true;
            lblUrl.Location = new Point(12, 48);
            lblUrl.Name = "lblUrl";
            lblUrl.Size = new Size(85, 15);
            lblUrl.TabIndex = 3;
            lblUrl.Text = "URL do board:";
            //
            // txtUrl
            //
            txtUrl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtUrl.Location = new Point(150, 45);
            txtUrl.Name = "txtUrl";
            txtUrl.PlaceholderText = "https://www.pinterest.com/usuario/board/";
            txtUrl.Size = new Size(558, 23);
            txtUrl.TabIndex = 4;
            //
            // lblPasta
            //
            lblPasta.AutoSize = true;
            lblPasta.Location = new Point(12, 80);
            lblPasta.Name = "lblPasta";
            lblPasta.Size = new Size(82, 15);
            lblPasta.TabIndex = 5;
            lblPasta.Text = "Pasta destino:";
            //
            // txtPasta
            //
            txtPasta.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtPasta.Location = new Point(150, 77);
            txtPasta.Name = "txtPasta";
            txtPasta.Size = new Size(486, 23);
            txtPasta.TabIndex = 6;
            //
            // btnSelecionarPasta
            //
            btnSelecionarPasta.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSelecionarPasta.Location = new Point(644, 76);
            btnSelecionarPasta.Name = "btnSelecionarPasta";
            btnSelecionarPasta.Size = new Size(64, 25);
            btnSelecionarPasta.TabIndex = 7;
            btnSelecionarPasta.Text = "Procurar";
            btnSelecionarPasta.UseVisualStyleBackColor = true;
            btnSelecionarPasta.Click += btnSelecionarPasta_Click;
            //
            // chkImagens
            //
            chkImagens.AutoSize = true;
            chkImagens.Checked = true;
            chkImagens.CheckState = CheckState.Checked;
            chkImagens.Location = new Point(150, 111);
            chkImagens.Name = "chkImagens";
            chkImagens.Size = new Size(74, 19);
            chkImagens.TabIndex = 8;
            chkImagens.Text = "Imagens";
            chkImagens.UseVisualStyleBackColor = true;
            //
            // chkVideos
            //
            chkVideos.AutoSize = true;
            chkVideos.Checked = true;
            chkVideos.CheckState = CheckState.Checked;
            chkVideos.Location = new Point(240, 111);
            chkVideos.Name = "chkVideos";
            chkVideos.Size = new Size(62, 19);
            chkVideos.TabIndex = 9;
            chkVideos.Text = "Videos";
            chkVideos.UseVisualStyleBackColor = true;
            //
            // lblSimultaneos
            //
            lblSimultaneos.AutoSize = true;
            lblSimultaneos.Location = new Point(330, 112);
            lblSimultaneos.Name = "lblSimultaneos";
            lblSimultaneos.Size = new Size(80, 15);
            lblSimultaneos.TabIndex = 10;
            lblSimultaneos.Text = "Simultaneos:";
            //
            // numSimultaneos
            //
            numSimultaneos.Location = new Point(416, 109);
            numSimultaneos.Maximum = new decimal(new int[] { 16, 0, 0, 0 });
            numSimultaneos.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numSimultaneos.Name = "numSimultaneos";
            numSimultaneos.Size = new Size(50, 23);
            numSimultaneos.TabIndex = 11;
            numSimultaneos.Value = new decimal(new int[] { 5, 0, 0, 0 });
            //
            // btnBaixar
            //
            btnBaixar.Location = new Point(150, 143);
            btnBaixar.Name = "btnBaixar";
            btnBaixar.Size = new Size(120, 32);
            btnBaixar.TabIndex = 12;
            btnBaixar.Text = "Baixar tudo";
            btnBaixar.UseVisualStyleBackColor = true;
            btnBaixar.Click += btnBaixar_Click;
            //
            // btnContinuar
            //
            btnContinuar.Enabled = false;
            btnContinuar.Location = new Point(280, 143);
            btnContinuar.Name = "btnContinuar";
            btnContinuar.Size = new Size(150, 32);
            btnContinuar.TabIndex = 13;
            btnContinuar.Text = "Ja fiz login";
            btnContinuar.UseVisualStyleBackColor = true;
            btnContinuar.Click += btnContinuar_Click;
            //
            // btnCancelar
            //
            btnCancelar.Enabled = false;
            btnCancelar.Location = new Point(440, 143);
            btnCancelar.Name = "btnCancelar";
            btnCancelar.Size = new Size(100, 32);
            btnCancelar.TabIndex = 14;
            btnCancelar.Text = "Cancelar";
            btnCancelar.UseVisualStyleBackColor = true;
            btnCancelar.Click += btnCancelar_Click;
            //
            // progressBar
            //
            progressBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            progressBar.Location = new Point(12, 188);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(696, 20);
            progressBar.TabIndex = 15;
            //
            // lblStatus
            //
            lblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblStatus.AutoEllipsis = true;
            lblStatus.Location = new Point(12, 218);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(696, 20);
            lblStatus.TabIndex = 16;
            lblStatus.Text = "Aguardando...";
            //
            // txtLog
            //
            txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtLog.BackColor = SystemColors.Window;
            txtLog.Location = new Point(12, 245);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(696, 233);
            txtLog.TabIndex = 17;
            txtLog.WordWrap = false;
            //
            // Form1
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(720, 490);
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
            Controls.Add(rbPin);
            Controls.Add(rbPasta);
            Controls.Add(lblModo);
            MinimumSize = new Size(560, 430);
            Name = "Form1";
            Text = "Pinterest Downloader";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)numSimultaneos).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblModo;
        private RadioButton rbPasta;
        private RadioButton rbPin;
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
