using Microsoft.AspNetCore.Mvc;

namespace NerdDinner.Web.Common
{
    public static class Extensions
    {
         public static string GetLocalUrl(this IUrlHelper urlHelper, string localUrl)
        {
            if (!urlHelper.IsLocalUrl(localUrl))
            {
                return urlHelper.Page("/Index");
            }

            return localUrl;
        }
    }
}