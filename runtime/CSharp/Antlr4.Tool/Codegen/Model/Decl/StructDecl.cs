// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System.Collections.Generic;
using Antlr4.Misc;
using Antlr4.Tool;

namespace Antlr4.Codegen.Model.Decl
{
    /**
     * This object models the structure holding all of the parameters,
     * return values, local variables, and labels associated with a rule.
     */
    public class StructDecl : Decl
    {
        public OrderedHashSet<Decl> attributeDecls = new();

        [ModelElement] public OrderedHashSet<Decl> attrs = new();

        [ModelElement] public ICollection<AttributeDecl> ctorAttrs;

        public string derivedFromName; // rule name or label name

        [ModelElement] public IList<DispatchMethod> dispatchMethods;

        [ModelElement] public IList<OutputModelObject> extensionMembers;

        [ModelElement] public OrderedHashSet<Decl> getters = new();

        [ModelElement] public IList<OutputModelObject> interfaces;

        public bool provideCopyFrom;
        public OrderedHashSet<Decl> ruleContextDecls = new();
        public OrderedHashSet<Decl> ruleContextListDecls = new();

        // Track these separately; Go target needs to generate getters/setters
        // Do not make them templates; we only need the Decl object not the ST
        // built from it. Avoids adding args to StructDecl template
        public OrderedHashSet<Decl> tokenDecls = new();
        public OrderedHashSet<Decl> tokenListDecls = new();
        public OrderedHashSet<Decl> tokenTypeDecls = new();

        public StructDecl(OutputModelFactory factory, Rule r)
            : base(factory, factory.GetTarget().GetRuleFunctionContextStructName(r))
        {
            AddDispatchMethods(r);
            derivedFromName = r.name;
            provideCopyFrom = r.HasAltSpecificContexts();
        }

        public virtual void AddDispatchMethods(Rule r)
        {
            dispatchMethods = new List<DispatchMethod>();
            if (!r.HasAltSpecificContexts())
            {
                // no enter/exit for this ruleContext if rule has labels
                if (factory.GetGrammar().tool.gen_listener)
                {
                    dispatchMethods.Add(new ListenerDispatchMethod(factory, true));
                    dispatchMethods.Add(new ListenerDispatchMethod(factory, false));
                }

                if (factory.GetGrammar().tool.gen_visitor)
                {
                    dispatchMethods.Add(new VisitorDispatchMethod(factory));
                }
            }
        }

        public virtual void AddDecl(Decl d)
        {
            d.ctx = this;

            if (d is ContextGetterDecl)
            {
                getters.Add(d);
            }
            else
            {
                attrs.Add(d);
            }

            // add to specific "lists"
            if (d is TokenTypeDecl)
            {
                tokenTypeDecls.Add(d);
            }
            else if (d is TokenListDecl)
            {
                tokenListDecls.Add(d);
            }
            else if (d is TokenDecl)
            {
                tokenDecls.Add(d);
            }
            else if (d is RuleContextListDecl)
            {
                ruleContextListDecls.Add(d);
            }
            else if (d is RuleContextDecl)
            {
                ruleContextDecls.Add(d);
            }
            else if (d is AttributeDecl)
            {
                attributeDecls.Add(d);
            }
        }

        public virtual void AddDecl(AttributeNode a)
        {
            AddDecl(new AttributeDecl(factory, a));
        }

        public virtual void AddDecls(ICollection<AttributeNode> attrList)
        {
            foreach (AttributeNode a in attrList)
            {
                AddDecl(a);
            }
        }

        public virtual void ImplementInterface(OutputModelObject value)
        {
            if (interfaces == null)
            {
                interfaces = new List<OutputModelObject>();
            }

            interfaces.Add(value);
        }

        public virtual void AddExtensionMember(OutputModelObject member)
        {
            if (extensionMembers == null)
            {
                extensionMembers = new List<OutputModelObject>();
            }

            extensionMembers.Add(member);
        }

        public virtual bool IsEmpty()
        {
            return attrs.Count == 0;
        }
    }
}