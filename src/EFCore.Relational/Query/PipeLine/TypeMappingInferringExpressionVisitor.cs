// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.Relational.Query.PipeLine
{
    public class TypeMappingInferringExpressionVisitor : ExpressionVisitor
    {
        private RelationalTypeMapping _currentTypeMapping;

        public TypeMappingInferringExpressionVisitor()
        {
        }

        protected override Expression VisitBinary(BinaryExpression binaryExpression)
        {
            var parentTypeMapping = _currentTypeMapping;
            _currentTypeMapping = null;
            var condition = false;
            RelationalTypeMapping aggregateTypeMapping = null;


            var left = binaryExpression.Left;
            var right = binaryExpression.Right;
            switch (binaryExpression.NodeType)
            {
                case ExpressionType.Equal:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.NotEqual:
                    if (left is SqlExpression leftSql)
                    {
                        _currentTypeMapping = leftSql.TypeMapping;

                        if (!(right is SqlExpression))
                        {
                            right = Visit(right);
                        }
                    }
                    else if (right is SqlExpression rightSql)
                    {
                        _currentTypeMapping = rightSql.TypeMapping;

                        left = Visit(left);
                    }

                    condition = true;

                    break;
            }

            _currentTypeMapping = parentTypeMapping;
            var updatedBinaryExpression = binaryExpression.Update(left, binaryExpression.Conversion, right);

            return left is SqlExpression && right is SqlExpression
                ? condition
                    ? new SqlExpression(updatedBinaryExpression, condition)
                    : new SqlExpression(updatedBinaryExpression, aggregateTypeMapping)
                : (Expression)updatedBinaryExpression;
        }

        protected override Expression VisitConstant(ConstantExpression constantExpression)
        {
            return _currentTypeMapping != null
                ? new SqlExpression(constantExpression, _currentTypeMapping)
                : (Expression)constantExpression;
        }

        protected override Expression VisitParameter(ParameterExpression parameterExpression)
        {
            return _currentTypeMapping != null
                ? new SqlExpression(parameterExpression, _currentTypeMapping)
                : (Expression)parameterExpression;
        }
    }
}
