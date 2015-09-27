using System;
using System.Configuration;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.ServiceModel.Description;
using System.Xml;

namespace HB.RabbitMQ.ServiceModel.Tests
{
    partial class BindingFactory
    {
        public class ListenUriElement : BindingElementExtensionElement
        {
            public ListenUriElement()
            {
                ListenUriRelativeAddress = string.Empty;
            }

            public Uri ListenUriBaseAddress { get; set; }
            public string ListenUriRelativeAddress { get; set; }
            public ListenUriMode ListenUriMode { get; set; }

            public override Type BindingElementType
            {
                get { return (typeof(ListenUriBindingElement)); }
            }

            public override void ApplyConfiguration(BindingElement bindingElement)
            {
                base.ApplyConfiguration(bindingElement);

                var element = (ListenUriBindingElement)bindingElement;

                ListenUriBaseAddress = element.ListenUriBaseAddress;
                ListenUriRelativeAddress = element.ListenUriRelativeAddress;
                ListenUriMode = element.ListenUriMode;
            }
            protected override ConfigurationPropertyCollection Properties
            {
                get
                {
                    var properties = new ConfigurationPropertyCollection();

                    properties.Add(new ConfigurationProperty("listenUriBaseAddress", typeof(Uri)));
                    properties.Add(new ConfigurationProperty("listenUriRelativeAddress", typeof(string), string.Empty));
                    properties.Add(new ConfigurationProperty("listenUriMode", typeof(ListenUriMode), ListenUriMode.Unique));

                    return (properties);
                }
            }

            protected override BindingElement CreateBindingElement()
            {
                var bindingElement = new ListenUriBindingElement();

                bindingElement.ListenUriBaseAddress = ListenUriBaseAddress;
                bindingElement.ListenUriRelativeAddress = ListenUriRelativeAddress;
                bindingElement.ListenUriMode = ListenUriMode;

                return bindingElement;
            }

            protected override void DeserializeElement(XmlReader reader, bool serializeCollectionKey)
            {
                ListenUriBaseAddress = new Uri(reader.GetAttribute("listenUriBaseAddress"));
                ListenUriRelativeAddress = reader.GetAttribute("listenUriRelativeAddress");
                ListenUriMode = (ListenUriMode)Enum.Parse(typeof(ListenUriMode), reader.GetAttribute("listenUriMode"));
            }
        }
    }
}