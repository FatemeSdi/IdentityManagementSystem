// IdentityManagementSystem.UI/Helpers/EnumHelper.cs
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace IdentityManagementSystem.UI.Helpers
{
    public static class EnumHelper
    {
        public static SelectList ToSelectList<TEnum>() where TEnum : struct, Enum
        {
            var values = Enum.GetValues(typeof(TEnum)).Cast<TEnum>();
            var items = values.Select(e => new SelectListItem
            {
                Value = ((int)(object)e).ToString(),
                Text = e.GetDisplayName()
            }).ToList();

            items.Insert(0, new SelectListItem { Value = "", Text = "-- انتخاب کنید --" });
            return new SelectList(items, "Value", "Text");
        }

        private static string GetDisplayName(this Enum enumValue)
        {
            var member = enumValue.GetType()
                                  .GetMember(enumValue.ToString())
                                  .FirstOrDefault();

            var displayAttr = member?.GetCustomAttribute<DisplayAttribute>();
            return displayAttr?.GetName() ?? enumValue.ToString();
        }
    }
}
