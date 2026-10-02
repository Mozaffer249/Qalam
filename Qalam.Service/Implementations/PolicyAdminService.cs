using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Qalam.Data.DTOs.Policy;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Helpers;

namespace Qalam.Service.Implementations;

public class PolicyAdminService : IPolicyAdminService
{
    public const string InvalidRules = "POLICY_INVALID_RULES";
    public const string NoDraft = "POLICY_NO_DRAFT";
    public const string EffectiveInPast = "POLICY_EFFECTIVE_DATE_IN_PAST";

    /// <summary>Tolerance for clock skew between the admin browser and the server.</summary>
    private static readonly TimeSpan PastTolerance = TimeSpan.FromMinutes(5);

    private readonly IPolicyVersionRepository _versions;
    private readonly IAuditService _audit;
    private readonly IHttpContextAccessor _http;

    public PolicyAdminService(IPolicyVersionRepository versions, IAuditService audit, IHttpContextAccessor http)
    {
        _versions = versions;
        _audit = audit;
        _http = http;
    }

    public async Task<PolicyVersionDto> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var current = await _versions.GetEffectiveAsync(DateTime.UtcNow, cancellationToken);
        return current == null
            ? new PolicyVersionDto { Status = PolicyVersionStatus.Published.ToString(), IsCurrent = true, Rules = CancellationPolicyDefaults.Create() }
            : ToDto(current, current.Id);
    }

    public async Task<List<PolicyVersionDto>> ListVersionsAsync(CancellationToken cancellationToken = default)
    {
        var currentId = (await _versions.GetEffectiveAsync(DateTime.UtcNow, cancellationToken))?.Id;
        return (await _versions.ListAsync(cancellationToken)).Select(v => ToDto(v, currentId)).ToList();
    }

    public async Task<PolicyVersionDto?> GetVersionAsync(int id, CancellationToken cancellationToken = default)
    {
        var v = await _versions.GetByIdAsync(id, cancellationToken: cancellationToken);
        if (v == null) return null;
        var currentId = (await _versions.GetEffectiveAsync(DateTime.UtcNow, cancellationToken))?.Id;
        return ToDto(v, currentId);
    }

    public async Task<PolicyVersionDto?> GetDraftAsync(CancellationToken cancellationToken = default)
    {
        var draft = await _versions.GetDraftAsync(cancellationToken: cancellationToken);
        return draft == null ? null : ToDto(draft, null);
    }

    public async Task<PolicyAdminResult<PolicyVersionDto>> SaveDraftAsync(
        SavePolicyDraftDto dto,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        var rules = dto.Rules ?? CancellationPolicyDefaults.Create();
        var errors = ValidateRules(rules);
        if (errors.Count > 0)
            return PolicyAdminResult<PolicyVersionDto>.Fail(InvalidRules, errors.ToArray());

        var draft = await _versions.GetDraftAsync(track: true, cancellationToken);
        if (draft == null)
        {
            draft = new PolicyVersion
            {
                VersionNumber = await _versions.GetMaxVersionNumberAsync(cancellationToken) + 1,
                Status = PolicyVersionStatus.Draft,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };
            await _versions.AddAsync(draft, cancellationToken);
        }

        draft.RulesJson = CancellationPolicyDefaults.ToJson(rules);
        draft.ChangeNote = string.IsNullOrWhiteSpace(dto.ChangeNote) ? draft.ChangeNote : dto.ChangeNote.Trim();
        draft.UpdatedAt = DateTime.UtcNow;
        draft.UpdatedBy = userId;
        await _versions.SaveChangesAsync(cancellationToken);
        return PolicyAdminResult<PolicyVersionDto>.Ok(ToDto(draft, null));
    }

    public async Task<PolicyAdminResult<PolicyVersionDto>> PublishDraftAsync(
        PublishPolicyDraftDto dto,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        var draft = await _versions.GetDraftAsync(track: true, cancellationToken);
        if (draft == null)
            return PolicyAdminResult<PolicyVersionDto>.Fail(NoDraft, "There is no draft to publish.");

        var now = DateTime.UtcNow;
        var effectiveFrom = dto.EffectiveFrom?.ToUniversalTime() ?? now;
        if (effectiveFrom < now - PastTolerance)
            return PolicyAdminResult<PolicyVersionDto>.Fail(EffectiveInPast, "The effective date cannot be in the past.");
        if (effectiveFrom < now) effectiveFrom = now;

        var rules = CancellationPolicyDefaults.FromJson(draft.RulesJson);
        var errors = ValidateRules(rules);
        if (errors.Count > 0)
            return PolicyAdminResult<PolicyVersionDto>.Fail(InvalidRules, errors.ToArray());

        var before = await _versions.GetEffectiveAsync(now, cancellationToken);

        // Close open versions at the new start; a not-yet-effective scheduled version is superseded entirely.
        foreach (var open in await _versions.ListOpenPublishedAsync(cancellationToken))
        {
            if (open.EffectiveFrom >= effectiveFrom)
            {
                open.Status = PolicyVersionStatus.Retired;
                open.EffectiveTo = open.EffectiveFrom;
            }
            else
            {
                open.EffectiveTo = effectiveFrom;
            }
            open.UpdatedAt = now;
            open.UpdatedBy = userId;
        }

        draft.Status = PolicyVersionStatus.Published;
        draft.EffectiveFrom = effectiveFrom;
        draft.EffectiveTo = null;
        draft.PublishedAt = now;
        draft.PublishedByUserId = userId;
        draft.UpdatedAt = now;
        draft.UpdatedBy = userId;
        await _versions.SaveChangesAsync(cancellationToken);

        await PricingAuditHelper.LogSettingChangeAsync(
            _audit,
            _http,
            "CancellationPolicy.Published",
            "PolicyVersion",
            draft.Id.ToString(),
            before == null ? null : new { before.VersionNumber, Rules = CancellationPolicyDefaults.FromJson(before.RulesJson) },
            new { draft.VersionNumber, draft.EffectiveFrom, Rules = rules });

        var currentId = (await _versions.GetEffectiveAsync(DateTime.UtcNow, cancellationToken))?.Id;
        return PolicyAdminResult<PolicyVersionDto>.Ok(ToDto(draft, currentId));
    }

    public async Task<bool> DiscardDraftAsync(CancellationToken cancellationToken = default)
    {
        var draft = await _versions.GetDraftAsync(track: true, cancellationToken);
        if (draft == null) return false;
        _versions.Remove(draft);
        await _versions.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PolicyCompareDto?> CompareAsync(int fromId, int toId, CancellationToken cancellationToken = default)
    {
        var from = await _versions.GetByIdAsync(fromId, cancellationToken: cancellationToken);
        var to = await _versions.GetByIdAsync(toId, cancellationToken: cancellationToken);
        if (from == null || to == null) return null;

        var a = Flatten(CancellationPolicyDefaults.FromJson(from.RulesJson));
        var b = Flatten(CancellationPolicyDefaults.FromJson(to.RulesJson));
        var changes = a.Keys.Union(b.Keys)
            .OrderBy(k => k, StringComparer.Ordinal)
            .Where(k => !Equals(a.GetValueOrDefault(k), b.GetValueOrDefault(k)))
            .Select(k => new PolicyDiffEntryDto { Path = k, From = a.GetValueOrDefault(k), To = b.GetValueOrDefault(k) })
            .ToList();
        return new PolicyCompareDto { FromVersionId = fromId, ToVersionId = toId, Changes = changes };
    }

    public List<string> ValidateRules(CancellationPolicyRules rules)
    {
        var errors = new List<string>();
        void Pct(string name, decimal v) { if (v < 0 || v > 100) errors.Add($"{name} must be between 0 and 100."); }
        void NonNeg(string name, decimal v) { if (v < 0) errors.Add($"{name} cannot be negative."); }

        Pct("beforeFirstSession.refundPct", rules.BeforeFirstSession.RefundPct);
        NonNeg("beforeFirstSession.fixedFee", rules.BeforeFirstSession.FixedFee);
        NonNeg("beforeFirstSession.minHoursBeforeFirstSession", rules.BeforeFirstSession.MinHoursBeforeFirstSession);
        Pct("afterFirstSession.refundPct", rules.AfterFirstSession.RefundPct);
        NonNeg("afterFirstSession.fixedFee", rules.AfterFirstSession.FixedFee);
        NonNeg("sessionCancellation.noticeHours", rules.SessionCancellation.NoticeHours);
        Pct("sessionCancellation.lateRefundPct", rules.SessionCancellation.LateRefundPct);
        NonNeg("sessionCancellation.fixedFee", rules.SessionCancellation.FixedFee);
        Pct("teacherNoShow.refundPct", rules.TeacherNoShow.RefundPct);
        NonNeg("teacherNoShow.teacherPenaltyAmount", rules.TeacherNoShow.TeacherPenaltyAmount);
        Pct("studentNoShow.refundPct", rules.StudentNoShow.RefundPct);
        Pct("technicalIssue.refundPct", rules.TechnicalIssue.RefundPct);
        NonNeg("technicalIssue.reportWindowHours", rules.TechnicalIssue.ReportWindowHours);
        NonNeg("adminExceptions.minReasonLength", rules.AdminExceptions.MinReasonLength);
        NonNeg("adminExceptions.maxAmountForAdmin", rules.AdminExceptions.MaxAmountForAdmin);

        if (rules.SessionCancellation.NoticeHours > 24 * 30)
            errors.Add("sessionCancellation.noticeHours cannot exceed 720.");
        if (rules.TeacherNoShow.Enabled && rules.TeacherNoShow.Outcome is TeacherNoShowOutcome.Refund or TeacherNoShowOutcome.RefundAndReplacement
            && rules.TeacherNoShow.RefundPct <= 0)
            errors.Add("teacherNoShow.refundPct must be greater than 0 when the outcome refunds the student.");
        return errors;
    }

    private static Dictionary<string, string?> Flatten(CancellationPolicyRules rules)
    {
        var node = JsonNode.Parse(CancellationPolicyDefaults.ToJson(rules))!;
        var result = new Dictionary<string, string?>(StringComparer.Ordinal);
        void Walk(JsonNode? n, string path)
        {
            switch (n)
            {
                case JsonObject obj:
                    foreach (var (key, child) in obj)
                        Walk(child, path.Length == 0 ? key : $"{path}.{key}");
                    break;
                case JsonArray arr:
                    result[path] = string.Join(",", arr.Select(x => x?.ToString()).OrderBy(x => x));
                    break;
                default:
                    result[path] = n?.ToJsonString().Trim('"');
                    break;
            }
        }
        Walk(node, "");
        return result;
    }

    private static PolicyVersionDto ToDto(PolicyVersion v, int? currentId) => new()
    {
        Id = v.Id,
        VersionNumber = v.VersionNumber,
        Status = v.Status.ToString(),
        EffectiveFrom = v.EffectiveFrom,
        EffectiveTo = v.EffectiveTo,
        IsCurrent = currentId == v.Id,
        ChangeNote = v.ChangeNote,
        PublishedByUserId = v.PublishedByUserId,
        PublishedAt = v.PublishedAt,
        CreatedAt = v.CreatedAt,
        Rules = CancellationPolicyDefaults.FromJson(v.RulesJson)
    };
}
