using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace HL7ParserWin_ParseView.Forms
{
    partial class MainForm
    {
        private DockPanel dockPanel;
        private DockContent dockTreeView;
        private DockContent dockParseView;
        private DockContent dockMessageView;

        private TreeView treeView;
        private TextBox txtParseView;   // Editable Parse View
        private TextBox txtMessageRaw;  // Message View input

        private Panel panelTop;
        private Button btnParse;
        private Button btnLoadSample;

        private WeifenLuo.WinFormsUI.Docking.DockContent dockVmdView;
        private System.Windows.Forms.TextBox txtVmdView;

        private System.Windows.Forms.Button btnPasteClipboard;
        private System.Windows.Forms.Button btnGetVMD;
        private System.Windows.Forms.Button btnClear;

        
        private void InitializeComponent()
        {
            this.panelTop = new Panel();
            this.btnLoadSample = new Button();
            this.btnParse = new Button();
            this.dockPanel = new DockPanel();
            this.dockTreeView = new DockContent();
            this.dockParseView = new DockContent();
            this.dockMessageView = new DockContent();
            this.treeView = new TreeView();
            this.txtParseView = new TextBox();
            this.txtMessageRaw = new TextBox();

            // panelTop
            this.panelTop.Dock = DockStyle.Top;
            this.panelTop.Height = 40;

            // Paste from Clipboard Button
            this.btnPasteClipboard = new Button();
            this.btnPasteClipboard.Text = "Paste Message";
            this.btnPasteClipboard.Width = 120;
            this.btnPasteClipboard.Left = 10;
            this.btnPasteClipboard.Top = 8;
            this.btnPasteClipboard.Click += new System.EventHandler(this.btnPasteClipboard_Click);

            // Get VMD Button
            this.btnGetVMD = new Button();
            this.btnGetVMD.Text = "Get VMD";
            this.btnGetVMD.Width = 100;
            this.btnGetVMD.Left = this.btnPasteClipboard.Right + 10;
            this.btnGetVMD.Top = 8;
            this.btnGetVMD.Click += new System.EventHandler(this.btnGetVMD_Click);

            // Clear Button
            this.btnClear = new Button();
            this.btnClear.Text = "Clear";
            this.btnClear.Width = 80;
            this.btnClear.Left = this.btnGetVMD.Right + 10;
            this.btnClear.Top = 8;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);

            // Parse Button (now immediately after Clear)
            this.btnParse.Text = "Parse →";
            this.btnParse.Width = 90;
            this.btnParse.Left = this.btnClear.Right + 10;
            this.btnParse.Top = 8;
            this.btnParse.Click += new System.EventHandler(this.btnParse_Click);

            // Add to panel
            this.panelTop.Controls.Add(this.btnPasteClipboard);
            this.panelTop.Controls.Add(this.btnGetVMD);
            this.panelTop.Controls.Add(this.btnClear);
            this.panelTop.Controls.Add(this.btnParse);

            // DockPanel
            this.dockPanel.Dock = DockStyle.Fill;
            this.dockPanel.DocumentStyle = DocumentStyle.DockingWindow;

            // VMD View Dock
            this.dockVmdView = new WeifenLuo.WinFormsUI.Docking.DockContent();
            this.txtVmdView = new System.Windows.Forms.TextBox();
            this.txtVmdView.Dock = DockStyle.Fill;
            this.txtVmdView.Multiline = true;
            this.txtVmdView.ReadOnly = true;
            this.txtVmdView.ScrollBars = ScrollBars.Both;
            this.txtVmdView.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtVmdView.WordWrap = false;
            this.dockVmdView.Text = "VMD View";
            this.dockVmdView.Controls.Add(this.txtVmdView);

            // Tree View Dock
            this.treeView.Dock = DockStyle.Fill;
            this.treeView.Font = new System.Drawing.Font("Consolas", 10F);
            this.dockTreeView.Text = "Tree View";
            this.dockTreeView.Controls.Add(this.treeView);

            // Parse View Dock (Editable)
            this.txtParseView.Dock = DockStyle.Fill;
            this.txtParseView.Font = new System.Drawing.Font("Consolas", 10F);
            this.txtParseView.Multiline = true;
            this.txtParseView.ScrollBars = ScrollBars.Both;
            this.txtParseView.WordWrap = true;
            this.txtParseView.ReadOnly = false; // Editable
            this.txtParseView.MouseClick += new System.Windows.Forms.MouseEventHandler(this.txtParseView_MouseClick);
            this.dockParseView.Text = "Parse View";
            this.dockParseView.Controls.Add(this.txtParseView);

            // Message View Dock (main input)
            this.txtMessageRaw.Dock = DockStyle.Fill;
            this.txtMessageRaw.Multiline = true;
            this.txtMessageRaw.ScrollBars = ScrollBars.Both;
            this.txtMessageRaw.Font = new System.Drawing.Font("Consolas", 10F);
            this.txtMessageRaw.WordWrap = true;
            txtMessageRaw.ScrollBars = ScrollBars.Vertical; // only vertical scroll
            this.dockMessageView.Text = "Message View";
            this.dockMessageView.Controls.Add(this.txtMessageRaw);

            // Form layout
            this.Controls.Add(this.dockPanel);
            this.Controls.Add(this.panelTop);
            this.Load += (s, e) =>
            {
                this.dockMessageView.Show(this.dockPanel, DockState.DockLeft);
                this.dockParseView.Show(this.dockPanel, DockState.DockRight);
                this.dockTreeView.Show(this.dockPanel, DockState.DockBottom);
                this.dockMessageView.Show(this.dockPanel, WeifenLuo.WinFormsUI.Docking.DockState.Document);
                this.dockParseView.Show(this.dockPanel, WeifenLuo.WinFormsUI.Docking.DockState.DockRight);
                this.dockTreeView.Show(this.dockPanel, WeifenLuo.WinFormsUI.Docking.DockState.DockBottom);
                this.dockVmdView.Show(this.dockPanel, WeifenLuo.WinFormsUI.Docking.DockState.DockBottom);

            };
            this.Text = "HL7 Parser (Dockable Views)";
            this.ClientSize = new System.Drawing.Size(1200, 700);
        }
    }
}
