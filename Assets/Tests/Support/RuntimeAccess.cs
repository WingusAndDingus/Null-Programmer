using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using UnityEngine;
using UnityEngine.Assemblies;

namespace NullProgrammer.Tests
{
    // The project uses Unity's predefined assemblies. Test asmdefs cannot reference
    // Assembly-CSharp, so resolve the real compiled types at this boundary instead
    // of moving gameplay scripts (and changing serialized assembly identities).
    public static class RuntimeAccess
    {
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic |
                                           BindingFlags.Instance | BindingFlags.Static;

        public static Type Type(string name)
        {
            foreach (Assembly assembly in CurrentAssemblies.GetLoadedAssemblies()) // Uses Unity's safe built-in alternative to avoid leaks/errors from stale assemblies
            {
                Type type = assembly.GetType(name);
                if (type != null) return type;
            }
            throw new InvalidOperationException("Project type was not compiled: " + name);
        }

        public static object New(string name) => Activator.CreateInstance(Type(name));
        public static Component Add(GameObject owner, string name) => owner.AddComponent(Type(name));
        public static object Enum(string name, int value) => System.Enum.ToObject(Type(name), value);

        public static T Get<T>(object target, string name)
        {
            Type type = target as Type ?? target.GetType();
            object instance = target is Type ? null : target;
            FieldInfo field = type.GetField(name, Flags);
            if (field != null) return (T)field.GetValue(instance);
            PropertyInfo property = type.GetProperty(name, Flags);
            if (property != null) return (T)property.GetValue(instance);
            throw new MissingMemberException(type.FullName, name);
        }

        public static void Set(object target, string name, object value)
        {
            Type type = target as Type ?? target.GetType();
            FieldInfo field = type.GetField(name, Flags);
            if (field == null) throw new MissingFieldException(type.FullName, name);
            field.SetValue(target is Type ? null : target, value);
        }

        public static object Call(object target, string name, params object[] args)
        {
            Type type = target as Type ?? target.GetType();
            MethodInfo method = type.GetMethod(name, Flags);
            if (method == null) throw new MissingMethodException(type.FullName, name);
            try { return method.Invoke(target is Type ? null : target, args); }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }
    }
}
