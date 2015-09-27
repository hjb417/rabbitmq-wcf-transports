using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace HB.RabbitMQ.ServiceModel.Tests
{
    partial class BindingFactory
    {
        public class ListenUriBindingElement : BindingElement
        {
            public ListenUriBindingElement()
            {
                ListenUriRelativeAddress = string.Empty;
            }

            public ListenUriBindingElement(Uri listenUriBaseAddress, string listenUriRelativeAddress, ListenUriMode listenUriMode)
            {
                ListenUriBaseAddress = listenUriBaseAddress;
                ListenUriRelativeAddress = listenUriRelativeAddress;
                ListenUriMode = listenUriMode;
            }

            public Uri ListenUriBaseAddress { get; set; }
            public string ListenUriRelativeAddress { get; set; }
            public ListenUriMode ListenUriMode { get; set; }

            public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
            {
                context.ListenUriBaseAddress = ListenUriBaseAddress;
                context.ListenUriRelativeAddress = ListenUriRelativeAddress;
                context.ListenUriMode = ListenUriMode;
                return base.BuildChannelListener<TChannel>(context);
            }

            public override BindingElement Clone()
            {
                return new ListenUriBindingElement(ListenUriBaseAddress, ListenUriRelativeAddress, ListenUriMode);
            }

            public override T GetProperty<T>(BindingContext context)
            {
                return context.GetInnerProperty<T>();
            }
        }
    }
}
