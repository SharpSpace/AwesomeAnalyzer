using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace AwesomeAnalyzer
{
    public sealed class ClassInformation
    {
        public List<ClassInformation> BaseClasses { get; set; }

        public string ClassName { get; set; }

        public TextSpan FullSpan { get; set; }

        public string IdentifierName => $"{this.NameSpaceName}.{this.ClassName}";

        public string NameSpaceName { get; set; }
    }
}