﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MicroOrm.Dapper.Repositories.Extensions;

namespace MicroOrm.Dapper.Repositories.SqlGenerator
{
    internal static class ExpressionHelper
    {
        /// <summary>
        ///     Gets the name of the property.
        /// </summary>
        /// <param name="body">The body.</param>
        /// <param name="isNested">Out. Is nested property.</param>
        /// <returns>The property name for the property expression.</returns>
        public static string GetPropertyName(BinaryExpression body, out bool isNested)
        {
            isNested = false;

            var propertyNameArray = body.Left.ToString().Split('.').Skip(1).ToArray();
            var propertyName = string.Join("", propertyNameArray);

            if (propertyNameArray.Length > 2)
                throw new ArgumentException("Only one degree of nesting is supported");

            if (propertyNameArray.Length == 2)
                isNested = true;

            if (body.Left.NodeType == ExpressionType.Convert || body.Left.NodeType == ExpressionType.Not)
                propertyName = propertyName.Replace(")", string.Empty);

            return propertyName;
        }

        public static string GetPropertyName<TSource, TField>(Expression<Func<TSource, TField>> field)
        {
            if (Equals(field, null))
                throw new NullReferenceException("Field is required");

            MemberExpression expr;

            var body = field.Body as MemberExpression;
            if (body != null)
            {
                expr = body;
            }
            else
            {
                var expression = field.Body as UnaryExpression;
                if (expression != null)
                    expr = (MemberExpression)expression.Operand;
                else
                    throw new ArgumentException("Expression" + field + " is not supported.", nameof(field));
            }

            return expr.Member.Name;
        }

        public static object GetValue(Expression member)
        {
            var objectMember = Expression.Convert(member, typeof(object));
            var getterLambda = Expression.Lambda<Func<object>>(objectMember);
            var getter = getterLambda.Compile();
            return getter();
        }

        public static string GetSqlOperator(ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.Equal:
                    return "=";

                case ExpressionType.NotEqual:
                    return "!=";

                case ExpressionType.LessThan:
                    return "<";

                case ExpressionType.LessThanOrEqual:
                    return "<=";

                case ExpressionType.GreaterThan:
                    return ">";

                case ExpressionType.GreaterThanOrEqual:
                    return ">=";

                case ExpressionType.AndAlso:
                case ExpressionType.And:
                    return "AND";

                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    return "OR";

                case ExpressionType.Default:
                    return string.Empty;

                default:
                    throw new NotImplementedException();
            }
        }

        public static BinaryExpression GetBinaryExpression(Expression expression)
        {
            var binaryExpression = expression as BinaryExpression;
            var body = binaryExpression ?? Expression.MakeBinary(ExpressionType.Equal, expression, expression.NodeType == ExpressionType.Not ? Expression.Constant(false) : Expression.Constant(true));
            return body;
        }

        public static Func<PropertyInfo, bool> GetPrimitivePropertiesPredicate()
        {
            return p => p.CanWrite && (p.PropertyType.IsValueType() || p.PropertyType == typeof(string) || p.PropertyType == typeof(byte[]));
        }
    }
}