﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace Glimpse.TelerikDataAccess.Plugin.Tracing
{
    public static class Interfacer
    {
        public static bool WireUp(string where, object instance)
        {
            string[] pieces = where.Split(':');
            foreach (var z in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = z.GetType(pieces[0], false);
                if (t != null)
                {
                    var m = t.GetMember(pieces[1], BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
                    if (m != null && m.Length > 0)
                    {
                        var pi = m[0] as PropertyInfo;
                        if (pi != null)
                        {
                            pi.SetValue(null, Interfacer.Create(pi.PropertyType, instance, OnError), null);
                            return true;
                        }
                        var fi = m[0] as FieldInfo;
                        if (fi != null)
                        {
                            fi.SetValue(null, Interfacer.Create(fi.FieldType, instance, OnError));
                            return true;
                        }
                        throw new NotImplementedException("Neither a property nor a field: " + where);
                    }
                }
            }
            throw new ArgumentOutOfRangeException("where", where, "Specified property/field not resolvable.");
        }

        private static void OnError(MethodInfo missing, Type t)
        {
            //throw new Exception("Missing interface method " + missing.Name);
        }

        public static T As<T>(object instance) where T : class
        {
            return (T)Create(typeof(T), instance);
        }

        public static object Create(Type targetType, object instance, Action<MethodInfo, Type> onError = null)
        {
            if (targetType == null)
                throw new ArgumentNullException("targetType");
            if (targetType.IsPublic == false)
                throw new ArgumentOutOfRangeException("targetType", targetType.FullName, "TargetType is not public.");
            if ((targetType.IsInterface || targetType.IsAbstract) == false)
                throw new ArgumentOutOfRangeException("targetType", targetType.FullName, "TargetType is not an interface or abstract type.");
            if (instance == null)
                throw new ArgumentNullException("instance");
            Type instanceType = instance.GetType();
            if (instanceType.IsPublic == false)
                throw new ArgumentOutOfRangeException("instance", instanceType.FullName, "Instance not of a public type.");
            if (targetType.IsAssignableFrom(instanceType))
                return instance;

            Tuple<Type, Type> key = new Tuple<Type, Type>(targetType, instanceType);
            Type wrapperType;

            lock (_wrappers)
            {
                if (_wrappers.TryGetValue(key, out wrapperType) == false)
                    _wrappers.Add(key, wrapperType = InterfaceWrapper.GenerateWrapper(key.Item1, key.Item2, onError));
            }
            var result = Activator.CreateInstance(wrapperType);
            var ifwb = result as IInterfaceWrapper;
            return ifwb.SetWrapped(instance);
        }

        private static readonly Dictionary<Tuple<Type,Type>, Type> _wrappers = new Dictionary<Tuple<Type,Type>,Type>();

        class MethodComparer : IEqualityComparer<MethodInfo>
        {
            public bool Equals(MethodInfo x, MethodInfo y)
            {
                if (x.Name.Equals(y.Name) == false) return false;
                if (x.ReturnType != y.ReturnType) return false;
                var xp = x.GetParameters();
                var yp = y.GetParameters();
                if (xp.Length != yp.Length) return false;
                for (int i = 0; i < xp.Length; i++)
                {
                    if (xp[i].ParameterType != yp[i].ParameterType)
                        return false;
                }
                return true;
            }

            public int GetHashCode(MethodInfo obj)
            {
                return obj.Name.GetHashCode() ^ obj.ReturnType.GetHashCode();
            }
        }       

        internal class InterfaceWrapper
        {        
            private static readonly ModuleBuilder _moduleBuilder;

            static InterfaceWrapper()
            {
                var assembly = Thread.GetDomain().DefineDynamicAssembly(new AssemblyName("InterfaceWrapper"), AssemblyBuilderAccess.Run);
                _moduleBuilder = assembly.DefineDynamicModule("InterfaceWrapperModule", false);
            }

            internal static Type GenerateWrapper(Type resultType, Type instanceType, Action<MethodInfo, Type> onError)
            {
                var wrapperName = string.Format("{0}_Wraps_{1}", resultType.Name, instanceType.Name);

                TypeBuilder wrapperBuilder;
                if (resultType.IsInterface)
                {
                    wrapperBuilder = _moduleBuilder.DefineType(wrapperName,
                                                                TypeAttributes.NotPublic | TypeAttributes.Sealed,
                                                                typeof(InterfaceWrapperBase),
                                                                new[] { resultType });
                    var wrapperMethod = new WrapperMethodBuilder(instanceType, wrapperBuilder, onError, false);

                    foreach (MethodInfo method in AllInterfaceMethods(resultType))
                    {
                        wrapperMethod.GenerateWrappingMethod(method);
                    }
                }
                else
                {
                    wrapperBuilder = _moduleBuilder.DefineType(wrapperName,
                                                                TypeAttributes.NotPublic | TypeAttributes.Sealed,
                                                                resultType,
                                                                new[] { typeof(IInterfaceWrapper) });
                    var wrapperMethod = new WrapperMethodBuilder(instanceType, wrapperBuilder, onError, true);

                    foreach (MethodInfo method in AllVirtualMethods(resultType))
                    {
                        wrapperMethod.GenerateWrappingMethod(method);
                    }
                    wrapperMethod.GenerateWrappingMethod(typeof(IInterfaceWrapper).GetMethod("SetWrapped"), false);
                }
                return wrapperBuilder.CreateType();
            }

            private static IEnumerable<Type> AllInterfaces(Type target)
            {
                foreach (var IF in target.GetInterfaces())
                {
                    yield return IF;
                    foreach (var childIF in AllInterfaces(IF))
                    {
                        yield return childIF;
                    }
                }
            }

            private static IEnumerable<MethodInfo> AllInterfaceMethods(Type target)
            {
                var allTypes = AllInterfaces(target).ToList();
                allTypes.Add(target);

                return from type in allTypes
                       from method in type.GetMethods()
                       select method;
            }

            private static IEnumerable<MethodInfo> AllVirtualMethods(Type target)
            {
                if (target != null)
                {
                    var ms = target.GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(x => x.IsVirtual).ToList();
                    return ms;
                }
                return new MethodInfo[0];
            }
        }

        internal class WrapperMethodBuilder
        {
            private readonly Type _wrappedType;
            private readonly TypeBuilder _wrapperBuilder;
            private readonly Dictionary<MethodInfo, MethodInfo> _implementedMethods;
            private readonly ConstructorInfo _stepThrough;
            private readonly MethodInfo _defaultMethod;
            private readonly Action<MethodInfo, Type> _onError;
            private readonly FieldInfo _wrapperField;
            private readonly bool _fromClass;
            private readonly static MethodInfo _standardMethod = typeof(InterfaceWrapperBase).GetMethod("MissingInterfaceMethod", BindingFlags.Instance | BindingFlags.NonPublic);

            internal WrapperMethodBuilder(Type realObjectType, TypeBuilder wrapperBuilder, Action<MethodInfo, Type> onError, bool fromClass)
            {
                _wrappedType = realObjectType;
                _wrapperBuilder = wrapperBuilder;
                _fromClass = fromClass;
                _implementedMethods = _wrappedType.GetMethods(BindingFlags.Public | BindingFlags.Instance).ToDictionary(mi => mi, new MethodComparer());
                _stepThrough = typeof(System.Diagnostics.DebuggerStepThroughAttribute).GetConstructor(Type.EmptyTypes);
                _defaultMethod = _wrappedType.GetMethod("MissingInterfaceMethod", BindingFlags.Instance | BindingFlags.Public);
                _onError = onError;
                if (_fromClass)
                {
                    _wrapperField = _wrapperBuilder.DefineField("wrapper", _wrappedType, FieldAttributes.FamORAssem);
                }
                else
                    _wrapperField = typeof(InterfaceWrapperBase).GetField("wrapped", BindingFlags.Instance | BindingFlags.NonPublic);
            }

            internal void GenerateWrappingMethod(MethodInfo newMethod, bool existing = true)
            {
                if (newMethod.IsGenericMethod)
                    newMethod = newMethod.GetGenericMethodDefinition();

                MethodInfo implemented;
                _implementedMethods.TryGetValue(newMethod, out implemented);

                var parameters = newMethod.GetParameters();
                var parameterTypes = parameters.Select(parameter => parameter.ParameterType).ToArray();

                var methodBuilder = _wrapperBuilder.DefineMethod(
                    newMethod.Name,
                    MethodAttributes.Public | (_fromClass & existing ? MethodAttributes.ReuseSlot | MethodAttributes.Virtual : MethodAttributes.Virtual),
                    newMethod.ReturnType,
                    parameterTypes);

                if (newMethod.IsGenericMethod)
                {
                    methodBuilder.DefineGenericParameters(
                        newMethod.GetGenericArguments().Select(arg => arg.Name).ToArray());
                }
                methodBuilder.SetCustomAttribute(_stepThrough, new byte[0]);

                ILGenerator ilGenerator = methodBuilder.GetILGenerator();
                if (existing == false)
                {
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    ilGenerator.Emit(OpCodes.Ldarg_1);
                    ilGenerator.Emit(OpCodes.Stfld, _wrapperField);
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                }
                else if (implemented == null)
                {
                    if (_onError != null)
                        _onError(newMethod, _wrappedType);

                    var loc = ilGenerator.DeclareLocal(typeof(object[]));
                    ilGenerator.Emit(OpCodes.Ldc_I4, parameters.Length);
                    ilGenerator.Emit(OpCodes.Newarr, typeof(object));
                    ilGenerator.Emit(OpCodes.Stloc, loc);
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        ilGenerator.Emit(OpCodes.Ldloc, loc);
                        ilGenerator.Emit(OpCodes.Ldc_I4, i);
                        ilGenerator.Emit(OpCodes.Ldarg, i+1);
                        if (parameters[i].ParameterType.IsValueType)
                        {
                            ilGenerator.Emit(OpCodes.Box, parameters[i].ParameterType);
                            ilGenerator.Emit(OpCodes.Stelem_Ref);
                        }
                        else
                        {
                            ilGenerator.Emit(OpCodes.Stelem_Ref);
                        }
                    }
                    var rt = newMethod.ReturnType == typeof(void) ? typeof(object) : newMethod.ReturnType;
                    if (_defaultMethod != null)
                    {
                        ilGenerator.Emit(OpCodes.Ldarg_0);
                        ilGenerator.Emit(OpCodes.Ldfld, _wrapperField);
                        ilGenerator.Emit(OpCodes.Ldstr, newMethod.Name);
                        ilGenerator.Emit(OpCodes.Ldloc, loc);
                        ilGenerator.Emit(OpCodes.Call, _defaultMethod.MakeGenericMethod(rt));
                    }
                    else
                    {
                        ilGenerator.Emit(OpCodes.Ldstr, newMethod.Name);
                        ilGenerator.Emit(OpCodes.Ldloc, loc);
                        ilGenerator.Emit(OpCodes.Call, _standardMethod.MakeGenericMethod(rt));
                    }
                    if (rt != newMethod.ReturnType)
                        ilGenerator.Emit(OpCodes.Pop);
                }
                else
                {
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    ilGenerator.Emit(OpCodes.Ldfld, _wrapperField);
                    for (int i = 1; i < parameters.Length + 1; i++)
                        ilGenerator.Emit(OpCodes.Ldarg, i);
                    ilGenerator.Emit(OpCodes.Call, implemented);
                }
                ilGenerator.Emit(OpCodes.Ret);
            }           
        }
        
        public interface IInterfaceWrapper
        {
            object SetWrapped(object o);
        }

        public class InterfaceWrapperBase : IInterfaceWrapper
        {
            internal protected object wrapped;

            internal protected T MissingInterfaceMethod<T>(string name, object[] args)
            {
                return default(T);
            }

            public object SetWrapped(object o)
            {
                wrapped = o;
                return this;
            }

            public override bool Equals(object obj)
            {
                return wrapped.Equals(obj);
            }

            public override int GetHashCode()
            {
                return wrapped.GetHashCode();
            }

            public override string ToString()
            {
                return wrapped.ToString();
            }
        }
    }
}