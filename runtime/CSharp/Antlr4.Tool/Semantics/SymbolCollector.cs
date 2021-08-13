// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System.Collections.Generic;
using Antlr4.Tool;
using Antlr4.Tool.Ast;

namespace Antlr4.Semantics
{
    /**
     * Collects (create) rules, terminals, strings, actions, scopes etc... from AST
     * side-effects: sets resolver field of asts for actions and
     * defines predicates via definePredicateInAlt(), collects actions and stores
     * in alts.
     * TODO: remove side-effects!
     */
    public class SymbolCollector : GrammarTreeVisitor
    {
        public IList<GrammarAST> channelDefs = new List<GrammarAST>();

        // context
        public Rule currentRule;

        public ErrorManager errMgr;

        /**
         * which grammar are we checking
         */
        public Grammar g;

        /**
         * Track action name node in @parser::members {...} or @members {...}
         */
        internal IList<GrammarAST> namedActions = new List<GrammarAST>();

        public IList<GrammarAST> qualifiedRulerefs = new List<GrammarAST>();

        // stuff to collect
        public IList<GrammarAST> rulerefs = new List<GrammarAST>();
        public ISet<string> strings = new HashSet<string>();
        public IList<GrammarAST> terminals = new List<GrammarAST>();
        public IList<GrammarAST> tokenIDRefs = new List<GrammarAST>();
        public IList<GrammarAST> tokensDefs = new List<GrammarAST>();

        public SymbolCollector(Grammar g)
        {
            this.g = g;
            errMgr = g.tool.errMgr;
        }

        public override ErrorManager GetErrorManager()
        {
            return errMgr;
        }

        public virtual void Process(GrammarAST ast)
        {
            VisitGrammar(ast);
        }

        public override void GlobalNamedAction(GrammarAST scope, GrammarAST ID, ActionAST action)
        {
            namedActions.Add((GrammarAST) ID.Parent);
            action.resolver = g;
        }

        public override void DefineToken(GrammarAST ID)
        {
            terminals.Add(ID);
            tokenIDRefs.Add(ID);
            tokensDefs.Add(ID);
        }

        public override void DefineChannel(GrammarAST ID)
        {
            channelDefs.Add(ID);
        }

        public override void DiscoverRule(RuleAST rule, GrammarAST ID,
            IList<GrammarAST> modifiers, ActionAST arg,
            ActionAST returns, GrammarAST thrws,
            GrammarAST options, ActionAST locals,
            IList<GrammarAST> actions,
            GrammarAST block)
        {
            currentRule = g.GetRule(ID.Text);
        }

        public override void DiscoverLexerRule(RuleAST rule, GrammarAST ID, IList<GrammarAST> modifiers,
            GrammarAST block)
        {
            currentRule = g.GetRule(ID.Text);
        }

        public override void DiscoverOuterAlt(AltAST alt)
        {
            currentRule.alt[currentOuterAltNumber].ast = alt;
        }

        public override void ActionInAlt(ActionAST action)
        {
            currentRule.DefineActionInAlt(currentOuterAltNumber, action);
            action.resolver = currentRule.alt[currentOuterAltNumber];
        }

        public override void SempredInAlt(PredAST pred)
        {
            currentRule.DefinePredicateInAlt(currentOuterAltNumber, pred);
            pred.resolver = currentRule.alt[currentOuterAltNumber];
        }

        public override void RuleCatch(GrammarAST arg, ActionAST action)
        {
            GrammarAST catchme = (GrammarAST) action.Parent;
            currentRule.exceptions.Add(catchme);
            action.resolver = currentRule;
        }

        public override void FinallyAction(ActionAST action)
        {
            currentRule.finallyAction = action;
            action.resolver = currentRule;
        }

        public override void Label(GrammarAST op, GrammarAST ID, GrammarAST element)
        {
            LabelElementPair lp = new(g, ID, element, op.Type);
            currentRule.alt[currentOuterAltNumber].labelDefs.Map(ID.Text, lp);
        }

        public override void StringRef(TerminalAST @ref)
        {
            terminals.Add(@ref);
            strings.Add(@ref.Text);
            if (currentRule != null)
            {
                currentRule.alt[currentOuterAltNumber].tokenRefs.Map(@ref.Text, @ref);
            }
        }

        public override void TokenRef(TerminalAST @ref)
        {
            terminals.Add(@ref);
            tokenIDRefs.Add(@ref);
            if (currentRule != null)
            {
                currentRule.alt[currentOuterAltNumber].tokenRefs.Map(@ref.Text, @ref);
            }
        }

        public override void RuleRef(GrammarAST @ref, ActionAST arg)
        {
            //		if ( inContext("DOT ...") ) qualifiedRulerefs.add((GrammarAST)ref.getParent());
            rulerefs.Add(@ref);
            if (currentRule != null)
            {
                currentRule.alt[currentOuterAltNumber].ruleRefs.Map(@ref.Text, @ref);
            }
        }

        public override void GrammarOption(GrammarAST ID, GrammarAST valueAST)
        {
            SetActionResolver(valueAST);
        }

        public override void RuleOption(GrammarAST ID, GrammarAST valueAST)
        {
            SetActionResolver(valueAST);
        }

        public override void BlockOption(GrammarAST ID, GrammarAST valueAST)
        {
            SetActionResolver(valueAST);
        }

        public override void ElementOption(GrammarASTWithOptions t, GrammarAST ID, GrammarAST valueAST)
        {
            SetActionResolver(valueAST);
        }

        /**
         * In case of option id={...}, set resolve in case they use $foo
         */
        private void SetActionResolver(GrammarAST valueAST)
        {
            if (valueAST is ActionAST)
            {
                ((ActionAST) valueAST).resolver = currentRule.alt[currentOuterAltNumber];
            }
        }
    }
}