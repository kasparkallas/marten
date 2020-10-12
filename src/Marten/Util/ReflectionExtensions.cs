using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Baseline;

namespace Marten.Util
{
    public static class ReflectionExtensions
    {
        internal static readonly Dictionary<Type, string> Aliases = new Dictionary<Type, string>
        {
            {typeof(int), "int"},
            {typeof(void), "void"},
            {typeof(string), "string"},
            {typeof(long), "long"},
            {typeof(double), "double"},
            {typeof(bool), "bool"},
            {typeof(Task), "Task"},
            {typeof(object), "object"},
            {typeof(object[]), "object[]"}
        };

        public static string ToTableAlias(this MemberInfo[] members)
        {
            return members.Select(x => x.ToTableAlias()).Join("_");
        }

        public static string ToTableAlias(this MemberInfo member)
        {
            return member.Name.ToTableAlias();
        }

        public static string ToTableAlias(this string name)
        {
            return name.SplitPascalCase().ToLower().Replace(" ", "_");
        }

        public static Type GetMemberType(this MemberInfo member)
        {
            Type rawType = null;

            if (member is FieldInfo)
                rawType = member.As<FieldInfo>().FieldType;
            if (member is PropertyInfo)
                rawType = member.As<PropertyInfo>().PropertyType;

            return rawType.IsNullable() ? rawType.GetInnerTypeFromNullable() : rawType;
        }

        public static Type GetRawMemberType(this MemberInfo member)
        {
            Type rawType = null;

            if (member is FieldInfo)
                rawType = member.As<FieldInfo>().FieldType;
            if (member is PropertyInfo)
                rawType = member.As<PropertyInfo>().PropertyType;

            return rawType;
        }

        public static MemberInfo GetPublicPropertyOrField(this Type type, string memberName)
        {
            return type.GetPublicMembersFromTypeHierarchy(
                BindingFlags.GetProperty | BindingFlags.GetField
            ).Cast<MemberInfo>().FirstOrDefault(p => p.Name == memberName);
        }

        public static MemberInfo[] GetPublicMembersFromTypeHierarchy(this Type type, BindingFlags bindingFlags)
        {
            if (!type.IsInterface)
            {
                return type.GetMembers(
                    bindingFlags
                    | BindingFlags.FlattenHierarchy
                    | BindingFlags.Public
                    | BindingFlags.Instance);
            }

            var memberInfos = new List<MemberInfo>();

            var considered = new List<Type>();
            var queue = new Queue<Type>();
            considered.Add(type);
            queue.Enqueue(type);
            while (queue.Count > 0)
            {
                var subType = queue.Dequeue();
                foreach (var subInterface in subType.GetInterfaces())
                {
                    if (considered.Contains(subInterface))
                        continue;

                    considered.Add(subInterface);
                    queue.Enqueue(subInterface);
                }

                var typeProperties = subType.GetMembers(
                    bindingFlags
                    | BindingFlags.FlattenHierarchy
                    | BindingFlags.Public
                    | BindingFlags.Instance);

                var newPropertyInfos = typeProperties
                    .Where(x => !memberInfos.Contains(x));

                memberInfos.InsertRange(0, newPropertyInfos);
            }

            return memberInfos.ToArray();
        }

        public static string GetPrettyName(this Type t)
        {
            if (!t.GetTypeInfo().IsGenericType)
                return t.Name;

            var sb = new StringBuilder();

            sb.Append(t.Name.Substring(0, t.Name.LastIndexOf("`", StringComparison.Ordinal)));
            sb.Append(t.GetGenericArguments().Aggregate("<",
                (aggregate, type) => aggregate + (aggregate == "<" ? "" : ",") + GetPrettyName(type)));
            sb.Append(">");

            return sb.ToString();
        }

        public static string GetTypeName(this Type type)
        {
            var typeName = type.Name;

            if (type.GetTypeInfo().IsGenericType)
                typeName = GetPrettyName(type);

            return type.IsNested
                ? $"{type.DeclaringType.Name}.{typeName}"
                : typeName;
        }

        public static string GetTypeFullName(this Type type)
        {
            return type.IsNested
                ? $"{type.DeclaringType.FullName}.{type.Name}"
                : type.FullName;
        }

        public static bool IsGenericDictionary(this Type type)
        {
            return type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>);
        }

        // http://stackoverflow.com/a/15273117/426840
        public static bool IsAnonymousType(this object instance)
        {
            if (instance == null)
                return false;

            return instance.GetType().Namespace == null;
        }

    }
}
