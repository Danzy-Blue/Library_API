using Microsoft.AspNetCore.Mvc.ActionConstraints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library_API.Helpers
{
    public class RequestHeaderMatchesMediaTypeAttribute : Attribute, IActionConstraint
    {
        private readonly string requestHeaderToMatch;
        private readonly string[] mediaTypes;

        public RequestHeaderMatchesMediaTypeAttribute(string requestHeaderToMatch, string[] mediaTypes)
        {
            this.requestHeaderToMatch = requestHeaderToMatch;
            this.mediaTypes = mediaTypes;
        }

        public int Order
        {
            get
            {
                return 0;
            }
        }

        public bool Accept(ActionConstraintContext context)
        {
            var requestHeaders = context.RouteContext.HttpContext.Request.Headers;
            if (!requestHeaders.ContainsKey(requestHeaderToMatch))
            {
                return false;
            }

            foreach (var mediaType in mediaTypes)
            {
                var mediaTypeMatches = string.Equals(requestHeaders[requestHeaderToMatch], mediaType, StringComparison.OrdinalIgnoreCase);
                if (mediaTypeMatches)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
