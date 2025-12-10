using Microsoft.CodeAnalysis.Text;

namespace AwesomeAnalyzer
{
    public sealed class TypesInformation
    {
        public TypesInformation(
            SortVirtualizationVisitor.Types type,
            string name,
            TextSpan fullSpan,
            int modifiersOrder,
            string className
        )
        {
            Type = type;
            Name = name;
            FullSpan = fullSpan;
            ModifiersOrder = modifiersOrder;
            ClassName = className;
        }

        public string ClassName { get; set; }

        public TextSpan FullSpan { get; set; }

        public int ModifiersOrder { get; set; }

        public string Name { get; set; }

        public int Order => (((int)Type) * 1000) + ModifiersOrder;

        public SortVirtualizationVisitor.Types Type { get; set; }
    }
}