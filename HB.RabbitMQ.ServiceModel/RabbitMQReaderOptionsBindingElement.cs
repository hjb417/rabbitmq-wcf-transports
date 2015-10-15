/*
Copyright (c) 2015 HJB417

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Configuration;
using System.Configuration;
using System.Reflection;
using HB.RabbitMQ.ServiceModel.Throttling;

namespace HB.RabbitMQ.ServiceModel
{
    public partial class RabbitMQReaderOptionsBindingElement : ConfigurationElement
    {
        [ConfigurationProperty(BindingPropertyNames.IncludeProcessCommandLineInQueueArguments, DefaultValue = DefaultValues.IncludeProcessCommandLineInQueueArguments)]
        public bool IncludeProcessCommandLineInQueueArguments
        {
            get { return ((bool)base[BindingPropertyNames.IncludeProcessCommandLineInQueueArguments]); }
            set { base[BindingPropertyNames.IncludeProcessCommandLineInQueueArguments] = value; }
        }

        [ConfigurationProperty(BindingPropertyNames.DequeueThrottlerFactory, DefaultValue = DefaultValues.DequeueThrottlerFactory)]
        public Type DequeueThrottlerFactory
        {
            get { return ((Type)base[BindingPropertyNames.DequeueThrottlerFactory]); }
            set { base[BindingPropertyNames.DequeueThrottlerFactory] = value; }
        }

        protected override ConfigurationPropertyCollection Properties
        {
            get
            {
                ConfigurationPropertyCollection configProperties = base.Properties;
                foreach (var prop in GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance))
                {
                    foreach (ConfigurationPropertyAttribute attr in prop.GetCustomAttributes(typeof(ConfigurationPropertyAttribute), false))
                    {
                        configProperties.Add(new ConfigurationProperty(attr.Name, prop.PropertyType, attr.DefaultValue));
                    }
                }
                return configProperties;
            }
        }

        internal void ApplyConfiguration(RabbitMQReaderOptions readerOptions)
        {
            readerOptions.IncludeProcessCommandLineInQueueArguments = IncludeProcessCommandLineInQueueArguments;
            if (DequeueThrottlerFactory != null)
            {
                readerOptions.DequeueThrottlerFactory = (IDequeueThrottlerFactory) Activator.CreateInstance(DequeueThrottlerFactory);
            }
        }
    }
}
