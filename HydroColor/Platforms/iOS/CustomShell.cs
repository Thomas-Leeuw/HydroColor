using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIKit;

namespace HydroColor.Platforms.iOS
{
    public class CustomShell : ShellRenderer
    {

        protected override IShellTabBarAppearanceTracker CreateTabBarAppearanceTracker()
        {
            return new CustomShellTabBarAppearanceTracker();
        }
       
    }


    // A custom shell that switches tabs without animation.
    // iOS 18 and later fade between tabs. A page shown for the first time is laid out during that
    // fade, which appears as a black sweep. Animations are switched off for the tab change and back
    // on shortly after it.
    class CustomShellTabBarAppearanceTracker : ShellTabBarAppearanceTracker
    {
        // SetAppearance is called more than once; the tab handler is set only the first time
        bool _handlerSet;

        public override void SetAppearance(UITabBarController controller, ShellAppearance appearance)
        {
            base.SetAppearance(controller, appearance);

            if (_handlerSet)
            {
                return;
            }
            _handlerSet = true;

            controller.ShouldSelectViewController = (tabBarController, viewController) =>
            {
                if (viewController == null)
                {
                    return false;
                }

                UIView.AnimationsEnabled = false;
                CoreFoundation.DispatchQueue.MainQueue.DispatchAfter(
                    new CoreFoundation.DispatchTime(CoreFoundation.DispatchTime.Now, TimeSpan.FromMilliseconds(150)),
                    () => UIView.AnimationsEnabled = true);

                return true;
            };
        }

    }
}
