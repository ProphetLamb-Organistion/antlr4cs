using System.Collections.Generic;
#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif
using System.Runtime.CompilerServices;

using Antlr4.Runtime.Exceptions;

namespace Antlr4.Runtime.Utility
{
    internal static class Requires
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NotNull<T>([ValidatedNotNull, NotNullWhen(true)] T? argument, ExceptionArgument name = default, ExceptionResource message = default)
            where T : class
        {
            if (argument is null)
            {
                ThrowHelper.ThrowArgumentNullException(name, message);
                return false;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NotDefault<T>([ValidatedNotNull] in T argument, ExceptionArgument name = default, ExceptionResource message = default)
            where T : struct
        {
            if (EqualityComparer<T>.Default.Equals(default, argument))
            {
                ThrowHelper.ThrowArgumentNullException(name, message);
                return false;
            }
            return true;
        }
    }
}
