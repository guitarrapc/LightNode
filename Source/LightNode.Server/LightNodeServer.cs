﻿using System;
using System.Linq.Expressions;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace LightNode.Server
{
    internal class MessageContract
    {
        public string MethodName { get; set; }

        public ParameterInfo[] Arguments { get; set; }

        public Type ReturnType { get; set; }

        public MessageContractBodyType MessageContractBodyType { get; set; }

        public Func<object[], object> MethodFuncBody { get; set; } // 1

        public Func<object[], Task> MethodAsyncFuncBody { get; set; } // 2

        public Action<object[]> MethodActionBody { get; set; } // 3
        public Func<object[], Task> MethodAsyncActionBody { get; set; } // 4
    }
    internal enum MessageContractBodyType
    {
        Func = 1,
        AsyncFunc = 2,
        Action = 3,
        AsyncAction = 4
    }


    public static class LightNodeServer
    {
        // {Class,Method} => MessageContract
        readonly static Dictionary<Tuple<string, string>, MessageContract> handlers = new Dictionary<Tuple<string, string>, MessageContract>();
        readonly static ConcurrentDictionary<Type, Func<object, object>> taskResultExtractorCache = new ConcurrentDictionary<Type, Func<object, object>>();

        public static void RegisterHandler(Assembly[] hostAssemblies)
        {
            var contractTypes = hostAssemblies
                .SelectMany(x => x.GetTypes())
                .Where(x => typeof(ILightNodeContract).IsAssignableFrom(x));

            // TODO:validation, duplicate entry, non support arguments.

            foreach (var classType in contractTypes)
            {
                var className = classType.Name;
                foreach (var methodInfo in classType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    var contract = new MessageContract();

                    var methodName = methodInfo.Name;

                    contract.MethodName = methodName;
                    contract.Arguments = methodInfo.GetParameters();
                    contract.ReturnType = methodInfo.ReturnType;

                    if (typeof(Task).IsAssignableFrom(contract.ReturnType))
                    {
                        // (object[] args) => new X().M((T1)args[0], (T2)args[1])...
                        var args = Expression.Parameter(typeof(object[]), "args");

                        var parameters = methodInfo.GetParameters()
                            .Select((x, i) => Expression.Convert(Expression.ArrayIndex(args, Expression.Constant(i)), x.ParameterType))
                            .ToArray();

                        var lambda = Expression.Lambda<Func<object[], Task>>(
                            Expression.Call(
                                Expression.New(classType),
                                methodInfo,
                                parameters),
                            args);

                        if (contract.ReturnType.IsGenericType && contract.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                        {
                            contract.MessageContractBodyType = MessageContractBodyType.AsyncFunc;
                            contract.MethodAsyncFuncBody = lambda.Compile();
                        }
                        else
                        {
                            contract.MessageContractBodyType = MessageContractBodyType.AsyncAction;
                            contract.MethodAsyncActionBody = lambda.Compile();
                        }
                    }
                    else if (contract.ReturnType == typeof(void))
                    {
                        // (object[] args) => { new X().M((T1)args[0], (T2)args[1])... }
                        var args = Expression.Parameter(typeof(object[]), "args");

                        var parameters = methodInfo.GetParameters()
                            .Select((x, i) => Expression.Convert(Expression.ArrayIndex(args, Expression.Constant(i)), x.ParameterType))
                            .ToArray();

                        var lambda = Expression.Lambda<Action<object[]>>(
                            Expression.Call(
                                Expression.New(classType),
                                methodInfo,
                                parameters),
                            args);

                        contract.MessageContractBodyType = MessageContractBodyType.Action;
                        contract.MethodActionBody = lambda.Compile();
                    }
                    else
                    {
                        // (object[] args) => (object)new X().M((T1)args[0], (T2)args[1])...
                        var args = Expression.Parameter(typeof(object[]), "args");

                        var parameters = methodInfo.GetParameters()
                            .Select((x, i) => Expression.Convert(Expression.ArrayIndex(args, Expression.Constant(i)), x.ParameterType))
                            .ToArray();

                        var lambda = Expression.Lambda<Func<object[], object>>(
                            Expression.Convert(
                                Expression.Call(
                                    Expression.New(classType),
                                    methodInfo,
                                    parameters)
                            , typeof(object)),
                            args);

                        contract.MessageContractBodyType = MessageContractBodyType.Func;
                        contract.MethodFuncBody = lambda.Compile();
                    }

                    handlers.Add(Tuple.Create(className, methodName), contract);
                }
            }
        }

        public static async Task HandleRequest(IDictionary<string, object> environment)
        {
            // var path = environment["owin.Request...."];
            // URL Trim

            // TODO:get path & classname
            var key = Tuple.Create("MyClass", "Test4");

            MessageContract handler;
            if (handlers.TryGetValue(key, out handler))
            {
                // TODO:get parameters
                switch (handler.MessageContractBodyType)
                {
                    case MessageContractBodyType.Action:
                        break;
                    case MessageContractBodyType.Func:
                        // handler.MethodFuncBody(new object[] { });

                        break;
                    case MessageContractBodyType.AsyncAction:
                        var actionTask = handler.MethodAsyncActionBody(new object[] { });
                        await actionTask;
                        break;
                    case MessageContractBodyType.AsyncFunc:
                        var funcTask = handler.MethodAsyncFuncBody(new object[] { });
                        await funcTask;
                        var result = ExtractTaskResult(funcTask);
                        break;
                    default:
                        throw new InvalidOperationException("critical:register code is broken");
                }


                // invoke handler
                // set response
                // exception handling
            }
            else
            {
                // TODO:return 404 Message
            }


        }

        static object ExtractTaskResult(object targetTaskObject)
        {
            var extractor = taskResultExtractorCache.GetOrAdd(targetTaskObject.GetType(), t =>
            {
                // (object task) => (object)((Task<>).Result)
                var taskParameter = Expression.Parameter(typeof(object), "task");
                var lambda = Expression.Lambda<Func<object, object>>(
                    Expression.Convert(
                        Expression.Property(
                            Expression.Convert(taskParameter, t),
                            "Result"),
                        typeof(object)),
                    taskParameter);

                return lambda.Compile();
            });

            return extractor(targetTaskObject);
        }
    }



    public interface ILightNodeContract
    {

    }


    public class ContractOptionAttribute : Attribute
    {
        public string Name { get; private set; }


    }

    public interface ISerializer
    {

    }



}