// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;

namespace Antlr4.Runtime.Atn
{
#pragma warning disable 0659 // 'class' overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public sealed class EmptyPredictionContext : PredictionContext
    {
        public static readonly EmptyPredictionContext LocalContext = new(false);

        public static readonly EmptyPredictionContext FullContext = new(true);

        private EmptyPredictionContext(bool fullContext)
            : base(19)
        {
            this.IsFullContext = fullContext;
        }

        public bool IsFullContext { get; }

        public override int Size => 0;

        public override bool IsEmpty => true;

        public override bool HasEmpty => true;

        protected internal override PredictionContext AddEmptyContext()
        {
            return this;
        }

        protected internal override PredictionContext RemoveEmptyContext()
        {
            throw new NotSupportedException("Cannot remove the empty context from itself.");
        }

        public override PredictionContext GetParent(int index)
        {
            throw new ArgumentOutOfRangeException();
        }

        public override int GetReturnState(int index)
        {
            throw new ArgumentOutOfRangeException();
        }

        public override int FindReturnState(int returnState)
        {
            return -1;
        }

        public override PredictionContext AppendContext(int returnContext, PredictionContextCache contextCache)
        {
            return contextCache.GetChild(this, returnContext);
        }

        public override PredictionContext AppendContext(PredictionContext suffix, PredictionContextCache contextCache)
        {
            return suffix;
        }

        public override bool Equals(object o)
        {
            return this == o;
        }

        public override string[] ToStrings(IRecognizer recognizer, int currentState)
        {
            return new[] {"[]"};
        }

        public override string[] ToStrings(IRecognizer recognizer, PredictionContext stop, int currentState)
        {
            return new[] {"[]"};
        }
    }
}