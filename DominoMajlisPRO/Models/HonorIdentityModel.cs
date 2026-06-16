namespace DominoMajlisPRO.Models;

public class HonorIdentityModel
{
    public string HonorOwnerId { get; set; } = "";

    public string PlayerId { get; set; } = "";

    public string HonorType { get; set; } = "";

    public string RecoveryKey { get; set; } = "";

    public string MasterRecoveryKey { get; set; } = "";

    public string SecuritySignature { get; set; } = "";

    public DateTime ActivatedAt { get; set; }

    // نوع الصلاحية
    public HonorRoleType Role { get; set; }
        = HonorRoleType.None;

    // هل تم التفعيل
    public bool IsActivated { get; set; }
        = false;

    // مفتاح التفعيل المستخدم
    public string ActivationKey { get; set; }
        = "";

    // رقم المؤسس (للـ Founder فقط)
    public int FounderNumber { get; set; }
        = 0;

    // تاريخ التفعيل
    public DateTime ActivationDate { get; set; }
        = DateTime.MinValue;

    // الجهاز الذي تم التفعيل عليه
    public string DeviceId { get; set; }
        = "";

    // اسم صاحب الصلاحية
    public string DisplayName { get; set; }
        = "";
}
