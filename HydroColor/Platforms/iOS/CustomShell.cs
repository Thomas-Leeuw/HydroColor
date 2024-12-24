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


    // added a custom shell to disable animation when switching tabs
    // the animation was causing a flicker when switching tabs
    class CustomShellTabBarAppearanceTracker : ShellTabBarAppearanceTracker
    {

        public override void SetAppearance(UITabBarController controller, ShellAppearance appearance)
        {
            base.SetAppearance(controller, appearance);
            controller.ShouldSelectViewController += (tabBarController, viewController) =>
            {
                if (viewController == null)
                    return false;

                var fromController = tabBarController.SelectedViewController;

                UIView fromView = fromController?.View;
                UIView toView = viewController.View;

                if (fromView != toView)
                {
                    // turn off animation
                    UIView.Transition(fromView, toView, 0, UIViewAnimationOptions.TransitionNone, () => { });
                }

                return true;
            };
        }

    }
}
