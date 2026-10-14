using System.Security.Cryptography;
using System.Text;

namespace HaveTickets.Infrastructure.Persistence;

/// <summary>
/// Derives a stable identifier from a natural key.
/// <para>
/// Seeded rows keep the same identifiers across machines, containers and reruns,
/// so a bookmarked demo URL keeps working and the seeder can be re-entered
/// without creating duplicates.
/// </para>
/// </summary>
internal static class DeterministicId
{
    public static Guid From(string key)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return new Guid(hash.AsSpan(0, 16));
    }
}
