namespace Qalam.Data.Entity.Common.Enums;

public enum FreeTrialConsumptionSource
{
    CourseEnrollment = 1,
    OpenSessionRequest = 2
}

public enum FreeTrialConsumptionStatus
{
    Reserved = 1,
    Consumed = 2,
    CancelledBeforeStart = 3
}

public enum InterviewUnlockSource
{
    None = 0,
    AutoFromSession = 1,
    Admin = 2
}
