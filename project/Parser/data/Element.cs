namespace Parser.data
{
    internal class Element
    {
        string Value { get; set; } = string.Empty;
        public bool OwnValue { get; private set; } = true;
        public uint Line { get; private set; } = 0;
        public uint Column { get; private set; } = 0;
        public Element(string value, uint line, uint column)
        {
            OwnValue = true;
            Value = value.ToString();
            Line = line;
            Column = column;
        }
        public char Head()
        {
            return Value.FirstOrDefault();
        }
        public string GetValue()
        {
            OwnValue = false;
            return Value;
        }
    }
}
