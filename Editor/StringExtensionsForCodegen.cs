#if UNITY_EDITOR
namespace StringExtensionsForCodegen
{
    public static class StringExtensionsForCodegen
    {
        public static string WithOffset(this string source, int offset)
        {
            for(int i = 0; i < offset; i++)
                source = "\t" + source;
            return source;
        }
        public static string GetMark(this string markName) => "<#" + markName + "#>";
    }
}
#endif