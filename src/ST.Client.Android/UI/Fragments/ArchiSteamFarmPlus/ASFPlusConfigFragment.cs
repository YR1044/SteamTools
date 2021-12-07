using Android.Views;
using Binding;
using ReactiveUI;
using System.Application.UI.Resx;
using System.Application.UI.ViewModels;
using static System.Application.UI.Resx.AppResources;

// ReSharper disable once CheckNamespace
namespace System.Application.UI.Fragments
{
    internal sealed class ASFPlusConfigFragment : ASFPlusFragment<fragment_asf_plus_config>
    {
        protected override int? LayoutResource => Resource.Layout.fragment_asf_plus_config;

        public override void OnCreateView(View view)
        {
            base.OnCreateView(view);
            binding!.textView.Text = ASF_GlobalConfig + Environment.NewLine + AppResources.UnderConstruction;
            binding!.textView.Gravity = GravityFlags.Center;
        }
    }
}