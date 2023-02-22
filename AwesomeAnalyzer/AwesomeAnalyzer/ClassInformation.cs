using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace AwesomeAnalyzer
{
    public class ClassInformation
    {
        public string ClassName { get; set; }

        public string NameSpaceName { get; set; }

        public List<ClassInformation> BaseClasses { get; set; }

        public string IdentifierName => $"{this.NameSpaceName}.{this.ClassName}";

        public TextSpan FullSpan { get; set; }
    }
}