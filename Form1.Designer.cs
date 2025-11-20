using System.Drawing.Drawing2D;

namespace QueueXrayApp;

partial class Form1
{
   private System.ComponentModel.IContainer components = null;

    [System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
    private static extern IntPtr CreateRoundRectRgn(
        int nLeftRect, int nTopRect,
        int nRightRect, int nBottomRect,
        int nWidthEllipse, int nHeightEllipse);
    
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    // ทำให้พื้นหลังเป็น Gradient
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using (LinearGradientBrush brush = new LinearGradientBrush(
            this.ClientRectangle,
            Color.FromArgb(245, 247, 250),   // สีบน (ฟ้าอ่อน)
            Color.FromArgb(200, 220, 255),   // สีล่าง (ฟ้าอมม่วง)
            LinearGradientMode.Vertical))    // ไล่สีแนวตั้ง
        {
            e.Graphics.FillRectangle(brush, this.ClientRectangle);
        }
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1920 , 1080);
        Text = "ระบบจัดการคิวผู้ป่วย";
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
    }

    // ====== NEW: UI สำหรับสร้างคิว ======
    private void InitializeQueueUI()
    {
        // Panel หลัก
        var mainPanel = new Panel
        {
            BorderStyle = BorderStyle.None,
            BackColor = Color.White,
            Size = new Size(1000, 700), // เพิ่มความสูงเพื่อให้มีพื้นที่สำหรับสถานะ SignalR
            Location = new Point((this.ClientSize.Width - 1000) / 2, (this.ClientSize.Height - 750) / 2),
            Padding = new Padding(20)
        };
        mainPanel.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, mainPanel.Width, mainPanel.Height, 20, 20));
        this.Controls.Add(mainPanel);

        // Title
        var lblTitle = new Label
        {
            Text = "สร้างใบนำทางแผนกรังสีวิทยา",
            Font = new Font("Segoe UI", 55F, FontStyle.Bold),
            ForeColor = Color.FromArgb(44, 62, 80),
            AutoSize = true,
            Location = new Point(15, 20)
        };
        mainPanel.Controls.Add(lblTitle);

        var lblHn = new Label
        {
            Text = "HN",
            Font = new Font("Segoe UI", 50F, FontStyle.Bold),
            ForeColor = Color.FromArgb(44, 62, 80),
            AutoSize = true,
            Location = new Point(265, 155)
        };
        mainPanel.Controls.Add(lblHn);

        // TextBox HN
        txtHN = new TextBox
        {
            Name = "txtHN",
            Width = 300,
            Height = 55,
            Location = new Point(415, 155),
            Font = new Font("Segoe UI", 50F),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.WhiteSmoke,
            ForeColor = Color.Black
        };
        txtHN.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, txtHN.Width, txtHN.Height, 10, 10));
        mainPanel.Controls.Add(txtHN);

         // ====== NEW: ComboBox สำหรับเลือกประเภทรถ ======
        var lblVehicleType = new Label
        {
            Text = "เลือกประเภทรถ",
            Font = new Font("Segoe UI", 26F, FontStyle.Bold),
            ForeColor = Color.FromArgb(44, 62, 80),
            AutoSize = true,
            Location = new Point(110, 260),
            BackColor = Color.Transparent
        };
        mainPanel.Controls.Add(lblVehicleType);

        // ComboBox Vehicle Type
        // cboVehicleType = new ComboBox
        // {
        //     Name = "cboVehicleType",
        //     DropDownStyle = ComboBoxStyle.DropDownList,
        //     Width = 300,
        //     Height = 60,
        //     Location = new Point(370, 265),
        //     Font = new Font("Segoe UI", 20F),
        //     BackColor = Color.WhiteSmoke,
        //     ForeColor = Color.Black,
        //     FlatStyle = FlatStyle.Flat
        // };
        // cboVehicleType.Items.AddRange(new object[] { "เดินได้","รถนั่ง", "รถนอน" });
        // cboVehicleType.SelectedIndex = 0; // เลือก "รถนั่ง" เป็นค่าเริ่มต้น
        // cboVehicleType.Region = Region.FromHrgn(
        //     CreateRoundRectRgn(0, 0, cboVehicleType.Width, 200, 10, 10)
        // );
        // mainPanel.Controls.Add(cboVehicleType);

        // // ปรับการวาด Item
        // cboVehicleType.DrawItem += (s, e) =>
        // {
        //     if (e.Index < 0) return;

        //     // background
        //     if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
        //     {
        //         e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(230, 240, 250)), e.Bounds);
        //     }
        //     else
        //     {
        //         e.Graphics.FillRectangle(new SolidBrush(Color.WhiteSmoke), e.Bounds);
        //     }

        //     // text
        //     string text = cboVehicleType.Items[e.Index].ToString();
        //     using (var brush = new SolidBrush(Color.Black))
        //     {
        //         e.Graphics.DrawString(text, new Font("Segoe UI", 18F), brush, e.Bounds.Left + 10, e.Bounds.Top + 5);
        //     }
        // };
        // ====== NEW: CheckBox สำหรับเลือกประเภทรถ ======
        // var lblVehicleType = new Label
        // {
        //     Text = "เลือกประเภทรถ",
        //     Font = new Font("Segoe UI", 26F, FontStyle.Bold),
        //     ForeColor = Color.FromArgb(44, 62, 80),
        //     AutoSize = true,
        //     Location = new Point(110, 260),
        //     BackColor = Color.Transparent
        // };
        // mainPanel.Controls.Add(lblVehicleType);

        // CheckBox "เดินได้"
        chkWalk = new LargeCheckBox()
        {
            Name = "chkWalk",
            Text = "  เดินได้",
            Font = new Font("Segoe UI", 20F),
            ForeColor = Color.FromArgb(44, 62, 80),
            Size = new Size(130, 40),
            Location = new Point(370, 265),
            BackColor = Color.Transparent,
            Checked = true
        };
        mainPanel.Controls.Add(chkWalk);

        // CheckBox "รถนั่ง"
        chkSitting = new LargeCheckBox() // ★ เปลี่ยนเป็น LargeCheckBox ★
        {
            Name = "chkSitting",
            Text = "  รถนั่ง", // ★ เพิ่มช่องว่างหน้า ★
            Font = new Font("Segoe UI", 20F),
            ForeColor = Color.FromArgb(44, 62, 80),
            Size = new Size(130, 40),
            Location = new Point(500, 265),
            BackColor = Color.Transparent
        };
        mainPanel.Controls.Add(chkSitting);

        // CheckBox "รถนอน"
        chkLying = new LargeCheckBox() // ★ เปลี่ยนเป็น LargeCheckBox ★
        {
            Name = "chkLying",
            Text = "  รถนอน", // ★ เพิ่มช่องว่างหน้า ★
            Font = new Font("Segoe UI", 20F),
            ForeColor = Color.FromArgb(44, 62, 80),
            Size = new Size(130, 40),
            Location = new Point(630, 265),
            BackColor = Color.Transparent
        };
        mainPanel.Controls.Add(chkLying);

        // Event Handler สำหรับให้เลือกได้เพียงอันเดียว
        chkWalk.CheckedChanged += (s, e) => {
            if (chkWalk.Checked)
            {
                chkSitting.Checked = false;
                chkLying.Checked = false;
            }
        };

        chkSitting.CheckedChanged += (s, e) => {
            if (chkSitting.Checked)
            {
                chkWalk.Checked = false;
                chkLying.Checked = false;
            }
        };

        chkLying.CheckedChanged += (s, e) => {
            if (chkLying.Checked)
            {
                chkWalk.Checked = false;
                chkSitting.Checked = false;
            }
        };


        var lblDepartment = new Label
        {
            Text = "เลือกประเภท",
            Font = new Font("Segoe UI", 26F, FontStyle.Bold),
            ForeColor = Color.FromArgb(44, 62, 80),
            AutoSize = true,
            Location = new Point(110, 310)
        };
        mainPanel.Controls.Add(lblDepartment);

        // ComboBox Department
        // cboDepartment = new ComboBox
        // {
        //     Name = "cboDepartment",
        //     DropDownStyle = ComboBoxStyle.DropDownList,
        //     Width = 243,
        //     Height = 45,
        //     Location = new Point(160, 150),
        //     Font = new Font("Segoe UI", 20F),
        //     BackColor = Color.White,
        //     ForeColor = Color.Black,
        //     FlatStyle = FlatStyle.Flat,
        //     DrawMode = DrawMode.OwnerDrawFixed,
        //     ItemHeight = 40,
        //     DropDownHeight = 150
        // };
        // cboDepartment.Region = Region.FromHrgn(
        //     CreateRoundRectRgn(0, 0, cboDepartment.Width, cboDepartment.Height, 10, 10)
        // );
        // mainPanel.Controls.Add(cboDepartment);

        // // ปรับการวาด Item เอง
        // cboDepartment.DrawItem += (s, e) =>
        // {
        //     if (e.Index < 0) return;

        //     // background
        //     if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
        //     {
        //         e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(230, 240, 250)), e.Bounds);
        //     }
        //     else
        //     {
        //         e.Graphics.FillRectangle(new SolidBrush(Color.WhiteSmoke), e.Bounds);
        //     }

        //     // text
        //     string text = cboDepartment.Items[e.Index].ToString();
        //     using (var brush = new SolidBrush(Color.Black))
        //     {
        //         e.Graphics.DrawString(text, new Font("Segoe UI", 18F), brush, e.Bounds.Left + 10, e.Bounds.Top + 5);
        //     }
        // };

        // Button Create Queue
        btnCreateQueue = new Button
        {
            Name = "btnCreateQueue",
            Text = "นัดตรวจเอกซเรย์",
            Size = new Size(400, 100),
            Location = new Point(80, 380),
            Font = new Font("Segoe UI", 30F, FontStyle.Bold),
            BackColor = Color.FromArgb(110, 210, 250),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnCreateQueue.FlatAppearance.BorderSize = 0;
        btnCreateQueue.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, btnCreateQueue.Width, btnCreateQueue.Height, 10, 10));
        this.btnCreateQueue.Click += new System.EventHandler(this.BtnCreateQueue_Click);
        mainPanel.Controls.Add(btnCreateQueue);

        // Button Create Queue
        btnCreateQueue1 = new Button
        {
            Name = "btnCreateQueue1",
            Text = "ตรวจเอกซเรย์พิเศษ",
            Size = new Size(400, 100),
            Location = new Point(520, 380),
            Font = new Font("Segoe UI", 30F, FontStyle.Bold),
            BackColor = Color.FromArgb(110, 210, 250),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnCreateQueue1.FlatAppearance.BorderSize = 0;
        btnCreateQueue1.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, btnCreateQueue1.Width, btnCreateQueue1.Height, 10, 10));
        this.btnCreateQueue1.Click += new System.EventHandler(this.BtnCreateQueue_Click1);
        mainPanel.Controls.Add(btnCreateQueue1);
        
        // Button Create Queue
        btnCreateQueue2 = new Button
        {
            Name = "btnCreateQueue2",
            Text = "ตรวจเอกซเรย์ทั่วไป",
            Size = new Size(400, 100),
            Location = new Point(80, 490),
            Font = new Font("Segoe UI", 30F, FontStyle.Bold),
            BackColor = Color.FromArgb(110, 210, 250),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnCreateQueue2.FlatAppearance.BorderSize = 0;
        btnCreateQueue2.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, btnCreateQueue2.Width, btnCreateQueue2.Height, 10, 10));
        this.btnCreateQueue2.Click += new System.EventHandler(this.BtnCreateQueue_Click2);
        mainPanel.Controls.Add(btnCreateQueue2);
        
        // Button Create Queue
        btnCreateQueue3 = new Button
        {
            Name = "btnCreateQueue3",
            Text = "ขอ/ลง ข้อมูลเอกซเรย์",
            Size = new Size(400, 100),
            Location = new Point(520, 490),
            Font = new Font("Segoe UI", 30F, FontStyle.Bold),
            BackColor = Color.FromArgb(110, 210, 250),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnCreateQueue3.FlatAppearance.BorderSize = 0;
        btnCreateQueue3.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, btnCreateQueue3.Width, btnCreateQueue3.Height, 10, 10));
        this.btnCreateQueue3.Click += new System.EventHandler(this.BtnCreateQueue_Click3);
        mainPanel.Controls.Add(btnCreateQueue3);

        // Label สถานะ
        lblCreateStatus = new Label
        {
            Name = "lblCreateStatus",
            AutoSize = true,
            Location = new Point(80, 590),
            ForeColor = Color.FromArgb(46, 204, 113),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Text = ""
        };
        mainPanel.Controls.Add(lblCreateStatus);

        // Label สถานะ SignalR (แสดงสถานะการเชื่อมต่อ)
        lblSignalRStatus = new Label
        {
            Text = "กำลังเชื่อมต่อ SignalR...",
            ForeColor = Color.Orange,
            AutoSize = true,
            Location = new Point(80, 620),
            Font = new Font("Segoe UI", 9F)
        };
        mainPanel.Controls.Add(lblSignalRStatus);

        // โหลดข้อมูลแผนก
        // LoadDepartmentsFromJson();
    }

    #endregion
}
