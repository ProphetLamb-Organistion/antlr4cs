// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Antlr4.Parse;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Tree;
using Antlr4.Runtime.Utility;

namespace Antlr4.Tool.Ast
{
    public class GrammarAST : CommonTree
    {
        /**
         * If we build an ATN, we make AST node point at left edge of ATN construct
         */
        public ATNState atnState;

        /**
         * For error msgs, nice to know which grammar this AST lives in
         */
        // TODO: try to remove
        public Grammar g;

        public string textOverride;

        public GrammarAST()
        {
        }

        public GrammarAST(IToken t)
            : base(t)
        {
        }

        public GrammarAST(GrammarAST node)
            : base(node)
        {
            g = node.g;
            atnState = node.atnState;
            textOverride = node.textOverride;
        }

        public GrammarAST(int type)
            : base(new CommonToken(type, ANTLRParser.tokenNames[type]))
        {
        }

        public GrammarAST(int type, IToken t)
            : this(new CommonToken(t))
        {
            Token.Type = type;
        }

        public GrammarAST(int type, IToken t, string text)
            : this(new CommonToken(t))
        {
            Token.Type = type;
            Token.Text = text;
        }

        public virtual GrammarAST[] GetChildrenAsArray()
        {
            return Children.Cast<GrammarAST>().ToArray();
        }

        public virtual IList<GrammarAST> GetNodesWithType(int ttype)
        {
            return GetNodesWithType(IntervalSet.Of(ttype));
        }

        public override ITree GetFirstChildWithType(int type)
        {
            if (ChildCount == 0)
            {
                return null;
            }

            return base.GetFirstChildWithType(type);
        }

        public virtual IList<GrammarAST> GetAllChildrenWithType(int type)
        {
            IList<GrammarAST> nodes = new List<GrammarAST>();
            for (int i = 0;
                Children != null && i < Children.Count;
                i++)
            {
                ITree t = Children[i];
                if (t.Type == type)
                {
                    nodes.Add((GrammarAST) t);
                }
            }

            return nodes;
        }

        public virtual IList<GrammarAST> GetNodesWithType(IntervalSet types)
        {
            IList<GrammarAST> nodes = new List<GrammarAST>();
            var work = new LinkedList<GrammarAST>();
            work.AddLast(this);
            GrammarAST t;
            while (work.Count > 0)
            {
                t = work.First.Value;
                work.RemoveFirst();
                if (types == null || types.Contains(t.Type))
                {
                    nodes.Add(t);
                }

                if (t.Children != null)
                {
                    foreach (GrammarAST child in t.GetChildrenAsArray())
                    {
                        work.AddLast(child);
                    }
                }
            }

            return nodes;
        }

        public virtual IList<GrammarAST> GetNodesWithTypePreorderDFS(IntervalSet types)
        {
            var nodes = new List<GrammarAST>();
            GetNodesWithTypePreorderDFS_(nodes, types);
            return nodes;
        }

        public virtual void GetNodesWithTypePreorderDFS_(IList<GrammarAST> nodes, IntervalSet types)
        {
            if (types.Contains(Type))
            {
                nodes.Add(this);
            }

            // walk all children of root.
            for (int i = 0;
                i < ChildCount;
                i++)
            {
                GrammarAST child = (GrammarAST) GetChild(i);
                child.GetNodesWithTypePreorderDFS_(nodes, types);
            }
        }

        public virtual GrammarAST GetNodeWithTokenIndex(int index)
        {
            if (Token != null && Token.TokenIndex == index)
            {
                return this;
            }

            // walk all children of root.
            for (int i = 0;
                i < ChildCount;
                i++)
            {
                GrammarAST child = (GrammarAST) GetChild(i);
                GrammarAST result = child.GetNodeWithTokenIndex(index);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public virtual AltAST GetOutermostAltNode()
        {
            if (this is AltAST && Parent.Parent is RuleAST)
            {
                return (AltAST) this;
            }

            if (Parent != null)
            {
                return ((GrammarAST) Parent).GetOutermostAltNode();
            }

            return null;
        }

        /**
         * Walk ancestors of this node until we find ALT with
         * alt!=null or leftRecursiveAltInfo!=null. Then grab label if any.
         * If not a rule element, just returns null.
         */
        public virtual string GetAltLabel()
        {
            IList<ITree> ancestors = GetAncestors();
            if (ancestors == null)
            {
                return null;
            }

            for (int i = ancestors.Count - 1;
                i >= 0;
                i--)
            {
                GrammarAST p = (GrammarAST) ancestors[i];
                if (p.Type == ANTLRParser.ALT)
                {
                    AltAST a = (AltAST) p;
                    if (a.altLabel != null)
                    {
                        return a.altLabel.Text;
                    }

                    if (a.leftRecursiveAltInfo != null)
                    {
                        return a.leftRecursiveAltInfo.altLabel;
                    }
                }
            }

            return null;
        }

        public virtual bool DeleteChild(ITree t)
        {
            for (int i = 0;
                i < Children.Count;
                i++)
            {
                object c = Children[i];
                if (c == t)
                {
                    DeleteChild(t.ChildIndex);
                    return true;
                }
            }

            return false;
        }

        // TODO: move to basetree when i settle on how runtime works
        // TODO: don't include this node!!
        // TODO: reuse other method
        public virtual CommonTree GetFirstDescendantWithType(int type)
        {
            if (Type == type)
            {
                return this;
            }

            if (Children == null)
            {
                return null;
            }

            foreach (object c in Children)
            {
                GrammarAST t = (GrammarAST) c;
                if (t.Type == type)
                {
                    return t;
                }

                CommonTree d = t.GetFirstDescendantWithType(type);
                if (d != null)
                {
                    return d;
                }
            }

            return null;
        }

        // TODO: don't include this node!!
        public virtual CommonTree GetFirstDescendantWithType(BitSet types)
        {
            if (types.Member(Type))
            {
                return this;
            }

            if (Children == null)
            {
                return null;
            }

            foreach (object c in Children)
            {
                GrammarAST t = (GrammarAST) c;
                if (types.Member(t.Type))
                {
                    return t;
                }

                CommonTree d = t.GetFirstDescendantWithType(types);
                if (d != null)
                {
                    return d;
                }
            }

            return null;
        }

        public virtual void SetType(int type)
        {
            Token.Type = type;
        }
        //
        //	@Override
        //	public String getText() {
        //		if ( textOverride!=null ) return textOverride;
        //        if ( token!=null ) {
        //            return token.getText();
        //        }
        //        return "";
        //	}

        public virtual void SetText(string text)
        {
            //		textOverride = text; // don't alt tokens as others might see
            Token.Text = text; // we delete surrounding tree, so ok to alter
        }

        //	@Override
        //	public boolean equals(Object obj) {
        //		return super.equals(obj);
        //	}

        public override ITree DupNode()
        {
            return new GrammarAST(this);
        }

        public virtual GrammarAST DupTree()
        {
            GrammarAST t = this;
            ICharStream input = Token.InputStream;
            GrammarASTAdaptor adaptor = new(input);
            return (GrammarAST) adaptor.DupTree(t);
        }

        public virtual string ToTokenString()
        {
            ICharStream input = Token.InputStream;
            GrammarASTAdaptor adaptor = new(input);
            CommonTreeNodeStream nodes =
                new(adaptor, this);
            StringBuilder buf = new();
            GrammarAST o = (GrammarAST) nodes.LT(1);
            int type = adaptor.GetType(o);
            while (type != TokenTypes.EndOfFile)
            {
                buf.Append(" ");
                buf.Append(o.Text);
                nodes.Consume();
                o = (GrammarAST) nodes.LT(1);
                type = adaptor.GetType(o);
            }

            return buf.ToString();
        }

        public virtual object Visit(GrammarASTVisitor v)
        {
            return v.Visit(this);
        }
    }
}