using System;
using System.Linq;
using CsvHelper.Configuration.Attributes;
using Mono.Cecil;

namespace PublishInternalApiUsage
{
    public class DictionaryKey : IEquatable<DictionaryKey>
    {
        public DictionaryKey(MemberReference memberReference)
        {
            TypeNamespace = memberReference.DeclaringType.Namespace;
            TypeName = memberReference.DeclaringType.FullName;

            if (memberReference is MethodReference methodReference)
            {
                MemberReferenceKind = "Method";

                var returnType = methodReference.ReturnType;
                var parameterString = string.Join(", ", methodReference.Parameters.Select(pd => $"{pd.ParameterType.FullName}"));
                Name = $"{memberReference.Name}({parameterString}) : {returnType.FullName}";
            }
            else if (memberReference is FieldReference fieldReference)
            {
                MemberReferenceKind = "Field";

                var returnType = fieldReference.FieldType;
                Name = $"{memberReference.Name} : {returnType.FullName}";
            }
            else
            {
                MemberReferenceKind = "Unknown";
                Name = $"{memberReference.FullName}";
            }
        }

        [Index(0)]
        public string TypeNamespace { get; set; }

        [Index(1)]
        public string TypeName { get; set; }

        [Index(1)]
        public string MemberReferenceKind { get; set; }

        [Index(2)]
        public string Name { get; set; }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)2166136261;
                hash = (hash ^ TypeNamespace.GetHashCode()) * 16777619;
                hash = (hash ^ TypeName.GetHashCode()) * 16777619;
                hash = (hash ^ MemberReferenceKind.GetHashCode()) * 16777619;
                hash = (hash ^ Name.GetHashCode()) * 16777619;
                return hash;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is DictionaryKey other &&
                   TypeNamespace.Equals(other.TypeNamespace, StringComparison.OrdinalIgnoreCase) &&
                   TypeName.Equals(other.TypeName, StringComparison.OrdinalIgnoreCase) &&
                   MemberReferenceKind.Equals(other.MemberReferenceKind, StringComparison.OrdinalIgnoreCase) &&
                   Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase);
        }

        public bool Equals(DictionaryKey other)
        {
            return TypeNamespace.Equals(other.TypeNamespace, StringComparison.OrdinalIgnoreCase) &&
                   TypeName.Equals(other.TypeName, StringComparison.OrdinalIgnoreCase) &&
                   MemberReferenceKind.Equals(other.MemberReferenceKind, StringComparison.OrdinalIgnoreCase) &&
                   Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
