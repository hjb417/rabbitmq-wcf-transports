using System;
using System.ServiceModel.Channels;

namespace HB
{
    internal static class BindingContextExtensionMethods
    {
        public static MessageEncoderFactory GetMessageEncoderFactory(this BindingContext bindingContext)
        {
            var collection = bindingContext.BindingParameters.FindAll<MessageEncodingBindingElement>();
            if (collection.Count > 1)
            {
                throw new InvalidOperationException("There are multiple message encoders.");
            }
            if (collection.Count == 1)
            {
                return collection[0].CreateMessageEncoderFactory();
            }
            else
            {
                return new TextMessageEncodingBindingElement().CreateMessageEncoderFactory();
            }
        }
    }
}