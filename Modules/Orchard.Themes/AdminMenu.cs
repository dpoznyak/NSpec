﻿using Orchard.Localization;
using Orchard.UI.Navigation;

namespace Orchard.Themes {
    public class AdminMenu : INavigationProvider {
        public Localizer T { get; set; }
        public string MenuName { get { return "admin"; } }

        public void GetNavigation(NavigationBuilder builder) {
            builder.Add(T("Themes"), "25",
                menu => menu.Add(T("List"), "0", item => item.Action("Index", "Admin", new { area = "Orchard.Themes" })
                    .Permission(Permissions.ApplyTheme)));
        }
    }
}