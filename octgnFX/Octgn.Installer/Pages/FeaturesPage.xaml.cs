using Octgn.Installer.Plans;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Octgn.Installer.Pages
{
    public partial class FeaturesPage : UserControl
    {
        public FeaturesPage() {
            InitializeComponent();
        }

        private void Feature_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            var checkBox = (CheckBox)sender;

            var treeViewItem = FindParentOfType<TreeViewItem>(checkBox);

            treeViewItem.IsSelected = true;
        }

        private T FindParentOfType<T>(FrameworkElement element) where T : FrameworkElement {
            var parent = VisualTreeHelper.GetParent(element);

            if (parent == null)
                throw new InvalidOperationException($"Could not find parent of type {typeof(T).Name}");

            if (parent is FrameworkElement parentElement) {
                if (parentElement is T expectedParent) {
                    return expectedParent;
                }

                return FindParentOfType<T>(parentElement);
            } else throw new InvalidOperationException($"Could not find parent of type {typeof(T).Name}");
        }
    }

    public class FeaturesPageViewModel : PageViewModel
    {
        public Features.Features Features { get; }

        public FeaturesPageViewModel(App app) : base(app) {
            Button1Text = "Next";

            if(app.Plan is Install installPlan) {
                Features = installPlan.Features;
            } else {
                throw new InvalidOperationException($"Plan {app.Plan} is not valid here");
            }

            Page = new FeaturesPage();
            Page.DataContext = this;
        }

        public override void Button1_Action() {
            App.Plan.Next();
        }
    }
}