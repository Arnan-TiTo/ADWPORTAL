using System.Linq;

namespace miniApp.AdminPortal.Shared
{
    public static class PagingExtensions
    {
        public static List<T> Page<T>(this IEnumerable<T> source, PaginationState state)
        {
            var (skip, take) = state.Range();
            return source.Skip(skip).Take(take).ToList();
        }
    }
}

