using System;
using System.IO;
using System.Net;
using System.Xml;

namespace Chummer
{
    // Modern .NET requires an explicit resolver for the sheets' xsl:include elements.
    // Resolve only files within the selected template directory, including language subfolders.
    internal sealed class BundledXmlResolver : XmlResolver
    {
        private readonly string _strRoot;
        public BundledXmlResolver(string root) => _strRoot = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
        public override ICredentials Credentials { set { } }
        public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
        {
            if (!absoluteUri.IsFile) throw new XmlException("Character sheets require local template files.");
            string strPath = Path.GetFullPath(absoluteUri.LocalPath);
            if (!strPath.StartsWith(_strRoot, StringComparison.Ordinal))
                throw new XmlException("Template include is outside the template folder.");
            return File.OpenRead(strPath);
        }
    }
}
