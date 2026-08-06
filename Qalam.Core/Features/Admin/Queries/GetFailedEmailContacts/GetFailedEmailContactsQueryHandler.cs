using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.context;

namespace Qalam.Core.Features.Admin.Queries.GetFailedEmailContacts;

public class GetFailedEmailContactsQueryHandler
    : ResponseHandler, IRequestHandler<GetFailedEmailContactsQuery, Response<List<FailedEmailContactDto>>>
{
    private readonly ApplicationDBContext _db;

    public GetFailedEmailContactsQueryHandler(
        ApplicationDBContext db,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _db = db;
    }

    public async Task<Response<List<FailedEmailContactDto>>> Handle(
        GetFailedEmailContactsQuery request,
        CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize switch
        {
            < 1 => 20,
            > 50 => 50,
            _ => request.PageSize
        };

        var search = string.IsNullOrWhiteSpace(request.Search)
            ? null
            : request.Search.Trim().ToLowerInvariant();

        // OTP / registration verification emails (EN + AR subjects).
        var query =
            from log in _db.MessageLogs.AsNoTracking()
            where log.Type == MessageType.Email
                  && log.Status == MessageStatus.Failed
                  && log.Subject != null
                  && (log.Subject.Contains("verification code")
                      || log.Subject.Contains("رمز تحقق"))
            select log;

        if (search != null)
        {
            query = query.Where(l =>
                l.Recipient.ToLower().Contains(search)
                || (l.ErrorMessage != null && l.ErrorMessage.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var logs = await query
            .OrderByDescending(l => l.QueuedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new
            {
                l.Id,
                l.Recipient,
                l.Subject,
                l.ErrorMessage,
                l.QueuedAt,
                l.ProcessedAt,
                l.RetryCount
            })
            .ToListAsync(cancellationToken);

        var emails = logs
            .Select(l => l.Recipient.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        var emailsUpper = emails.Select(e => e.ToUpperInvariant()).ToList();

        var users = await _db.Users
            .AsNoTracking()
            .Where(u => u.NormalizedEmail != null && emailsUpper.Contains(u.NormalizedEmail))
            .Select(u => new
            {
                u.Id,
                Email = u.Email!,
                NormalizedEmail = u.NormalizedEmail!,
                u.FirstName,
                u.LastName,
                u.PhoneNumber
            })
            .ToListAsync(cancellationToken);

        var userByEmail = users
            .GroupBy(u => u.NormalizedEmail.ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        var suppressed = await _db.EmailSuppressions
            .AsNoTracking()
            .Where(s => emails.Contains(s.Email))
            .Select(s => s.Email)
            .ToListAsync(cancellationToken);
        var suppressedSet = suppressed.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var items = logs.Select(l =>
        {
            var emailKey = l.Recipient.Trim().ToLowerInvariant();
            userByEmail.TryGetValue(emailKey, out var user);
            var name = user == null
                ? null
                : $"{user.FirstName} {user.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(name))
                name = null;

            return new FailedEmailContactDto
            {
                MessageLogId = l.Id,
                Email = l.Recipient,
                UserId = user?.Id,
                UserName = name,
                PhoneNumber = user?.PhoneNumber,
                Subject = l.Subject ?? string.Empty,
                ErrorMessage = l.ErrorMessage,
                QueuedAt = l.QueuedAt,
                ProcessedAt = l.ProcessedAt,
                RetryCount = l.RetryCount,
                IsSuppressed = suppressedSet.Contains(emailKey)
            };
        }).ToList();

        return Success(entity: items, Meta: BuildPaginationMeta(pageNumber, pageSize, totalCount));
    }
}
