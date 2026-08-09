namespace Qalam.Service.Abstracts;

public interface ILegalContentSanitizer
{
    string? Sanitize(string? html);
}
