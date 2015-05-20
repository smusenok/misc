using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Temp
{
    class Program
    {
        static void Main()
        {
            var method = GetInvoker(typeof(My).GetMethod("Get"));
            var param = new object[] { 500, 0, 1 };
            try { method(new My(), param); } catch { }
            Console.WriteLine("{0}, {1}, {2}", param[0],param[1],param[2]);
        }

        static Action<object, object[]> GetInvoker(MethodInfo method)
        {
            var instanceType = method.DeclaringType;
            var paramInfos = method.GetParameters();
            var instance = Expression.Parameter(typeof(object));

            var outputParameters = Expression.Parameter(typeof(object[]));
            var variables = new ParameterExpression[paramInfos.Length];
            var preAssigns = new Expression[paramInfos.Length];
            var postAssigns = new Expression[paramInfos.Length];

            for (var i = 0; i < paramInfos.Length; i++)
            {
                var paramType = paramInfos[i].ParameterType;
                if (paramType.IsByRef)
                    paramType = paramType.GetElementType();

                variables[i] = Expression.Parameter(paramType);
                preAssigns[i] = Expression.Assign(variables[i],
                    Expression.Convert(
                        Expression.ArrayAccess(outputParameters,
                            Expression.Constant(i)), paramType));
                postAssigns[i] = Expression.Assign(
                    Expression.ArrayAccess(outputParameters,
                        Expression.Constant(i)),
                    Expression.Convert(variables[i], typeof(object)));
            }

            return Expression.Lambda<Action<object, object[]>>(
                Expression.Block(
                variables,
                    Expression.Block(Enumerable.Empty<ParameterExpression>(), preAssigns),
                    Expression.TryFinally(
                        Expression.Call(instanceType == null ? null : Expression.Convert(instance, instanceType), method, variables),
                        Expression.Block(Enumerable.Empty<ParameterExpression>(), postAssigns)))
                , instance, outputParameters)
                .Compile();
        }

        class My
        {
            public void Get(int input, out int result, ref int refval)
            {
                refval++;
                result = 1000000 + input;
                throw new Exception();
            }
        }
    }
}
