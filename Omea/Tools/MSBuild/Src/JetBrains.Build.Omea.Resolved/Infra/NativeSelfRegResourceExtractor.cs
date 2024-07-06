// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml;

using Microsoft.Tools.WindowsInstallerXml.Serialize;

using Component=Microsoft.Tools.WindowsInstallerXml.Serialize.Component;
using Directory=Microsoft.Tools.WindowsInstallerXml.Serialize.Directory;

namespace JetBrains.Build.Omea.Resolved.Infra
{
	public static unsafe class NativeSelfRegResourceExtractor
	{
		#region Data

		private static readonly string ResourceName = "SELFREG";

		private static readonly string ResourceType = "WINDOWSINSTALLERXML";

		#endregion

		#region Operations

		public static void ExtractWxsResource(FileInfo fi, Component wixComponent)
		{
			byte[] data = ReadNativeResource(fi);

			XmlDocument xml = GetXmlDocument(data);

			Component wixComponentOuter = ParseComponent(xml);

			CopyRegistry(wixComponentOuter, wixComponent);
		}

		#endregion

		#region Implementation

		private static List<ISchemaElement> ChildrenToArray(IParentElement parent)
		{
			var children = new List<ISchemaElement>();
			foreach(ISchemaElement child in parent.Children)
				children.Add(child);

			return children;
		}

		private static ISchemaElement CodeDomReader_CreateObjectFromElement(CodeDomReader reader, XmlNode node)
		{
			return (ISchemaElement)reader.GetType().InvokeMember("CreateObjectFromElement", BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.NonPublic, null, reader, new object[] {node});
		}

		private static void CodeDomReader_ParseObjectFromElement(CodeDomReader reader, ISchemaElement element, XmlNode node)
		{
			reader.GetType().InvokeMember("ParseObjectFromElement", BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.NonPublic, null, reader, new object[] {element, node});
		}

		private static void CopyRegistry(Component wixComponentSource, Component wixComponentTarget)
		{
			foreach(ISchemaElement child in wixComponentSource.Children)
			{
				if((child is RegistryKey) || (child is RegistryValue))
					wixComponentTarget.AddChild(child);
				else
					throw new InvalidOperationException(string.Format("Unexpected WiX component child “{0}”.", child.GetType().AssemblyQualifiedName));
			}
		}

		private static T ExpectChild<T>(IParentElement parent)
		{
			List<ISchemaElement> children = ChildrenToArray(parent);
			if(children.Count != 1)
				throw new InvalidOperationException(string.Format("The element “{0}” is expected to have exactly one child of type “{1}”, but found some {2} children.", parent.GetType().AssemblyQualifiedName, typeof(T).AssemblyQualifiedName, children.Count));

			if(!(children[0] is T))
				throw new InvalidOperationException(string.Format("The element “{0}” is expected to have exactly one child of type “{1}”, but found a child of type “{2}” instead.", parent.GetType().AssemblyQualifiedName, typeof(T).AssemblyQualifiedName, children[0].GetType().AssemblyQualifiedName));

			return (T)children[0];
		}

		private static XmlDocument GetXmlDocument(byte[] data)
		{
			var xml = new XmlDocument();
			using(var stream = new MemoryStream(data))
				xml.Load(stream);
			return xml;
		}

		private static Component ParseComponent(XmlDocument xml)
		{
			/*
			XmlNamespaceManager nsman = new XmlNamespaceManager(xml.NameTable);
			nsman.AddNamespace("wix", "http://schemas.microsoft.com/wix/2006/wi");
			string xpath = "/wix:Wix/wix:Fragment/wix:Directory/wix:Component";
			XmlNodeList xmlComponents = xml.SelectNodes(xpath, nsman);

			if(xmlComponents.Count == 0)
				throw new InvalidOperationException(string.Format("Could not find the WiX component at “{0}”.", xpath));
			if(xmlComponents.Count > 1)
				throw new InvalidOperationException(string.Format("Expected one WiX component at “{0}”, found {1}.", xpath, xmlComponents.Count));

			XmlNode xmlComponent = xmlComponents[0];
*/

			var reader = new CodeDomReader();
			ISchemaElement schemaElement = CodeDomReader_CreateObjectFromElement(reader, xml.DocumentElement);
			CodeDomReader_ParseObjectFromElement(reader, schemaElement, xml.DocumentElement);

			var wix = (Wix)schemaElement;
			var wixFragment = ExpectChild<Fragment>(wix);
			var wixDirectory = ExpectChild<Directory>(wixFragment);
			var wixComponent = ExpectChild<Component>(wixDirectory);

			return wixComponent;
		}

		private static byte[] ReadNativeResource(FileInfo fi)
		{
			void* hModule = WinApi.LoadLibraryW(fi.FullName);

			void* hResInfo = WinApi.FindResourceW(hModule, ResourceName, ResourceType);
			if(hResInfo == null)
			{
				var exSys = new Win32Exception();
				throw new InvalidOperationException(string.Format("Could not find the self-reg resource “{0}” of type “{1}”. {2}", ResourceName, ResourceType, exSys.Message), exSys);
			}

			void* hResData = WinApi.LoadResource(hModule, hResInfo);
			if(hResData == null)
				throw new Win32Exception();

			byte* pResBytes = WinApi.LockResource(hResData);
			if(pResBytes == null)
				throw new Win32Exception();

			var nResourceSize = unchecked((int)WinApi.SizeofResource(hModule, hResInfo));
			if(nResourceSize < 0)
				throw new InvalidOperationException(string.Format("Negative resource size encountered."));

			var data = new byte[nResourceSize];
			Marshal.Copy((IntPtr)pResBytes, data, 0, data.Length);

			return data;
		}

		#endregion

		#region WinApi Type

		internal class WinApi
		{
			#region Implementation

			[DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
			internal static extern void* FindResourceW(void* hModule, [MarshalAs(UnmanagedType.LPWStr)] string lpName, [MarshalAs(UnmanagedType.LPWStr)] string lpType);

			[DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
			internal static extern void* LoadLibraryW([MarshalAs(UnmanagedType.LPWStr)] string lpLibFileName);

			[DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
			internal static extern void* LoadResource(void* hModule, void* hResInfo);

			[DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
			internal static extern byte* LockResource(void* hResData);

			[DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
			internal static extern uint SizeofResource(void* hModule, void* hResInfo);

			#endregion
		}

		#endregion
	}
}
