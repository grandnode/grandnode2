using System.Xml;
using System.Xml.Schema;

namespace Grand.SharedKernel.Extensions;

public static class XmlExtensions
{
    public static XmlDocument LanguageXmlDocument(string xml)
    {
        var schemas = new XmlSchemaSet();
        schemas.Add("", XmlReader.Create(new StringReader(LanguageSchema.SchemaXsd)));

        var xmlDoc = new XmlDocument { Schemas = schemas, XmlResolver = null };
        //a language file has no DTD; refusing one also refuses entity expansion (e.g. "billion laughs")
        using (var reader = XmlReader.Create(new StringReader(xml),
                   new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
            xmlDoc.Load(reader);

        // Validate XML.
        xmlDoc.Validate((_, e) => throw new XmlException("XML data does not conform to the schema", e.Exception));
        return xmlDoc;
    }

    public static List<(string Name, string Value, string Area)> ParseTranslationResources(XmlDocument xmlDoc)
    {
        var resources = new List<(string Name, string Value, string Area)>();
        var nodes = xmlDoc.SelectNodes("//Language/Resource");

        if (nodes != null)
        {
            foreach (XmlNode node in nodes)
            {
                var name = node.Attributes?["Name"]?.InnerText.Trim();
                var area = node.Attributes?["Area"]?.InnerText.Trim();
                var value = "";
                var valueNode = node.SelectSingleNode("Value");
                if (valueNode != null)
                    value = valueNode.InnerText;

                if (string.IsNullOrEmpty(name))
                    continue;

                var resourceTuple = (
                    Name: name.ToLowerInvariant(),
                    Value: value,
                    Area: area
                );

                resources.Add(resourceTuple);
            }
        }
        return resources;
    }

}
