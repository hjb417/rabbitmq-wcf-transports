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
using System.Configuration;
using RabbitMQ.Client;

namespace HB.RabbitMQ.Activation.Configuration
{
    public class ConnectionElement : ConfigurationElement
    {
        public ConnectionElement()
        {
            Properties.Add(new ConfigurationProperty(ConnectionSectionAttributes.RequestedHeartbeat, typeof(TimeSpan), TimeSpan.FromSeconds(ConnectionFactory.DefaultHeartbeat)));
        }

        [ConfigurationProperty(ConnectionSectionAttributes.HostName, IsRequired = true)]
        public string HostName
        {

            get { return (string)this[ConnectionSectionAttributes.HostName]; }
            set { this[ConnectionSectionAttributes.HostName] = value; }
        }

        [ConfigurationProperty(ConnectionSectionAttributes.UserName, DefaultValue = ConnectionFactory.DefaultUser)]
        public string UserName
        {

            get { return (string)this[ConnectionSectionAttributes.UserName]; }
            set { this[ConnectionSectionAttributes.UserName] = value; }
        }

        [ConfigurationProperty(ConnectionSectionAttributes.Password, DefaultValue = ConnectionFactory.DefaultPass)]
        public string Password
        {

            get { return (string)this[ConnectionSectionAttributes.Password]; }
            set { this[ConnectionSectionAttributes.Password] = value; }
        }

        [ConfigurationProperty(ConnectionSectionAttributes.AutomaticRecoveryEnabled)]
        public bool? AutomaticRecoveryEnabled
        {

            get { return (bool?)this[ConnectionSectionAttributes.AutomaticRecoveryEnabled]; }
            set { this[ConnectionSectionAttributes.AutomaticRecoveryEnabled] = value; }
        }

        public TimeSpan RequestedHeartbeat
        {

            get { return (TimeSpan)this[ConnectionSectionAttributes.RequestedHeartbeat]; }
            set { this[ConnectionSectionAttributes.RequestedHeartbeat] = value; }
        }

        [ConfigurationProperty(ConnectionSectionAttributes.TopologyRecoveryEnabled)]
        public bool? TopologyRecoveryEnabled
        {

            get { return (bool?)this[ConnectionSectionAttributes.TopologyRecoveryEnabled]; }
            set { this[ConnectionSectionAttributes.TopologyRecoveryEnabled] = value; }
        }

        [ConfigurationProperty(ConnectionSectionAttributes.Port)]
        public int? Port
        {

            get { return (int?)this[ConnectionSectionAttributes.Port]; }
            set { this[ConnectionSectionAttributes.Port] = value; }
        }
    }
}