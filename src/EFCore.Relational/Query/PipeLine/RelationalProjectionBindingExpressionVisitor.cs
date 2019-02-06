// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query.PipeLine;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.Relational.Query.PipeLine
{
    public class RelationalProjectionBindingExpressionVisitor : ExpressionVisitor
    {
        private readonly SelectExpression _selectExpression;
        private readonly RelationalSqlTranslatingExpressionVisitor _sqlTranslator;
        private readonly IDictionary<ProjectionMember, Expression> _projectionMapping
            = new Dictionary<ProjectionMember, Expression>();

        private readonly Stack<ProjectionMember> _projectionMembers = new Stack<ProjectionMember>();

        public RelationalProjectionBindingExpressionVisitor(
            IRelationalTypeMappingSource typeMappingSource, SelectExpression selectExpression)
        {
            _sqlTranslator = new RelationalSqlTranslatingExpressionVisitor(typeMappingSource, selectExpression);
            _selectExpression = selectExpression;
        }

        public Expression Translate(Expression expression)
        {
            _projectionMembers.Push(new ProjectionMember());

            var result = Visit(expression);

            _selectExpression.ApplyProjection(_projectionMapping);

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
                var translation = _sqlTranslator.Translate(expression, false);

                if (!(translation is SqlExpression))
                {
                    throw new InvalidOperationException();
                }

                _projectionMapping[_projectionMembers.Peek()] = translation;

                return new ProjectionBindingExpression(_selectExpression, _projectionMembers.Peek(), expression.Type);
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
