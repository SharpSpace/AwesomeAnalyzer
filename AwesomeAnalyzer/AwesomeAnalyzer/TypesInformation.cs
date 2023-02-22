using Microsoft.CodeAnalysis.Text;

namespace AwesomeAnalyzer
{
    public sealed class TypesInformation
    {
        public TypesInformation(SortVirtualizationVisitor.Types type, string name, TextSpan fullSpan, string modifiers, int modifiersOrder, string className)
        {
            Type = type;
            Name = name;
            FullSpan = fullSpan;
            Modifiers = modifiers;
            ModifiersOrder = modifiersOrder;
            ClassName = className;
        }

        public SortVirtualizationVisitor.Types Type { get; set; }

        public string Name { get; set; }

        public TextSpan FullSpan { get; set; }

        public string Modifiers { get; set; }

        public int ModifiersOrder { get; set; }

        public string ClassName { get; set; }
    }
}