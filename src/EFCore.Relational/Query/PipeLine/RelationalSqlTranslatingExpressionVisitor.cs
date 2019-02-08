// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Extensions.Internal;
using Microsoft.EntityFrameworkCore.Query.PipeLine;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.Relational.Query.PipeLine
{
    public class RelationalSqlTranslatingExpressionVisitor : ExpressionVisitor
    {
        private readonly IRelationalTypeMappingSource _typeMappingSource;
        private readonly IMethodCallTranslatorProvider _methodCallTranslatorProvider;
        private readonly TypeMappingInferringExpressionVisitor _typeInference;

        private SelectExpression _selectExpression;

        public RelationalSqlTranslatingExpressionVisitor(
            IRelationalTypeMappingSource typeMappingSource, IMethodCallTranslatorProvider methodCallTranslatorProvider)
        {
            _typeInference = new TypeMappingInferringExpressionVisitor();
            _typeMappingSource = typeMappingSource;
            _methodCallTranslatorProvider = methodCallTranslatorProvider;
        }

        public SqlExpression Translate(SelectExpression selectExpression, Expression expression, bool condition)
        {
            _selectExpression = selectExpression;

            var translation = Visit(expression);

            if (!(translation is SqlExpression sqlExpression))
            {
                sqlExpression = new SqlExpression(translation, _typeMappingSource.FindMapping(translation.Type));
            }

            if (condition
                && !sqlExpression.IsCondition
                && sqlExpression.TypeMapping == _typeMappingSource.FindMapping(typeof(bool)))
            {
                sqlExpression = new SqlExpression(sqlExpression.Expression, true);
            }

            _selectExpression = null;

            return sqlExpression;
        }

        protected override Expression VisitMember(MemberExpression memberExpression)
        {
            var innerExpression = Visit(memberExpression.Expression);
            if (innerExpression is EntityShaperExpression entityShaper)
            {
                var entityType = entityShaper.EntityType;
                var property = entityType.FindProperty(memberExpression.Member.GetSimpleMemberName());

                return _selectExpression.BindProperty(entityShaper.ValueBufferExpression, property);
            }

            return memberExpression.Update(innerExpression);
        }

        protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.IsEFPropertyMethod())
            {
                var firstArgument = Visit(methodCallExpression.Arguments[0]);

                // In certain cases EF.Property would have convert node around the source.
                if (firstArgument.NodeType == ExpressionType.Convert
                    && firstArgument.Type == typeof(object))
                {
                    firstArgument = ((UnaryExpression)firstArgument).Operand;
                }

                if (firstArgument is EntityShaperExpression entityShaper)
                {
                    var entityType = entityShaper.EntityType;
                    var property = entityType.FindProperty((string)((ConstantExpression)methodCallExpression.Arguments[1]).Value);

                    return _selectExpression.BindProperty(entityShaper.ValueBufferExpression, property);
                }
            }

            var @object = Visit(methodCallExpression.Object);
            var arguments = new Expression[methodCallExpression.Arguments.Count];
            for (var i = 0; i < arguments.Length; i++)
            {
                arguments[i] = Visit(methodCallExpression.Arguments[i]);
            }

            var updatedMethodCallExpression = methodCallExpression.Update(@object, arguments);

            return _methodCallTranslatorProvider.Translate(updatedMethodCallExpression);
        }

        protected override Expression VisitBinary(BinaryExpression binaryExpression)
        {
            var newExpression = base.VisitBinary(binaryExpression);

            newExpression = _typeInference.Visit(newExpression);

            return newExpression;
        }

        protected override Expression VisitExtension(Expression extensionExpression)
        {
            if (extensionExpression is EntityShaperExpression)
            {
                return extensionExpression;
            }

            return base.VisitExtension(extensionExpression);
        }

        protected override Expression VisitUnary(UnaryExpression unaryExpression)
        {
            var operand = Visit(unaryExpression.Operand);

            if (operand is SqlExpression
                && unaryExpression.Type != typeof(object)
                && unaryExpression.NodeType == ExpressionType.Convert)
            {
                var typeMapping = _typeMappingSource.FindMapping(unaryExpression.Type);
                return new SqlExpression(
                    new SqlCastExpression(operand, unaryExpression.Type, typeMapping.StoreType),
                    typeMapping);
            }

            return unaryExpression.Update(operand);
        }
    }

    public class RelationalMethodCallTranslatorProvider : IMethodCallTranslatorProvider
    {
        private readonly List<IMethodCallTranslator> _methodCallTranslators = new List<IMethodCallTranslator>();

        public RelationalMethodCallTranslatorProvider()
        {
            _methodCallTranslators.AddRange(
                new[] {
                    new EqualsTranslator()
                });
        }

        public Expression Translate(MethodCallExpression methodCallExpression)
        {
            return _methodCallTranslators.Select(t => t.Translate(methodCallExpression)).FirstOrDefault(t => t != null);
        }

        protected virtual void AddTranslators(IEnumerable<IMethodCallTranslator> translators)
            => _methodCallTranslators.InsertRange(0, translators);
    }

    public class EqualsTranslator : IMethodCallTranslator
    {
        public Expression Translate(MethodCallExpression methodCallExpression)
        {
            Expression left = null;
            Expression right = null;
            if (methodCallExpression.Method.Name == nameof(object.Equals)
                && methodCallExpression.Arguments.Count == 1
                && methodCallExpression.Object != null)
            {
                left = methodCallExpression.Object;
                right = methodCallExpression.Arguments[0];
            }
            else if (methodCallExpression.Method.Name == nameof(object.Equals)
                && methodCallExpression.Arguments.Count == 2
                && methodCallExpression.Arguments[0].Type == methodCallExpression.Arguments[1].Type)
            {
                left = methodCallExpression.Arguments[0];
                right = methodCallExpression.Arguments[1];
            }

            if (left != null && right != null && left.Type == right.Type)
            {
                if (left is SqlExpression leftSql)
                {
                    if (!(right is SqlExpression))
                    {
                        right = new SqlExpression(right, leftSql.TypeMapping);
                    }
                }
                else if (right is SqlExpression rightSql)
                {
                    left = new SqlExpression(left, rightSql.TypeMapping);
                }

                return new SqlExpression(Expression.Equal(left, right), true);
            }

            return null;
        }
    }
}
