// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Relational.Query.PipeLine;

namespace Microsoft.EntityFrameworkCore.SqlServer.Query.Pipeline
{
    public class SqlServerMethodCallTranslatorProvider : RelationalMethodCallTranslatorProvider
    {
        private static readonly IMethodCallTranslator[] _methodCallTranslators =
        {
            new SqlServerStartsWithOptimizedTranslator()
        };

        public SqlServerMethodCallTranslatorProvider()
        {
            AddTranslators(_methodCallTranslators);
        }
    }

    public class SqlServerStartsWithOptimizedTranslator : IMethodCallTranslator
    {
        private static readonly MethodInfo _methodInfo
            = typeof(string).GetRuntimeMethod(nameof(string.StartsWith), new[] { typeof(string) });

        private static readonly MethodInfo _concat
            = typeof(string).GetRuntimeMethod(nameof(string.Concat), new[] { typeof(string), typeof(string) });

        public Expression Translate(MethodCallExpression methodCallExpression)
        {
            //if (Equals(methodCallExpression.Method, _methodInfo))
            //{
            //    var patternExpression = methodCallExpression.Arguments[0];

            //    var startsWithExpression = Expression.AndAlso(
            //        new LikeExpression(
            //            // ReSharper disable once AssignNullToNotNullAttribute
            //            methodCallExpression.Object,
            //            Expression.Add(
            //                methodCallExpression.Arguments[0],
            //                Expression.Constant("%", typeof(string)),
            //                _concat)),
            //        new NullCompensatedExpression(
            //            Expression.Equal(
            //                new SqlFunctionExpression(
            //                    "LEFT",
            //                    // ReSharper disable once PossibleNullReferenceException
            //                    methodCallExpression.Object.Type,
            //                    new[] { methodCallExpression.Object, new SqlFunctionExpression("LEN", typeof(int), new[] { patternExpression }) }),
            //                patternExpression)));

            //    return patternExpression is ConstantExpression patternConstantExpression
            //        ? ((string)patternConstantExpression.Value)?.Length == 0
            //            ? (Expression)Expression.Constant(true)
            //            : startsWithExpression
            //        : Expression.OrElse(
            //            startsWithExpression,
            //            Expression.Equal(patternExpression, Expression.Constant(string.Empty)));
            //}

            return null;
        }
    }
}
