// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

namespace Antlr4.Runtime.Atn
{
    /// <author>Sam Harwell</author>
    public class OrderedATNConfigSet : ATNConfigSet
    {
        public OrderedATNConfigSet()
        {
        }

        public OrderedATNConfigSet(ATNConfigSet set, bool @readonly)
            : base(set, @readonly)
        {
        }

        public override ATNConfigSet Clone(bool @readonly)
        {
            OrderedATNConfigSet copy = new(this, @readonly);
            if (!@readonly && IsReadOnly)
            {
                copy.AddAll(this);
            }

            return copy;
        }

        protected internal override long GetKey(ATNConfig e)
        {
            return e.GetHashCode();
        }

        protected internal override bool CanMerge(ATNConfig left, long leftKey, ATNConfig right)
        {
            return left.Equals(right);
        }
    }
}