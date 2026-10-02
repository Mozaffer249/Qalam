using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Qalam.Data.DTOs.Policy;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;
using Qalam.Service.Models.Policy;

namespace Qalam.Service.Implementations;

public class PolicyCaseAdminService : IPolicyCaseAdminService
{
    private readonly ApplicationDBContext _db;
    private readonly IPolicyResolver _resolver;
    private readonly IPolicyContextBuilder _builder;
    private readonly ICancellationPolicyEngine _engine;
    private readonly IPolicyCaseExecutor _executor;
    private readonly IStudentWalletService _wallets;
    private readonly IReplacementScheduleService _replacements;

    public PolicyCaseAdminService(
        ApplicationDBContext db,
        IPolicyResolver resolver,
        IPolicyContextBuilder builder,
        ICancellationPolicyEngine engine,
        IPolicyCaseExecutor executor,
        IStudentWalletService wallets,
        IReplacementScheduleService replacements)
    {
        _db = db;
        _resolver = resolver;
        _builder = builder;
        _engine = engine;
        _executor = executor;
        _wallets = wallets;
        _replacements = replacements;
    }

    public async Task<PolicyCaseListResultDto> ListAsync(PolicyCaseListFilter filter, CancellationToken cancellationToken = default)
    {
        var q = _db.PolicyCases.AsNoTracking().AsQueryable();
        if (Enum.TryParse<PolicyCaseKind>(filter.Kind, true, out var kind)) q = q.Where(c => c.Kind == kind);
        if (Enum.TryParse<PolicyCaseStatus>(filter.Status, true, out var status)) q = q.Where(c => c.Status == status);
        if (filter.From.HasValue) q = q.Where(c => c.CreatedAt >= filter.From);
        if (filter.To.HasValue) q = q.Where(c => c.CreatedAt <= filter.To);
        if (filter.EnrollmentId.HasValue) q = q.Where(c => c.EnrollmentId == filter.EnrollmentId);
        if (filter.StudentId.HasValue) q = q.Where(c => c.Enrollment.Participants.Any(p => p.StudentId == filter.StudentId));
        if (filter.TeacherId.HasValue) q = q.Where(c => c.Enrollment.ApprovedByTeacherId == filter.TeacherId);

        var pageSize = Math.Clamp(filter.PageSize, 1, 100);
        var page = Math.Max(filter.Page, 1);
        var total = await q.CountAsync(cancellationToken);
        var ids = await q.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        return new PolicyCaseListResultDto
        {
            Items = await LoadItemsAsync(ids, cancellationToken),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PolicyCaseDetailDto?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var c = await _db.PolicyCases.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (c == null)
            return null;

        var item = (await LoadItemsAsync(new List<int> { id }, cancellationToken)).FirstOrDefault()
                   ?? ToItem(c, null, null, null);
        var dto = new PolicyCaseDetailDto();
        CopyItem(item, dto);
        dto.PaymentId = c.PaymentId;
        dto.RefundId = c.RefundId;
        dto.ReplacementScheduleId = c.ReplacementScheduleId;
        dto.ComplaintId = c.ComplaintId;
        dto.GrossValue = c.GrossValue;
        dto.RuleSectionJson = c.RuleSectionJson;
        dto.InputsJson = c.InputsJson;
        dto.Explanation = ParseExplanation(c.ExplanationJson);

        var refunds = await _db.Refunds.AsNoTracking()
            .Where(r => r.PolicyCaseId == id)
            .OrderBy(r => r.Id)
            .ToListAsync(cancellationToken);
        dto.Refunds = refunds.Select(r => new PolicyCaseLinkedRefundDto
        {
            Id = r.Id,
            PaymentId = r.PaymentId,
            Amount = r.Amount,
            FeeAmount = r.FeeAmount,
            Currency = r.Currency,
            Destination = r.Destination.ToString(),
            Status = r.Status.ToString(),
            CreatedAt = r.CreatedAt
        }).ToList();

        var refundIds = refunds.Select(r => r.Id).ToList();
        dto.WalletTransactions = await _db.WalletTransactions.AsNoTracking()
            .Where(t => t.PolicyCaseId == id || (t.RefundId != null && refundIds.Contains(t.RefundId.Value)))
            .OrderBy(t => t.Id)
            .Select(t => new PolicyCaseLinkedWalletTxDto
            {
                Id = t.Id,
                Type = t.Type.ToString(),
                Amount = t.Amount,
                BalanceBefore = t.BalanceBefore,
                BalanceAfter = t.BalanceAfter,
                Status = t.Status.ToString(),
                CreatedAt = t.CreatedAt
            })
            .ToListAsync(cancellationToken);

        dto.TeacherAdjustments = await _db.TeacherBalanceAdjustments.AsNoTracking()
            .Where(a => a.PolicyCaseId == id)
            .OrderBy(a => a.Id)
            .Select(a => new PolicyCaseLinkedAdjustmentDto
            {
                Id = a.Id,
                TeacherId = a.TeacherId,
                Kind = a.Kind.ToString(),
                Status = a.Status.ToString(),
                Amount = a.Amount,
                ReasonCode = a.ReasonCode,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(cancellationToken);

        dto.CannotReverseReason = CannotReverseReason(c, refunds);
        dto.CanReverse = dto.CannotReverseReason == null;
        return dto;
    }

    public async Task<PolicyCaseDetailDto> ApplyExceptionAsync(
        AdminPolicyExceptionRequest request,
        int adminUserId,
        bool isSuperAdmin,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<AdminExceptionAction>(request.Action, true, out var action) || !Enum.IsDefined(action))
            throw new InvalidOperationException("Unknown exception action.");

        var current = await _resolver.GetCurrentAsync(cancellationToken);
        var teacherAdjustment = Math.Round(request.TeacherAdjustment ?? 0m, 2);
        var check = _engine.ValidateException(
            current.Rules.AdminExceptions,
            action,
            Math.Max(Math.Abs(request.Amount), Math.Abs(teacherAdjustment)),
            request.Reason,
            isSuperAdmin);
        if (!check.Allowed)
            throw new PolicyDeniedException(check);

        var bundle = await _builder.BuildAsync(
                request.EnrollmentId, PolicyCaseKind.AdminException, request.ScheduleId, PolicyStudentChoice.None, cancellationToken)
            ?? throw new InvalidOperationException("Enrollment not found.");
        var ctx = bundle.Context;
        var schedule = request.ScheduleId is int sid
            ? bundle.Enrollment.CourseSchedules.FirstOrDefault(s => s.Id == sid)
              ?? throw new InvalidOperationException("The session does not belong to this enrollment.")
            : null;

        var decision = check;
        decision.Currency = ctx.Currency;
        decision.TeacherEarningImpact = teacherAdjustment;
        decimal walletCredit = 0m;

        switch (action)
        {
            case AdminExceptionAction.FullRefund:
            case AdminExceptionAction.PartialRefund:
                {
                    var payments = request.PaymentId is int pid
                        ? ctx.Payments.Where(p => p.PaymentId == pid).ToList()
                        : ctx.Payments;
                    if (payments.Count == 0)
                        throw new InvalidOperationException("No paid payment found for this enrollment.");
                    var refundable = payments.Sum(p => p.Refundable);
                    var amount = action == AdminExceptionAction.FullRefund ? refundable : Math.Round(request.Amount, 2);
                    if (amount <= 0 || amount > refundable + 0.001m)
                        throw new InvalidOperationException($"The refund must be between 0 and the refundable {refundable:0.##} {ctx.Currency}.");
                    decision.RefundAmount = amount;
                    decision.Destination = Enum.TryParse<RefundDestination>(request.Destination, true, out var dest)
                        ? dest
                        : RefundDestination.Wallet;
                    decision.Allocations = Allocate(payments, amount);
                    break;
                }
            case AdminExceptionAction.WalletCredit:
                walletCredit = Math.Round(request.Amount, 2);
                if (walletCredit <= 0)
                    throw new InvalidOperationException("The wallet credit must be greater than 0.");
                decision.RefundAmount = walletCredit;
                decision.Destination = RefundDestination.Wallet;
                break;
            case AdminExceptionAction.Replacement:
                if (schedule == null)
                    throw new InvalidOperationException("Choose the session to replace.");
                decision.CreateReplacement = true;
                break;
            case AdminExceptionAction.Reschedule:
                if (schedule == null || request.NewDate == null || request.NewTeacherAvailabilityId == null)
                    throw new InvalidOperationException("Choose the session and its new date and time.");
                if (schedule.Status is not (ScheduleStatus.Scheduled or ScheduleStatus.InProgress))
                    throw new InvalidOperationException("Only upcoming sessions can be rescheduled.");
                decision.Reschedule = true;
                break;
            case AdminExceptionAction.TeacherEarningAdjustment:
                if (teacherAdjustment == 0)
                    throw new InvalidOperationException("Enter the teacher adjustment amount.");
                break;
        }

        decision.PlatformRevenueImpact = Math.Round(-decision.RefundAmount - decision.TeacherEarningImpact, 2);
        decision.Explanation.Add(new PolicyExplanationLine
        {
            Ar = $"استثناء إداري: {ActionAr(action)}.",
            En = $"Admin exception: {action}."
        });

        var payerUserId = bundle.Enrollment.OwnerUserId ?? bundle.Enrollment.EnrollmentRequest?.RequestedByUserId;
        var policyCase = await _executor.ApplyAsync(
            bundle,
            decision,
            new PolicyActor(adminUserId, isSuperAdmin ? "SuperAdmin" : "Admin"),
            new PolicyApplyOptions
            {
                Reason = request.Reason.Trim(),
                ScheduleReason = ScheduleCancellationReason.AdminCancel,
                BeforeCommit = async pc =>
                {
                    if (walletCredit > 0)
                    {
                        if (payerUserId is not int payer)
                            throw new InvalidOperationException("The enrollment has no payer wallet.");
                        var credit = await _wallets.CreditAsync(new WalletEntryRequest
                        {
                            UserId = payer,
                            Amount = walletCredit,
                            Type = WalletTransactionType.AdminCredit,
                            EnrollmentId = bundle.Enrollment.Id,
                            CourseScheduleId = request.ScheduleId,
                            PolicyCaseId = pc.Id,
                            Description = $"Admin exception (policy case #{pc.Id})",
                            ReasonCode = "POLICY_ADMIN_EXCEPTION",
                            CreatedByUserId = adminUserId
                        }, cancellationToken);
                        if (!credit.Succeeded)
                            throw new InvalidOperationException($"Wallet credit failed: {credit.ErrorCode}.");
                    }

                    if (decision.Reschedule && schedule != null)
                    {
                        var replacement = await _replacements.RescheduleAsync(
                            schedule,
                            bundle.Enrollment.ApprovedByTeacherId,
                            request.NewDate!.Value,
                            request.NewTeacherAvailabilityId!.Value,
                            ScheduleCancellationReason.AdminCancel,
                            pc.Id,
                            $"Rescheduled by admin (policy case #{pc.Id})",
                            cancellationToken);
                        pc.ReplacementScheduleId = replacement.Id;
                    }

                    if (teacherAdjustment != 0)
                    {
                        _db.TeacherBalanceAdjustments.Add(TeacherAdjustment(
                            bundle.Enrollment.ApprovedByTeacherId,
                            teacherAdjustment,
                            pc,
                            "POLICY_ADMIN_EXCEPTION",
                            adminUserId));
                    }
                }
            },
            cancellationToken);

        return (await GetAsync(policyCase.Id, cancellationToken))!;
    }

    public async Task<PolicyCaseDetailDto?> ReverseAsync(int id, string reason, int adminUserId, CancellationToken cancellationToken = default)
    {
        var original = await _db.PolicyCases.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (original == null)
            return null;

        var refunds = await _db.Refunds
            .Where(r => r.PolicyCaseId == id && r.Status == RefundStatus.Succeeded)
            .ToListAsync(cancellationToken);
        var blocked = CannotReverseReason(original, refunds);
        if (blocked != null)
            throw new InvalidOperationException(blocked);

        var minReason = (await _resolver.GetCurrentAsync(cancellationToken)).Rules.AdminExceptions.MinReasonLength;
        if ((reason ?? "").Trim().Length < Math.Max(1, minReason))
            throw new InvalidOperationException($"A reason of at least {minReason} characters is required.");

        IDbContextTransaction? tx = null;
        if (_db.Database.IsRelational() && _db.Database.CurrentTransaction == null)
            tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var reversal = new PolicyCase
            {
                Kind = PolicyCaseKind.Reversal,
                Status = PolicyCaseStatus.Applied,
                EnrollmentId = original.EnrollmentId,
                CourseScheduleId = original.CourseScheduleId,
                PaymentId = original.PaymentId,
                ReversesCaseId = original.Id,
                PolicyVersionId = original.PolicyVersionId,
                Currency = original.Currency,
                Reason = reason!.Trim().Length > 1000 ? reason.Trim()[..1000] : reason.Trim(),
                ActorUserId = adminUserId,
                ActorRole = "Admin",
                ExplanationJson = JsonSerializer.Serialize(new List<PolicyExplanationLine>
                {
                    new() { Ar = $"عكس الحالة رقم {original.Id}.", En = $"Reversal of policy case #{original.Id}." }
                }, CancellationPolicyDefaults.JsonOptions),
                CreatedAt = DateTime.UtcNow
            };
            _db.PolicyCases.Add(reversal);
            await _db.SaveChangesAsync(cancellationToken);

            decimal studentBack = 0m;
            foreach (var refund in refunds)
            {
                studentBack += await ReverseWalletRefundAsync(refund, original, reversal, adminUserId, cancellationToken);
                reversal.RefundId ??= refund.Id;
            }

            var refundIds = refunds.Select(r => r.Id).ToList();
            var otherCredits = await _db.WalletTransactions
                .Where(t => t.PolicyCaseId == original.Id
                            && t.Amount > 0
                            && t.Status == WalletTransactionStatus.Completed
                            && (t.RefundId == null || !refundIds.Contains(t.RefundId.Value)))
                .ToListAsync(cancellationToken);
            foreach (var credit in otherCredits)
            {
                await DebitWalletAsync(credit, null, original, reversal, adminUserId, cancellationToken);
                studentBack += credit.Amount;
            }

            var teacherBack = await ReverseTeacherEffectsAsync(original, reversal, adminUserId, cancellationToken);

            if (original.ReplacementScheduleId is int replacementId)
            {
                var replacement = await _db.CourseSchedules.FirstOrDefaultAsync(s => s.Id == replacementId, cancellationToken);
                if (replacement is { Status: ScheduleStatus.Scheduled })
                {
                    replacement.Status = ScheduleStatus.Cancelled;
                    replacement.CancellationReason = ScheduleCancellationReason.AdminCancel;
                    replacement.PolicyCaseId = reversal.Id;
                }
            }

            reversal.RefundAmount = -studentBack;
            reversal.TeacherEarningImpact = teacherBack;
            reversal.PlatformRevenueImpact = Math.Round(studentBack - teacherBack, 2);
            original.Status = PolicyCaseStatus.Reversed;
            original.ReversedByCaseId = reversal.Id;

            await _db.SaveChangesAsync(cancellationToken);
            if (tx != null)
                await tx.CommitAsync(cancellationToken);

            return await GetAsync(reversal.Id, cancellationToken);
        }
        catch
        {
            if (tx != null)
                await tx.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (tx != null)
                await tx.DisposeAsync();
        }
    }

    public async Task<List<FinancialTimelineEntryDto>?> GetEnrollmentTimelineAsync(int enrollmentId, CancellationToken cancellationToken = default)
    {
        var enrollment = await _db.Enrollments.AsNoTracking()
            .Where(e => e.Id == enrollmentId)
            .Select(e => new
            {
                e.Id,
                e.CreatedAt,
                e.ActivatedAt,
                e.CancelledAt,
                e.CompletedAt,
                e.EnrollmentStatus,
                VersionNumber = e.PolicyVersion != null ? (int?)e.PolicyVersion.VersionNumber : null
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (enrollment == null)
            return null;

        var entries = new List<FinancialTimelineEntryDto>
        {
            new()
            {
                Key = $"enr-{enrollment.Id}-created", OccurredAt = enrollment.CreatedAt, Category = "Enrollment",
                Title = "EnrollmentCreated", PolicyVersionNumber = enrollment.VersionNumber
            }
        };
        if (enrollment.ActivatedAt is DateTime activated)
            entries.Add(new() { Key = $"enr-{enrollment.Id}-activated", OccurredAt = activated, Category = "Enrollment", Title = "EnrollmentActivated", PolicyVersionNumber = enrollment.VersionNumber });
        if (enrollment.CancelledAt is DateTime cancelled)
            entries.Add(new() { Key = $"enr-{enrollment.Id}-cancelled", OccurredAt = cancelled, Category = "Enrollment", Title = "EnrollmentCancelled" });
        if (enrollment.CompletedAt is DateTime completed)
            entries.Add(new() { Key = $"enr-{enrollment.Id}-completed", OccurredAt = completed, Category = "Enrollment", Title = "EnrollmentCompleted" });

        var paymentEvents = await _db.PaymentTransactionEvents.AsNoTracking()
            .Where(e => e.EnrollmentId == enrollmentId)
            .Select(e => new { e.Id, e.EventType, e.Result, e.Amount, e.Currency, e.Source, e.PaymentId, e.ReceivedAt, e.CreatedAt, e.Notes })
            .ToListAsync(cancellationToken);
        entries.AddRange(paymentEvents.Select(e => new FinancialTimelineEntryDto
        {
            Key = $"pev-{e.Id}",
            OccurredAt = e.ReceivedAt == default ? e.CreatedAt : e.ReceivedAt,
            Category = "Payment",
            Title = e.EventType.ToString(),
            Detail = e.PaymentId.HasValue ? $"Payment #{e.PaymentId}" : e.Notes,
            ActorRole = e.Source.ToString(),
            Amount = e.Amount,
            Currency = e.Currency,
            Status = e.Result.ToString()
        }));

        var cases = await _db.PolicyCases.AsNoTracking()
            .Where(c => c.EnrollmentId == enrollmentId)
            .Select(c => new
            {
                c.Id, c.Kind, c.Status, c.RefundAmount, c.Currency, c.ActorRole, c.ActorUserId, c.Reason,
                c.CourseScheduleId, c.CreatedAt,
                VersionNumber = c.PolicyVersion != null ? (int?)c.PolicyVersion.VersionNumber : null
            })
            .ToListAsync(cancellationToken);
        entries.AddRange(cases.Select(c => new FinancialTimelineEntryDto
        {
            Key = $"case-{c.Id}",
            OccurredAt = c.CreatedAt,
            Category = "PolicyCase",
            Title = c.Kind.ToString(),
            Detail = c.Reason,
            ActorRole = c.ActorRole,
            ActorUserId = c.ActorUserId,
            Amount = c.RefundAmount,
            Currency = c.Currency,
            PolicyVersionNumber = c.VersionNumber,
            PolicyCaseId = c.Id,
            CourseScheduleId = c.CourseScheduleId,
            Status = c.Status.ToString()
        }));

        var refunds = await _db.Refunds.AsNoTracking()
            .Where(r => r.EnrollmentId == enrollmentId)
            .Select(r => new { r.Id, r.Amount, r.FeeAmount, r.Currency, r.Destination, r.Status, r.PolicyCaseId, r.InitiatedByUserId, r.CreatedAt, r.Reason })
            .ToListAsync(cancellationToken);
        entries.AddRange(refunds.Select(r => new FinancialTimelineEntryDto
        {
            Key = $"ref-{r.Id}",
            OccurredAt = r.CreatedAt,
            Category = "Refund",
            Title = r.Amount < 0 ? "RefundReversed" : "Refund",
            Detail = $"{r.Destination}{(r.FeeAmount != 0 ? $" · fee {r.FeeAmount:0.##}" : "")} · {r.Reason}",
            ActorUserId = r.InitiatedByUserId,
            Amount = r.Amount,
            Currency = r.Currency,
            PolicyCaseId = r.PolicyCaseId,
            Status = r.Status.ToString()
        }));

        var walletTx = await _db.WalletTransactions.AsNoTracking()
            .Where(t => t.EnrollmentId == enrollmentId)
            .Select(t => new { t.Id, t.Type, t.Amount, t.BalanceBefore, t.BalanceAfter, t.Currency, t.Status, t.PolicyCaseId, t.CreatedByUserId, t.CreatedAt, t.CourseScheduleId })
            .ToListAsync(cancellationToken);
        entries.AddRange(walletTx.Select(t => new FinancialTimelineEntryDto
        {
            Key = $"wtx-{t.Id}",
            OccurredAt = t.CreatedAt,
            Category = "Wallet",
            Title = t.Type.ToString(),
            Detail = $"{t.BalanceBefore:0.##} → {t.BalanceAfter:0.##}",
            ActorUserId = t.CreatedByUserId,
            Amount = t.Amount,
            Currency = t.Currency,
            PolicyCaseId = t.PolicyCaseId,
            CourseScheduleId = t.CourseScheduleId,
            Status = t.Status.ToString()
        }));

        var earnings = await _db.TeacherEarningLines.AsNoTracking()
            .Where(l => l.EnrollmentId == enrollmentId)
            .Select(l => new { l.Id, l.Amount, l.Currency, l.Status, l.CourseScheduleId, l.CreatedAt, l.UpdatedAt })
            .ToListAsync(cancellationToken);
        foreach (var l in earnings)
        {
            entries.Add(new FinancialTimelineEntryDto
            {
                Key = $"earn-{l.Id}",
                OccurredAt = l.CreatedAt,
                Category = "Earning",
                Title = "EarningAccrued",
                Amount = l.Amount,
                Currency = l.Currency,
                CourseScheduleId = l.CourseScheduleId,
                Status = l.Status.ToString()
            });
            if (l.Status == TeacherEarningLineStatus.Voided)
            {
                entries.Add(new FinancialTimelineEntryDto
                {
                    Key = $"earn-{l.Id}-void",
                    OccurredAt = l.UpdatedAt ?? l.CreatedAt,
                    Category = "Earning",
                    Title = "EarningVoided",
                    Amount = -l.Amount,
                    Currency = l.Currency,
                    CourseScheduleId = l.CourseScheduleId,
                    Status = l.Status.ToString()
                });
            }
        }

        var caseIds = cases.Select(c => c.Id).ToList();
        var refundIds = refunds.Select(r => r.Id).ToList();
        var adjustments = await _db.TeacherBalanceAdjustments.AsNoTracking()
            .Where(a => (a.PolicyCaseId != null && caseIds.Contains(a.PolicyCaseId.Value))
                        || (a.RelatedRefundId != null && refundIds.Contains(a.RelatedRefundId.Value)))
            .Select(a => new { a.Id, a.Kind, a.Status, a.Amount, a.Currency, a.ReasonCode, a.ReasonText, a.PolicyCaseId, a.CreatedByUserId, a.CreatedAt })
            .ToListAsync(cancellationToken);
        entries.AddRange(adjustments.Select(a => new FinancialTimelineEntryDto
        {
            Key = $"adj-{a.Id}",
            OccurredAt = a.CreatedAt,
            Category = "Adjustment",
            Title = a.Kind.ToString(),
            Detail = $"{a.ReasonCode} · {a.ReasonText}",
            ActorUserId = a.CreatedByUserId,
            Amount = -a.Amount,
            Currency = a.Currency,
            PolicyCaseId = a.PolicyCaseId,
            Status = a.Status.ToString()
        }));

        var audits = await _db.SessionAuditLogs.AsNoTracking()
            .Where(a => a.CourseSchedule.EnrollmentId == enrollmentId)
            .Select(a => new { a.Id, a.ActionType, a.ActorRole, a.ActorUserId, a.CourseScheduleId, a.CreatedAt, a.PayloadJson })
            .ToListAsync(cancellationToken);
        entries.AddRange(audits.Select(a => new FinancialTimelineEntryDto
        {
            Key = $"aud-{a.Id}",
            OccurredAt = a.CreatedAt,
            Category = "Session",
            Title = a.ActionType.ToString(),
            Detail = a.PayloadJson,
            ActorRole = a.ActorRole,
            ActorUserId = a.ActorUserId,
            CourseScheduleId = a.CourseScheduleId
        }));

        return entries.OrderBy(e => e.OccurredAt).ThenBy(e => e.Key, StringComparer.Ordinal).ToList();
    }

    private async Task<List<PolicyCaseListItemDto>> LoadItemsAsync(List<int> ids, CancellationToken cancellationToken)
    {
        var rows = await _db.PolicyCases.AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .Select(c => new
            {
                Case = c,
                VersionNumber = c.PolicyVersion != null ? (int?)c.PolicyVersion.VersionNumber : null,
                TeacherName = ((c.Enrollment.ApprovedByTeacher.User.FirstName ?? "") + " " + (c.Enrollment.ApprovedByTeacher.User.LastName ?? "")).Trim(),
                StudentNames = c.Enrollment.Participants
                    .Select(p => ((p.Student.User.FirstName ?? "") + " " + (p.Student.User.LastName ?? "")).Trim())
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var byId = rows.ToDictionary(r => r.Case.Id);
        return ids.Where(byId.ContainsKey).Select(id =>
        {
            var r = byId[id];
            return ToItem(r.Case, r.VersionNumber, r.TeacherName, string.Join(", ", r.StudentNames.Where(n => n.Length > 0)));
        }).ToList();
    }

    private static PolicyCaseListItemDto ToItem(PolicyCase c, int? versionNumber, string? teacherName, string? studentName) => new()
    {
        Id = c.Id,
        Kind = c.Kind.ToString(),
        Status = c.Status.ToString(),
        EnrollmentId = c.EnrollmentId,
        CourseScheduleId = c.CourseScheduleId,
        StudentName = studentName,
        TeacherName = teacherName,
        PolicyVersionNumber = versionNumber,
        RefundAmount = c.RefundAmount,
        FeeAmount = c.FeeAmount,
        TeacherEarningImpact = c.TeacherEarningImpact,
        PlatformRevenueImpact = c.PlatformRevenueImpact,
        Currency = c.Currency,
        Destination = c.Destination?.ToString(),
        ActorRole = c.ActorRole,
        ActorUserId = c.ActorUserId,
        Reason = c.Reason,
        ReversesCaseId = c.ReversesCaseId,
        ReversedByCaseId = c.ReversedByCaseId,
        CreatedAt = c.CreatedAt
    };

    private static void CopyItem(PolicyCaseListItemDto from, PolicyCaseListItemDto to)
    {
        to.Id = from.Id;
        to.Kind = from.Kind;
        to.Status = from.Status;
        to.EnrollmentId = from.EnrollmentId;
        to.CourseScheduleId = from.CourseScheduleId;
        to.StudentName = from.StudentName;
        to.TeacherName = from.TeacherName;
        to.PolicyVersionNumber = from.PolicyVersionNumber;
        to.RefundAmount = from.RefundAmount;
        to.FeeAmount = from.FeeAmount;
        to.TeacherEarningImpact = from.TeacherEarningImpact;
        to.PlatformRevenueImpact = from.PlatformRevenueImpact;
        to.Currency = from.Currency;
        to.Destination = from.Destination;
        to.ActorRole = from.ActorRole;
        to.ActorUserId = from.ActorUserId;
        to.Reason = from.Reason;
        to.ReversesCaseId = from.ReversesCaseId;
        to.ReversedByCaseId = from.ReversedByCaseId;
        to.CreatedAt = from.CreatedAt;
    }

    private static string? CannotReverseReason(PolicyCase c, IEnumerable<Refund> refunds)
    {
        if (c.Status == PolicyCaseStatus.Reversed)
            return "This case has already been reversed.";
        if (c.Kind == PolicyCaseKind.Reversal)
            return "A reversal cannot be reversed; apply an admin exception instead.";
        if (c.Status != PolicyCaseStatus.Applied)
            return "Only applied cases can be reversed.";
        if (refunds.Any(r => r.Status == RefundStatus.Succeeded && r.Destination == RefundDestination.OriginalMethod))
            return "Refunds sent to the original payment method cannot be reversed. Recover the money outside the platform, then record an admin exception.";
        return null;
    }

    /// <summary>Adds a negative refund row and debits the wallet credit it produced. Returns the amount taken back.</summary>
    private async Task<decimal> ReverseWalletRefundAsync(
        Refund refund, PolicyCase original, PolicyCase reversal, int adminUserId, CancellationToken cancellationToken)
    {
        var compensating = new Refund
        {
            PaymentId = refund.PaymentId,
            EnrollmentId = refund.EnrollmentId,
            Amount = -refund.Amount,
            FeeAmount = -refund.FeeAmount,
            Currency = refund.Currency,
            Reason = $"Reversal of refund #{refund.Id} (policy case #{original.Id})",
            Status = RefundStatus.Succeeded,
            Destination = RefundDestination.Wallet,
            InitiatedByUserId = adminUserId,
            PolicyCaseId = reversal.Id,
            CreatedAt = DateTime.UtcNow
        };
        _db.Refunds.Add(compensating);
        await _db.SaveChangesAsync(cancellationToken);

        var credit = await _db.WalletTransactions.FirstOrDefaultAsync(t => t.RefundId == refund.Id
                                                                         && t.Amount > 0
                                                                         && t.Status == WalletTransactionStatus.Completed,
            cancellationToken);
        if (credit != null)
            await DebitWalletAsync(credit, compensating.Id, original, reversal, adminUserId, cancellationToken);

        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.Id == refund.PaymentId, cancellationToken);
        if (payment is { Status: PaymentStatus.Refunded })
        {
            payment.Status = PaymentStatus.Succeeded;
            var links = await _db.EnrollmentPayments
                .Where(ep => ep.PaymentId == payment.Id && ep.Status == PaymentStatus.Refunded)
                .ToListAsync(cancellationToken);
            foreach (var link in links)
                link.Status = PaymentStatus.Succeeded;
        }

        return refund.Amount;
    }

    private async Task DebitWalletAsync(
        WalletTransaction credit, int? compensatingRefundId, PolicyCase original, PolicyCase reversal, int adminUserId,
        CancellationToken cancellationToken)
    {
        var walletUserId = await _db.StudentWallets.Where(w => w.Id == credit.WalletId).Select(w => w.UserId).FirstAsync(cancellationToken);
        var debit = await _wallets.DebitAsync(new WalletEntryRequest
        {
            UserId = walletUserId,
            Amount = credit.Amount,
            Type = WalletTransactionType.PolicyReversal,
            PaymentId = credit.PaymentId,
            RefundId = compensatingRefundId,
            EnrollmentId = credit.EnrollmentId ?? original.EnrollmentId,
            CourseScheduleId = credit.CourseScheduleId,
            PolicyCaseId = reversal.Id,
            ReversesTransactionId = credit.Id,
            Description = $"Reversal of policy case #{original.Id}",
            ReasonCode = "POLICY_REVERSAL",
            CreatedByUserId = adminUserId
        }, cancellationToken);
        if (!debit.Succeeded)
            throw new InvalidOperationException(
                $"The student's wallet balance is too low to reverse {credit.Amount:0.##} {credit.Currency} ({debit.ErrorCode}).");
    }

    /// <summary>
    /// Compensates the teacher-side effects of <paramref name="original"/>: each adjustment gets an opposite entry,
    /// and session earnings voided by the case are re-accrued as a new line. Returns the signed change for the teacher.
    /// </summary>
    private async Task<decimal> ReverseTeacherEffectsAsync(
        PolicyCase original, PolicyCase reversal, int adminUserId, CancellationToken cancellationToken)
    {
        decimal teacherBack = 0m;
        var adjustments = await _db.TeacherBalanceAdjustments.AsNoTracking()
            .Where(a => a.PolicyCaseId == original.Id)
            .ToListAsync(cancellationToken);
        foreach (var a in adjustments)
        {
            var signedForTeacher = a.Kind == TeacherBalanceAdjustmentKind.Correction ? -a.Amount : -Math.Abs(a.Amount);
            _db.TeacherBalanceAdjustments.Add(TeacherAdjustment(a.TeacherId, -signedForTeacher, reversal, "POLICY_REVERSAL", adminUserId, a.RelatedEarningLineId));
            teacherBack += -signedForTeacher;
        }

        var voidedByCase = Math.Abs(Math.Min(0m, original.TeacherEarningImpact)) - adjustments
            .Where(a => a.Kind == TeacherBalanceAdjustmentKind.Deduction)
            .Sum(a => a.Amount);
        if (original.CourseScheduleId is int scheduleId && voidedByCase > 0)
        {
            var lines = await _db.TeacherEarningLines.AsNoTracking()
                .Where(l => l.CourseScheduleId == scheduleId)
                .ToListAsync(cancellationToken);
            var voided = lines.Where(l => l.Status == TeacherEarningLineStatus.Voided).ToList();
            if (voided.Count > 0 && lines.All(l => l.Status == TeacherEarningLineStatus.Voided))
            {
                var amount = Math.Min(voidedByCase, voided.Sum(l => l.Amount));
                _db.TeacherEarningLines.Add(new TeacherEarningLine
                {
                    TeacherId = voided[0].TeacherId,
                    EnrollmentId = voided[0].EnrollmentId,
                    CourseScheduleId = scheduleId,
                    Amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero),
                    Currency = voided[0].Currency,
                    Source = voided[0].Source,
                    Status = TeacherEarningLineStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                });
                teacherBack += amount;
            }
        }

        return Math.Round(teacherBack, 2);
    }

    /// <summary>Signed for the teacher: negative is a Deduction, positive a Correction credit (stored as a negative amount).</summary>
    private static TeacherBalanceAdjustment TeacherAdjustment(
        int teacherId, decimal signedForTeacher, PolicyCase policyCase, string code, int adminUserId, int? earningLineId = null) => new()
    {
        TeacherId = teacherId,
        Amount = signedForTeacher < 0 ? Math.Round(-signedForTeacher, 2) : -Math.Round(signedForTeacher, 2),
        Currency = policyCase.Currency,
        Kind = signedForTeacher < 0 ? TeacherBalanceAdjustmentKind.Deduction : TeacherBalanceAdjustmentKind.Correction,
        Status = TeacherBalanceAdjustmentStatus.Pending,
        ReasonCode = code,
        ReasonText = $"Policy case #{policyCase.Id} ({policyCase.Kind})",
        RelatedRefundId = policyCase.RefundId,
        RelatedEarningLineId = earningLineId,
        RelatedComplaintId = policyCase.ComplaintId,
        PolicyCaseId = policyCase.Id,
        CreatedByUserId = adminUserId
    };

    private static List<PolicyRefundAllocation> Allocate(List<PolicyPaymentInfo> payments, decimal amount)
    {
        var refundable = payments.Where(p => p.Refundable > 0).ToList();
        var total = refundable.Sum(p => p.Refundable);
        var result = new List<PolicyRefundAllocation>();
        var left = amount;
        for (var i = 0; i < refundable.Count; i++)
        {
            var share = i == refundable.Count - 1
                ? left
                : Math.Min(refundable[i].Refundable, Math.Round(amount * refundable[i].Refundable / total, 2, MidpointRounding.AwayFromZero));
            if (share <= 0) continue;
            result.Add(new PolicyRefundAllocation { PaymentId = refundable[i].PaymentId, Amount = share });
            left -= share;
        }
        return result;
    }

    private static List<PolicyExplanationDto> ParseExplanation(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<PolicyExplanationDto>();
        try
        {
            return (JsonSerializer.Deserialize<List<PolicyExplanationLine>>(json, CancellationPolicyDefaults.JsonOptions) ?? new())
                .Select(l => new PolicyExplanationDto { Ar = l.Ar, En = l.En })
                .ToList();
        }
        catch (JsonException)
        {
            return new List<PolicyExplanationDto>();
        }
    }

    private static string ActionAr(AdminExceptionAction action) => action switch
    {
        AdminExceptionAction.FullRefund => "استرداد كامل",
        AdminExceptionAction.PartialRefund => "استرداد جزئي",
        AdminExceptionAction.WalletCredit => "رصيد في المحفظة",
        AdminExceptionAction.Replacement => "حصة بديلة",
        AdminExceptionAction.Reschedule => "إعادة جدولة",
        AdminExceptionAction.TeacherEarningAdjustment => "تعديل أرباح المعلم",
        _ => action.ToString()
    };
}
