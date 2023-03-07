using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace AwesomeAnalyzer
{
    public sealed class ClassInformation
    {
        public List<ClassInformation> BaseClasses { get; set; } = new List<ClassInformation>();

        public string ClassName { get; set; }

        public TextSpan FullSpan { get; set; }

        public string IdentifierName => string.IsNullOrWhiteSpace(NameSpaceName) ? ClassName : $"{NameSpaceName}.{ClassName}";

        public string NameSpaceName { get; set; }
    }
}