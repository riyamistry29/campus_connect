namespace CampusConnect.Models;

public enum EventStatus
{
    Draft,
    PendingApproval,
    Published,
    Rejected,
    Cancelled
}

public enum RegistrationStatus
{
    Confirmed,
    Cancelled,
    Waitlisted
}

public enum AttendanceStatus
{
    NotMarked,
    Present,
    Absent
}
