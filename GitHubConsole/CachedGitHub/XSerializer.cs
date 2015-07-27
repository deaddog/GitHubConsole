using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace GitHubConsole.CachedGitHub
{
    public static class XSerializer
    {
        public static XElement Serialize(string name, object o)
        {
            XElement element = new XElement(name);

            var type = o.GetType();

            var properties = type.GetProperties(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.GetProperty |
                BindingFlags.SetProperty).Where(x => x.GetIndexParameters().Length == 0).ToArray();

            foreach (var p in properties)
            {
                string n = p.Name.ToLower();
                object v = p.GetMethod.Invoke(o, null);

                if (v != null)
                {
                    if (v.GetType().IsValueType)
                        element.Add(SerializeValue(n, (dynamic)v));
                    else
                        element.Add(Serialize(n, (dynamic)v));
                }
            }

            return element;
        }

        public static XElement SerializeValue<T>(string name, T value) where T : struct
        {
            return new XElement(name, value);
        }

        public static XElement Serialize(string name, string value)
        {
            return new XElement(name, value);
        }
        public static XElement Serialize(string name, Uri value)
        {
            return new XElement(name, value.AbsoluteUri);
        }

        public static XElement Serialize<T>(string name, IReadOnlyList<T> value)
        {
            string n = name.EndsWith("s") ? name.TrimEnd('s') : name + "_item";

            return new XElement(name, value.Select(x => Serialize(n, x)));
        }
    }
}
