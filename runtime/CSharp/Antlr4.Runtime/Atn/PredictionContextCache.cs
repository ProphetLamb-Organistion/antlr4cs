// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System.Collections.Generic;

namespace Antlr4.Runtime.Atn
{
    /// <summary>
    ///     Used to cache
    ///     <see cref="PredictionContext" />
    ///     objects. Its used for the shared
    ///     context cash associated with contexts in DFA states. This cache
    ///     can be used for both lexers and parsers.
    /// </summary>
    /// <author>Sam Harwell</author>
    public class PredictionContextCache
    {
        public static readonly PredictionContextCache Uncached = new(false);

        private readonly IDictionary<PredictionContextAndInt, PredictionContext> childContexts = new Dictionary<PredictionContextAndInt, PredictionContext>();

        private readonly IDictionary<PredictionContext, PredictionContext> contexts = new Dictionary<PredictionContext, PredictionContext>();

        private readonly bool enableCache;

        private readonly IDictionary<IdentityCommutativePredictionContextOperands, PredictionContext> joinContexts =
            new Dictionary<IdentityCommutativePredictionContextOperands, PredictionContext>();

        public PredictionContextCache()
            : this(true)
        {
        }

        private PredictionContextCache(bool enableCache)
        {
            this.enableCache = enableCache;
        }

        public virtual PredictionContext GetAsCached(PredictionContext context)
        {
            if (!enableCache)
            {
                return context;
            }

            PredictionContext result;
            if (!contexts.TryGetValue(context, out result))
            {
                result = context;
                contexts[context] = context;
            }

            return result;
        }

        public virtual PredictionContext GetChild(PredictionContext context, int invokingState)
        {
            if (!enableCache)
            {
                return context.GetChild(invokingState);
            }

            PredictionContextAndInt operands = new(context, invokingState);
            PredictionContext result;
            if (!childContexts.TryGetValue(operands, out result))
            {
                result = context.GetChild(invokingState);
                result = GetAsCached(result);
                childContexts[operands] = result;
            }

            return result;
        }

        public virtual PredictionContext Join(PredictionContext x, PredictionContext y)
        {
            if (!enableCache)
            {
                return PredictionContext.Join(x, y, this);
            }

            IdentityCommutativePredictionContextOperands operands = new(x, y);
            PredictionContext result;
            if (joinContexts.TryGetValue(operands, out result))
            {
                return result;
            }

            result = PredictionContext.Join(x, y, this);
            result = GetAsCached(result);
            joinContexts[operands] = result;
            return result;
        }

        protected internal sealed class PredictionContextAndInt
        {
            private readonly PredictionContext obj;

            private readonly int value;

            public PredictionContextAndInt(PredictionContext obj, int value)
            {
                this.obj = obj;
                this.value = value;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is PredictionContextAndInt))
                {
                    return false;
                }

                if (obj == this)
                {
                    return true;
                }

                PredictionContextAndInt other = (PredictionContextAndInt) obj;
                return value == other.value && (this.obj == other.obj || this.obj != null && this.obj.Equals(other.obj));
            }

            public override int GetHashCode()
            {
                int hashCode = 5;
                hashCode = 7 * hashCode + (obj != null ? obj.GetHashCode() : 0);
                hashCode = 7 * hashCode + value;
                return hashCode;
            }
        }

        protected internal sealed class IdentityCommutativePredictionContextOperands
        {
            public IdentityCommutativePredictionContextOperands(PredictionContext x, PredictionContext y)
            {
                this.X = x;
                this.Y = y;
            }

            public PredictionContext X { get; }

            public PredictionContext Y { get; }

            public override bool Equals(object obj)
            {
                if (!(obj is IdentityCommutativePredictionContextOperands))
                {
                    return false;
                }

                if (this == obj)
                {
                    return true;
                }

                IdentityCommutativePredictionContextOperands other = (IdentityCommutativePredictionContextOperands) obj;
                return X == other.X && Y == other.Y || X == other.Y && Y == other.X;
            }

            public override int GetHashCode()
            {
                return X.GetHashCode() ^ Y.GetHashCode();
            }
        }
    }
}