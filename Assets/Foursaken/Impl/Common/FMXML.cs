using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using UnityEngine;

public static class FMXML
{
    public static T Read<T>(string xml) where T : IFMXMLReadable
    {
        T t = (T)Activator.CreateInstance(typeof(T));

        XmlDocument document = new XmlDocument();
        document.LoadXml(xml);

        t.Read(document);

        return t;
    }

    public static object ReadValue(Type type, XmlNode node)
    {
        if (type == typeof(string))
        {
            return node.InnerText;
        }

        object element = Activator.CreateInstance(type);

        switch (element)
        {
            case byte:
                element = byte.Parse(node.InnerText);
                break;

            case short:
                element = short.Parse(node.InnerText);
                break;

            case int:
                element = int.Parse(node.InnerText);
                break;

            case long:
                element = long.Parse(node.InnerText);
                break;

            case sbyte:
                element = sbyte.Parse(node.InnerText);
                break;

            case ushort:
                element = ushort.Parse(node.InnerText);
                break;

            case uint:
                element = uint.Parse(node.InnerText);
                break;

            case ulong:
                element = ulong.Parse(node.InnerText);
                break;

            case float:
                element = float.Parse(node.InnerText);
                break;

            case double:
                element = double.Parse(node.InnerText);
                break;

            case decimal:
                element = decimal.Parse(node.InnerText);
                break;

            case bool:
                element = node.InnerText == "true" || node.InnerText == "1";
                break;

            default:
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    FieldInfo field = element.GetType().GetField(childNode.Name, BindingFlags.Public | BindingFlags.Instance);

                    if (field != null)
                    {
                        field.SetValue(element, ReadValue(field.FieldType, childNode));
                    }
                }
                break;
        }

        return element;
    }

    public static void Test(XmlNode node)
    {
        XmlNode parentNode = node;
        string debugString = "";

        while (true)
        {
            if (parentNode == null || parentNode.Name == "#document" || parentNode.Name == "data")
            {
                break;
            }
            
            if ((parentNode.ParentNode != null && parentNode.ParentNode.Name == "animations") || parentNode.Name.StartsWith("#"))
            {
                parentNode = parentNode.ParentNode;
                continue;
            }

            debugString = parentNode.Name + "/" + debugString;
            parentNode = parentNode.ParentNode;
        }

        Debug.LogError(debugString);

        foreach (XmlNode childNode in node.ChildNodes)
        {
            Test(childNode);
        }
    }
}

public interface IFMXMLReadable
{
    void Read(XmlNode node);
}