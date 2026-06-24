using System;

namespace Emerald.Runtime.Permissions
{
    [AttributeUsage(AttributeTargets.Field)]
    public class PermissionAttribute : Attribute
    {
        public PermissionAttribute(string slug)
        {
            Slug = slug;
        }

        public readonly string Slug;
    }
}