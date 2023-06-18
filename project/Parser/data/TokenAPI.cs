namespace Parser.data
{
    public interface TokenAPI
    {
        public abstract string Name { get; }
        public abstract uint Level { get; }
    }
    public interface TaggedValueAPI : TokenAPI
    {
        public abstract string Operator { get; }
        public abstract string Tag { get; }
        public abstract List<string> Value { get; }
    }
    public interface ValueArrayAPI : TokenAPI
    {
        public abstract List<List<string>> Value { get; }
    }
    public interface TagArrayAPI : TokenAPI
    {
        public abstract List<List<KeyValuePair<string, List<string>>>> Value { get; }
    }
    public interface ScopeAPI : TokenAPI
    {
        public abstract List<TokenAPI> Property { get; }
    }
}
