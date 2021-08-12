// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif
using System.Text;

using Antlr4.Runtime.Utility;

namespace Antlr4.Runtime.Atn
{
    public abstract class PredictionContext
    {
        public const int EmptyLocalStateKey = Int32.MinValue;

        public const int EmptyFullStateKey = Int32.MaxValue;

        [NotNull] public static readonly PredictionContext EmptyLocal = EmptyPredictionContext.LocalContext;

        [NotNull] public static readonly PredictionContext EmptyFull = EmptyPredictionContext.FullContext;

        /// <summary>
        ///     Stores the computed hash code of this
        ///     <see cref="PredictionContext" />
        ///     . The hash
        ///     code is computed in parts to match the following reference algorithm.
        ///     <pre>
        ///         private int referenceHashCode() {
        ///         int hash =
        ///         <see cref="Antlr4.Runtime.MurmurHash.Initialize()">MurmurHash.initialize</see>
        ///         (
        ///         <see cref="InitialHash" />
        ///         );
        ///         for (int i = 0; i &lt;
        ///         <see cref="Size()" />
        ///         ; i++) {
        ///         hash =
        ///         <see cref="Antlr4.Runtime.MurmurHash.Update(int, int)">MurmurHash.update</see>
        ///         (hash,
        ///         <see cref="GetParent(int)">getParent</see>
        ///         (i));
        ///         }
        ///         for (int i = 0; i &lt;
        ///         <see cref="Size()" />
        ///         ; i++) {
        ///         hash =
        ///         <see cref="Antlr4.Runtime.MurmurHash.Update(int, int)">MurmurHash.update</see>
        ///         (hash,
        ///         <see cref="GetReturnState(int)">getReturnState</see>
        ///         (i));
        ///         }
        ///         hash =
        ///         <see cref="Antlr4.Runtime.MurmurHash.Finish(int, int)">MurmurHash.finish</see>
        ///         (hash, 2 *
        ///         <see cref="Size()" />
        ///         );
        ///         return hash;
        ///         }
        ///     </pre>
        /// </summary>
        private readonly int cachedHashCode;

        protected internal PredictionContext(int cachedHashCode)
        {
            this.cachedHashCode = cachedHashCode;
        }

        public abstract int Size { get; }

        public abstract bool IsEmpty { get; }

        public abstract bool HasEmpty { get; }

        protected internal static int CalculateHashCode(PredictionContext parent, int returnState)
        {
            return HashCode.Combine(19, parent, returnState);
        }

        protected internal static int CalculateHashCode(PredictionContext[] parents, int[] returnStates)
        {
            int hash = 19;
            foreach (PredictionContext parent in parents)
            {
                hash = HashCode.Combine(hash, parent);
            }

            foreach (int returnState in returnStates)
            {
                hash = HashCode.Combine(hash, returnState);
            }
            return hash;
        }

        public abstract int GetReturnState(int index);

        public abstract int FindReturnState(int returnState);

        [return: NotNull]
        public abstract PredictionContext GetParent(int index);

        protected internal abstract PredictionContext AddEmptyContext();

        protected internal abstract PredictionContext RemoveEmptyContext();

        public static PredictionContext FromRuleContext([NotNull] ATN atn, [NotNull] RuleContext outerContext)
        {
            return FromRuleContext(atn, outerContext, true);
        }

        public static PredictionContext FromRuleContext([NotNull] ATN atn, [NotNull] RuleContext outerContext, bool fullContext)
        {
            if (outerContext.IsEmpty)
            {
                return fullContext ? EmptyFull : EmptyLocal;
            }

            PredictionContext parent;
            if (outerContext.parent != null)
            {
                parent = FromRuleContext(atn, outerContext.parent, fullContext);
            }
            else
            {
                parent = fullContext ? EmptyFull : EmptyLocal;
            }

            ATNState state = atn.states[outerContext.invokingState];
            RuleTransition transition = (RuleTransition) state.Transition(0);
            return parent.GetChild(transition.followState.stateNumber);
        }

        private static PredictionContext AddEmptyContext(PredictionContext context)
        {
            return context.AddEmptyContext();
        }

        private static PredictionContext RemoveEmptyContext(PredictionContext context)
        {
            return context.RemoveEmptyContext();
        }

        public static PredictionContext Join(PredictionContext context0, PredictionContext context1)
        {
            return Join(context0, context1, PredictionContextCache.Uncached);
        }

        /*package*/
        internal static PredictionContext Join([NotNull] PredictionContext context0, [NotNull] PredictionContext context1, [NotNull] PredictionContextCache contextCache)
        {
            if (context0 == context1)
            {
                return context0;
            }

            if (context0.IsEmpty)
            {
                return IsEmptyLocal(context0) ? context0 : AddEmptyContext(context1);
            }

            if (context1.IsEmpty)
            {
                return IsEmptyLocal(context1) ? context1 : AddEmptyContext(context0);
            }

            int context0size = context0.Size;
            int context1size = context1.Size;
            if (context0size == 1 && context1size == 1 && context0.GetReturnState(0) == context1.GetReturnState(0))
            {
                PredictionContext merged = contextCache.Join(context0.GetParent(0), context1.GetParent(0));
                if (merged == context0.GetParent(0))
                {
                    return context0;
                }

                if (merged == context1.GetParent(0))
                {
                    return context1;
                }

                return merged.GetChild(context0.GetReturnState(0));
            }

            int count = 0;
            var parentsList = new PredictionContext[context0size + context1size];
            int[] returnStatesList = new int[parentsList.Length];
            int leftIndex = 0;
            int rightIndex = 0;
            bool canReturnLeft = true;
            bool canReturnRight = true;
            while (leftIndex < context0size && rightIndex < context1size)
            {
                if (context0.GetReturnState(leftIndex) == context1.GetReturnState(rightIndex))
                {
                    parentsList[count] = contextCache.Join(context0.GetParent(leftIndex), context1.GetParent(rightIndex));
                    returnStatesList[count] = context0.GetReturnState(leftIndex);
                    canReturnLeft = canReturnLeft && parentsList[count] == context0.GetParent(leftIndex);
                    canReturnRight = canReturnRight && parentsList[count] == context1.GetParent(rightIndex);
                    leftIndex++;
                    rightIndex++;
                }
                else
                {
                    if (context0.GetReturnState(leftIndex) < context1.GetReturnState(rightIndex))
                    {
                        parentsList[count] = context0.GetParent(leftIndex);
                        returnStatesList[count] = context0.GetReturnState(leftIndex);
                        canReturnRight = false;
                        leftIndex++;
                    }
                    else
                    {
                        Debug.Assert(context1.GetReturnState(rightIndex) < context0.GetReturnState(leftIndex));
                        parentsList[count] = context1.GetParent(rightIndex);
                        returnStatesList[count] = context1.GetReturnState(rightIndex);
                        canReturnLeft = false;
                        rightIndex++;
                    }
                }

                count++;
            }

            while (leftIndex < context0size)
            {
                parentsList[count] = context0.GetParent(leftIndex);
                returnStatesList[count] = context0.GetReturnState(leftIndex);
                leftIndex++;
                canReturnRight = false;
                count++;
            }

            while (rightIndex < context1size)
            {
                parentsList[count] = context1.GetParent(rightIndex);
                returnStatesList[count] = context1.GetReturnState(rightIndex);
                rightIndex++;
                canReturnLeft = false;
                count++;
            }

            if (canReturnLeft)
            {
                return context0;
            }

            if (canReturnRight)
            {
                return context1;
            }

            if (count < parentsList.Length)
            {
                parentsList = Arrays.CopyOf(parentsList, count);
                returnStatesList = Arrays.CopyOf(returnStatesList, count);
            }

            if (parentsList.Length == 0)
            {
                // if one of them was EMPTY_LOCAL, it would be empty and handled at the beginning of the method
                return EmptyFull;
            }

            if (parentsList.Length == 1)
            {
                return new SingletonPredictionContext(parentsList[0], returnStatesList[0]);
            }

            return new ArrayPredictionContext(parentsList, returnStatesList);
        }

        public static bool IsEmptyLocal(PredictionContext context)
        {
            return context == EmptyLocal;
        }

        public static PredictionContext GetCachedContext([NotNull] PredictionContext context, [NotNull] ConcurrentDictionary<PredictionContext, PredictionContext> contextCache,
            [NotNull] IdentityHashMap visited)
        {
            if (context.IsEmpty)
            {
                return context;
            }

            PredictionContext existing;
            if (visited.TryGetValue(context, out existing))
            {
                return existing;
            }

            if (contextCache.TryGetValue(context, out existing))
            {
                visited[context] = existing;
                return existing;
            }

            bool changed = false;
            var parents = new PredictionContext[context.Size];
            for (int i = 0;
                i < parents.Length;
                i++)
            {
                PredictionContext parent = GetCachedContext(context.GetParent(i), contextCache, visited);
                if (changed || parent != context.GetParent(i))
                {
                    if (!changed)
                    {
                        parents = new PredictionContext[context.Size];
                        for (int j = 0;
                            j < context.Size;
                            j++)
                        {
                            parents[j] = context.GetParent(j);
                        }

                        changed = true;
                    }

                    parents[i] = parent;
                }
            }

            if (!changed)
            {
                existing = contextCache.GetOrAdd(context, context);
                visited[context] = existing;
                return context;
            }

            // We know parents.length>0 because context.isEmpty() is checked at the beginning of the method.
            PredictionContext updated;
            if (parents.Length == 1)
            {
                updated = new SingletonPredictionContext(parents[0], context.GetReturnState(0));
            }
            else
            {
                ArrayPredictionContext arrayPredictionContext = (ArrayPredictionContext) context;
                updated = new ArrayPredictionContext(parents, arrayPredictionContext.returnStates, context.cachedHashCode);
            }

            existing = contextCache.GetOrAdd(updated, updated);
            visited[updated] = existing;
            visited[context] = existing;
            return updated;
        }

        public virtual PredictionContext AppendContext(int returnContext, PredictionContextCache contextCache)
        {
            return AppendContext(EmptyFull.GetChild(returnContext), contextCache);
        }

        public abstract PredictionContext AppendContext(PredictionContext suffix, PredictionContextCache contextCache);

        public virtual PredictionContext GetChild(int returnState)
        {
            return new SingletonPredictionContext(this, returnState);
        }

        public sealed override int GetHashCode()
        {
            return cachedHashCode;
        }

        public abstract override bool Equals(object o);

        //@Override
        //public String toString() {
        //	return toString(null, Integer.MAX_VALUE);
        //}
        public virtual string[] ToStrings(IRecognizer recognizer, int currentState)
        {
            return ToStrings(recognizer, EmptyFull, currentState);
        }

        public virtual string[] ToStrings(IRecognizer recognizer, PredictionContext stop, int currentState)
        {
            var result = new List<string>();
            for (int perm = 0;;
                perm++)
            {
                int offset = 0;
                bool last = true;
                PredictionContext p = this;
                int stateNumber = currentState;
                StringBuilder localBuffer = new();
                localBuffer.Append("[");
                while (!p.IsEmpty && p != stop)
                {
                    int index = 0;
                    if (p.Size > 0)
                    {
                        int bits = 1;
                        while (1 << bits < p.Size)
                        {
                            bits++;
                        }

                        int mask = (1 << bits) - 1;
                        index = (perm >> offset) & mask;
                        last &= index >= p.Size - 1;
                        if (index >= p.Size)
                        {
                            goto outer_continue;
                        }

                        offset += bits;
                    }

                    if (recognizer != null)
                    {
                        if (localBuffer.Length > 1)
                        {
                            // first char is '[', if more than that this isn't the first rule
                            localBuffer.Append(' ');
                        }

                        ATN atn = recognizer.Atn;
                        ATNState s = atn.states[stateNumber];
                        string ruleName = recognizer.RuleNames[s.ruleIndex];
                        localBuffer.Append(ruleName);
                    }
                    else
                    {
                        if (p.GetReturnState(index) != EmptyFullStateKey)
                        {
                            if (!p.IsEmpty)
                            {
                                if (localBuffer.Length > 1)
                                {
                                    // first char is '[', if more than that this isn't the first rule
                                    localBuffer.Append(' ');
                                }

                                localBuffer.Append(p.GetReturnState(index));
                            }
                        }
                    }

                    stateNumber = p.GetReturnState(index);
                    p = p.GetParent(index);
                }

                localBuffer.Append("]");
                result.Add(localBuffer.ToString());
                if (last)
                {
                    break;
                }

                outer_continue: ;
            }

            return result.ToArray();
        }

        public sealed class IdentityHashMap : Dictionary<PredictionContext, PredictionContext>
        {
            public IdentityHashMap()
                : base(IdentityEqualityComparator.Instance)
            {
            }
        }

        public sealed class IdentityEqualityComparator : EqualityComparer<PredictionContext>
        {
            public static readonly IdentityEqualityComparator Instance = new();

            private IdentityEqualityComparator()
            {
            }

            public override int GetHashCode(PredictionContext obj)
            {
                return obj.GetHashCode();
            }

            public override bool Equals(PredictionContext a, PredictionContext b)
            {
                return a == b;
            }
        }
    }
}