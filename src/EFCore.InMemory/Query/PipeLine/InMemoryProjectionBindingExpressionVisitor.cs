// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query.PipeLine;

namespace Microsoft.EntityFrameworkCore.InMemory.Query.PipeLine
{
    public class InMemoryProjectionBindingExpressionVisitor : ExpressionVisitor
    {
        private readonly InMemoryQueryExpression _queryExpression;
        private readonly InMemoryExpressionTranslatingExpressionVisitor _translator;
        private readonly IDictionary<ProjectionMember, Expression> _projectionMapping
            = new Dictionary<ProjectionMember, Expression>();

        private readonly Stack<ProjectionMember> _projectionMembers = new Stack<ProjectionMember>();

        public InMemoryProjectionBindingExpressionVisitor(InMemoryQueryExpression queryExpression)
        {
            _queryExpression = queryExpression;
            _translator = new InMemoryExpressionTranslatingExpressionVisitor(queryExpression);
        }

        public Expression Translate(Expression expression)
        {
            _projectionMembers.Push(new ProjectionMember());

            var result = Visit(expression);

            _queryExpression.ApplyProjection(_projectionMapping);

            return result;
        }

        public override Expression Visit(Expression expression)
        {
            if (expression == null)
            {
                return null;
            }

            if (!(expression is NewExpression))
            {
                var translation = _translator.Visit(expression);

                _projectionMapping[_projectionMembers.Peek()] = translation;

                return new ProjectionBindingExpression(_queryExpression, _projectionMembers.Peek(), expression.Type);
            }

            return base.Visit(expression);
        }

        protected override Expression VisitNew(NewExpression newExpression)
        {
            var newArguments = new Expression[newExpression.Arguments.Count];
            for (var i = 0; i < newExpression.Arguments.Count; i++)
            {
                // TODO: Members can be null????
                var projectionMember = _projectionMembers.Peek().AddMember(newExpression.Members[i]);
                _projectionMembers.Push(projectionMember);

                newArguments[i] = Visit(newExpression.Arguments[i]);
            }

            return newExpression.Update(newArguments);
        }
    }
}
