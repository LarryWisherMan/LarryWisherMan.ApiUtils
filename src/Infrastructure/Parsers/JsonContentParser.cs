using System;
using System.Linq;
using LarryWisherMan.ApiUtils.Domain.Interfaces;

namespace LarryWisherMan.ApiUtils.Infrastructure.Parsers
{
    /// <summary>
    /// JSON content parser with explicit array handling
    /// </summary>
    public class JsonContentParser : IContentParser
    {
        public bool CanParse(string contentType)
        {
            return contentType?.IndexOf("json", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public object Parse(string content, string contentType)
        {
            try
            {
                // Check if content starts with '[' to detect arrays
                var trimmedContent = content?.Trim();
                var isArray = !string.IsNullOrEmpty(trimmedContent) && trimmedContent.StartsWith("[");

                using var ps = System.Management.Automation.PowerShell.Create();

                if (isArray)
                {
                    // For arrays, use a different approach to ensure we get all items
                    ps.AddScript($@"
                        $json = '{content.Replace("'", "''")}' | ConvertFrom-Json
                        if ($json -is [array]) {{
                            ,$json  # Force array preservation with unary comma operator
                        }} else {{
                            $json
                        }}
                    ");
                }
                else
                {
                    // For objects, use the standard approach
                    ps.AddScript($"'{content.Replace("'", "''")}' | ConvertFrom-Json");
                }

                var results = ps.Invoke();
                return results.Count > 0 ? results[0] : content;
            }
            catch
            {
                return content;
            }
        }
    }
}
