using CodeForge.Application.Common.Constants;

namespace CodeForge.Application.Materials.Common
{
    public static class MaterialValidationRules
    {
        public static readonly string[] ValidTypes =
        {
            MaterialTypes.File,
            MaterialTypes.Text,
            MaterialTypes.Link
        };

        public static readonly string[] ValidFileTypes = { "pdf", "ppt", "zip", "other" };
    }
}
