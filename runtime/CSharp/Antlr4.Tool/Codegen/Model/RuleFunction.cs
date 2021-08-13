// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Antlr4.Codegen.Model.Decl;
using Antlr4.Misc;
using Antlr4.Parse;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Utility;
using Antlr4.Tool;
using Antlr4.Tool.Ast;

using Utils = Antlr4.Misc.Utils;

namespace Antlr4.Codegen.Model
{
    /** */
    public class RuleFunction : OutputModelObject
    {
        [ModelElement] public IDictionary<string, AltLabelStructDecl> altLabelCtxs;

        [ModelElement] public ICollection<AttributeDecl> args;

        [ModelElement] public IList<SrcOp> code;

        public string ctxType;

        [ModelElement] public IList<ExceptionClause> exceptions;

        [ModelElement] public Action finallyAction;

        public bool hasLookaheadBlock;
        public int index;

        [ModelElement] public OrderedHashSet<Decl.Decl> locals; // TODO: move into ctx?

        public IList<string> modifiers;
        public string name;

        [ModelElement] public IDictionary<string, Action> namedActions;

        [ModelElement] public IList<SrcOp> postamble;

        public Rule rule;

        [ModelElement] public StructDecl ruleCtx;

        public ICollection<string> ruleLabels;
        public ATNState startState;
        public ICollection<string> tokenLabels;
        public string variantOf;

        public RuleFunction(OutputModelFactory factory, Rule r)
            : base(factory)
        {
            name = r.name;
            rule = r;
            if (r.modifiers != null && r.modifiers.Count > 0)
            {
                modifiers = new List<string>();
                foreach (GrammarAST t in r.modifiers)
                {
                    modifiers.Add(t.Text);
                }
            }

            modifiers = Utils.NodesToStrings(r.modifiers);

            index = r.index;
            int lfIndex = name.IndexOf(ATNSimulator.RuleVariantDelimiter);
            if (lfIndex >= 0)
            {
                variantOf = name.Substring(0, lfIndex);
            }

            if (r.name.Equals(r.GetBaseContext()))
            {
                ruleCtx = new StructDecl(factory, r);
                AddContextGetters(factory, r.g.contextASTs[r.name]);

                if (r.args != null)
                {
                    ICollection<Attribute> decls = r.args.attributes.Values;
                    if (decls.Count > 0)
                    {
                        args = new List<AttributeDecl>();
                        ruleCtx.AddDecls(decls);
                        foreach (Attribute a in decls)
                        {
                            args.Add(new AttributeDecl(factory, a));
                        }

                        ruleCtx.ctorAttrs = args;
                    }
                }

                if (r.retvals != null)
                {
                    ruleCtx.AddDecls(r.retvals.attributes.Values);
                }

                if (r.locals != null)
                {
                    ruleCtx.AddDecls(r.locals.attributes.Values);
                }
            }
            else
            {
                if (r.args != null || r.retvals != null || r.locals != null)
                {
                    throw new NotSupportedException("customized fields are not yet supported for customized context objects");
                }
            }

            ruleLabels = r.GetElementLabelNames();
            tokenLabels = r.GetTokenRefs();
            if (r.exceptions != null)
            {
                exceptions = new List<ExceptionClause>();
                foreach (GrammarAST e in r.exceptions)
                {
                    ActionAST catchArg = (ActionAST) e.GetChild(0);
                    ActionAST catchAction = (ActionAST) e.GetChild(1);
                    exceptions.Add(new ExceptionClause(factory, catchArg, catchAction));
                }
            }

            startState = factory.GetGrammar().atn.ruleToStartState[r.index];
        }

        public virtual void AddContextGetters(OutputModelFactory factory, ICollection<RuleAST> contextASTs)
        {
            IList<AltAST> unlabeledAlternatives = new List<AltAST>();
            IDictionary<string, IList<AltAST>> labeledAlternatives = new Dictionary<string, IList<AltAST>>();

            foreach (RuleAST ast in contextASTs)
            {
                try
                {
                    foreach (AltAST altAst in rule.g.GetUnlabeledAlternatives(ast))
                    {
                        unlabeledAlternatives.Add(altAst);
                    }

                    foreach (KeyValuePair<string, IList<Tuple<int, AltAST>>> entry in rule.g.GetLabeledAlternatives(ast))
                    {
                        IList<AltAST> list;
                        if (!labeledAlternatives.TryGetValue(entry.Key, out list))
                        {
                            list = new List<AltAST>();
                            labeledAlternatives[entry.Key] = list;
                        }

                        foreach (Tuple<int, AltAST> tuple in entry.Value)
                        {
                            list.Add(tuple.Item2);
                        }
                    }
                }
                catch (RecognitionException)
                {
                }
            }

            // Add ctx labels for elements in alts with no '#' label
            if (unlabeledAlternatives.Count > 0)
            {
                ISet<Decl.Decl> decls = GetDeclsForAllElements(unlabeledAlternatives);

                // put directly in base context
                foreach (Decl.Decl decl in decls)
                {
                    ruleCtx.AddDecl(decl);
                }
            }

            // make structs for '#' labeled alts, define ctx labels for elements
            altLabelCtxs = new Dictionary<string, AltLabelStructDecl>();
            if (labeledAlternatives.Count > 0)
            {
                foreach (KeyValuePair<string, IList<AltAST>> entry in labeledAlternatives)
                {
                    AltLabelStructDecl labelDecl = new(factory, rule, entry.Key);
                    altLabelCtxs[entry.Key] = labelDecl;
                    ISet<Decl.Decl> decls = GetDeclsForAllElements(entry.Value);
                    foreach (Decl.Decl decl in decls)
                    {
                        labelDecl.AddDecl(decl);
                    }
                }
            }
        }

        public virtual void FillNamedActions(OutputModelFactory factory, Rule r)
        {
            if (r.finallyAction != null)
            {
                finallyAction = new Action(factory, r.finallyAction);
            }

            namedActions = new Dictionary<string, Action>();
            foreach (string name in r.namedActions.Keys)
            {
                ActionAST ast;
                r.namedActions.TryGetValue(name, out ast);
                namedActions[name] = new Action(factory, ast);
            }
        }

        /**
         * for all alts, find which ref X or r needs List
         * Must see across alts.  If any alt needs X or r as list, then
         * define as list.
         */
        public virtual ISet<Decl.Decl> GetDeclsForAllElements(IList<AltAST> altASTs)
        {
            ISet<string> needsList = new HashSet<string>();
            ISet<string> optional = new HashSet<string>();
            ISet<string> suppress = new HashSet<string>();
            IList<GrammarAST> allRefs = new List<GrammarAST>();
            foreach (AltAST ast in altASTs)
            {
                IntervalSet reftypes = new(ANTLRParser.RULE_REF, ANTLRParser.TOKEN_REF);
                IList<GrammarAST> refs = ast.GetNodesWithType(reftypes);
                foreach (GrammarAST @ref in refs)
                {
                    allRefs.Add(@ref);
                }

                Tuple<FrequencySet<string>, FrequencySet<string>> minAndAltFreq = GetElementFrequenciesForAlt(ast);
                FrequencySet<string> minFreq = minAndAltFreq.Item1;
                FrequencySet<string> altFreq = minAndAltFreq.Item2;
                foreach (GrammarAST t in refs)
                {
                    string refLabelName = GetLabelName(rule.g, t);
                    if (altFreq.GetCount(refLabelName) == 0)
                    {
                        suppress.Add(refLabelName);
                    }

                    if (minFreq.GetCount(refLabelName) == 0)
                    {
                        optional.Add(refLabelName);
                    }

                    if (altFreq.GetCount(refLabelName) > 1)
                    {
                        needsList.Add(refLabelName);
                    }
                }
            }

            ISet<Decl.Decl> decls = new LinkedHashSet<Decl.Decl>();
            foreach (GrammarAST t in allRefs)
            {
                string refLabelName = GetLabelName(rule.g, t);
                if (suppress.Contains(refLabelName))
                {
                    continue;
                }

                IList<Decl.Decl> d = GetDeclForAltElement(t,
                    refLabelName,
                    needsList.Contains(refLabelName),
                    optional.Contains(refLabelName));
                decls.UnionWith(d);
            }

            return decls;
        }

        public static string GetLabelName(Grammar g, GrammarAST t)
        {
            string labelName = t.Text;
            Rule referencedRule;
            if (g.rules.TryGetValue(labelName, out referencedRule) && referencedRule != null)
            {
                labelName = referencedRule.GetBaseContext();
            }

            return labelName;
        }

        /**
         * Given list of X and r refs in alt, compute how many of each there are
         */
        protected virtual Tuple<FrequencySet<string>, FrequencySet<string>> GetElementFrequenciesForAlt(AltAST ast)
        {
            try
            {
                ElementFrequenciesVisitor visitor = new(rule.g, new CommonTreeNodeStream(new GrammarASTAdaptor(), ast));
                visitor.outerAlternative();
                if (visitor.frequencies.Count != 1)
                {
                    factory.GetGrammar().tool.errMgr.ToolError(ErrorType.INTERNAL_ERROR);
                    return Tuple.Create(new FrequencySet<string>(), new FrequencySet<string>());
                }

                return Tuple.Create(visitor.GetMinFrequencies(), visitor.frequencies.Peek());
            }
            catch (RecognitionException ex)
            {
                factory.GetGrammar().tool.errMgr.ToolError(ErrorType.INTERNAL_ERROR, ex);
                return Tuple.Create(new FrequencySet<string>(), new FrequencySet<string>());
            }
        }

        public virtual IList<Decl.Decl> GetDeclForAltElement(GrammarAST t, string refLabelName, bool needList, bool optional)
        {
            int lfIndex = refLabelName.IndexOf(ATNSimulator.RuleVariantDelimiter);
            if (lfIndex >= 0)
            {
                refLabelName = refLabelName.Substring(0, lfIndex);
            }

            IList<Decl.Decl> decls = new List<Decl.Decl>();
            if (t.Type == ANTLRParser.RULE_REF)
            {
                Rule rref = factory.GetGrammar().GetRule(t.Text);
                string ctxName = factory.GetTarget()
                    .GetRuleFunctionContextStructName(rref);
                if (needList)
                {
                    if (factory.GetTarget().SupportsOverloadedMethods())
                    {
                        decls.Add(new ContextRuleListGetterDecl(factory, refLabelName, ctxName));
                    }

                    decls.Add(new ContextRuleListIndexedGetterDecl(factory, refLabelName, ctxName));
                }
                else
                {
                    decls.Add(new ContextRuleGetterDecl(factory, refLabelName, ctxName, optional));
                }
            }
            else
            {
                if (needList)
                {
                    if (factory.GetTarget().SupportsOverloadedMethods())
                    {
                        decls.Add(new ContextTokenListGetterDecl(factory, refLabelName));
                    }

                    decls.Add(new ContextTokenListIndexedGetterDecl(factory, refLabelName));
                }
                else
                {
                    decls.Add(new ContextTokenGetterDecl(factory, refLabelName, optional));
                }
            }

            return decls;
        }

        /**
         * Add local var decl
         */
        public virtual void AddLocalDecl(Decl.Decl d)
        {
            if (locals == null)
            {
                locals = new OrderedHashSet<Decl.Decl>();
            }

            locals.Add(d);
            d.isLocal = true;
        }

        /**
         * Add decl to struct ctx for rule or alt if labeled
         */
        public virtual void AddContextDecl(string altLabel, Decl.Decl d)
        {
            CodeBlockForOuterMostAlt alt = d.GetOuterMostAltCodeBlock();
            // if we found code blk and might be alt label, try to add to that label ctx
            if (alt != null && altLabelCtxs != null)
            {
                //System.Console.WriteLine(d.name + " lives in alt " + alt.alt.altNum);
                AltLabelStructDecl altCtx;
                if (altLabel != null && altLabelCtxs.TryGetValue(altLabel, out altCtx))
                {
                    // we have an alt ctx
                    //System.Console.WriteLine("ctx is " + altCtx.name);
                    altCtx.AddDecl(d);
                    return;
                }
            }

            ruleCtx.AddDecl(d); // stick in overall rule's ctx
        }
    }
}