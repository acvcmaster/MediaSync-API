using System;

namespace MediaSync.Shared
{
    public static class SharedExtensions
    {
        public static bool EndsWithAny(this string @string, string[] terminators)
        {
            if (terminators == null || terminators.Length == 0)
                return true;

            foreach (string terminator in terminators)
                if (@string.EndsWith(terminator))
                    return true;

            return false;
        }
    }
}