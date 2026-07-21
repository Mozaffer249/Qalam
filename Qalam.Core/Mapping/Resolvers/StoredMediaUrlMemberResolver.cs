using AutoMapper;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Mapping.Resolvers;

/// <summary>
/// Absolute-izes a stored media path member (relative upload or already-absolute URL).
/// </summary>
public class StoredMediaUrlMemberResolver : IMemberValueResolver<object, object, string?, string?>
{
    private readonly IMediaUrlResolver _mediaUrlResolver;

    public StoredMediaUrlMemberResolver(IMediaUrlResolver mediaUrlResolver)
    {
        _mediaUrlResolver = mediaUrlResolver;
    }

    public string? Resolve(
        object source,
        object destination,
        string? sourceMember,
        string? destMember,
        ResolutionContext context)
        => _mediaUrlResolver.ToPublicUrl(sourceMember);
}
