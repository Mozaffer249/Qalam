using Microsoft.Extensions.Logging;
using Qalam.Data.DTOs.Payment;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Payment;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Payments;

namespace Qalam.Service.Implementations;

public class PaymentConfirmationService : IPaymentConfirmationService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IEnrollmentPaymentRepository _enrollmentPaymentRepository;
    private readonly ITeacherAvailabilityRepository _teacherAvailabilityRepository;
    private readonly ICourseScheduleRepository _scheduleRepository;
    private readonly IScheduleGenerationService _scheduleGenerator;
    private readonly IOpenSessionRequestReleaseService _releaseService;
    private readonly IRefundService _refundService;
    private readonly IPaymentGatewayResolver _gatewayResolver;
    private readonly IPaymentTransactionEventService _events;
    private readonly ILogger<PaymentConfirmationService> _logger;

    public PaymentConfirmationService(
        IPaymentRepository paymentRepository,
        IEnrollmentRepository enrollmentRepository,
        IEnrollmentPaymentRepository enrollmentPaymentRepository,
        ITeacherAvailabilityRepository teacherAvailabilityRepository,
        ICourseScheduleRepository scheduleRepository,
        IScheduleGenerationService scheduleGenerator,
        IOpenSessionRequestReleaseService releaseService,
        IRefundService refundService,
        IPaymentGatewayResolver gatewayResolver,
        IPaymentTransactionEventService events,
        ILogger<PaymentConfirmationService> logger)
    {
        _paymentRepository = paymentRepository;
        _enrollmentRepository = enrollmentRepository;
        _enrollmentPaymentRepository = enrollmentPaymentRepository;
        _teacherAvailabilityRepository = teacherAvailabilityRepository;
        _scheduleRepository = scheduleRepository;
        _scheduleGenerator = scheduleGenerator;
        _releaseService = releaseService;
        _refundService = refundService;
        _gatewayResolver = gatewayResolver;
        _events = events;
        _logger = logger;
    }

    public async Task<PaymentOwnershipDto?> ResolveLocalPaymentAsync(
        string providerRef,
        CancellationToken cancellationToken = default)
    {
        var payment = await ResolveLocalPaymentEntityAsync(providerRef, cancellationToken);
        if (payment == null)
            return null;

        return new PaymentOwnershipDto
        {
            PaymentId = payment.Id,
            PayerUserId = payment.PayerUserId
        };
    }

    public async Task<PaymentConfirmationOutcome> ConfirmFromGatewayAsync(
        string providerTransactionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(providerTransactionId))
            return PaymentConfirmationOutcome.Fail("MISSING_REF", "Provider transaction id is required.");

        var payment = await ResolveLocalPaymentEntityAsync(providerTransactionId, cancellationToken);
        if (payment == null)
            return PaymentConfirmationOutcome.Fail("PAYMENT_NOT_FOUND", "Payment intent not found.");

        if (payment.Status == PaymentStatus.Succeeded)
        {
            var replay = await ConfirmAsync(payment.Id, cancellationToken);
            await RecordConfirmEventAsync(
                payment,
                PaymentTransactionEventType.ConfirmIdempotentReplay,
                replay.Succeeded ? PaymentTransactionEventResult.Success : PaymentTransactionEventResult.Failed,
                PaymentStatus.Succeeded,
                payment.Status,
                providerTransactionId,
                replay.ErrorCode,
                cancellationToken);
            return replay;
        }

        if (payment.Status is PaymentStatus.Failed or PaymentStatus.Cancelled or PaymentStatus.Refunded)
            return PaymentConfirmationOutcome.Fail(
                "PAYMENT_NOT_PAYABLE",
                $"Payment is {payment.Status}.");

        var statusBefore = payment.Status;
        await RecordConfirmEventAsync(
            payment,
            PaymentTransactionEventType.ConfirmRequested,
            PaymentTransactionEventResult.Pending,
            statusBefore,
            null,
            providerTransactionId,
            null,
            cancellationToken);

        IPaymentGateway gateway;
        try
        {
            gateway = _gatewayResolver.Resolve(payment.PaymentProvider);
        }
        catch (InvalidOperationException ex)
        {
            return PaymentConfirmationOutcome.Fail("UNKNOWN_PROVIDER", ex.Message);
        }

        // Prefer the stored ref (invoice id for Moyasar hosted) so Fetch can load nested payments.
        // Also try the client-supplied ref (often the Moyasar payment id from return URL).
        var fetchRef = payment.ProviderTransactionId ?? providerTransactionId;
        var remote = await gateway.FetchAsync(fetchRef, cancellationToken);
        if (remote == null && !string.Equals(fetchRef, providerTransactionId, StringComparison.Ordinal))
            remote = await gateway.FetchAsync(providerTransactionId, cancellationToken);

        if (remote == null)
        {
            await RecordConfirmEventAsync(
                payment,
                PaymentTransactionEventType.ConfirmFailed,
                PaymentTransactionEventResult.NotFound,
                statusBefore,
                payment.Status,
                providerTransactionId,
                "PROVIDER_NOT_FOUND",
                cancellationToken);
            return PaymentConfirmationOutcome.Fail("PROVIDER_NOT_FOUND", "Provider payment not found.");
        }

        await RecordConfirmEventAsync(
            payment,
            PaymentTransactionEventType.ConfirmRemoteVerified,
            PaymentTransactionEventResult.Success,
            statusBefore,
            payment.Status,
            providerTransactionId,
            remote.Status,
            cancellationToken);

        if (remote.MappedStatus != PaymentStatus.Succeeded
            && !MoyasarStatusMapper.IsPaid(remote.Status))
        {
            if (remote.MappedStatus == PaymentStatus.Failed)
            {
                payment.Status = PaymentStatus.Failed;
                payment.FailureMessage = Truncate(remote.Message, 500);
                payment.UpdatedAt = DateTime.UtcNow;
                await _paymentRepository.UpdateAsync(payment);
            }

            await RecordConfirmEventAsync(
                payment,
                PaymentTransactionEventType.ConfirmFailed,
                PaymentTransactionEventResult.Failed,
                statusBefore,
                payment.Status,
                providerTransactionId,
                remote.Message ?? remote.Status,
                cancellationToken);

            return PaymentConfirmationOutcome.Fail(
                "NOT_PAID",
                remote.Message ?? $"Payment status is '{remote.Status}'.");
        }

        var expectedHalalas = MinorUnitConverter.ToHalalas(payment.TotalAmount);
        // Mock gateway returns AmountHalalas = 0; skip amount check for Mock.
        var isMock = gateway.ProviderName.Equals(MockPaymentGateway.Name, StringComparison.OrdinalIgnoreCase);
        if (!isMock
            && (remote.AmountHalalas != expectedHalalas
                || !string.Equals(remote.Currency, payment.Currency, StringComparison.OrdinalIgnoreCase)))
        {
            await RecordConfirmEventAsync(
                payment,
                PaymentTransactionEventType.ConfirmAmountMismatch,
                PaymentTransactionEventResult.Mismatch,
                statusBefore,
                payment.Status,
                providerTransactionId,
                "PAYMENT_AMOUNT_MISMATCH",
                cancellationToken);
            return PaymentConfirmationOutcome.Fail("PAYMENT_AMOUNT_MISMATCH", "PAYMENT_AMOUNT_MISMATCH");
        }

        // Retain invoice id before promoting ProviderTransactionId to the real payment id.
        if (!string.IsNullOrWhiteSpace(remote.InvoiceId)
            && string.IsNullOrWhiteSpace(payment.ProviderInvoiceId))
        {
            payment.ProviderInvoiceId = remote.InvoiceId;
        }
        else if (string.IsNullOrWhiteSpace(payment.ProviderInvoiceId)
                 && !string.IsNullOrWhiteSpace(payment.ProviderTransactionId)
                 && !string.IsNullOrWhiteSpace(remote.Id)
                 && !remote.Id.Equals(payment.ProviderTransactionId, StringComparison.OrdinalIgnoreCase)
                 && (string.IsNullOrWhiteSpace(remote.InvoiceId)
                     || payment.ProviderTransactionId.Equals(remote.InvoiceId, StringComparison.OrdinalIgnoreCase)))
        {
            // Current stored ref is the invoice; keep it when promoting to payment id.
            payment.ProviderInvoiceId = payment.ProviderTransactionId;
        }

        // Promote invoice id → real payment id so refunds hit /payments/{id}.
        // Hosted Fetch(invoice) maps nested paid payment → remote.Id is the payment id.
        if (!string.IsNullOrWhiteSpace(remote.Id)
            && !remote.Id.Equals(payment.ProviderTransactionId, StringComparison.OrdinalIgnoreCase)
            && (string.IsNullOrWhiteSpace(remote.InvoiceId)
                || !remote.Id.Equals(remote.InvoiceId, StringComparison.OrdinalIgnoreCase)))
        {
            payment.ProviderTransactionId = remote.Id;
        }

        payment.Status = PaymentStatus.Succeeded;
        if (remote.FeeHalalas.HasValue)
            payment.ProviderFee = MinorUnitConverter.FromHalalas(remote.FeeHalalas.Value);
        payment.UpdatedAt = DateTime.UtcNow;
        await _paymentRepository.UpdateAsync(payment);

        var outcome = await ConfirmAsync(payment.Id, cancellationToken);
        await RecordConfirmEventAsync(
            payment,
            outcome.Succeeded
                ? PaymentTransactionEventType.ConfirmSucceeded
                : PaymentTransactionEventType.ConfirmFailed,
            outcome.Succeeded
                ? PaymentTransactionEventResult.Success
                : PaymentTransactionEventResult.Failed,
            statusBefore,
            payment.Status,
            providerTransactionId,
            outcome.ErrorCode,
            cancellationToken);

        if (outcome.Succeeded)
        {
            await RecordConfirmEventAsync(
                payment,
                PaymentTransactionEventType.EnrollmentActivated,
                PaymentTransactionEventResult.Success,
                statusBefore,
                payment.Status,
                providerTransactionId,
                null,
                cancellationToken);
        }
        else if (outcome.ErrorCode is "SCHEDULE_CONFLICT_RELEASED" or "SCHEDULE_CONFLICT_REFUNDED")
        {
            await RecordConfirmEventAsync(
                payment,
                PaymentTransactionEventType.ScheduleConflict,
                PaymentTransactionEventResult.Failed,
                statusBefore,
                payment.Status,
                providerTransactionId,
                outcome.ErrorCode,
                cancellationToken);
        }

        return outcome;
    }

    private async Task RecordConfirmEventAsync(
        Data.Entity.Payment.Payment payment,
        PaymentTransactionEventType eventType,
        PaymentTransactionEventResult result,
        PaymentStatus? statusBefore,
        PaymentStatus? statusAfter,
        string? providerRef,
        string? notes,
        CancellationToken cancellationToken)
    {
        var req = new PaymentTransactionEventRequest
        {
            PaymentId = payment.Id,
            PaymentProvider = payment.PaymentProvider,
            Source = PaymentTransactionEventSource.ClientConfirm,
            EventType = eventType,
            Result = result,
            StatusBefore = statusBefore,
            StatusAfter = statusAfter,
            Amount = payment.TotalAmount,
            Currency = payment.Currency,
            ProviderPaymentId = payment.ProviderTransactionId ?? providerRef,
            ProviderInvoiceId = payment.ProviderInvoiceId,
            Notes = notes,
            ErrorMessage = result is PaymentTransactionEventResult.Failed
                or PaymentTransactionEventResult.Mismatch
                or PaymentTransactionEventResult.NotFound
                ? notes
                : null
        };
        await _events.EnrichLinksFromPaymentAsync(req, payment, cancellationToken);
        await _events.RecordAsync(req, cancellationToken);
    }

    /// <summary>
    /// Bidirectional Moyasar hosted resolve:
    /// - client sends payment id, DB has invoice id → Fetch payment → InvoiceId → local row
    /// - client sends invoice id, DB already promoted to payment id → Fetch invoice → nested payment Id → local row
    /// </summary>
    private async Task<Data.Entity.Payment.Payment?> ResolveLocalPaymentEntityAsync(
        string providerRef,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(providerRef))
            return null;

        var direct = await _paymentRepository.GetByProviderTransactionIdAsync(
            providerRef, cancellationToken);
        if (direct != null)
            return direct;

        foreach (var gateway in _gatewayResolver.All)
        {
            try
            {
                var remote = await gateway.FetchAsync(providerRef, cancellationToken);
                if (remote == null)
                    continue;

                // Payment id → invoice id still stored on Pending row.
                if (!string.IsNullOrWhiteSpace(remote.InvoiceId))
                {
                    var byInvoice = await _paymentRepository.GetByProviderTransactionIdAsync(
                        remote.InvoiceId, cancellationToken);
                    if (byInvoice != null)
                        return byInvoice;
                }

                // Invoice fetch mapped to nested payment: DB may already hold payment id (post-promote).
                if (!string.IsNullOrWhiteSpace(remote.Id)
                    && !remote.Id.Equals(providerRef, StringComparison.OrdinalIgnoreCase))
                {
                    var byPayment = await _paymentRepository.GetByProviderTransactionIdAsync(
                        remote.Id, cancellationToken);
                    if (byPayment != null)
                        return byPayment;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Payment resolve via {Provider} failed", gateway.ProviderName);
            }
        }

        return null;
    }

    private static string? Truncate(string? s, int max)
        => string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s[..max]);

    public async Task<PaymentConfirmationOutcome> ConfirmAsync(
        int paymentId,
        CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByIdWithItemsAsync(paymentId, cancellationToken);
        if (payment == null)
            return PaymentConfirmationOutcome.Fail("PAYMENT_NOT_FOUND", "Payment not found.");

        if (payment.Status is PaymentStatus.Failed or PaymentStatus.Cancelled)
            return PaymentConfirmationOutcome.Fail("PAYMENT_NOT_PAYABLE", $"Payment is {payment.Status}.");

        if (payment.Status == PaymentStatus.Refunded)
            return PaymentConfirmationOutcome.Fail("PAYMENT_REFUNDED", "Payment was already refunded.");

        var enrollmentId = payment.PaymentItems
            .FirstOrDefault(i => i.ItemType == PaymentItemType.CourseEnrollment)
            ?.ReferenceId;
        if (enrollmentId is null or <= 0)
            return PaymentConfirmationOutcome.Fail("PAYMENT_NO_ENROLLMENT", "Payment is not linked to an enrollment.");

        var enrollment = await _enrollmentRepository.GetByIdForPaymentAsync(enrollmentId.Value, cancellationToken);
        if (enrollment == null)
            return PaymentConfirmationOutcome.Fail("ENROLLMENT_NOT_FOUND", "Enrollment not found.");

        // Idempotent: already activated.
        if (payment.Status == PaymentStatus.Succeeded
            && enrollment.EnrollmentStatus == EnrollmentStatus.Active
            && enrollment.CourseSchedules.Count > 0)
        {
            return PaymentConfirmationOutcome.Ok(new PaymentResultDto
            {
                PaymentId = payment.Id,
                Status = payment.Status,
                TotalAmount = payment.TotalAmount,
                Currency = payment.Currency,
                PaidAt = enrollment.ActivatedAt ?? payment.UpdatedAt ?? payment.CreatedAt,
                EnrollmentActivated = true,
                SchedulesCreated = enrollment.CourseSchedules.Count
            });
        }

        if (payment.Status != PaymentStatus.Succeeded
            && payment.Status != PaymentStatus.Pending)
        {
            return PaymentConfirmationOutcome.Fail(
                "PAYMENT_INVALID_STATUS",
                $"Cannot confirm payment in status {payment.Status}.");
        }

        // Caller (confirm command / webhook) must mark Succeeded before ConfirmAsync for card pays.
        // Mock path creates Succeeded then calls ConfirmAsync. Pending + free (0) is also allowed.
        if (payment.Status == PaymentStatus.Pending && payment.TotalAmount > 0)
            return PaymentConfirmationOutcome.Fail(
                "PAYMENT_NOT_PAID",
                "Payment has not been marked paid yet.");

        if (payment.Status == PaymentStatus.Pending && payment.TotalAmount == 0)
            payment.Status = PaymentStatus.Succeeded;

        var now = DateTime.UtcNow;
        var transaction = await _paymentRepository.BeginTransactionAsync();
        try
        {
            var pendingParticipants = enrollment.Participants
                .Where(p => p.PaymentStatus == PaymentStatus.Pending)
                .ToList();

            foreach (var p in pendingParticipants)
            {
                if (payment.EnrollmentPayments.All(ep => ep.EnrollmentParticipantId != p.Id))
                {
                    await _enrollmentPaymentRepository.AddAsync(new EnrollmentPayment
                    {
                        EnrollmentParticipantId = p.Id,
                        PaymentId = payment.Id,
                        Status = PaymentStatus.Succeeded
                    });
                }

                p.PaymentStatus = PaymentStatus.Succeeded;
                p.PaidAt = now;
            }

            foreach (var ep in payment.EnrollmentPayments)
                ep.Status = PaymentStatus.Succeeded;

            enrollment.PaidByUserId = payment.PayerUserId;
            enrollment.AmountDue = payment.TotalAmount;

            var isSessionRequest = enrollment.Source == EnrollmentSource.SessionRequest
                || enrollment.CourseId == null;

            if (enrollment.EnrollmentStatus != EnrollmentStatus.Active)
            {
                enrollment.EnrollmentStatus = EnrollmentStatus.Active;
                enrollment.ActivatedAt = now;
            }

            var today = DateOnly.FromDateTime(now);
            var enrollmentRequest = enrollment.EnrollmentRequest;
            DateOnly preferredStart;
            DateOnly preferredEnd;
            List<(DateOnly Date, int TeacherAvailabilityId)> selections;
            Dictionary<int, TeacherAvailability> availabilityById;

            if (enrollmentRequest != null
                && enrollmentRequest.SelectedSessionSlots != null
                && enrollmentRequest.SelectedSessionSlots.Count > 0)
            {
                preferredStart = enrollmentRequest.PreferredStartDate;
                preferredEnd = enrollmentRequest.PreferredEndDate;
                var ordered = enrollmentRequest.SelectedSessionSlots.OrderBy(s => s.SessionNumber).ToList();
                selections = ordered.Select(s => (s.SessionDate, s.TeacherAvailabilityId)).ToList();
                availabilityById = new Dictionary<int, TeacherAvailability>();
                foreach (var row in ordered)
                {
                    if (row.TeacherAvailability != null)
                        availabilityById[row.TeacherAvailabilityId] = row.TeacherAvailability;
                }
                foreach (var sa in enrollmentRequest.SelectedAvailabilities)
                {
                    if (sa.TeacherAvailability != null)
                        availabilityById[sa.TeacherAvailability.Id] = sa.TeacherAvailability;
                }
            }
            else
            {
                preferredStart = enrollment.PreferredStartDate ?? today;
                preferredEnd = enrollment.PreferredEndDate ?? preferredStart.AddYears(2);
                var ordered = enrollment.SelectedSessionSlots.OrderBy(s => s.SessionNumber).ToList();
                selections = ordered.Select(s => (s.SessionDate, s.TeacherAvailabilityId)).ToList();
                availabilityById = new Dictionary<int, TeacherAvailability>();
                foreach (var row in ordered)
                {
                    if (row.TeacherAvailability != null)
                        availabilityById[row.TeacherAvailabilityId] = row.TeacherAvailability;
                }
            }

            var effectiveStart = preferredStart < today ? today : preferredStart;
            var teacherId = isSessionRequest
                ? enrollment.ApprovedByTeacherId
                : enrollment.Course!.TeacherId;

            var blockedExceptions = await _teacherAvailabilityRepository.GetTeacherExceptionsAsync(
                teacherId,
                effectiveStart,
                preferredEnd);

            var existingScheduledSlots = await _scheduleRepository.GetScheduledSlotsAsync(
                effectiveStart,
                preferredEnd,
                availabilityById.Keys.ToList(),
                cancellationToken);

            ScheduleGenerationResult preview;
            int teachingModeId;
            Dictionary<int, int>? courseSessionIdByNumber = null;

            if (isSessionRequest)
            {
                if (selections.Count == 0)
                {
                    await _paymentRepository.RollBackAsync();
                    return PaymentConfirmationOutcome.Fail(
                        "NO_SLOTS",
                        "Enrollment has no selected session slots to schedule.");
                }

                teachingModeId = enrollment.OpenSessionRequest?.TeachingModeId
                    ?? throw new InvalidOperationException("Session request enrollment missing OpenSessionRequest.");

                var durationBySession = enrollment.OpenSessionRequest!.Sessions
                    .ToDictionary(s => s.SequenceNumber, s => s.DurationMinutes);

                var proposed = selections
                    .Select((sel, idx) =>
                    {
                        var sessionNumber = enrollment.SelectedSessionSlots
                            .OrderBy(s => s.SessionNumber)
                            .ElementAt(idx).SessionNumber;
                        var duration = durationBySession.TryGetValue(sessionNumber, out var d)
                            ? d
                            : (availabilityById.TryGetValue(sel.TeacherAvailabilityId, out var ta)
                               && ta.TimeSlot != null
                                ? ta.TimeSlot.ResolveDurationMinutes()
                                : 60);
                        return new CourseRequestProposedSession
                        {
                            SessionNumber = sessionNumber,
                            DurationMinutes = duration
                        };
                    })
                    .ToList();

                var stubCourse = new Course { IsFlexible = true, TeachingModeId = teachingModeId };
                var stubRequest = new CourseEnrollmentRequest { ProposedSessions = proposed };

                preview = _scheduleGenerator.PreviewExplicit(
                    stubCourse,
                    stubRequest,
                    selections,
                    availabilityById,
                    blockedExceptions,
                    existingScheduledSlots,
                    preferredEnd);
            }
            else
            {
                teachingModeId = enrollment.Course!.TeachingModeId;
                var stubOrRequest = enrollmentRequest ?? new CourseEnrollmentRequest { ProposedSessions = [] };

                if (selections.Count > 0)
                {
                    preview = _scheduleGenerator.PreviewExplicit(
                        enrollment.Course!,
                        stubOrRequest,
                        selections,
                        availabilityById,
                        blockedExceptions,
                        existingScheduledSlots,
                        preferredEnd);
                }
                else if (enrollmentRequest != null)
                {
                    var slots = enrollmentRequest.SelectedAvailabilities
                        .Select(sa => sa.TeacherAvailability)
                        .Where(ta => ta != null)
                        .ToList();

                    var existingForAvail = await _scheduleRepository.GetScheduledSlotsAsync(
                        effectiveStart,
                        preferredEnd,
                        slots.Select(s => s!.Id).ToList(),
                        cancellationToken);

                    preview = _scheduleGenerator.Preview(
                        enrollment.Course!,
                        enrollmentRequest,
                        slots!,
                        blockedExceptions,
                        existingForAvail,
                        effectiveStart,
                        preferredEnd);
                }
                else
                {
                    await _paymentRepository.RollBackAsync();
                    return PaymentConfirmationOutcome.Fail(
                        "NO_SLOTS",
                        "Enrollment has no selected session slots to schedule.");
                }

                if (!enrollment.Course!.IsFlexible && enrollment.Course.Sessions != null)
                {
                    courseSessionIdByNumber = enrollment.Course.Sessions
                        .GroupBy(cs => cs.SessionNumber)
                        .ToDictionary(g => g.Key, g => g.First().Id);
                }
            }

            if (preview.Conflicts.Count > 0)
            {
                await _paymentRepository.RollBackAsync();

                // Money already captured — refund and release OSR.
                if (payment.TotalAmount > 0 && payment.Status == PaymentStatus.Succeeded)
                {
                    try
                    {
                        await _refundService.IssueRefundAsync(
                            payment.Id,
                            enrollment.Id,
                            payment.TotalAmount,
                            payment.Currency,
                            "SCHEDULE_CONFLICT_AUTO_REFUND",
                            initiatedByUserId: null,
                            cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Auto-refund failed after schedule conflict for payment {PaymentId}",
                            payment.Id);
                    }
                }

                if (isSessionRequest && enrollment.Id > 0)
                {
                    await _releaseService.ReleaseAfterPaymentConflictAsync(
                        enrollment.Id, cancellationToken);
                }

                return PaymentConfirmationOutcome.Fail(
                    payment.TotalAmount > 0
                        ? "SCHEDULE_CONFLICT_REFUNDED"
                        : "SCHEDULE_CONFLICT_RELEASED",
                    payment.TotalAmount > 0
                        ? "Schedule conflict after payment — refund issued."
                        : "SCHEDULE_CONFLICT_RELEASED");
            }

            if (!preview.FitsInWindow)
            {
                await _paymentRepository.RollBackAsync();
                return PaymentConfirmationOutcome.Fail(
                    "SCHEDULE_WINDOW",
                    $"Schedule no longer fits before {preferredEnd:yyyy-MM-dd}. Please re-submit with a longer window.");
            }

            // Avoid duplicating schedules on retry.
            if (enrollment.CourseSchedules.Count == 0)
            {
                foreach (var s in preview.Slots)
                {
                    int? courseSessionId = null;
                    if (courseSessionIdByNumber != null
                        && courseSessionIdByNumber.TryGetValue(s.SessionNumber, out var sid))
                    {
                        courseSessionId = sid;
                    }

                    enrollment.CourseSchedules.Add(new CourseSchedule
                    {
                        Date = s.Date,
                        TeacherAvailabilityId = s.TeacherAvailabilityId,
                        DurationMinutes = s.DurationMinutes,
                        TeachingModeId = teachingModeId,
                        CourseSessionId = courseSessionId,
                        LocationId = null,
                        Status = ScheduleStatus.Scheduled
                    });
                }
            }

            var schedulesCreated = enrollment.CourseSchedules.Count > 0
                ? enrollment.CourseSchedules.Count
                : preview.Slots.Count;

            if (isSessionRequest && enrollment.OpenSessionRequest != null)
            {
                enrollment.OpenSessionRequest.Status = OpenSessionRequestStatus.Paid;
                enrollment.OpenSessionRequest.UpdatedAt = now;
            }

            payment.Status = PaymentStatus.Succeeded;
            payment.UpdatedAt = now;

            await _paymentRepository.SaveChangesAsync();
            await _paymentRepository.CommitAsync();

            return PaymentConfirmationOutcome.Ok(new PaymentResultDto
            {
                PaymentId = payment.Id,
                Status = payment.Status,
                TotalAmount = payment.TotalAmount,
                Currency = payment.Currency,
                PaidAt = now,
                EnrollmentActivated = true,
                SchedulesCreated = schedulesCreated
            });
        }
        catch
        {
            await _paymentRepository.RollBackAsync();
            throw;
        }
    }
}
