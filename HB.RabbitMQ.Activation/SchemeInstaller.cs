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
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel.Activation;
using WebAdmin = Microsoft.Web.Administration;
using Microsoft.ServiceModel.Samples;
using System.Security.Principal;
using System.Web.Configuration;
using System.IO;
using System.Configuration;
using Microsoft.ServiceModel.Samples.Hosting;
using System.Reflection;

namespace HB.RabbitMQ.Activation
{

    public sealed class SchemeInstaller
    {
        const string ListenerAdapterPath = "system.applicationHost/listenerAdapters";
        const string ProtocolsPath = "system.web/protocols";

        public SchemeInstaller(string scheme)
        {
            Scheme = scheme;
        }

        public string Scheme { get; }

        public bool IsListenerAdapterInstalled
        {
            get
            {
                using (var sm = new WebAdmin.ServerManager())
                {
                    var wasConfiguration = sm.GetApplicationHostConfiguration();
                    var section = wasConfiguration.GetSection(ListenerAdapterPath);
                    var listenerAdaptersCollection = section.GetCollection();
                    foreach (var e in listenerAdaptersCollection)
                    {
                        if (string.Compare((string)e.GetAttribute("name").Value, Scheme, true) == 0)
                        {
                            // Already installed.
                            return true;
                        }
                    }

                    return false;
                }
            }
        }

        public bool IsProtocolHandlerInstalled
        {
            get
            {
                var rootWebConfig = GetRootWebConfiguration();
                var section = (ProtocolsSection)rootWebConfig.GetSection(ProtocolsPath);
                foreach (var protocol in section.Protocols)
                {
                    if (string.Compare(protocol.Name, Scheme, true) == 0)
                    {
                        // Already installed.
                        return true;
                    }
                }

                return false;
            }
        }

        public void Install(SecurityIdentifier identity)
        {
            if (options.ListenerAdapterChecked)
            {
                var sm = new WebAdmin.ServerManager();
                var wasConfiguration = sm.GetApplicationHostConfiguration();
                var section = wasConfiguration.GetSection(ListenerAdapterPath);
                var listenerAdaptersCollection = section.GetCollection();
                var element = listenerAdaptersCollection.CreateElement();
                element.GetAttribute("name").Value = Scheme;
                element.GetAttribute("identity").Value = identity.Value;
                listenerAdaptersCollection.Add(element);
                sm.CommitChanges();
                wasConfiguration = null;
                sm = null;
            }

            if (options.ProtocolHandlerChecked)
            {
                var rootWebConfig = GetRootWebConfiguration();
                var section = (ProtocolsSection)rootWebConfig.GetSection(ProtocolsPath);
                var element = new ProtocolElement(Scheme);

                element.ProcessHandlerType = typeof(UdpProcessProtocolHandler).AssemblyQualifiedName;
                element.AppDomainHandlerType = typeof(UdpAppDomainProtocolHandler).AssemblyQualifiedName;
                element.Validate = false;

                section.Protocols.Add(element);
                rootWebConfig.Save();
            }
        }

        private Configuration GetRootWebConfiguration()
        {
            return WebConfigurationManager.OpenWebConfiguration(string.Empty);
        }

        public void Uninstall(UdpInstallerOptions options)
        {
            if (options.ListenerAdapterChecked)
            {
                var sm = new WebAdmin.ServerManager();
                var wasConfiguration = sm.GetApplicationHostConfiguration();
                var section = wasConfiguration.GetSection(ListenerAdapterPath);
                var listenerAdaptersCollection = section.GetCollection();

                for (var i = 0; i < listenerAdaptersCollection.Count; i++)
                {
                    var element = listenerAdaptersCollection[i];

                    if (string.Compare((string)element.GetAttribute("name").Value,
                        Scheme, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        listenerAdaptersCollection.RemoveAt(i);
                    }
                }

                sm.CommitChanges();
                wasConfiguration = null;
                sm = null;
            }

            if (options.ProtocolHandlerChecked)
            {
                var rootWebConfig = GetRootWebConfiguration();
                var section = (ProtocolsSection)rootWebConfig.GetSection(ProtocolsPath);
                section.Protocols.Remove(Scheme);
                rootWebConfig.Save();
            }
        }
    }
}
