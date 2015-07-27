using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace GitHubConsole.CachedGitHub
{
    public static class XDeserializer
    {
        public static T Deserialize<T>(XElement element)
        {
            return (T)deserialize(element, typeof(T));
        }

        private static object deserialize(XElement element, Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>))
                return deserializeList(element, type);

            var obj = Activator.CreateInstance(type);

            var properties = type.GetProperties(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.GetProperty |
                BindingFlags.SetProperty).Where(x => x.GetIndexParameters().Length == 0).ToArray();

            foreach (var p in properties)
            {
                string n = p.Name.ToLower();
                XElement e = element.Element(n);

                if (e != null)
                    p.SetMethod.Invoke(obj, new object[] { get(p.PropertyType, e) });
            }

            return obj;
        }

        private static object get(Type type, XElement e)
        {
            if (type == typeof(Uri)) return new Uri(e.Value);
            else if (type == typeof(string)) return e.Value;

            else if (type == typeof(Int32)) return Int32.Parse(e.Value);
            else if (type == typeof(Int64)) return Int64.Parse(e.Value);
            else if (type == typeof(Boolean)) return Boolean.Parse(e.Value);
            else if (type == typeof(DateTimeOffset)) return DateTimeOffset.Parse(e.Value);

            else if (type.IsEnum) return Enum.Parse(type, e.Value);

            else if (IsNullable(type))
                return get(Nullable.GetUnderlyingType(type), e);

            else if (type.IsValueType)
                throw new InvalidOperationException("Unsupported type: " + type.Name);
            else return deserialize(e, type);
        }

        private static bool IsNullable(Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }

        private static object deserializeList(XElement element, Type type)
        {
            type = type.GenericTypeArguments[0];
            var listtype = typeof(List<>).MakeGenericType(type);
            var list = Activator.CreateInstance(listtype);

            var add = listtype.GetMethod("Add", new Type[] { type });
            foreach (var e in element.Elements())
                add.Invoke(list, new object[] { deserialize(e, type) });

            listtype = typeof(ReadOnlyCollection<>).MakeGenericType(type);
            return Activator.CreateInstance(listtype, list);
        }
    }
}
