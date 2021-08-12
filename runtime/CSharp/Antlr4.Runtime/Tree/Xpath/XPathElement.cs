// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Antlr4.Runtime.Tree.Xpath
{
    public abstract class XPathElement
    {
        protected internal bool invert;
        protected internal string nodeName;

        /// <summary>
        ///     Construct element like
        ///     <c>/ID</c>
        ///     or
        ///     <c>ID</c>
        ///     or
        ///     <c>/*</c>
        ///     etc...
        ///     op is null if just node
        /// </summary>
        public XPathElement(string nodeName)
        {
            this.nodeName = nodeName;
        }

        /// <summary>
        ///     Given tree rooted at
        ///     <paramref name="t" />
        ///     return all nodes matched by this path
        ///     element.
        /// </summary>
        public abstract ICollection<IParseTree> Evaluate(IParseTree t);

        public override string ToString()
        {
            string inv = invert ? "!" : String.Empty;
            return GetType().Name + "[" + inv + nodeName + "]";
        }
    }
}