namespace QueueXrayApp.Models;

public class QueuePatient
{
    public int Id { get; set; }
    public string? Hn { get; set; }
    public string? QueueName { get; set; }
    
    // ตรวจสอบว่าชื่อ property ถูกต้องหรือไม่
    // ถ้า JSON response ใช้ "queueHx" ให้ใช้แบบนี้:
    public int? queueHx { get; set; }
    
    // หรือถ้าใช้ "QueueHx" (ตัวใหญ่) ให้เปลี่ยนเป็น:
    // public int? QueueHx { get; set; }
    
    public DateTime? CreatedAt { get; set; }
    public string? QueueNameDep { get; set; }
    
    // ตรวจสอบชื่อ property นี้ด้วย
    public int? QueueDep { get; set; }
    
    public int? Status { get; set; }
    public int? StatusHx { get; set; }
}