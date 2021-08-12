// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System.Diagnostics;
#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif



namespace Antlr4.Runtime.Atn
{
#pragma warning disable 0659 // 'class' overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public class SingletonPredictionContext : PredictionContext
    {
        [NotNull] public readonly PredictionContext parent;

        public readonly int returnState;

        internal SingletonPredictionContext([NotNull] PredictionContext parent, int returnState)
            : base(CalculateHashCode(parent, returnState))
        {
            /*package*/
            Debug.Assert(returnState != EmptyFullStateKey && returnState != EmptyLocalStateKey);
            this.parent = parent;
            this.returnState = returnState;
        }

        public override int Size => 1;

        public override bool IsEmpty => false;

        public override bool HasEmpty => false;

        public override PredictionContext GetParent(int index)
        {
            Debug.Assert(index == 0);
            return parent;
        }

        public override int GetReturnState(int index)
        {
            Debug.Assert(index == 0);
            return returnState;
        }

        public override int FindReturnState(int returnState)
        {
            return this.returnState == returnState ? 0 : -1;
        }

        public override PredictionContext AppendContext(PredictionContext suffix, PredictionContextCache contextCache)
        {
            return contextCache.GetChild(parent.AppendContext(suffix, contextCache), returnState);
        }

        protected internal override PredictionContext AddEmptyContext()
        {
            PredictionContext[] parents = {parent, EmptyFull};
            int[] returnStates = {returnState, EmptyFullStateKey};
            return new ArrayPredictionContext(parents, returnStates);
        }

        protected internal override PredictionContext RemoveEmptyContext()
        {
            return this;
        }

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }

            if (!(o is SingletonPredictionContext))
            {
                return false;
            }

            SingletonPredictionContext other = (SingletonPredictionContext) o;
            if (GetHashCode() != other.GetHashCode())
            {
                return false;
            }

            return returnState == other.returnState && parent.Equals(other.parent);
        }
    }
}