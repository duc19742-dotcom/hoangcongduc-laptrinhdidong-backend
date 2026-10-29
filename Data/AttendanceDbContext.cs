using Attendance.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Api.Data;

public sealed class AttendanceDbContext(DbContextOptions<AttendanceDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<EmployeeShift> EmployeeShifts => Set<EmployeeShift>();
    public DbSet<WorkLocation> WorkLocations => Set<WorkLocation>();
    public DbSet<QrSession> QrSessions => Set<QrSession>();
    public DbSet<QrRedemption> QrRedemptions => Set<QrRedemption>();
    public DbSet<AttendanceAttempt> AttendanceAttempts => Set<AttendanceAttempt>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<AttendanceRequest> AttendanceRequests => Set<AttendanceRequest>();
    public DbSet<AttendanceRequestEvent> AttendanceRequestEvents => Set<AttendanceRequestEvent>();
    public DbSet<AuthFlow> AuthFlows => Set<AuthFlow>();
    public DbSet<OtpChallenge> OtpChallenges => Set<OtpChallenge>();
    public DbSet<AuthSession> AuthSessions => Set<AuthSession>();
    public DbSet<ExternalAuthProof> ExternalAuthProofs => Set<ExternalAuthProof>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Attendance.Api.Services.RememberedDevice>().ToTable("RememberedDevices");
        b.Entity<Attendance.Api.Services.RememberedDevice>().HasIndex(x=>x.TokenHash).IsUnique();
        b.Entity<AppUser>().HasOne(x => x.Employee).WithOne(x => x.User).HasForeignKey<Employee>(x => x.UserId);
        b.Entity<AppUser>().HasIndex(x => x.NormalizedUserName).IsUnique();
        b.Entity<AppUser>().HasIndex(x => x.NormalizedEmail).IsUnique();
        b.Entity<QrSession>().HasOne<AppUser>().WithMany().HasForeignKey(x=>x.CreatedByUserId);
        b.Entity<OtpChallenge>().HasOne<AppUser>().WithMany().HasForeignKey(x=>x.UserId);
        b.Entity<AttendanceRequest>().HasOne<AppUser>().WithMany().HasForeignKey(x=>x.ReviewedByUserId);
        b.Entity<AttendanceRequestEvent>().HasOne<AppUser>().WithMany().HasForeignKey(x=>x.ActorUserId);
        b.Entity<AuditLog>().HasOne<AppUser>().WithMany().HasForeignKey(x=>x.ActorUserId);
        b.Entity<AttendanceRecord>().HasOne<AttendanceRequest>().WithMany().HasForeignKey(x=>x.ApprovedRequestId);
        b.Entity<AppUser>().ToTable("Users", t => {
            t.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Admin','Employee')");
            t.HasCheckConstraint("CK_Users_Approval", "[ApprovalStatus] IN ('Pending','Approved','Rejected')");
        });
        b.Entity<Employee>().HasIndex(x => x.EmployeeCode).IsUnique();
        b.Entity<Employee>().HasIndex(x => new { x.DepartmentId, x.IsActive });
        b.Entity<Department>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Shift>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Shift>().ToTable(t=>t.HasCheckConstraint("CK_Shift_Grace","[LateGraceMinutes] BETWEEN 0 AND 120"));
        b.Entity<EmployeeShift>().HasIndex(x => new { x.EmployeeId, x.ShiftId, x.WorkDate }).IsUnique();
        b.Entity<EmployeeShift>().HasIndex(x => new { x.EmployeeId, x.WorkDate });
        b.Entity<EmployeeShift>().ToTable(t => t.HasCheckConstraint("CK_Assignment_Time", "[EndUtc] > [StartUtc]"));
        b.Entity<WorkLocation>().ToTable(t => {
            t.HasCheckConstraint("CK_Location_Coordinates", "[Latitude] BETWEEN -90 AND 90 AND [Longitude] BETWEEN -180 AND 180");
            t.HasCheckConstraint("CK_Location_Policy", "[RadiusMeters] > 0 AND [MaxAccuracyMeters] > 0");
        });
        b.Entity<QrSession>().HasIndex(x => x.TokenHash).IsUnique();
        b.Entity<QrSession>().ToTable(t => t.HasCheckConstraint("CK_QR_Action", "[Action] IN ('CheckIn','CheckOut')"));
        b.Entity<QrSession>().ToTable(t=>t.HasCheckConstraint("CK_QR_Expiry","[ExpiresAtUtc] > [CreatedAtUtc]"));
        b.Entity<QrRedemption>().HasIndex(x => new { x.QrSessionId, x.EmployeeId }).IsUnique();
        b.Entity<QrRedemption>().HasIndex(x => x.AttemptId).IsUnique();
        b.Entity<AttendanceAttempt>().HasIndex(x => new { x.EmployeeId, x.IdempotencyKey }).IsUnique();
        b.Entity<AttendanceAttempt>().HasIndex(x => new { x.EmployeeId, x.ReceivedAtUtc });
        b.Entity<AttendanceAttempt>().ToTable(t=> {
            t.HasCheckConstraint("CK_Attempt_Position","[Latitude] BETWEEN -90 AND 90 AND [Longitude] BETWEEN -180 AND 180 AND [AccuracyMeters] > 0 AND [DistanceMeters] >= 0");
            t.HasCheckConstraint("CK_Attempt_Outcome","[Outcome] IN ('Accepted','NeedsReview')");
            t.HasCheckConstraint("CK_Attempt_Action","[Action] IN ('CheckIn','CheckOut')");
        });
        b.Entity<AttendanceRecord>().HasIndex(x => new { x.EmployeeShiftId, x.Action }).IsUnique();
        b.Entity<AttendanceRecord>().HasIndex(x => x.AttemptId).IsUnique();
        b.Entity<AttendanceRecord>().ToTable(t=>t.HasCheckConstraint("CK_Record_Action","[Action] IN ('CheckIn','CheckOut')"));
        b.Entity<AttendanceRequest>().HasIndex(x => x.AttemptId).IsUnique();
        b.Entity<AttendanceRequest>().HasIndex(x => new { x.Status, x.CreatedAtUtc });
        b.Entity<AttendanceRequest>().ToTable(t => t.HasCheckConstraint("CK_Request_Status", "[Status] IN ('Pending','Approved','Rejected','Cancelled')"));
        b.Entity<OtpChallenge>().HasIndex(x => new { x.UserId, x.Purpose, x.CreatedAtUtc });
        b.Entity<OtpChallenge>().ToTable(t=>t.HasCheckConstraint("CK_OTP_Attempts","[FailedAttempts] BETWEEN 0 AND 5 AND [ExpiresAtUtc] > [CreatedAtUtc]"));
        b.Entity<AuthFlow>().HasIndex(x => new { x.UserId, x.CreatedAtUtc });
        b.Entity<AuthSession>().HasIndex(x => x.JtiHash).IsUnique();
        b.Entity<ExternalAuthProof>().HasIndex(x => x.ProofHash).IsUnique();
        b.Entity<AuditLog>().HasIndex(x => new { x.EntityType, x.EntityId, x.CreatedAtUtc });
        foreach (var entity in b.Model.GetEntityTypes())
        {
            foreach (var fk in entity.GetForeignKeys()) fk.DeleteBehavior = DeleteBehavior.Restrict;
            foreach (var property in entity.GetProperties().Where(p => p.ClrType == typeof(string)))
                property.SetMaxLength(property.Name.Contains("Json") || property.Name == "PasswordHash" ? 4000 :
                    property.Name is "Reason" or "Note" or "DecisionReason" ? 1000 : 256);
        }
    }
}
