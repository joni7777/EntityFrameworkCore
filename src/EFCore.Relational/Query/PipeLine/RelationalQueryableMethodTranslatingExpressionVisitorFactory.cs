// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query.PipeLine;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.Relational.Query.PipeLine
{
    public class RelationalQueryableMethodTranslatingExpressionVisitorFactory : IQueryableMethodTranslatingExpressionVisitorFactory
    {
        private readonly IRelationalTypeMappingSource _typeMappingSource;
        private readonly IMethodCallTranslatorProvider _methodCallTranslatorProvider;

        public RelationalQueryableMethodTranslatingExpressionVisitorFactory(IRelationalTypeMappingSource typeMappingSource,
            IMethodCallTranslatorProvider methodCallTranslatorProvider)
        {
            _typeMappingSource = typeMappingSource;
            _methodCallTranslatorProvider = methodCallTranslatorProvider;
        }

        public QueryableMethodTranslatingExpressionVisitor Create(IDictionary<Expression, Expression> parameterBindings)
        {
            return new RelationalQueryableMethodTranslatingExpressionVisitor(_typeMappingSource, _methodCallTranslatorProvider);
        }
    }
}
