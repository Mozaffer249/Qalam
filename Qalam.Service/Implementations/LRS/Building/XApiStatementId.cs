using System.Security.Cryptography;
using System.Text;

namespace Qalam.Service.Implementations.LRS.Building;

/// <summary>
/// Produces a deterministic (UUIDv5-style, SHA1) statement id from the logical event's parts.
/// The same learner + verb + object always yields the same id, so re-dispatching a journey is an
/// idempotent LRS upsert instead of a duplicate (validator rule SEQ003).
/// </summary>
public static class XApiStatementId
{
    public static string Deterministic(params string[] parts)
    {
        var input = string.Join("|", parts);
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(input));

        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x50); // version 5
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80); // RFC 4122 variant

        return new Guid(bytes).ToString();
    }
}
