// Ocarune: Ocarina of Time on PC. Setup wizard: a window around install.ps1 (next to this exe).
// Build: csc /target:winexe /win32icon:oot.ico /out:Setup.exe Setup.cs /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:Microsoft.CSharp.dll
// Check the look without installing: Setup.exe --shot <folder>   (writes one PNG per page)
using System; using System.Diagnostics; using System.Drawing; using System.Drawing.Drawing2D; using System.IO; using System.Runtime.InteropServices; using System.Windows.Forms;

class Setup : Form {
    static readonly Color Bg = Color.FromArgb(19, 21, 24), Deep = Color.FromArgb(9, 11, 12), Card = Color.FromArgb(28, 31, 35), CardHi = Color.FromArgb(38, 42, 47),
        Rule = Color.FromArgb(48, 52, 58), Gold = Color.FromArgb(226, 184, 74), GoldHi = Color.FromArgb(244, 210, 120), GoldDk = Color.FromArgb(150, 118, 40),
        Txt = Color.FromArgb(238, 236, 228), Dim = Color.FromArgb(156, 160, 164), Faint = Color.FromArgb(102, 106, 112), Ok = Color.FromArgb(104, 196, 124), Bad = Color.FromArgb(226, 100, 88);
    static readonly string[] Steps = { "Welcome", "Game file", "Install", "Finish" };
    static float F = 1f;   // (the screen's scale: every pixel below is written for 96 dpi and goes through S)
    static bool Demo;      // (--shot: pretend install.ps1 is present)
    static int S(float v) { return (int)Math.Round(v * F); }
    static Font Ui(float pt, FontStyle st = FontStyle.Regular) { return new Font("Segoe UI", pt, st); }
    static readonly Font fBody = Ui(10), fSmall = Ui(9), fBold = Ui(10, FontStyle.Bold), fSemi = new Font("Segoe UI Semibold", 11.5f), fTitle = new Font("Segoe UI Semibold", 24),
        fBig = new Font("Segoe UI Semibold", 26), fMono = new Font("Consolas", 8.5f), fIcon = new Font("Segoe UI Symbol", 11), fKey = new Font("Segoe UI Semibold", 9);
    const int W = 920, H = 600, HeroW = 300, StepH = 64, FootH = 72, Pad = 40, CW = W - HeroW - 2 * Pad;   // CW: the width of the content column

    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr SendMessage(IntPtr h, int m, IntPtr w, string l);
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr h, int attr, ref int v, int size);
    [StructLayout(LayoutKind.Sequential)] struct MemStatus { public uint len, load; public ulong total, avail, pageTotal, pageAvail, virtTotal, virtAvail, ext; }
    [DllImport("kernel32.dll")] static extern bool GlobalMemoryStatusEx(ref MemStatus m);

    Panel[] pages = new Panel[4]; Panel footer; HeroPanel hero; StepBar stepper; int page; bool failed;
    StagePanel stages; Label doneDir = new Label(); DropZone nup; Field dir; Btn back, next, cancel, details; Bar bar = new Bar();
    Label status = new Label(), pct = new Label(), dirNote = new Label();
    TextBox log = new TextBox(); CheckBox launch = new CheckBox(), shortcut = new CheckBox(); Process proc;
    string here = AppDomain.CurrentDomain.BaseDirectory;
    const string ExeName = "OcarinaOfTime.exe";

    static bool Packed(string d) { return Demo || (File.Exists(Path.Combine(d, "install.ps1")) && Directory.Exists(Path.Combine(d, "src"))); }
    Setup() {
        if (!Packed(here)) { string alt = Path.GetFullPath(Path.Combine(here, @"..\Ocarune-Setup")) + @"\"; if (Packed(alt)) here = alt; }   // (run from the build folder: use the packaged one next to it)
        Text = "Ocarune Setup"; ClientSize = new Size(S(W), S(H)); FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false; StartPosition = FormStartPosition.CenterScreen; BackColor = Bg; ForeColor = Txt; Font = fBody; DoubleBuffered = true; AllowDrop = true;
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
        hero = new HeroPanel { Bounds = new Rectangle(0, 0, S(HeroW), S(H)) }; Controls.Add(hero);
        stepper = new StepBar(this) { Bounds = new Rectangle(S(HeroW), 0, S(W - HeroW), S(StepH)) }; Controls.Add(stepper);
        footer = new Panel { Bounds = new Rectangle(S(HeroW), S(H - FootH), S(W - HeroW), S(FootH)), BackColor = Bg }; Controls.Add(footer);
        footer.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Rule), 0, 0, footer.Width, 0);
        for (int i = 0; i < 4; i++) { pages[i] = new Panel { Bounds = new Rectangle(S(HeroW), S(StepH), S(W - HeroW), S(H - StepH - FootH)), BackColor = Bg, AllowDrop = true }; Controls.Add(pages[i]); }
        BuildWelcome(); BuildGame(); BuildProgress(); BuildDone();
        int cw = W - HeroW;
        next = Foot("Next", cw - Pad - 140, 140, true); back = Foot("Back", cw - Pad - 140 - 10 - 100, 100, false); cancel = Foot("Cancel", Pad, 100, false, true);
        cancel.Click += (s, e) => Close();
        back.Click += (s, e) => Go(page - 1);   // (one page back, whichever page this is: Game file to Welcome, a failed install to Game file)
        next.Click += (s, e) => Next();
        AcceptButton = next;
        DropTarget(this); foreach (var p in pages) DropTarget(p); DropTarget(nup);
        dir.tb.TextChanged += (s, e) => Refresh1(); nup.Changed += (s, e) => Refresh1();
        Go(0);
    }
    protected override void OnHandleCreated(EventArgs e) {
        base.OnHandleCreated(e);
        try { int one = 1; DwmSetWindowAttribute(Handle, 20, ref one, 4); int cap = Bg.R | (Bg.G << 8) | (Bg.B << 16); DwmSetWindowAttribute(Handle, 35, ref cap, 4); } catch { }   // (a dark title bar; Windows 11 also takes the colour)
    }

    // ---- pages ----
    void BuildWelcome() {
        Head(0, "Welcome to Ocarune", "Ocarina of Time, native on your PC.");
        Para(0, "This sets up a native Windows version of the game from your own copy, tuned for high resolution and a smooth frame rate. It takes a while once; after that you just press Play.", Pad, 100, CW, 66, Txt);
        string[,] req = Checks(); int tw = (CW - 12) / 2;
        for (int i = 0; i < 4; i++) {
            var t = new Tile { Bounds = new Rectangle(S(Pad + (i % 2) * (tw + 12)), S(170 + (i / 2) * 76), S(tw), S(64)), Title = req[i, 0], Detail = req[i, 2], Good = req[i, 1] == "1" }; pages[0].Controls.Add(t);
        }
        int cx = Pad; foreach (var t in new[] { "2K to 4K", "Frame generation", "FSR upscaling", "Gamepad and keyboard" }) { var ch = new Chip { Text = t }; ch.SetBounds(S(cx), S(334), ch.Want, S(28)); pages[0].Controls.Add(ch); cx += (int)(ch.Width / F) + 8; }
        var note = new CardPanel { Bounds = new Rectangle(S(Pad), S(380), S(CW), S(66)) }; pages[0].Controls.Add(note);
        note.Controls.Add(new Shield { Bounds = new Rectangle(S(16), S(16), S(28), S(34)) });
        note.Controls.Add(new Label { Text = "Your game stays yours. Nothing is bundled and your file never leaves this PC: only open build tools are downloaded, each checked against a known SHA-256.", ForeColor = Dim, BackColor = Card, Font = fSmall, Bounds = new Rectangle(S(58), S(10), S(CW - 74), S(48)) });
    }
    string[,] Checks() {
        var m = new MemStatus(); m.len = (uint)Marshal.SizeOf(typeof(MemStatus)); long ram = 0; try { if (GlobalMemoryStatusEx(ref m)) ram = (long)m.total; } catch { }
        bool vk = File.Exists(Path.Combine(Environment.SystemDirectory, "vulkan-1.dll"));
        long free = FreeGb(@"C:\Games\OoT");
        int build = 0; string prod = "Windows";
        try { var k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"); build = int.Parse((string)k.GetValue("CurrentBuildNumber")); prod = (string)k.GetValue("ProductName"); if (build >= 22000) prod = prod.Replace("Windows 10", "Windows 11"); } catch { }
        return new string[,] {
            { "Windows 10 or 11", build >= 10240 ? "1" : "0", prod },
            { "Vulkan graphics", vk ? "1" : "0", vk ? "Driver found" : "Update your graphics driver" },
            { "Memory", ram == 0 || ram >= 15L * 1024 * 1024 * 1024 ? "1" : "0", ram == 0 ? "Unknown" : (ram >> 30) + " GB (16 GB recommended)" },
            { "Disk space", free < 0 || free >= 10 ? "1" : "0", free < 0 ? "Unknown" : free + " GB free (20 GB recommended)" } };
    }
    static long FreeGb(string path) {
        try { return new DriveInfo(Path.GetPathRoot(Path.GetFullPath(path))).AvailableFreeSpace >> 30; } catch { return -1; }
    }
    void BuildGame() {
        Head(1, "Your game", "Choose your game file and where to install.");
        Para(1, "Game file", Pad, 104, CW, 20, Dim, fSmall);
        nup = new DropZone { Bounds = new Rectangle(S(Pad), S(128), S(CW), S(128)) }; pages[1].Controls.Add(nup);
        nup.Browse += (s, e) => { var d = new OpenFileDialog { Filter = "Switch 2 game package (*.nup)|*.nup|All files|*.*" }; if (d.ShowDialog() == DialogResult.OK) nup.Path = d.FileName; };
        Para(1, "Install folder", Pad, 282, CW, 20, Dim, fSmall);
        dir = new Field("") { Bounds = new Rectangle(S(Pad), S(306), S(CW - 112), S(40)) }; pages[1].Controls.Add(dir); dir.tb.Text = @"C:\Games\OoT";
        var b = new Btn { Text = "Change...", Bounds = new Rectangle(S(Pad + CW - 100), S(306), S(100), S(40)) }; pages[1].Controls.Add(b);
        b.Click += (s, e) => { var d = new FolderBrowserDialog(); if (d.ShowDialog() == DialogResult.OK) dir.tb.Text = d.SelectedPath; };
        dirNote.SetBounds(S(Pad), S(352), S(CW), S(22)); dirNote.Font = fSmall; pages[1].Controls.Add(dirNote);
        var note = new CardPanel { Bounds = new Rectangle(S(Pad), S(388), S(CW), S(58)) }; pages[1].Controls.Add(note);
        note.Controls.Add(new Label { Text = "i", Font = Ui(11, FontStyle.Bold), ForeColor = Gold, BackColor = Card, TextAlign = ContentAlignment.MiddleCenter, Bounds = new Rectangle(S(14), S(17), S(24), S(24)) });
        note.Controls.Add(new Label { Text = "The first build takes a while. The first start of the game then prepares its shaders once; later starts are fast.", ForeColor = Dim, BackColor = Card, Font = fSmall, Bounds = new Rectangle(S(50), S(9), S(CW - 66), S(42)) });
    }
    void BuildProgress() {
        Head(2, "Installing", "You can leave this window open in the background.");
        status.SetBounds(S(Pad), S(108), S(CW - 90), S(28)); status.ForeColor = Txt; status.Font = fSemi; pages[2].Controls.Add(status);
        pct.SetBounds(S(Pad + CW - 90), S(100), S(90), S(40)); pct.ForeColor = Gold; pct.TextAlign = ContentAlignment.MiddleRight; pct.Font = fBig; pages[2].Controls.Add(pct);
        bar.SetBounds(S(Pad), S(148), S(CW), S(10)); pages[2].Controls.Add(bar);
        stages = new StagePanel { Bounds = new Rectangle(S(Pad), S(178), S(CW), S(4 * 50 + 16)) }; pages[2].Controls.Add(stages);
        details = new Btn { Text = "Show details", Kind = 2, Bounds = new Rectangle(S(Pad), S(410), S(130), S(32)) };
        details.Click += (s, e) => { log.Visible = !log.Visible; stages.Visible = !log.Visible; details.Text = log.Visible ? "Hide details" : "Show details"; };
        pages[2].Controls.Add(details);
        log.SetBounds(S(Pad), S(178), S(CW), S(224)); log.Multiline = true; log.ReadOnly = true; log.ScrollBars = ScrollBars.Vertical; log.BackColor = Deep; log.ForeColor = Dim; log.BorderStyle = BorderStyle.None; log.Font = fMono; log.Visible = false;
        pages[2].Controls.Add(log);
    }
    void BuildDone() {
        Head(3, "You're ready", "Ocarune is installed.");
        pages[3].Controls.Add(new Badge { Bounds = new Rectangle(S(Pad), S(108), S(52), S(52)) });
        pages[3].Controls.Add(new Label { Text = "Installation complete", Font = fSemi, ForeColor = Txt, AutoSize = true, Location = new Point(S(Pad + 66), S(110)) });
        doneDir.ForeColor = Dim; doneDir.Font = fSmall; doneDir.SetBounds(S(Pad + 66), S(138), S(CW - 66), S(22)); pages[3].Controls.Add(doneDir);
        launch.Text = "Start the game now"; launch.Checked = true; launch.SetBounds(S(Pad), S(184), S(CW), S(26));
        shortcut.Text = "Add a desktop shortcut"; shortcut.Checked = true; shortcut.SetBounds(S(Pad), S(214), S(CW), S(26));
        pages[3].Controls.AddRange(new Control[] { launch, shortcut });
        pages[3].Controls.Add(new KeyCard { Bounds = new Rectangle(S(Pad), S(260), S(CW), S(148)) });
        pages[3].Controls.Add(new Label { Text = "The first start prepares shaders once and can take several minutes. Never share your game files. If you share this installer, share its source with it (GPL-3.0).", ForeColor = Faint, Font = fSmall, Bounds = new Rectangle(S(Pad), S(416), S(CW), S(36)) });
    }

    // ---- widgets ----
    Btn Foot(string t, int x, int w, bool primary, bool ghost = false) {
        var b = new Btn { Text = t, Kind = primary ? 1 : ghost ? 2 : 0, Bounds = new Rectangle(S(x), S(18), S(w), S(38)) };
        footer.Controls.Add(b); return b;
    }
    void Head(int p, string t, string sub) {
        pages[p].Controls.Add(new Label { Text = t, Font = fTitle, ForeColor = Txt, AutoSize = true, Location = new Point(S(Pad - 3), S(10)) });
        pages[p].Controls.Add(new Label { Text = sub, ForeColor = Dim, AutoSize = true, Location = new Point(S(Pad), S(58)) });
    }
    void Para(int p, string t, int x, int y, int w, int h, Color c, Font f = null) { pages[p].Controls.Add(new Label { Text = t, ForeColor = c, Font = f ?? fBody, Bounds = new Rectangle(S(x), S(y), S(w), S(h)) }); }
    void DropTarget(Control c) {
        c.AllowDrop = true;
        c.DragEnter += (s, e) => { var f = e.Data.GetData(DataFormats.FileDrop) as string[]; bool ok = f != null && f.Length == 1 && f[0].EndsWith(".nup", StringComparison.OrdinalIgnoreCase) && page <= 1; e.Effect = ok ? DragDropEffects.Copy : DragDropEffects.None; nup.Hot = ok && page == 1; };
        c.DragLeave += (s, e) => nup.Hot = false;
        c.DragDrop += (s, e) => { nup.Hot = false; var f = e.Data.GetData(DataFormats.FileDrop) as string[]; if (f != null && f.Length == 1) { nup.Path = f[0]; if (page == 0) Go(1); } };
    }

    // ---- flow ----
    void Go(int p) {
        page = p; for (int i = 0; i < 4; i++) pages[i].Visible = i == p;
        back.Visible = p == 1 || (p == 2 && failed); cancel.Visible = p != 3; cancel.Enabled = true; next.Visible = true;
        next.Text = p == 0 ? "Next" : p == 1 ? "Install" : p == 2 ? (failed ? "Retry" : "Installing...") : "Finish";
        next.Enabled = p == 2 ? failed : true;
        if (p == 1) Refresh1();
        if (p == 3) doneDir.Text = dir.tb.Text;
        stepper.Invalidate();
    }
    // The two entries checked as they are typed: what is wrong shows with the field, and Install waits until nothing is.
    string NupErr() {
        if (nup.Path.Length == 0) return "";
        if (!nup.Path.EndsWith(".nup", StringComparison.OrdinalIgnoreCase)) return "That is not a .nup file.";
        if (!File.Exists(nup.Path)) return "That file was not found.";
        return null;
    }
    string DirErr() {
        if (string.IsNullOrWhiteSpace(dir.tb.Text) || dir.tb.Text.IndexOfAny(Path.GetInvalidPathChars()) >= 0 || !Path.IsPathRooted(dir.tb.Text)) return "Choose a full, valid install folder.";
        long free = FreeGb(dir.tb.Text); if (free >= 0 && free < 10) return "Not enough free space there: " + free + " GB free, 10 GB needed.";
        if (!Packed(here)) return "install.ps1 and the src folder must sit next to Setup.exe.";
        return null;
    }
    void Refresh1() {
        string a = NupErr(), b = DirErr();
        if (a == null) { string sz = ""; try { sz = "  \u00b7  " + (new FileInfo(nup.Path).Length >> 20) + " MB"; } catch { } nup.Set(1, System.IO.Path.GetFileName(nup.Path), "Ready" + sz + "  \u00b7  click to change"); }
        else if (a.Length == 0) nup.Set(0, "Drop your .nup here", "or click to browse. Your own Ocarina of Time Switch 2 package.");
        else nup.Set(2, a, "Drop another file or click to browse.");
        if (b == null) { dirNote.ForeColor = Ok; long free = FreeGb(dir.tb.Text); dirNote.Text = "\u2714  " + (free < 0 ? "Ready" : free + " GB free"); }
        else { dirNote.ForeColor = Bad; dirNote.Text = b; }
        if (page == 1) next.Enabled = a == null && b == null;
    }
    void Next() {
        if (page == 0) Go(1);
        else if (page == 1) { if (!next.Enabled) return; failed = false; Go(2); Run(); }
        else if (page == 2 && failed) { failed = false; Go(2); Run(); }
        else if (page == 3) {
            string exe = Path.Combine(dir.tb.Text, ExeName);
            if (shortcut.Checked) try {
                dynamic sh = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
                var l = sh.CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Ocarina of Time.lnk"));
                l.TargetPath = exe; l.WorkingDirectory = dir.tb.Text; l.IconLocation = exe; l.Description = "The Legend of Zelda: Ocarina of Time"; l.Save();
            } catch { }
            if (launch.Checked) Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = dir.tb.Text });
            Close();
        }
    }
    void Run() {
        string args = "-NoProfile -ExecutionPolicy Bypass -File \"" + Path.Combine(here, "install.ps1") + "\" -Nup \"" + nup.Path + "\" -Out \"" + dir.tb.Text + "\"";
        proc = new Process { StartInfo = new ProcessStartInfo("powershell.exe", args) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = here }, EnableRaisingEvents = true };
        DataReceivedEventHandler h = (s, e) => { if (e.Data != null) BeginInvoke((Action)(() => Line(e.Data))); };
        proc.OutputDataReceived += h; proc.ErrorDataReceived += h;
        proc.Exited += (s, e) => BeginInvoke((Action)Finished);
        status.Text = "Starting..."; status.ForeColor = Txt; stages.Cur = 0; stages.Fail = false; stages.Invalidate(); pct.Text = "0%"; log.Clear(); bar.Value = 0; bar.Invalidate();
        proc.Start(); proc.BeginOutputReadLine(); proc.BeginErrorReadLine();
    }
    void Line(string l) {
        if (l.StartsWith("@@ ")) { var p = l.Substring(3).Split(new[] { ' ' }, 2); int v; if (int.TryParse(p[0], out v)) { bar.Value = Math.Max(0, Math.Min(100, v)); bar.Invalidate(); pct.Text = bar.Value + "%"; stages.Cur = v < 65 ? 0 : v < 72 ? 1 : v < 78 ? 2 : v < 100 ? 3 : 4; stages.Invalidate(); } status.Text = p.Length > 1 ? p[1] : ""; }
        else log.AppendText(l + "\r\n");
    }
    void Finished() {
        if (proc.ExitCode == 0 && File.Exists(Path.Combine(dir.tb.Text, ExeName))) { bar.Value = 100; bar.Invalidate(); stages.Cur = 4; Go(3); return; }
        failed = true; stages.Fail = true; stages.Invalidate(); status.Text = "Setup stopped. Open the details to see why."; status.ForeColor = Bad; log.Visible = true; stages.Visible = false; details.Text = "Hide details";
        Go(2);
    }
    protected override void OnFormClosing(FormClosingEventArgs e) {
        bool running = false; try { running = proc != null && !proc.HasExited; } catch { }
        if (running) {
            if (MessageBox.Show(this, "The installation is still running. Stop it and close?", "Setup", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) { e.Cancel = true; return; }
            try { proc.Kill(); } catch { }
        }
        base.OnFormClosing(e);
    }

    // ---- screenshots (--shot <folder>): every page as a PNG, no install ----
    void Shot(string folder) {
        Directory.CreateDirectory(folder);
        string fake = Path.Combine(Path.GetTempPath(), "Ocarina of Time [0400486024DEC000][v131072].nup");
        using (var fs = File.Create(fake)) fs.SetLength(5L * 1024 * 1024 * 1024);
        var shotHandle = Handle; Opacity = 0; StartPosition = FormStartPosition.Manual; Location = new Point(-5000, -5000); Show();
        Action<string> snap = n => { Application.DoEvents(); using (var bmp = new Bitmap(ClientSize.Width, ClientSize.Height)) { DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height)); bmp.Save(Path.Combine(folder, n + ".png")); } };
        Go(0); snap("1-welcome");
        Go(1); snap("2-game-empty");
        nup.Path = fake; snap("3-game-ready");
        nup.Path = fake.Replace(".nup", ".txt"); snap("4-game-error");
        nup.Path = fake;
        Go(2); status.Text = "Building the game"; bar.Value = 82; pct.Text = "82%"; stages.Cur = 3; snap("5-install");
        failed = true; stages.Fail = true; status.Text = "Setup stopped. Open the details to see why."; status.ForeColor = Bad; log.Visible = true; stages.Visible = false; log.Text = "error: example line\r\nsecond line"; Go(2); snap("6-failed");
        failed = false; Go(3); snap("7-done");
        try { File.Delete(fake); } catch { }
    }

    // ---- custom controls ----
    static GraphicsPath RR(RectangleF r, float d) {
        var p = new GraphicsPath(); p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90); p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90); p.CloseFigure(); return p;
    }
    static void Quality(Graphics g) { g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit; }
    static void Fill(Graphics g, RectangleF r, float rad, Color fill, Color edge) {
        using (var p = RR(r, rad)) { using (var b = new SolidBrush(fill)) g.FillPath(b, p); if (edge != Color.Empty) using (var pen = new Pen(edge)) g.DrawPath(pen, p); }
    }
    // Kind 0 plain, 1 gold (the main action), 2 text only
    class Btn : Button {
        public int Kind; bool hot, down;
        public Btn() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); Cursor = Cursors.Hand; FlatStyle = FlatStyle.Flat; Font = fBody; }
        protected override void OnMouseEnter(EventArgs e) { hot = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hot = down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override void OnPaint(PaintEventArgs e) {
            var g = e.Graphics; Quality(g); g.Clear(Parent != null ? Parent.BackColor : Bg);
            Color fill = Kind == 1 ? (!Enabled ? Color.FromArgb(66, 58, 36) : down ? GoldDk : hot ? GoldHi : Gold) : Kind == 2 ? (hot ? Card : Bg) : (down ? Color.FromArgb(48, 52, 58) : hot ? CardHi : Card);
            Color edge = Kind == 1 ? fill : Kind == 2 ? fill : (Focused ? Gold : Rule);
            Color txt = !Enabled ? Faint : Kind == 1 ? Deep : Kind == 2 ? (hot ? Txt : Dim) : Txt;
            Fill(g, new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), S(10), fill, edge);
            if (Focused && ShowFocusCues && Kind == 1) using (var p = RR(new RectangleF(S(3), S(3), Width - S(6) - 1, Height - S(6) - 1), S(7))) using (var pen = new Pen(Deep, 1.5f)) g.DrawPath(pen, p);
            TextRenderer.DrawText(g, Text, Kind == 1 ? fBold : Font, new Rectangle(0, 0, Width, Height), txt, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }
    }
    class CardPanel : Panel {
        public CardPanel() { DoubleBuffered = true; BackColor = Bg; }
        protected override void OnPaint(PaintEventArgs e) { Quality(e.Graphics); Fill(e.Graphics, new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), S(12), Card, Rule); }
    }
    // A system check: a coloured mark, what was checked, and what was found
    class Tile : Control {
        public string Title = "", Detail = ""; public bool Good;
        public Tile() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true); }
        protected override void OnPaint(PaintEventArgs e) {
            var g = e.Graphics; Quality(g); g.Clear(Bg);
            Fill(g, new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), S(12), Card, Rule);
            var d = new Rectangle(S(16), (Height - S(28)) / 2, S(28), S(28)); Color c = Good ? Ok : Bad;
            using (var b = new SolidBrush(Color.FromArgb(40, c))) g.FillEllipse(b, d);
            using (var p = new Pen(c, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round }) {
                if (Good) g.DrawLines(p, new[] { new PointF(d.X + d.Width * 0.28f, d.Y + d.Height * 0.52f), new PointF(d.X + d.Width * 0.44f, d.Y + d.Height * 0.68f), new PointF(d.X + d.Width * 0.73f, d.Y + d.Height * 0.34f) });
                else { g.DrawLine(p, d.X + d.Width * 0.32f, d.Y + d.Height * 0.32f, d.X + d.Width * 0.68f, d.Y + d.Height * 0.68f); g.DrawLine(p, d.X + d.Width * 0.68f, d.Y + d.Height * 0.32f, d.X + d.Width * 0.32f, d.Y + d.Height * 0.68f); }
            }
            int tx = S(58), w = Width - tx - S(10);
            TextRenderer.DrawText(g, Title, fBold, new Rectangle(tx, S(11), w, S(22)), Txt, TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
            TextRenderer.DrawText(g, Detail, fSmall, new Rectangle(tx, S(33), w, S(20)), Dim, TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
        }
    }
    // The game file: a drop target that says what it holds. State 0 empty, 1 ready, 2 wrong
    class DropZone : Control {
        string path = ""; int kind; string l1 = "", l2 = ""; bool hot, over; public event EventHandler Changed, Browse;
        public DropZone() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true); Cursor = Cursors.Hand; TabStop = true; }
        public string Path { get { return path; } set { path = value ?? ""; if (Changed != null) Changed(this, EventArgs.Empty); } }
        public bool Hot { get { return hot; } set { if (hot != value) { hot = value; Invalidate(); } } }
        public void Set(int k, string a, string b) { kind = k; l1 = a; l2 = b; Invalidate(); }
        protected override void OnMouseEnter(EventArgs e) { over = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { over = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnClick(EventArgs e) { if (Browse != null) Browse(this, EventArgs.Empty); base.OnClick(e); }
        protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode == Keys.Space && Browse != null) Browse(this, EventArgs.Empty); base.OnKeyDown(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override void OnPaint(PaintEventArgs e) {
            var g = e.Graphics; Quality(g); g.Clear(Bg);
            Color edge = kind == 1 ? Ok : kind == 2 ? Bad : (hot || over || Focused) ? Gold : Faint;
            var r = new RectangleF(1, 1, Width - 3, Height - 3);
            Fill(g, r, S(14), hot || over ? CardHi : Card, Color.Empty);
            using (var p = RR(r, S(14))) using (var pen = new Pen(edge, 1.5f)) { if (kind == 0) pen.DashStyle = DashStyle.Dash; g.DrawPath(pen, p); }
            // icon: a disc with an arrow (empty), a tick (ready) or a cross (wrong)
            var d = new Rectangle((Width - S(44)) / 2, S(16), S(44), S(44)); Color c = kind == 1 ? Ok : kind == 2 ? Bad : Gold;
            using (var b = new SolidBrush(Color.FromArgb(38, c))) g.FillEllipse(b, d);
            using (var p = new Pen(c, 2.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round }) {
                float x = d.X, y = d.Y, w = d.Width, h = d.Height;
                if (kind == 1) g.DrawLines(p, new[] { new PointF(x + w * 0.28f, y + h * 0.52f), new PointF(x + w * 0.44f, y + h * 0.68f), new PointF(x + w * 0.73f, y + h * 0.34f) });
                else if (kind == 2) { g.DrawLine(p, x + w * 0.32f, y + h * 0.32f, x + w * 0.68f, y + h * 0.68f); g.DrawLine(p, x + w * 0.68f, y + h * 0.32f, x + w * 0.32f, y + h * 0.68f); }
                else { g.DrawLine(p, x + w * 0.5f, y + h * 0.26f, x + w * 0.5f, y + h * 0.62f); g.DrawLines(p, new[] { new PointF(x + w * 0.34f, y + h * 0.48f), new PointF(x + w * 0.5f, y + h * 0.64f), new PointF(x + w * 0.66f, y + h * 0.48f) }); g.DrawLine(p, x + w * 0.3f, y + h * 0.76f, x + w * 0.7f, y + h * 0.76f); }
            }
            var fl = TextFormatFlags.HorizontalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;
            TextRenderer.DrawText(g, l1, fSemi, new Rectangle(S(16), S(68), Width - S(32), S(26)), kind == 2 ? Bad : Txt, fl);
            TextRenderer.DrawText(g, l2, fSmall, new Rectangle(S(16), S(95), Width - S(32), S(22)), Dim, fl);
        }
    }
    // A text box in a rounded frame that turns gold while it has the focus
    class Field : Panel {
        public TextBox tb = new TextBox();
        public Field(string cue) {
            DoubleBuffered = true; BackColor = Bg;
            tb.BorderStyle = BorderStyle.None; tb.BackColor = Card; tb.ForeColor = Txt; tb.Font = Ui(10.5f);
            tb.GotFocus += (s, e) => Invalidate(); tb.LostFocus += (s, e) => Invalidate();
            tb.HandleCreated += (s, e) => { if (cue.Length > 0) SendMessage(tb.Handle, 0x1501, (IntPtr)1, cue); };
            Controls.Add(tb);
        }
        protected override void OnLayout(LayoutEventArgs e) { base.OnLayout(e); tb.SetBounds(S(14), (Height - tb.PreferredHeight) / 2, Width - S(28), tb.PreferredHeight); }
        protected override void OnPaint(PaintEventArgs e) { Quality(e.Graphics); Fill(e.Graphics, new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), S(10), Card, tb.Focused ? Gold : Rule); }
    }
    class Chip : Control {
        public int Want;
        public Chip() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true); Font = fSmall; }
        protected override void OnTextChanged(EventArgs e) { Want = TextRenderer.MeasureText(Text, fSmall).Width + S(24); base.OnTextChanged(e); }
        protected override void OnPaint(PaintEventArgs e) {
            var g = e.Graphics; Quality(g); g.Clear(Bg);
            Fill(g, new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), Height - 1, Color.FromArgb(34, 30, 20), GoldDk);
            TextRenderer.DrawText(g, Text, fSmall, new Rectangle(0, 0, Width, Height), GoldHi, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }
    }
    class Shield : Control {
        public Shield() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true); BackColor = Card; }
        protected override void OnPaint(PaintEventArgs e) {
            var g = e.Graphics; Quality(g); g.Clear(Card); float w = Width - 2, h = Height - 2;
            using (var p = new GraphicsPath()) {
                p.AddLine(w / 2 + 1, 1, w + 1, h * 0.18f); p.AddLine(w + 1, h * 0.18f, w + 1, h * 0.52f); p.AddBezier(w + 1, h * 0.52f, w + 1, h * 0.8f, w / 2 + 1, h + 1, w / 2 + 1, h + 1);
                p.AddBezier(w / 2 + 1, h + 1, w / 2 + 1, h + 1, 1, h * 0.8f, 1, h * 0.52f); p.AddLine(1, h * 0.52f, 1, h * 0.18f); p.CloseFigure();
                using (var b = new SolidBrush(Color.FromArgb(46, Gold))) g.FillPath(b, p); using (var pen = new Pen(Gold, 1.6f)) g.DrawPath(pen, p);
            }
            using (var pen = new Pen(Gold, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round }) g.DrawLines(pen, new[] { new PointF(w * 0.3f, h * 0.45f), new PointF(w * 0.46f, h * 0.6f), new PointF(w * 0.74f, h * 0.3f) });
        }
    }
    class Badge : Control {
        public Badge() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true); }
        protected override void OnPaint(PaintEventArgs e) {
            var g = e.Graphics; Quality(g); g.Clear(Bg);
            using (var b = new LinearGradientBrush(ClientRectangle, GoldHi, GoldDk, 45f)) g.FillEllipse(b, 1, 1, Width - 3, Height - 3);
            using (var p = new Pen(Deep, S(4)) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                g.DrawLines(p, new[] { new PointF(Width * 0.28f, Height * 0.52f), new PointF(Width * 0.44f, Height * 0.68f), new PointF(Width * 0.73f, Height * 0.34f) });
        }
    }
    // The first keys to know, shown as keycaps
    class KeyCard : Panel {
        static readonly string[][] Rows = { new[] { "Insert", "F1" }, new[] { "F11" }, new[] { "Esc" } };
        static readonly string[,] Txts = { { "Open the settings", "Picture, frame rate, controls. Select + Start on a controller." }, { "Fullscreen", "Switch between windowed and fullscreen." }, { "Close the menu", "Back to the game." } };
        public KeyCard() { DoubleBuffered = true; BackColor = Bg; }
        protected override void OnPaint(PaintEventArgs e) {
            var g = e.Graphics; Quality(g); Fill(g, new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), S(12), Card, Rule);
            for (int i = 0; i < 3; i++) {
                int y = S(12 + i * 44), x = S(18);
                foreach (var k in Rows[i]) {
                    int kw = Math.Max(S(34), TextRenderer.MeasureText(k, fKey).Width + S(18));
                    Fill(g, new RectangleF(x, y + S(4), kw, S(28)), S(7), CardHi, Rule);
                    using (var p = new Pen(Color.FromArgb(70, 76, 84), 2f)) g.DrawLine(p, x + S(5), y + S(4) + S(28) - 1, x + kw - S(5), y + S(4) + S(28) - 1);
                    TextRenderer.DrawText(g, k, fKey, new Rectangle(x, y + S(4), kw, S(27)), Txt, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
                    x += kw + S(6);
                }
                int tx = S(130);
                TextRenderer.DrawText(g, Txts[i, 0], fBold, new Rectangle(tx, y, Width - tx - S(10), S(20)), Txt, TextFormatFlags.SingleLine);
                TextRenderer.DrawText(g, Txts[i, 1], fSmall, new Rectangle(tx, y + S(19), Width - tx - S(10), S(18)), Dim, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
            }
        }
    }
    // The install's stages as a list: done ticked, the current one spinning, the rest waiting
    class StagePanel : Panel {
        public int Cur; public bool Fail; int ang; Timer t = new Timer { Interval = 60 };
        static readonly string[,] St = { { "Download build tools", "Open tools, checked against known hashes" }, { "Prepare Python", "The tool runtime" }, { "Check your game file", "Reads your .nup, nothing leaves your PC" }, { "Build the game", "The long step: roughly 10 to 30 minutes" } };
        public StagePanel() { DoubleBuffered = true; BackColor = Bg; t.Tick += (s, e) => { ang = (ang + 24) % 360; if (Visible && Cur < 4 && !Fail) Invalidate(); }; t.Start(); }
        protected override void OnPaint(PaintEventArgs e) {
            var g = e.Graphics; Quality(g);
            Fill(g, new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), S(12), Card, Rule);
            for (int i = 0; i < 4; i++) {
                int y = S(8 + i * 50); bool done = i < Cur, cur = i == Cur; var d = new Rectangle(S(22), y + S(13), S(24), S(24));
                if (cur) using (var p = RR(new RectangleF(S(6), y + 2, Width - S(12), S(46)), S(9))) using (var b = new SolidBrush(CardHi)) g.FillPath(b, p);
                if (done) { using (var b = new SolidBrush(Gold)) g.FillEllipse(b, d); using (var p = new Pen(Deep, 2.2f)) g.DrawLines(p, new[] { new PointF(d.X + S(6), d.Y + S(12)), new PointF(d.X + S(10), d.Y + S(16)), new PointF(d.X + S(17), d.Y + S(8)) }); }
                else if (cur && Fail) { using (var b = new SolidBrush(Bad)) g.FillEllipse(b, d); using (var p = new Pen(Deep, 2.2f)) { g.DrawLine(p, d.X + S(8), d.Y + S(8), d.Right - S(8), d.Bottom - S(8)); g.DrawLine(p, d.Right - S(8), d.Y + S(8), d.X + S(8), d.Bottom - S(8)); } }
                else if (cur) { using (var p = new Pen(Rule, 2f)) g.DrawEllipse(p, d); using (var p = new Pen(Gold, 2.5f)) g.DrawArc(p, d, ang, 100); }
                else using (var p = new Pen(Rule, 1.5f)) g.DrawEllipse(p, d);
                TextRenderer.DrawText(g, St[i, 0], cur ? fBold : fBody, new Point(S(60), y + S(6)), cur || done ? Txt : Faint, TextFormatFlags.NoPadding);
                TextRenderer.DrawText(g, St[i, 1], fSmall, new Point(S(60), y + S(26)), cur ? Dim : Faint, TextFormatFlags.NoPadding);
            }
        }
    }
    class Bar : ProgressBar {
        public Bar() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true); }
        protected override void OnPaint(PaintEventArgs e) {
            var g = e.Graphics; Quality(g); g.Clear(Bg);
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            Fill(g, r, Height - 1, Card, Rule);
            int w = (int)((Width - 1) * (Value / 100.0)); if (w < Height) w = Value > 0 ? Height : 0;
            if (w > 0) using (var p = RR(new Rectangle(0, 0, w, Height - 1), Height - 1)) using (var b = new LinearGradientBrush(r, GoldDk, GoldHi, 0f)) g.FillPath(b, p);
        }
    }
    // The four steps along the top of the content
    class StepBar : Panel {
        Setup s;
        public StepBar(Setup s) { this.s = s; DoubleBuffered = true; BackColor = Bg; }
        protected override void OnPaint(PaintEventArgs e) {
            var g = e.Graphics; Quality(g); int cy = Height / 2, x = S(Pad), d = S(26);
            for (int i = 0; i < Steps.Length; i++) {
                bool cur = i == s.page, done = i < s.page; var dot = new Rectangle(x, cy - d / 2, d, d);
                if (cur) { using (var b = new SolidBrush(Gold)) g.FillEllipse(b, dot); TextRenderer.DrawText(g, (i + 1).ToString(), fKey, dot, Deep, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding); }
                else if (done) { using (var b = new SolidBrush(GoldDk)) g.FillEllipse(b, dot); using (var p = new Pen(Deep, 2f)) g.DrawLines(p, new[] { new PointF(dot.X + d * 0.28f, dot.Y + d * 0.52f), new PointF(dot.X + d * 0.43f, dot.Y + d * 0.67f), new PointF(dot.X + d * 0.72f, dot.Y + d * 0.34f) }); }
                else { using (var p = new Pen(Rule, 1.5f)) g.DrawEllipse(p, dot); TextRenderer.DrawText(g, (i + 1).ToString(), fKey, dot, Faint, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding); }
                int lw = TextRenderer.MeasureText(Steps[i], cur ? fBold : fBody).Width;
                TextRenderer.DrawText(g, Steps[i], cur ? fBold : fBody, new Point(x + d + S(10), cy - S(10)), cur ? Txt : done ? Dim : Faint, TextFormatFlags.NoPadding);
                x += d + S(10) + lw + S(14);
                if (i < Steps.Length - 1) { using (var p = new Pen(done ? GoldDk : Rule, 1.5f)) g.DrawLine(p, x, cy, x + S(22), cy); x += S(22) + S(14); }
            }
            using (var p = new Pen(Rule)) g.DrawLine(p, 0, Height - 1, Width, Height - 1);
        }
    }
    // The left side: the brand, drawn (no pictures are bundled)
    class HeroPanel : Panel {
        public HeroPanel() { DoubleBuffered = true; BackColor = Deep; }
        protected override void OnPaint(PaintEventArgs e) {
            var g = e.Graphics; Quality(g);
            using (var b = new LinearGradientBrush(ClientRectangle, Color.FromArgb(16, 44, 48), Deep, 80f)) g.FillRectangle(b, ClientRectangle);
            using (var p = new Pen(Rule)) g.DrawLine(p, Width - 1, 0, Width - 1, Height);
            float sz = S(60), gap = S(4), left = S(Pad), top = S(60), cx = left + sz + gap / 2;
            var glow = new Rectangle((int)(cx - S(150)), (int)(top - S(70)), S(300), S(300));
            using (var gp = new GraphicsPath()) { gp.AddEllipse(glow); using (var pb = new PathGradientBrush(gp) { CenterColor = Color.FromArgb(64, Gold), SurroundColors = new[] { Color.FromArgb(0, Gold) } }) g.FillPath(pb, gp); }
            // (watermark: a large, faint triforce bleeding off the bottom of the panel)
            using (var wb = new SolidBrush(Color.FromArgb(14, Gold))) {
                float ws = S(120), wg = S(6), wx = Width - S(20) - ws / 2, wy = Height - S(250);
                g.FillPolygon(wb, new[] { new PointF(wx, wy), new PointF(wx - ws / 2, wy + ws * 0.86f), new PointF(wx + ws / 2, wy + ws * 0.86f) });
                g.FillPolygon(wb, new[] { new PointF(wx - ws / 2 - wg, wy + ws * 0.86f + wg), new PointF(wx - ws - wg, wy + ws * 1.72f + wg), new PointF(wx - wg, wy + ws * 1.72f + wg) });
                g.FillPolygon(wb, new[] { new PointF(wx + ws / 2 + wg, wy + ws * 0.86f + wg), new PointF(wx + wg, wy + ws * 1.72f + wg), new PointF(wx + ws + wg, wy + ws * 1.72f + wg) });
            }
            Action<PointF, PointF, PointF> tri = (a, b2, c2) => {
                var pts = new[] { a, b2, c2 }; float y0 = Math.Min(a.Y, Math.Min(b2.Y, c2.Y)), y1 = Math.Max(a.Y, Math.Max(b2.Y, c2.Y));
                using (var br = new LinearGradientBrush(new PointF(0, y0), new PointF(0, y1), GoldHi, GoldDk)) g.FillPolygon(br, pts);
            };
            float tx = left + sz;
            tri(new PointF(tx, top), new PointF(tx - sz / 2, top + sz * 0.86f), new PointF(tx + sz / 2, top + sz * 0.86f));
            tri(new PointF(tx - sz / 2 - gap, top + sz * 0.86f + gap), new PointF(tx - sz - gap, top + sz * 1.72f + gap), new PointF(tx - gap, top + sz * 1.72f + gap));
            tri(new PointF(tx + sz / 2 + gap, top + sz * 0.86f + gap), new PointF(tx + gap, top + sz * 1.72f + gap), new PointF(tx + sz + gap, top + sz * 1.72f + gap));
            using (var f = new Font("Segoe UI", 9, FontStyle.Bold)) TextRenderer.DrawText(g, "O  C  A  R  U  N  E", f, new Point(S(Pad), S(216)), Gold, TextFormatFlags.NoPadding);
            using (var f = new Font("Georgia", 31, FontStyle.Bold)) {
                TextRenderer.DrawText(g, "Ocarina", f, new Point(S(Pad - 2), S(240)), Txt, TextFormatFlags.NoPadding);
                TextRenderer.DrawText(g, "of Time", f, new Point(S(Pad - 2), S(284)), Gold, TextFormatFlags.NoPadding);
            }
            TextRenderer.DrawText(g, "Native on your PC.\nUp to 4K, frame generation,\nFSR upscaling.", fBody, new Rectangle(S(Pad), S(348), Width - S(Pad) - S(20), S(70)), Dim, TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(g, "Free and open source  \u00b7  GPL-3.0", fSmall, new Point(S(Pad), Height - S(62)), Faint, TextFormatFlags.NoPadding);
            TextRenderer.DrawText(g, "github.com/TechHookDev/Ocarune", fSmall, new Point(S(Pad), Height - S(42)), Dim, TextFormatFlags.NoPadding);
        }
    }
    [STAThread] static void Main(string[] a) {
        try { SetProcessDPIAware(); } catch { }
        using (var g = Graphics.FromHwnd(IntPtr.Zero)) F = g.DpiX / 96f;
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        if (a.Length == 2 && a[0] == "--shot") { Demo = true; var s = new Setup(); s.Shot(a[1]); return; }
        Application.Run(new Setup());
    }
}
