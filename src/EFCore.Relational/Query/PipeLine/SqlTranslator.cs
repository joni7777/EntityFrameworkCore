// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
        private readonly SelectExpression _selectExpression;
        private readonly TypeMappingInferringExpressionVisitor _typeInference;

        public RelationalSqlTranslatingExpressionVisitor(
            IRelationalTypeMappingSource typeMappingSource, SelectExpression selectExpression)
        {
            _typeInference = new TypeMappingInferringExpressionVisitor();
            _typeMappingSource = typeMappingSource;
            _selectExpression = selectExpression;
        }

        public SqlExpression Translate(Expression expression, bool condition)
        {
            var translation = Visit(expression);

            var sqlExpression = translation as SqlExpression;

            if (sqlExpression == null)
            {
                sqlExpression = new SqlExpression(translation, _typeMappingSource.FindMapping(translation.Type));
            }

            if (condition
                && !sqlExpression.IsCondition
                && sqlExpression.TypeMapping == _typeMappingSource.FindMapping(typeof(bool)))
            {
                sqlExpression = new SqlExpression(sqlExpression.Expression, true);
            }

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
                if (firstArgument is EntityShaperExpression entityShaper)
                {
                    var entityType = entityShaper.EntityType;
                    var property = entityType.FindProperty((string)((ConstantExpression)methodCallExpression.Arguments[1]).Value);

                    return _selectExpression.BindProperty(entityShaper.ValueBufferExpression, property);
                }
            }

            return base.VisitMethodCall(methodCallExpression);
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
}
