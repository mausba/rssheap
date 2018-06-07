using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Enums;

namespace System
{
    public static class EnumExtensions
    {
        public static string GetName(this ArticleActions articleAction)
        {
            return Enum.GetName(typeof(ArticleActions), articleAction);
        }
    }
}
