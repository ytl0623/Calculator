// dotnet run
// dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true

using System;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MyCalcApp
{
    // ==========================================
    // 1. 啟動畫面 (Splash Screen)
    // ==========================================
    class SplashForm : Form
    {
        public SplashForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(400, 400);
            this.TransparencyKey = this.BackColor = Color.White; 

            PictureBox pb = new PictureBox();
            pb.Dock = DockStyle.Fill;
            pb.SizeMode = PictureBoxSizeMode.CenterImage; 

            // 資源名稱格式： "命名空間.檔名"
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var resourceName = "MyCalc.loading.gif"; 

            Stream stream = assembly.GetManifestResourceStream(resourceName);

            if (stream != null)
            {
                // 從記憶體串流建立圖片
                pb.Image = Image.FromStream(stream);
            }
            else
            {
                // 如果讀取失敗 (例如檔名打錯)，顯示文字備案
                Label lbl = new Label { Text = "Loading...", Font = new Font("Arial", 24), AutoSize = true, Location = new Point(80, 130) };
                this.Controls.Add(lbl);
            }
            this.Controls.Add(pb);

            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 1300; 
            timer.Tick += (s, e) => { timer.Stop(); this.Close(); };
            timer.Start();
        }
    }

    // ==========================================
    // 2. [新增] 全螢幕圖片檢視視窗
    // ==========================================
    class FullScreenForm : Form
    {
        public FullScreenForm(Image image)
        {
            // 設定全螢幕屬性
            this.FormBorderStyle = FormBorderStyle.None; // 無邊框
            this.WindowState = FormWindowState.Maximized; // 最大化
            this.BackColor = Color.Black; // 背景全黑
            this.TopMost = true; // 最上層顯示

            // 建立圖片框
            PictureBox pb = new PictureBox();
            pb.Image = image;
            pb.Dock = DockStyle.Fill;
            pb.SizeMode = PictureBoxSizeMode.Zoom; // 保持比例縮放
            this.Controls.Add(pb);

            // 加入關閉事件 (點擊或按 Esc)
            pb.Click += (s, e) => this.Close();
            this.Click += (s, e) => this.Close();
            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) this.Close(); };
        }
    }

    // ==========================================
    // 3. 主視窗
    // ==========================================
    class MainForm : Form
    {
        private TabControl tabControl;

        public MainForm()
        {
            this.Text = "20260109";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;

            tabControl = new TabControl { Dock = DockStyle.Fill };
            tabControl.TabPages.Add(CreateCalculatorTab());
            tabControl.TabPages.Add(CreateDemuraTab());

            this.Controls.Add(tabControl);
        }

        // --- 分頁 1: 計算機 ---
        private TabPage CreateCalculatorTab()
        {
            TabPage tab = new TabPage("Calculator");
            Panel centerPanel = new Panel { Size = new Size(300, 220), Location = new Point(350, 200) };
            
            TextBox input = new TextBox { Dock = DockStyle.Top, Font = new Font("Arial", 20) };
            Button btn = new Button { Dock = DockStyle.Top, Text = "計算 (=)", Height = 40, Font = new Font("微軟正黑體", 12) };
            Label result = new Label { Dock = DockStyle.Fill, Font = new Font("Arial", 20), TextAlign = ContentAlignment.MiddleCenter, Text = "0", BackColor = Color.WhiteSmoke, BorderStyle = BorderStyle.FixedSingle };

            btn.Click += (s, e) => {
                try { result.Text = new DataTable().Compute(input.Text, null).ToString(); }
                catch { result.Text = "Error"; }
            };
            input.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) btn.PerformClick(); };

            Label lblCopyright = new Label { 
                Text = "© 2026 Can Liu. All rights reserved.", 
                Dock = DockStyle.Bottom, 
                ForeColor = Color.DimGray,
                Font = new Font("Arial", 8),
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 30
            };    

            centerPanel.Controls.Add(result);
            centerPanel.Controls.Add(btn);
            centerPanel.Controls.Add(input);
            centerPanel.Controls.Add(lblCopyright);
            tab.Controls.Add(centerPanel);
            return tab;
        }

        // --- 分頁 2: Demura ---
        private string pathInput, pathC0, pathC1, pathC2, pathC3;
        private Label lblInput, lblC0, lblC1, lblC2, lblC3, lblStatus;
        private PictureBox pbResult;

        private TabPage CreateDemuraTab()
        {
            TabPage tab = new TabPage("Demura");
            Panel panelLeft = new Panel { Dock = DockStyle.Left, Width = 300, Padding = new Padding(10), BackColor = Color.LightGray };
            
            Control CreateFileRow(string title, ref Label pathLabel, Action<string> onSelect)
            {
                Panel p = new Panel { Height = 60, Dock = DockStyle.Top };
                Button btn = new Button { Text = title, Height = 30, Width = 80, Location = new Point(5, 5) };
                Label lbl = new Label { Text = "尚未選擇", Location = new Point(90, 10), AutoSize = true, ForeColor = Color.DarkSlateGray };
                pathLabel = lbl;

                btn.Click += (s, e) => {
                    using (OpenFileDialog ofd = new OpenFileDialog())
                    {
                        ofd.Filter = "Image Files|*.bmp;*.jpg;*.png";
                        if (ofd.ShowDialog() == DialogResult.OK) {
                            lbl.Text = Path.GetFileName(ofd.FileName);
                            onSelect(ofd.FileName);
                        }
                    }
                };
                p.Controls.Add(btn);
                p.Controls.Add(lbl);
                return p;
            }

            // 建立運算按鈕
            Button btnRun = new Button { Text = "開始運算", Dock = DockStyle.Bottom, Height = 50, Font = new Font("微軟正黑體", 12, FontStyle.Bold), BackColor = Color.SteelBlue, ForeColor = Color.White };
            lblStatus = new Label { Text = "準備就緒", Dock = DockStyle.Bottom, Height = 30, TextAlign = ContentAlignment.MiddleCenter };

            // [新增] 全螢幕按鈕
            Button btnFullScreen = new Button { Text = "全螢幕檢視結果", Dock = DockStyle.Bottom, Height = 40, Font = new Font("微軟正黑體", 10), BackColor = Color.DimGray, ForeColor = Color.White };
            
            // 全螢幕按鈕事件
            btnFullScreen.Click += (s, e) => {
                if (pbResult.Image == null) {
                    MessageBox.Show("請先執行運算產生結果圖！", "提示");
                    return;
                }
                // 開啟全螢幕視窗
                using (FullScreenForm f = new FullScreenForm(pbResult.Image)) {
                    f.ShowDialog();
                }
            };

            btnRun.Click += (s, e) => RunDemuraProcess();

            Label lblCopyright = new Label { 
                Text = "© 2026 Can Liu. All rights reserved.", 
                Dock = DockStyle.Bottom, 
                ForeColor = Color.DimGray,
                Font = new Font("Arial", 8),
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 30
            };
            
            // 注意：Dock 是由下往上堆疊，所以加入順序很重要
            panelLeft.Controls.Add(lblStatus);
            panelLeft.Controls.Add(btnRun);
            panelLeft.Controls.Add(btnFullScreen);
            panelLeft.Controls.Add(lblCopyright);

            panelLeft.Controls.Add(CreateFileRow("CP 3", ref lblC3, p => pathC3 = p));
            panelLeft.Controls.Add(CreateFileRow("CP 2", ref lblC2, p => pathC2 = p));
            panelLeft.Controls.Add(CreateFileRow("CP 1", ref lblC1, p => pathC1 = p));
            panelLeft.Controls.Add(CreateFileRow("CP 0", ref lblC0, p => pathC0 = p));
            panelLeft.Controls.Add(CreateFileRow("原始圖", ref lblInput, p => pathInput = p));

            pbResult = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Black };
            // 點兩下結果圖也可以全螢幕
            pbResult.DoubleClick += (s, e) => btnFullScreen.PerformClick();

            tab.Controls.Add(pbResult);
            tab.Controls.Add(panelLeft);
            return tab;
        }

        private async void RunDemuraProcess()
        {
            if (string.IsNullOrEmpty(pathInput) || string.IsNullOrEmpty(pathC0) || 
                string.IsNullOrEmpty(pathC1) || string.IsNullOrEmpty(pathC2) || string.IsNullOrEmpty(pathC3))
            {
                MessageBox.Show("請先選擇所有圖片檔案！", "錯誤");
                return;
            }

            if (tabControl.SelectedTab.Controls[1] is Panel p) 
                foreach(Control c in p.Controls) if (c is Button) c.Enabled = false;
            
            lblStatus.Text = "運算中...";

            try
            {
                Bitmap resultBmp = await Task.Run(() => 
                {
                    using (Bitmap bmpIn = new Bitmap(pathInput))
                    using (Bitmap bmpC0 = new Bitmap(pathC0))
                    using (Bitmap bmpC1 = new Bitmap(pathC1))
                    using (Bitmap bmpC2 = new Bitmap(pathC2))
                    using (Bitmap bmpC3 = new Bitmap(pathC3))
                    {
                        if (bmpIn.Width != bmpC0.Width || bmpIn.Height != bmpC0.Height) throw new Exception("尺寸不一致");
                        Bitmap outBmp = new Bitmap(bmpIn.Width, bmpIn.Height, PixelFormat.Format24bppRgb);
                        ProcessImageUnsafe(bmpIn, bmpC0, bmpC1, bmpC2, bmpC3, outBmp);
                        return outBmp;
                    }
                });

                if (pbResult.Image != null) pbResult.Image.Dispose();
                pbResult.Image = resultBmp;
                lblStatus.Text = "運算完成！";
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); lblStatus.Text = "錯誤"; }
            finally {
                if (tabControl.SelectedTab.Controls[1] is Panel p2) 
                    foreach(Control c in p2.Controls) if (c is Button) c.Enabled = true;
            }
        }

        // --- 核心演算法 ---
        private static readonly float[] FIXED_X = { 64f/255f, 95f/255f, 128f/255f, 156f/255f };

        private static float Interpolate(float x, float y0, float y1, float y2, float y3)
        {
            float x0=FIXED_X[0], x1=FIXED_X[1], x2=FIXED_X[2], x3=FIXED_X[3];
            float x_low, x_high, y_low, y_high;

            if (x < x0) { x_low=0f; x_high=x0; y_low=0f; y_high=y0; }
            else if (x < x1) { x_low=x0; x_high=x1; y_low=y0; y_high=y1; }
            else if (x < x2) { x_low=x1; x_high=x2; y_low=y1; y_high=y2; }
            else if (x < x3) { x_low=x2; x_high=x3; y_low=y2; y_high=y3; }
            else { x_low=x3; x_high=1f; y_low=y3; y_high=1f; }

            float den = x_high - x_low;
            if (den <= 1e-5f) return y_high;
            float t = (x - x_low) / den;
            float y = y_low * (1 - t) + y_high * t;
            return y < 0 ? 0 : (y > 1 ? 1 : y);
        }

        private unsafe void ProcessImageUnsafe(Bitmap src, Bitmap c0, Bitmap c1, Bitmap c2, Bitmap c3, Bitmap dst)
        {
            int w = src.Width, h = src.Height;
            BitmapData dSrc = src.LockBits(new Rectangle(0,0,w,h), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData dC0 = c0.LockBits(new Rectangle(0,0,w,h), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData dC1 = c1.LockBits(new Rectangle(0,0,w,h), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData dC2 = c2.LockBits(new Rectangle(0,0,w,h), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData dC3 = c3.LockBits(new Rectangle(0,0,w,h), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData dDst = dst.LockBits(new Rectangle(0,0,w,h), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            int stride = dSrc.Stride;

            Parallel.For(0, h, y =>
            {
                byte* pS = (byte*)dSrc.Scan0 + (y * stride);
                byte* p0 = (byte*)dC0.Scan0 + (y * stride);
                byte* p1 = (byte*)dC1.Scan0 + (y * stride);
                byte* p2 = (byte*)dC2.Scan0 + (y * stride);
                byte* p3 = (byte*)dC3.Scan0 + (y * stride);
                byte* pD = (byte*)dDst.Scan0 + (y * stride);

                for (int x = 0; x < w; x++)
                {
                    int i = x * 3;
                    pD[i]   = (byte)(Interpolate(pS[i]/255f, p0[i]/255f, p1[i]/255f, p2[i]/255f, p3[i]/255f) * 255f);
                    pD[i+1] = (byte)(Interpolate(pS[i+1]/255f, p0[i+1]/255f, p1[i+1]/255f, p2[i+1]/255f, p3[i+1]/255f) * 255f);
                    pD[i+2] = (byte)(Interpolate(pS[i+2]/255f, p0[i+2]/255f, p1[i+2]/255f, p2[i+2]/255f, p3[i+2]/255f) * 255f);
                }
            });

            src.UnlockBits(dSrc); c0.UnlockBits(dC0); c1.UnlockBits(dC1); c2.UnlockBits(dC2); c3.UnlockBits(dC3); dst.UnlockBits(dDst);
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SplashForm());
            Application.Run(new MainForm());
        }
    }
}