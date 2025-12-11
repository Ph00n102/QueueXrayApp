using System.Drawing.Printing;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using QueueXrayApp.Models;

namespace QueueXrayApp;

public partial class Form1 : Form
{
    // ====== NEW: สำหรับ dropdown / queue printing ======
    // private ComboBox cboDepartment;
    private CheckBox chkWalk;
    private CheckBox chkSitting;
    private CheckBox chkLying;
    private ComboBox cboVehicleType;
    private string _selectedVehicleType = "เดินได้";
    private TextBox txtHN;
    private Button btnCreateQueue;
    private Button btnCreateQueue1;
    private Button btnCreateQueue2;
    private Button btnCreateQueue3;
    private Label lblCreateStatus;

    // ====== NEW: สำหรับ SignalR ======
    private HubConnection _hubConnection;
    private Label lblSignalRStatus;

    private List<Department> _departments = new List<Department>();
    private readonly PrintDocument printDocumentQueue = new PrintDocument();
    private readonly PrintDocument printDocumentQueue1 = new PrintDocument();
    private string _lastQueueHN = "";
    private int _lastQueueHx;
    private int _lastQueueDep;
    private string _lastQueueNameDep = "";
    private string _lastQueueDeptName = "";
    private int _lastQueueDdepartmentId = 0;
    
    // ====== FIX: ตัวแปรข้อมูลผู้ป่วย ======
    private string _lastcid = "";
    private string _lastfname = "";
    private string _lastlname = "";
    private string _lastpname = "";
    private string _lastsex = "";
    private string _lastage = "";
    private string _lastpttype = "";
    private string _lastname = "";
    
    private readonly HttpClient _httpClient = new HttpClient();

    // ====== FIX: เพิ่มตัวแปรนี้กลับมา ======
    private bool _printSecondCopy = false;

    public class Department
    {
        public int id { get; set; }
        public string name { get; set; } = string.Empty;
        public override string ToString() => name;
    }

    // ====== NEW: Model Classes ======
    public class QueuePatient
    {
        public int? queueHx { get; set; } // FIX: เปลี่ยนเป็น nullable int
        public string QueueNameDep { get; set; } = string.Empty;
        public int? QueueDep { get; set; } // FIX: เปลี่ยนเป็น nullable int
    }

    public class LatestOpdDep
    {
        public string cid { get; set; } = string.Empty;
        public string fname { get; set; } = string.Empty;
        public string lname { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string pname { get; set; } = string.Empty;
        public string sex { get; set; } = string.Empty;
        public string pttype { get; set; } = string.Empty;
        public DateOnly? birthday { get; set; }
        public string title { get; set; } = string.Empty; // FIX: เพิ่ม property title
    }

    public class Patienthnimage
    {
        public string image_data { get; set; } = string.Empty;
    }

    public Form1()
    {
        InitializeComponent();
        InitializeQueueUI();
        printDocumentQueue.PrintPage += PrintDocumentQueue_PrintPage;
        printDocumentQueue1.PrintPage += PrintDocumentQueue_PrintPage1;
        InitializeSignalR();
    }

    private async void Form1_Load(object sender, EventArgs e)
    {
        await ConnectSignalR();
        InitializeQueueUI();
    }

    // ====== NEW: Initialize SignalR Connection ======
    private void InitializeSignalR()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl("http://172.16.200.202:5221/queuehub")
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.Reconnecting += (sender) =>
        {
            this.Invoke((MethodInvoker)delegate
            {
                lblSignalRStatus.Text = "กำลังเชื่อมต่อใหม่...";
                lblSignalRStatus.ForeColor = Color.Orange;
            });
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += (sender) =>
        {
            this.Invoke((MethodInvoker)delegate
            {
                lblSignalRStatus.Text = "เชื่อมต่อแล้ว";
                lblSignalRStatus.ForeColor = Color.Green;
            });
            return Task.CompletedTask;
        };

        _hubConnection.Closed += async (sender) =>
        {
            this.Invoke((MethodInvoker)delegate
            {
                lblSignalRStatus.Text = "เชื่อมต่อขาด, กำลังลองใหม่...";
                lblSignalRStatus.ForeColor = Color.Red;
            });
            
            await Task.Delay(3000);
            await ConnectSignalR();
        };

        _hubConnection.On<object>("NewQueueAdded", (queueData) =>
        {
            this.Invoke((MethodInvoker)delegate
            {
                HandleNewQueueAdded(queueData);
            });
        });

        _hubConnection.On<object>("QueueStatusUpdated", (queueData) =>
        {
            this.Invoke((MethodInvoker)delegate
            {
                HandleQueueStatusUpdated(queueData);
            });
        });

        _hubConnection.On<object>("QueueStatusHxUpdated", (queueData) =>
        {
            this.Invoke((MethodInvoker)delegate
            {
                HandleQueueStatusHxUpdated(queueData);
            });
        });
    }

    // ====== NEW: Connect to SignalR Hub ======
    private async Task ConnectSignalR()
    {
        try
        {
            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                lblSignalRStatus.Text = "กำลังเชื่อมต่อ SignalR...";
                lblSignalRStatus.ForeColor = Color.Orange;
                
                await _hubConnection.StartAsync();
                
                lblSignalRStatus.Text = "เชื่อมต่อ SignalR สำเร็จ";
                lblSignalRStatus.ForeColor = Color.Green;
                await _hubConnection.InvokeAsync("JoinAllQueuesGroup");
            }
        }
        catch (Exception ex)
        {
            lblSignalRStatus.Text = "เชื่อมต่อ SignalR ล้มเหลว";
            lblSignalRStatus.ForeColor = Color.Red;
            await Task.Delay(5000);
            await ConnectSignalR();
        }
    }

    // ====== NEW: Handle New Queue Added ======
    private void HandleNewQueueAdded(object queueData)
    {
        try
        {
            var json = JsonConvert.SerializeObject(queueData);
            var queue = JsonConvert.DeserializeObject<dynamic>(json);

            // ====== FIX: แก้ไข conversion error ======
            string hn = queue?.Hn?.ToString() ?? "";
            int? queueHx = queue?.QueueHx;
            int? queueDep = queue?.QueueDep;
            string queueNameDep = queue?.QueueNameDep?.ToString() ?? "";

            MessageBox.Show($"มีคิวใหม่: HN {hn}\nคิวที่ {queueHx}\nแผนก {queueNameDep}",
                "แจ้งเตือนคิวใหม่", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling new queue: {ex.Message}");
        }
    }

    // ====== NEW: Handle Queue Status Updated ======
    private void HandleQueueStatusUpdated(object queueData)
    {
        try
        {
            var json = JsonConvert.SerializeObject(queueData);
            var queue = JsonConvert.DeserializeObject<dynamic>(json);

            string hn = queue?.Hn;
            string status = queue?.Status;

            // อัปเดต UI ตามสถานะคิว
            // สามารถเพิ่ม logic การอัปเดตตามต้องการ
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling queue status update: {ex.Message}");
        }
    }

    // ====== NEW: Handle Queue Status Hx Updated ======
    private void HandleQueueStatusHxUpdated(object queueData)
    {
        try
        {
            var json = JsonConvert.SerializeObject(queueData);
            var queue = JsonConvert.DeserializeObject<dynamic>(json);

            string hn = queue?.Hn;
            string statusHx = queue?.StatusHx;

            // อัปเดต UI ตามสถานะประวัติคิว
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling queue status Hx update: {ex.Message}");
        }
    }

    private async Task<string> SafeGetStringAsync(HttpClient client, string url)
    {
        try
        {
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"API Error: {url} returned {response.StatusCode}");
                return "{}"; // Return empty JSON object
            }
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get data from {url}: {ex.Message}");
            return "{}"; // Return empty JSON object
        }
    }

    // ====== NEW: ตั้งค่า default values ======
    private void SetDefaultPatientValues()
    {
        _lastcid = "ไม่พบข้อมูล";
        _lastfname = "ไม่พบข้อมูล";
        _lastlname = "ไม่พบข้อมูล";
        _lastname = "ไม่พบข้อมูล";
        _lastpname = "";
        _lastsex = "";
        _lastage = "";
        _lastpttype = "";
    }

    // private async void LoadDepartmentsFromJson()
    // {
    //     try
    //     {
    //         using (var client = new HttpClient())
    //         {
    //             // ดึงข้อมูลจาก API
    //             var response = await client.GetAsync("http://localhost:5221/api/DepartmentName");

    //             if (response.IsSuccessStatusCode)
    //             {
    //                 var json = await response.Content.ReadAsStringAsync();

    //                 // Deserialize JSON เป็น List<Department>
    //                 _departments = JsonConvert.DeserializeObject<List<Department>>(json) ?? new List<Department>();

    //                 // ตั้งค่า DataSource ให้ ComboBox
    //                 cboDepartment.DataSource = _departments;
    //                 cboDepartment.DisplayMember = "name";
    //                 cboDepartment.ValueMember = "id";

    //                 if (_departments.Count > 0)
    //                     cboDepartment.SelectedIndex = 0;
    //             }
    //             else
    //             {
    //                 MessageBox.Show($"ไม่สามารถดึงข้อมูลแผนกได้: {response.StatusCode}", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    //                 LoadFallbackDepartments(); // ใช้ข้อมูลสำรอง
    //             }
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         MessageBox.Show($"เกิดข้อผิดพลาดในการโหลดแผนก: {ex.Message}", "ผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
    //         LoadFallbackDepartments(); // ใช้ข้อมูลสำรอง
    //     }
    // }

    // ====== NEW: โหลดรายการแผนกจาก JSON ======
    // private void LoadFallbackDepartments()
    // {
    //     var json = @"
    // [
    // { ""id"": 2, ""name"": ""ห้องตรวจศัลยกรรมทั่วไป"" },
    // { ""id"": 3, ""name"": ""ห้องตรวจศัลยกรรมทรวงอก"" }
    // ]";
    //     _departments = JsonConvert.DeserializeObject<List<Department>>(json) ?? new List<Department>();
    //     cboDepartment.DataSource = _departments;
    //     if (_departments.Count > 0) cboDepartment.SelectedIndex = 0;
    // }
    // สร้าง Custom CheckBox Class สำหรับสี่เหลี่ยมใหญ่
       public class LargeCheckBox : CheckBox
        {
            private int _boxSize = 25; // ขนาดสี่เหลี่ยม

            public LargeCheckBox()
            {
                this.SetStyle(ControlStyles.UserPaint, true);
                this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
                this.AutoSize = false;
                this.Height = 35;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                
                // ปรับขนาดสี่เหลี่ยม CheckBox
                int boxSize = _boxSize;
                Rectangle boxRect = new Rectangle(0, (this.Height - boxSize) / 2, boxSize, boxSize);
                
                // วาดพื้นหลัง
                if (this.Checked)
                {
                    e.Graphics.FillRectangle(Brushes.DodgerBlue, boxRect);
                }
                else
                {
                    e.Graphics.FillRectangle(Brushes.White, boxRect);
                }
                
                // วาดขอบ
                e.Graphics.DrawRectangle(Pens.Black, boxRect);
                
                // วาดเครื่องหมายถูก
                if (this.Checked)
                {
                    using (Pen checkPen = new Pen(Color.White, 3))
                    {
                        e.Graphics.DrawLine(checkPen, 
                            boxRect.Left + 5, boxRect.Top + boxRect.Height / 2,
                            boxRect.Left + boxRect.Width / 2, boxRect.Bottom - 5);
                        e.Graphics.DrawLine(checkPen,
                            boxRect.Left + boxRect.Width / 2, boxRect.Bottom - 5,
                            boxRect.Right - 5, boxRect.Top + 5);
                    }
                }
                
                // // ★ ยกเลิกการ comment ส่วนวาดข้อความ ★
                // using (Brush textBrush = new SolidBrush(this.ForeColor))
                // {
                //     StringFormat sf = new StringFormat();
                //     sf.LineAlignment = StringAlignment.Center;
                //     e.Graphics.DrawString(this.Text, this.Font, textBrush, 
                //         new Rectangle(boxSize + 8, 0, this.Width - boxSize - 8, this.Height), sf);
                // }
            }
        }
    // ====== NEW: ฟังก์ชันดึงประเภทรถที่เลือก ======
    private string GetSelectedVehicleType()
    {
        if (chkWalk.Checked) return "เดินได้";
        if (chkSitting.Checked) return "รถนั่ง";
        if (chkLying.Checked) return "รถนอน";
        return "เดินได้"; // ค่าเริ่มต้น
    }

    // ====== FIX: คลิกปุ่มสร้างคิว - แก้ไขลำดับการทำงาน ======
    private async void BtnCreateQueue_Click(object sender, EventArgs e)
    {
        try
        {
            lblCreateStatus.Text = "";
            btnCreateQueue.Enabled = false;

            var hn = txtHN.Text.Trim();
            if (string.IsNullOrWhiteSpace(hn))
            {
                MessageBox.Show("กรุณากรอก HN", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                btnCreateQueue.Enabled = true;
                return;
            }

            // var selectedDept = cboDepartment.SelectedItem as Department;
            // int departmentId = selectedDept?.id ?? 1;
            // บันทึกประเภทรถที่เลือก
            _selectedVehicleType = GetSelectedVehicleType();
            int departmentId = 1;

            // บันทึกข้อมูลพื้นฐาน
            _lastQueueHN = hn;
            // _lastQueueDeptName = selectedDept?.name ?? "ไม่ระบุแผนก";
            // _lastQueueDdepartmentId = selectedDept?.id ?? 1;
            _lastQueueDeptName = "นัดตรวจเอกซเรย์";
            _lastQueueDdepartmentId = 1;

            // ====== FIX: ส่งข้อมูลไปยัง API และรอให้เสร็จก่อน ======
            var (success, newQueueHx) = await SendQueueToAPIAndGetResponse(hn, departmentId);

            if (success)
            {
                // ====== FIX: ใช้คิวที่ได้จาก response โดยตรง ======
                if (newQueueHx.HasValue)
                {
                    _lastQueueHx = newQueueHx.Value;
                    Console.WriteLine($"ใช้คิวจาก API Response: {_lastQueueHx}");
                }
                else
                {
                    // ถ้าไม่ได้คิวจาก response ให้ดึงข้อมูลคิวล่าสุด
                    await GetAdditionalQueueData(hn);
                }

                // ดึงข้อมูลผู้ป่วย
                await GetPatientData(hn);

                // พิมพ์หลังจากโหลดข้อมูลผู้ป่วยเสร็จแล้ว
                PrintBothQueueTicketsImmediately();

                // ล้างค่าและอัพเดท UI
                lblCreateStatus.Text = "สร้างคิวสำเร็จแล้ว";
                txtHN.Clear();
                // if (_departments.Count > 0) cboDepartment.SelectedIndex = 0;
                txtHN.Focus();
            }
            else
            {
                MessageBox.Show("สร้างคิวไม่สำเร็จ", "ผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"เกิดข้อผิดพลาด: {ex.Message}", "ผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnCreateQueue.Enabled = true;
        }
    }
    private async void BtnCreateQueue_Click1(object sender, EventArgs e)
    {
        try
        {
            lblCreateStatus.Text = "";
            btnCreateQueue1.Enabled = false;

            var hn = txtHN.Text.Trim();
            if (string.IsNullOrWhiteSpace(hn))
            {
                MessageBox.Show("กรุณากรอก HN", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                btnCreateQueue1.Enabled = true;
                return;
            }

            // var selectedDept = cboDepartment.SelectedItem as Department;
            // int departmentId = selectedDept?.id ?? 1;
            // บันทึกประเภทรถที่เลือก
            _selectedVehicleType = GetSelectedVehicleType();

            int departmentId =  2;

            // บันทึกข้อมูลพื้นฐาน
            _lastQueueHN = hn;
            // _lastQueueDeptName = selectedDept?.name ?? "ไม่ระบุแผนก";
            // _lastQueueDdepartmentId = selectedDept?.id ?? 1;
            _lastQueueDeptName = "ตรวจเอกซเรย์พิเศษ";
            _lastQueueDdepartmentId = 2;

            // ====== FIX: ส่งข้อมูลไปยัง API และรอให้เสร็จก่อน ======
            var (success, newQueueHx) = await SendQueueToAPIAndGetResponse(hn, departmentId);
            
            if (success)
            {
                // ====== FIX: ใช้คิวที่ได้จาก response โดยตรง ======
                if (newQueueHx.HasValue)
                {
                    _lastQueueHx = newQueueHx.Value;
                    Console.WriteLine($"ใช้คิวจาก API Response: {_lastQueueHx}");
                }
                else
                {
                    // ถ้าไม่ได้คิวจาก response ให้ดึงข้อมูลคิวล่าสุด
                    await GetAdditionalQueueData(hn);
                }

                // ดึงข้อมูลผู้ป่วย
                await GetPatientData(hn);

                // พิมพ์หลังจากโหลดข้อมูลผู้ป่วยเสร็จแล้ว
                PrintBothQueueTicketsImmediately();

                // ล้างค่าและอัพเดท UI
                lblCreateStatus.Text = "สร้างคิวสำเร็จแล้ว";
                txtHN.Clear();
                // if (_departments.Count > 0) cboDepartment.SelectedIndex = 0;
                txtHN.Focus();
            }
            else
            {
                MessageBox.Show("สร้างคิวไม่สำเร็จ", "ผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"เกิดข้อผิดพลาด: {ex.Message}", "ผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnCreateQueue1.Enabled = true;
        }
    }
    private async void BtnCreateQueue_Click2(object sender, EventArgs e)
    {
        try
        {
            lblCreateStatus.Text = "";
            btnCreateQueue2.Enabled = false;

            var hn = txtHN.Text.Trim();
            if (string.IsNullOrWhiteSpace(hn))
            {
                MessageBox.Show("กรุณากรอก HN", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                btnCreateQueue2.Enabled = true;
                return;
            }

            // var selectedDept = cboDepartment.SelectedItem as Department;
            // int departmentId = selectedDept?.id ?? 1;
            // บันทึกประเภทรถที่เลือก
            _selectedVehicleType = GetSelectedVehicleType();
            int departmentId =  3;

            // บันทึกข้อมูลพื้นฐาน
            _lastQueueHN = hn;
            // _lastQueueDeptName = selectedDept?.name ?? "ไม่ระบุแผนก";
            // _lastQueueDdepartmentId = selectedDept?.id ?? 1;
            _lastQueueDeptName = "ตรวจเอกซเรย์ทั่วไป";
            _lastQueueDdepartmentId = 3;

            // ====== FIX: ส่งข้อมูลไปยัง API และรอให้เสร็จก่อน ======
            var (success, newQueueHx) = await SendQueueToAPIAndGetResponse(hn, departmentId);
            
            if (success)
            {
                // ====== FIX: ใช้คิวที่ได้จาก response โดยตรง ======
                if (newQueueHx.HasValue)
                {
                    _lastQueueHx = newQueueHx.Value;
                    Console.WriteLine($"ใช้คิวจาก API Response: {_lastQueueHx}");
                }
                else
                {
                    // ถ้าไม่ได้คิวจาก response ให้ดึงข้อมูลคิวล่าสุด
                    await GetAdditionalQueueData(hn);
                }

                // ดึงข้อมูลผู้ป่วย
                await GetPatientData(hn);

                // พิมพ์หลังจากโหลดข้อมูลผู้ป่วยเสร็จแล้ว
                PrintBothQueueTicketsImmediately();

                // ล้างค่าและอัพเดท UI
                lblCreateStatus.Text = "สร้างคิวสำเร็จแล้ว";
                txtHN.Clear();
                // if (_departments.Count > 0) cboDepartment.SelectedIndex = 0;
                txtHN.Focus();
            }
            else
            {
                MessageBox.Show("สร้างคิวไม่สำเร็จ", "ผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"เกิดข้อผิดพลาด: {ex.Message}", "ผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnCreateQueue2.Enabled = true;
        }
    }
    private async void BtnCreateQueue_Click3(object sender, EventArgs e)
    {
        try
        {
            lblCreateStatus.Text = "";
            btnCreateQueue3.Enabled = false;

            var hn = txtHN.Text.Trim();
            if (string.IsNullOrWhiteSpace(hn))
            {
                MessageBox.Show("กรุณากรอก HN", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                btnCreateQueue3.Enabled = true;
                return;
            }

            // var selectedDept = cboDepartment.SelectedItem as Department;
            // int departmentId = selectedDept?.id ?? 1;
            // บันทึกประเภทรถที่เลือก
            _selectedVehicleType = GetSelectedVehicleType();

            int departmentId =  4;

            // บันทึกข้อมูลพื้นฐาน
            _lastQueueHN = hn;
            // _lastQueueDeptName = selectedDept?.name ?? "ไม่ระบุแผนก";
            // _lastQueueDdepartmentId = selectedDept?.id ?? 1;
            _lastQueueDeptName = "ขอ/ลง ข้อมูลทางรังสี";
            _lastQueueDdepartmentId = 4;

            // ====== FIX: ส่งข้อมูลไปยัง API และรอให้เสร็จก่อน ======
            var (success, newQueueHx) = await SendQueueToAPIAndGetResponse(hn, departmentId);
            
            if (success)
            {
                // ====== FIX: ใช้คิวที่ได้จาก response โดยตรง ======
                if (newQueueHx.HasValue)
                {
                    _lastQueueHx = newQueueHx.Value;
                    Console.WriteLine($"ใช้คิวจาก API Response: {_lastQueueHx}");
                }
                else
                {
                    // ถ้าไม่ได้คิวจาก response ให้ดึงข้อมูลคิวล่าสุด
                    await GetAdditionalQueueData(hn);
                }

                // ดึงข้อมูลผู้ป่วย
                await GetPatientData(hn);

                // พิมพ์หลังจากโหลดข้อมูลผู้ป่วยเสร็จแล้ว
                PrintBothQueueTicketsImmediately();

                // ล้างค่าและอัพเดท UI
                lblCreateStatus.Text = "สร้างคิวสำเร็จแล้ว";
                txtHN.Clear();
                // if (_departments.Count > 0) cboDepartment.SelectedIndex = 0;
                txtHN.Focus();
            }
            else
            {
                MessageBox.Show("สร้างคิวไม่สำเร็จ", "ผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"เกิดข้อผิดพลาด: {ex.Message}", "ผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnCreateQueue3.Enabled = true;
        }
    }

    // ====== FIX: ส่งข้อมูลไปยัง API และรับ response ======
    private async Task<(bool success, int? queueHx)> SendQueueToAPIAndGetResponse(string hn, int departmentId)
    {
        try
        {
            string url = $"http://172.16.200.202:5221/api/QueueXray/AddQueueHx1?hn={Uri.EscapeDataString(hn)}&departmentId={departmentId}";

            using (var req = new HttpRequestMessage(HttpMethod.Post, url))
            using (var res = await _httpClient.SendAsync(req))
            {
                if (res.IsSuccessStatusCode)
                {
                    var responseBody = await res.Content.ReadAsStringAsync();
                    Console.WriteLine($"API Response: {responseBody}");

                    // พยายามอ่านคิวจาก response
                    try
                    {
                        var responseObj = JsonConvert.DeserializeObject<dynamic>(responseBody);
                        if (responseObj != null && responseObj.queueHx != null)
                        {
                            int? queueHx = responseObj.queueHx;
                            Console.WriteLine($"ได้คิวจาก API Response: {queueHx}");
                            return (true, queueHx);
                        }
                    }
                    catch (Exception jsonEx)
                    {
                        Console.WriteLine($"ไม่สามารถอ่านคิวจาก response: {jsonEx.Message}");
                    }

                    return (true, null);
                }
                else
                {
                    var body = await res.Content.ReadAsStringAsync();
                    Console.WriteLine($"สร้างคิวไม่สำเร็จ: {body}");
                    return (false, null);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending to API: {ex.Message}");
            return (false, null);
        }
    }

    // ====== FIX: ดึงข้อมูลผู้ป่วยแยกจากข้อมูลคิว ======
 private async Task GetPatientData(string hn)
    {
        try
        {
            using (var client = new HttpClient())
            {
                string urlc = $"http://172.16.200.202:5221/api/QueueXray/GetPatientVisits/{hn}";
                string responsec = await SafeGetStringAsync(client, urlc);
                
                Console.WriteLine($"Patient API Response: {responsec}");
                
                if (string.IsNullOrEmpty(responsec) || responsec == "null" || responsec == "[]")
                {
                    Console.WriteLine("Patient API returned empty response");
                    SetDefaultPatientValues();
                }
                else
                {
                    var latestOpdDep = JsonConvert.DeserializeObject<LatestOpdDep>(responsec);

                    if (latestOpdDep != null)
                    {
                        _lastcid = latestOpdDep.cid ?? "ไม่พบข้อมูล";
                        _lastfname = latestOpdDep.fname ?? "ไม่พบข้อมูล";
                        _lastlname = latestOpdDep.lname ?? "ไม่พบข้อมูล";
                        _lastname = latestOpdDep.name ?? "ไม่พบข้อมูล";
                        _lastsex = latestOpdDep.sex ?? "ไม่พบข้อมูล";
                        _lastpttype = latestOpdDep.pttype ?? "ไม่พบข้อมูล";
                        
                        // FIX: แปลงวันเกิดเป็นอายุ - แก้ไขตรงนี้
                        if (latestOpdDep.birthday.HasValue)
                        {
                            DateTime birthDate = latestOpdDep.birthday.Value.ToDateTime(TimeOnly.MinValue);
                            int age = CalculateAge(birthDate);
                            _lastage = age.ToString() + " ปี";
                        }
                        else
                        {
                            _lastage = "ไม่พบข้อมูล";
                        }
                        
                        // FIX: ใช้ property title ที่เพิ่มเข้ามา
                        _lastpname = latestOpdDep.pname ?? $"{latestOpdDep.title ?? ""} {latestOpdDep.fname ?? ""}".Trim();
                        
                        Console.WriteLine($"Successfully loaded patient: {_lastpname}{_lastfname} {_lastlname}, CID: {_lastcid}");
                    }
                    else
                    {
                        SetDefaultPatientValues();
                        Console.WriteLine("Failed to deserialize patient data");
                    }
                }

                // ดึงรูปภาพ (ถ้ามี) - ส่วนนี้เหมือนเดิม
                if (!string.IsNullOrEmpty(_lastcid) && _lastcid != "ไม่พบข้อมูล")
                {
                    try
                    {
                        string urlim = $"http://172.16.200.202:8089/api/Hos/getpatienthnimage?_cid={_lastcid}";
                        string responseim = await SafeGetStringAsync(client, urlim);
                        
                        if (!string.IsNullOrEmpty(responseim) && responseim != "null")
                        {
                            var patienthnimage = JsonConvert.DeserializeObject<Patienthnimage>(responseim);
                            Console.WriteLine("Patient image data loaded");
                        }
                        else
                        {
                            Console.WriteLine("No patient image data found");
                        }
                    }
                    catch (Exception imageEx)
                    {
                        Console.WriteLine($"Error loading patient image: {imageEx.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting patient data: {ex.Message}");
            SetDefaultPatientValues();
        }
    }

    // เพิ่ม method คำนวณอายุ
    private int CalculateAge(DateTime birthDate)
    {
        DateTime today = DateTime.Today;
        int age = today.Year - birthDate.Year;
        
        // ถ้ายังไม่ถึงวันเกิดในปีนี้ ให้ลดอายุลง 1
        if (birthDate.Date > today.AddYears(-age)) 
        {
            age--;
        }
        
        return age;
    }

    // ====== FIX: ดึงข้อมูลคิวล่าสุด ======
   private async Task GetAdditionalQueueData(string hn)
    {
        try
        {
            using (var client = new HttpClient())
            {
                string urlp = $"http://172.16.200.202:5221/api/QueueXray/GetLatestQueue?hn={hn}";
                string responsep = await SafeGetStringAsync(client, urlp);
                
                Console.WriteLine($"🔍 Raw JSON Response: {responsep}");
                
                if (string.IsNullOrEmpty(responsep) || responsep == "null" || responsep == "[]")
                {
                    Console.WriteLine("❌ Queue API returned empty response");
                    _lastQueueHx = 0;
                    _lastQueueNameDep = "";
                    _lastQueueDep = 0;
                    return;
                }

                // ★ ลองตรวจสอบว่า response เป็น Array หรือ Object ★
                if (responsep.Trim().StartsWith("["))
                {
                    // เป็น Array - ใช้ List
                    Console.WriteLine("📦 Response is an array, using List deserialization");
                    var patientsList = JsonConvert.DeserializeObject<List<QueuePatient>>(responsep);
                    var patients = patientsList?.FirstOrDefault();
                    
                    if (patients != null)
                    {
                        ExtractQueueData(patients);
                    }
                    else
                    {
                        Console.WriteLine("❌ No patient data in array");
                        SetDefaultQueueValues();
                    }
                }
                else
                {
                    // เป็น Object - ใช้ direct deserialization
                    Console.WriteLine("📄 Response is an object, using direct deserialization");
                    var patients = JsonConvert.DeserializeObject<QueuePatient>(responsep);
                    
                    if (patients != null)
                    {
                        ExtractQueueData(patients);
                    }
                    else
                    {
                        Console.WriteLine("❌ Failed to deserialize queue data");
                        SetDefaultQueueValues();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💥 Error getting queue data: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            SetDefaultQueueValues();
        }
    }

    // ★ Method แยกสำหรับดึงข้อมูล ★
    private void ExtractQueueData(QueuePatient patients)
    {
        Console.WriteLine($"✅ Deserialized successfully!");
        Console.WriteLine($"📊 QueueHx: {patients.queueHx}");
        Console.WriteLine($"📊 QueueDep: {patients.QueueDep}");
        Console.WriteLine($"📊 QueueNameDep: {patients.QueueNameDep}");
        Console.WriteLine($"📊 Full object: {JsonConvert.SerializeObject(patients)}");
        
        _lastQueueHx = patients.queueHx ?? 0;
        _lastQueueNameDep = patients.QueueNameDep ?? "";
        _lastQueueDep = patients.QueueDep ?? 0;
        
        Console.WriteLine($"🎯 Final values - Hx: {_lastQueueHx}, Dep: {_lastQueueDep}, NameDep: {_lastQueueNameDep}");
    }

    // ★ Method ตั้งค่า default ★
    private void SetDefaultQueueValues()
    {
        _lastQueueHx = 0;
        _lastQueueNameDep = "";
        _lastQueueDep = 0;
    }

    // ====== NEW: พิมพ์ทั้งสองแบบแบบเร็วที่สุด ======
    private void PrintBothQueueTicketsImmediately()
    {
        try
        {
            // พิมพ์ครั้งที่ 1 - ทันที
            _printSecondCopy = false;
            printDocumentQueue.Print();

            // พิมพ์ครั้งที่ 2 - ทันที
            _printSecondCopy = true;
            printDocumentQueue1.Print();
            
            Console.WriteLine($"Printed both queue tickets successfully - Queue: {_lastQueueDdepartmentId}{_lastQueueHx:D2}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"พิมพ์ใบคิวไม่สำเร็จ: {ex.Message}", "ผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void PrintDocumentQueue_PrintPage(object sender, PrintPageEventArgs e)
    {
        // กำหนดฟอนต์ตามแบบในรูปภาพ
        var fontHeader = new Font("TH-Sarabun-PSK", 16, FontStyle.Bold);
        var fontSubHeader = new Font("TH-Sarabun-PSK", 14, FontStyle.Bold);
        var fontBody = new Font("TH-Sarabun-PSK", 12);
        var fontSmall = new Font("TH-Sarabun-PSK", 10);
        var fontQueue = new Font("TH-Sarabun-PSK", 28);
        var fontQueue1 = new Font("TH-Sarabun-PSK", 24, FontStyle.Bold);

        var g = e.Graphics;

        // วาดกรอบสี่เหลี่ยมรอบบัตรคิว (เหมือนในรูป)
        Pen borderPen = new Pen(Brushes.Black, 1);
        StringFormat sfCenter = new StringFormat();
        sfCenter.Alignment = StringAlignment.Center;
        g.DrawRectangle(borderPen, 5, 5, 278, 320);

        // วันที่และเวลา - จัดรูปแบบให้เหมือนในรูป
        g.DrawString($"วันที่ {DateTime.Now:dd MMM yyyy HH:mm:ss}", fontSubHeader, Brushes.Black, 8, 9);

        // วาดเส้นคั่น (เหมือนในรูป)
        g.DrawLine(new Pen(Brushes.Black, 1), 5, 37, 284, 37);

        // คิว
        g.DrawString($"ประเภท{_lastQueueDeptName}", fontBody, Brushes.Black, 128, 43, sfCenter);

        // วาดเส้นคั่น (เหมือนในรูป)
        g.DrawLine(new Pen(Brushes.Black, 1), 5, 68, 284, 68);

        // ====== FIX: แสดงคิวให้ถูกต้อง ======
        string queueNumber = _lastQueueHx < 10 ?
            $"{_lastQueueDdepartmentId}0{_lastQueueHx}" :
            $"{_lastQueueDdepartmentId}{_lastQueueHx}";

        g.DrawString(queueNumber, fontQueue, Brushes.Black, new RectangleF(0, 75, 284, 40), sfCenter);

        // วาดเส้นคั่น (เหมือนในรูป)
        g.DrawLine(new Pen(Brushes.Black, 1), 5, 128, 284, 128);
        
         g.DrawString($"ประเภทรถ: {_selectedVehicleType}", fontQueue1, Brushes.Black, 10, 138);

        // วาดเส้นคั่น (เหมือนในรูป)
        g.DrawLine(new Pen(Brushes.Black, 1), 5, 187, 284, 187);


        g.DrawLine(new Pen(Brushes.Black, 1), 5, 237, 284, 237);

        // ชื่อ
        g.DrawString($"ชื่อ-สกุล: {_lastpname}{_lastfname} {_lastlname}", fontSmall, Brushes.Black, 8, 245);

        // HN และ CID
        g.DrawString($"HN: {_lastQueueHN}", fontSmall, Brushes.Black, 8, 265);
        g.DrawString($"อายุ: {_lastage}", fontSmall, Brushes.Black, 110, 265);
        string queueSex = _lastsex == "1" ?
            $"ชาย" :
            $"หญิง";
        g.DrawString($"เพศ: {queueSex}", fontSmall, Brushes.Black, 180, 265);

        g.DrawString($"CID: {_lastcid}", fontSmall, Brushes.Black, 8, 285);
        g.DrawString($"สิทธิ: ({_lastpttype}) {_lastname}", fontSmall, Brushes.Black, 8, 305);
    }

    private void PrintDocumentQueue_PrintPage1(object sender, PrintPageEventArgs e)
    {
        // กำหนดฟอนต์ตามแบบในรูปภาพ
        var fontHeader = new Font("TH-Sarabun-PSK", 16, FontStyle.Bold);
        var fontSubHeader = new Font("TH-Sarabun-PSK", 14, FontStyle.Bold);
        var fontBody = new Font("TH-Sarabun-PSK", 12);
        var fontSmall = new Font("TH-Sarabun-PSK", 10);
        var fontQueue = new Font("TH-Sarabun-PSK", 50, FontStyle.Bold);
        var fontQueue1 = new Font("TH-Sarabun-PSK", 20, FontStyle.Bold);

        var g = e.Graphics;

        // ====== เพิ่มรูปภาพ ======
        // วิธีที่ 1: โหลดรูปจากไฟล์
        if (File.Exists(@"D:\QueueXrayApp\Images\kkk.png")) // เปลี่ยน path ตามตำแหน่งไฟล์รูป
        {
            Image logo = Image.FromFile(@"D:\QueueXrayApp\Images\kkk.png");
            g.DrawImage(logo, 5, -2, 60, 60); // ตำแหน่งและขนาด
            logo.Dispose();
        }

        // วิธีที่ 2: โหลดรูปจาก Resources (แนะนำ)
        // g.DrawImage(Properties.Resources.HospitalLogo, 10, 10, 50, 50);

        // วิธีที่ 3: โหลดรูปจาก Embedded Resources
        // using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("YourNamespace.logo.png"))
        // {
        //     if (stream != null)
        //     {
        //         Image logo = Image.FromStream(stream);
        //         g.DrawImage(logo, 10, 10, 50, 50);
        //         logo.Dispose();
        //     }
        // }

        // วาดกรอบสี่เหลี่ยมรอบบัตรคิว (เหมือนในรูป)
        Pen borderPen = new Pen(Brushes.Black, 1);
        StringFormat sfCenter = new StringFormat();
        sfCenter.Alignment = StringAlignment.Center;
        g.DrawRectangle(borderPen, 5, 58, 278, 250);

        // ปรับตำแหน่งข้อความให้ไม่ทับรูป
        g.DrawString($"ใบนำทางรังสีวิทยา", fontHeader, Brushes.Black, 75, 13); // เลื่อนไปทางขวา

        // วันที่และเวลา - จัดรูปแบบให้เหมือนในรูป
        g.DrawString($"วันที่ {DateTime.Now:dd MMM yyyy HH:mm:ss}", fontSubHeader, Brushes.Black, 8, 60);

        // วาดเส้นคั่น (เหมือนในรูป)
        g.DrawLine(new Pen(Brushes.Black, 1), 5, 88, 284, 88);

        // คิว
        g.DrawString($"ประเภท{_lastQueueDeptName}", fontBody, Brushes.Black, 128, 93, sfCenter);

        // วาดเส้นคั่น (เหมือนในรูป)
        g.DrawLine(new Pen(Brushes.Black, 1), 5, 120, 284, 120);

        g.DrawString($"เลขที่ใบนำทาง (ไม่ใช่เลขที่คิวการตรวจ)", fontSmall, Brushes.Black, 20, 123);

        // ====== FIX: แสดงคิวให้ถูกต้อง ======
        string queueNumber = _lastQueueHx < 10 ?
            $"{_lastQueueDdepartmentId}0{_lastQueueHx}" :
            $"{_lastQueueDdepartmentId}{_lastQueueHx}";

        g.DrawString(queueNumber, fontQueue, Brushes.Black, new RectangleF(0, 145, 284, 70), sfCenter);

        // วาดเส้นคั่น (เหมือนในรูป)
        g.DrawLine(new Pen(Brushes.Black, 1), 5, 227, 284, 227);

        // ชื่อ
        g.DrawString($"ชื่อ-สกุล: {_lastpname}{_lastfname} {_lastlname}", fontSmall, Brushes.Black, 8, 230);

        // HN และ CID
        g.DrawString($"HN: {_lastQueueHN}", fontSmall, Brushes.Black, 8, 250);
        g.DrawString($"อายุ: {_lastage}", fontSmall, Brushes.Black, 110, 250);
        string queueSex = _lastsex == "1" ?
            $"ชาย" :
            $"หญิง";
        g.DrawString($"เพศ: {queueSex}", fontSmall, Brushes.Black, 180, 250);

        g.DrawString($"CID: {_lastcid}", fontSmall, Brushes.Black, 8, 270);

        g.DrawString($"สิทธิ: ({_lastpttype}) {_lastname}", fontSmall, Brushes.Black, 8, 290);
        // }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _hubConnection?.StopAsync();
        _hubConnection?.DisposeAsync();
        base.OnFormClosed(e);
    }
}