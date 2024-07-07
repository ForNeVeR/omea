// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Net;
using System.Xml;

namespace JetBrains.ExceptionReport
{
	public class ProxySettings
	{
    private const string ourCustomProxyTag = "CustomProxy";
    private const string ourHostTag = "HostProxy";
    private const string ourPortTag = "Port";
    private const string ourAuthenticationTag = "Authentication";
    private const string ourLoginTag = "Login";
    private const string ourPasswordTag = "Password";

    private bool myCustomProxy = false;
    private string myHost = "";
    private int myPort = 0;
    private bool myAuthentication = false;
    private string myLogin = "";
    private string myPassword = "";

	  public bool CustomProxy
	  {
	    get { return myCustomProxy; }
	    set { myCustomProxy = value; }
	  }

	  public string Host
	  {
	    get { return myHost; }
	    set { myHost = value; }
	  }

	  public int Port
	  {
	    get { return myPort; }
	    set { myPort = value; }
	  }

	  public bool Authentication
	  {
	    get { return myAuthentication; }
	    set { myAuthentication = value; }
	  }

	  public string Login
	  {
	    get { return myLogin; }
	    set { myLogin = value; }
	  }

	  public string Password
	  {
	    get { return myPassword; }
	    set { myPassword = value; }
	  }

    public ProxySettings()
    {
    }

    public void ReadXML (XmlElement element)
    {
      XmlElement customProxyElement = element.SelectSingleNode(ourCustomProxyTag) as XmlElement;
      if (customProxyElement != null)
        CustomProxy = bool.Parse(customProxyElement.InnerText);

      XmlElement hostElement = element.SelectSingleNode(ourHostTag) as XmlElement;
      if (hostElement != null)
        Host = hostElement.InnerText;

      XmlElement portElement = element.SelectSingleNode(ourPortTag) as XmlElement;
      if (portElement != null)
        Port = int.Parse(portElement.InnerText);

      XmlElement authenticationElement = element.SelectSingleNode(ourAuthenticationTag) as XmlElement;
      if (authenticationElement != null)
        Authentication = bool.Parse(authenticationElement.InnerText);

      XmlElement loginElement = element.SelectSingleNode(ourLoginTag) as XmlElement;
      if (loginElement != null)
        Login = loginElement.InnerText;

      XmlElement passwordElement = element.SelectSingleNode(ourPasswordTag) as XmlElement;
      if (passwordElement != null)
        Password = passwordElement.InnerText;
    }

    public void WriteXML (XmlDocument document, XmlElement element)
    {
      XmlElement customProxyElement = document.CreateElement(ourCustomProxyTag);
      customProxyElement.InnerText = CustomProxy.ToString();
      element.AppendChild(customProxyElement);

      XmlElement hostElement = document.CreateElement(ourHostTag);
      hostElement.InnerText = Host.ToString();
      element.AppendChild(hostElement);

      XmlElement portElement = document.CreateElement(ourPortTag);
      portElement.InnerText = Port.ToString();
      element.AppendChild(portElement);

      XmlElement authenticationElement = document.CreateElement(ourAuthenticationTag);
      authenticationElement.InnerText = Authentication.ToString();
      element.AppendChild(authenticationElement);

      XmlElement loginElement = document.CreateElement(ourLoginTag);
      loginElement.InnerText = Login.ToString();
      element.AppendChild(loginElement);

      XmlElement passwordElement = document.CreateElement(ourPasswordTag);
      passwordElement.InnerText = Password.ToString();
      element.AppendChild(passwordElement);
    }

    public IWebProxy Proxy
    {
      get
      {
        if (!CustomProxy)
          return WebProxy.GetDefaultProxy();

        WebProxy proxy = new WebProxy (Host, Port);
        if (Authentication)
          proxy.Credentials = new NetworkCredential(Login, Password);

        return proxy;
      }
    }
	}
}
