using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotVVM.Framework.ViewModel;

namespace DotVVM.Samples.BasicSamples.ViewModels.ComplexSamples.RedirectAndUrl
{
    public class ScrollingPageViewModel : DotvvmViewModelBase
    {
        public string Message { get; set; }

        public void GoToParagraph1()
        {
            Message = "ToParagraph1";
        }

        public void GoToParagraph2()
        {
            Message = "ToParagraph2";
        }

        public void GoToParagraph2WithRedirectToUrl()
        {
            Message = "GoToParagraph2_With_RedirectToUrl";
            var path = Context.OwinContext.Request.Uri;
            Context.RedirectToUrl($"{path}#paragraph2");
        }

        public void TestQueryString()
        {
            Message = "TestQuerystring";
            var path = Context.OwinContext.Request.Uri;
            Context.RedirectToUrl($"{path}?q='delf'&meep");
        }

        public void TestMessage ()
        {
            Message = "TestMessage";
        }
    }
}
