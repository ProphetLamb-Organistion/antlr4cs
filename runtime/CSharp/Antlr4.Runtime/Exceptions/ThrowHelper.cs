using System;
using System.Diagnostics;
#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif

namespace Antlr4.Runtime.Exceptions
{
    internal static class ThrowHelper
    {
        [DebuggerStepThrough]
        private static string? GetArgumentName(ExceptionArgument argument)
        {
            return argument switch
            {
                _ => null,
            };
        }

        [DebuggerStepThrough]
        private static string? GetResourceString(ExceptionResource resource)
        {
            return resource switch
            {
                _ => null,
            };
        }

        [DoesNotReturn]
        [DebuggerStepThrough]
        public static void ThrowArgumentException(ExceptionResource message = default, ExceptionArgument argument = default, Exception? inner = null)
        {
            if (inner is not null)
            {
                throw new ArgumentException(GetResourceString(message), GetArgumentName(argument), inner);
            }

            if (argument != default)
            {
                throw new ArgumentException(GetResourceString(message), GetArgumentName(argument));
            }
            
            if (message != default)
            {
                throw new ArgumentException(GetResourceString(message));
            }

            throw new ArgumentException();
        }

        [DoesNotReturn]
        [DebuggerStepThrough]
        public static void ThrowArgumentOutOfRangeException(ExceptionArgument argument = default, object? actualValue = null, ExceptionResource message = default)
        {
            if (actualValue is not null)
            {
                throw new ArgumentOutOfRangeException(GetArgumentName(argument), actualValue, GetResourceString(message));
            }

            if (message != default)
            {
                throw new ArgumentOutOfRangeException(GetArgumentName(argument), GetResourceString(message));
            }
            
            if (argument != default)
            {
                throw new ArgumentOutOfRangeException(GetArgumentName(argument));
            }

            throw new ArgumentOutOfRangeException();
        }

        [DoesNotReturn]
        [DebuggerStepThrough]
        public static void ThrowArgumentNullException(ExceptionArgument argument = default, ExceptionResource message = default)
        {
            if (message != default)
            {
                throw new ArgumentNullException(GetArgumentName(argument), GetResourceString(message));
            }
            
            if (argument != default)
            {
                throw new ArgumentNullException(GetArgumentName(argument));
            }

            throw new ArgumentNullException();
        }

        [DoesNotReturn]
        [DebuggerStepThrough]
        public static void ThrowInvalidOperationException(ExceptionResource message = default, Exception? inner = default)
        {
            if (inner is not null)
            {
                throw new InvalidOperationException(GetResourceString(message), inner);
            }

            if (message != default)
            {
                throw new InvalidOperationException(GetResourceString(message));
            }
            
            throw new InvalidOperationException();
        }
        
        
        public static void ThrowArgumentException_TupleIncorrectType(object tuple)
        {
            throw new ArgumentException("TupleIncorrectType");
        }
    }

    internal enum ExceptionArgument
    {
        None = 0, listener, ctx
    }

    internal enum ExceptionResource
    {
        None = 0,
    }
}
