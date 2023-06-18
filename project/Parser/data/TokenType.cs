namespace Parser.data
{
    internal class NullToken : TokenAPI
    {
        public string Name { get; init; }
        public uint Level { get; init; }
        public NullToken()
        {
            Name = string.Empty;
            Level = 0;
        }
    }

    internal class Token : TokenAPI
    {
        public string Name { get; init; }
        public uint Level { get; init; }
        public Token(string name, uint level)
        {
            Name = name;
            Level = level;
        }
    }

    internal class TaggedValue : TaggedValueAPI
    {
        public string Name { get; init; }
        public uint Level { get; init; }
        public string Operator { get; init; }
        public string Tag { get; init; }
        public List<string> Value { get; private set; }
        public TaggedValue(string name, uint level, string operat, string tag)
        {
            Name = name;
            Level = level;
            Operator = operat;
            Tag = tag;
            Value = new();
        }
        public void Append(string value)
        {
            Value.Add(value);
        }
    }

    internal class ValueArray : ValueArrayAPI
    {
        public string Name { get; init; }
        public uint Level { get; init; }
        public List<List<string>> Value { get; private set; }
        public ValueArray(string name, uint level)
        {
            Name = name;
            Level = level;
            Value = new();
        }
        public void Append(string value)
        {
            Value.LastOrDefault()?.Add(value);
        }
        public void AppendNew(string value)
        {
            Value.Add(new() { value });
        }
    }

    internal class TagArray : TagArrayAPI
    {
        public string Name { get; init; }
        public uint Level { get; init; }
        public List<List<KeyValuePair<string, List<string>>>> Value { get; private set; }
        public TagArray(string name, uint level)
        {
            Name = name;
            Level = level;
            Value = new();
        }
        public void Append(string value)
        {
            Value.LastOrDefault()?.LastOrDefault().Value.Add(value);
        }
        public void AppendTag(string value)
        {
            Value.LastOrDefault()?.Add(new(value, new()));
        }
        public void AppendNew(string value)
        {
            Value.Add(new() { new(value, new()) });
        }
    }

    internal class Scope : ScopeAPI
    {
        public string Name { get; init; }
        public uint Level { get; init; }
        public List<TokenAPI> Property { get; private set; }
        public Scope(string name, uint level)
        {
            Name = name;
            Level = level;
            Property = new();
        }
        public void Append(TokenAPI? property)
        {
            if (property == null)
                return;
            if (property.Level != Level + 1)
            {
                Exceptions.Exception("level mismatched of Appending in Scope");
                return;
            }
            Property.Add(property);
        }
    }
}
