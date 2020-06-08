using System.Collections.Generic;

namespace Simple.Api.Contract
{
    /// <summary>
    /// Mapping index.json.
    /// </summary>
    public class InfoDto
    {
        public int PageSize { get; set; }

        public int TotalPage { get; set; }

        public int TotalItem { get; set; }

        public IDictionary<string, object> Metadata { get; set; }
    }
}