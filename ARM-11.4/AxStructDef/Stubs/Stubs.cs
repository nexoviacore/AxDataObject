
using AxStructDef.Models;

namespace AxStructDef.Stubs
{
    public class Stubs
    {
    

        // Stub for Delphi IXMLNode
        public interface IXmlNode
        {
            string Name { get; }
            string InnerText { get; }
            Dictionary<string, string> Attributes { get; }
        }

        // Stub for Delphi TEnterFieldProc (callback/procedure pointer)
        public delegate void EnterFieldProc(TField field);

        // Stub for Delphi TPickListMode (enum in Delphi)
        public enum PickListMode
        {
            None = 0,
            Single = 1,
            Multiple = 2
            // Add values as per Delphi definition
        }

    }
}