// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;

using JetBrains.Annotations;
using JetBrains.Build.InstallationData;
using JetBrains.Omea.Containers;

namespace JetBrains.Omea.Base.Install
{
	/// <summary>
	/// Invokes installation/uninstallation related services.
	/// This class should not be made static (and, conscequently, public interface methods on it), so that it were created and disposed accordingly.
	/// </summary>
	public class Installer : IDisposable
	{
		#region Data

		/// <summary>
		/// Maps the attributes storing the installation data to the objects that process their registration and unregistration.
		/// Don't use, see <see cref="MapAttributeToInstallers"/>.
		/// </summary>
		[NotNull]
		protected OneToSetMap<Type, IInstallAttributes> myMapAttributeToInstallers;

		/// <summary>
		/// Maps the attribute-installer types to the created instances of their objects.
		/// Don't use, see <see cref="MapInstallerTypeToInstance"/>.
		/// </summary>
		[NotNull]
		protected Dictionary<Type, IInstallAttributes> myMapInstallerTypeToInstance;

		[NotNull]
		private readonly ICollection<Assembly> myAssemblies;

		private Func<SourceRootXml, DirectoryInfo> myResolveSourceDirRoot;

		#endregion

		#region Init

		/// <summary>
		/// A constructor for spawning the installer on an application descriptor that is defined in an attribute.
		/// </summary>
		public Installer([NotNull] ICollection<Assembly> assemblies)
		{
			if(assemblies == null)
				throw new ArgumentNullException("assemblies");

			// Validate
			var hashAssemblies = new HashSet<Assembly>(assemblies.Count);
			foreach(Assembly assembly in assemblies)
			{
				if(hashAssemblies.Contains(assembly))
					throw new InvalidOperationException(string.Format("Duplicate assembly “{0}”.", assembly.FullName));
				hashAssemblies.Add(assembly);
			}

			myAssemblies = assemblies;
		}

		#endregion

		#region Attributes

		/// <summary>
		/// Gets the list of all the product assemblies that are searched for attributes.
		/// </summary>
		[NotNull]
		public ICollection<Assembly> Assemblies
		{
			get
			{
				return myAssemblies;
			}
		}

		/// <summary>
		/// Maps the attribute-installer types to the created instances of their objects.
		/// </summary>
		public Dictionary<Type, IInstallAttributes> MapInstallerTypeToInstance
		{
			get
			{
				if(myMapInstallerTypeToInstance == null)
					CollectAttributeInstallers();
				if(myMapInstallerTypeToInstance == null)
					throw new InvalidOperationException(string.Format("Failed to collect the attribute installers."));
				return myMapInstallerTypeToInstance;
			}
		}

		#endregion

		#region Operations

		/// <summary>
		/// Dupms the given Registry data into a file.
		/// </summary>
		public static void DumpInstallationData(InstallationDataXml data, string sRegistryRegistrationDataFile)
		{
			var serializer = new XmlSerializer(typeof(RegistryXml));
			using(var stream = new FileStream(sRegistryRegistrationDataFile, FileMode.Create, FileAccess.Write, FileShare.Read))
				serializer.Serialize(stream, data);
		}

		/// <summary>
		/// Gets all the Registry data that should be written to or erased from the Registry upon installation or uninstallation.
		/// This includes the static app-global Registry data and dynamic registration info collected from the Assembly attributes.
		/// </summary>
		[NotNull]
		public InstallationDataXml HarvestInstallationData()
		{
			var data = new InstallationDataXml();

			// Run the attribute installers statically (each instance once)
			InvokeAttributeInstallersStatic(data);

			// Run the attribute installers per each attribute instance
			InvokeAttributeInstallersInstance(data);

			data.EnsureNotNull();
			data.RemoveDuplicates();
			data.AssertValid();

			return data;
		}

		#endregion

		#region Implementation

		/// <summary>
		/// Collects the handlers that implement installation against the assembly attributes, see <see cref="InstallAttributesAttribute"/>.
		/// Do not call, use <see cref="MapAttributeToInstallers"/>.
		/// </summary>
		protected void CollectAttributeInstallers()
		{
			// Init only once
			if(myMapAttributeToInstallers != null)
				return;
			myMapAttributeToInstallers = new OneToSetMap<Type, IInstallAttributes>();

			// Cache the installer objects to singleton them in case they handle multiple attrs
			myMapInstallerTypeToInstance = new Dictionary<Type, IInstallAttributes>();

			foreach(Assembly assembly in Assemblies) // All the assemblies
			{
				try
				{
					foreach(Type typeInstaller in assembly.GetTypes()) // All the types
					{
						// Does the current type declare it installs any attrs?
						object[] attributes = typeInstaller.GetCustomAttributes(typeof(InstallAttributesAttribute), false);
						if(attributes.Length == 0)
							continue;

						// Create the installer instance, if not created yet
						IInstallAttributes installer;
						if(!myMapInstallerTypeToInstance.TryGetValue(typeInstaller, out installer))
						{
							object objectInstaller = Activator.CreateInstance(typeInstaller);
							installer = objectInstaller as IInstallAttributes;
							if(installer == null)
								throw new InvalidOperationException(string.Format("The attribute-installer object of type “{0}” does not implement the required “{1}” interface.", typeInstaller.FullName, typeof(IInstallAttributes).FullName));
							myMapInstallerTypeToInstance.Add(typeInstaller, installer);
						}

						// Add attributes
						foreach(InstallAttributesAttribute attribute in attributes)
						{
							if(attribute.AttributeToInstall != null) // A single-time installer
							{
								// Duplicate?
								if((myMapAttributeToInstallers.ContainsKey(attribute.AttributeToInstall)) && (myMapAttributeToInstallers[attribute.AttributeToInstall].Contains(installer)))
									throw new InvalidOperationException(string.Format("The installer class “{0}” registers for the “{1}” attribute twice.", typeInstaller.FullName, attribute.AttributeToInstall.FullName));
								myMapAttributeToInstallers.Add(attribute.AttributeToInstall, installer);
							}
						}
					}
				}
				catch(Exception ex)
				{
					throw new InvalidOperationException(string.Format("Failed to collect attribute-installers from the “{0}” assembly. {1}", assembly.FullName, ex.Message), ex);
				}
			}
		}

		/// <summary>
		/// Invokes the registration handlers for assembly attributes, see <see cref="MapAttributeToInstallers"/>, from the assemblies listed in the <c>AllAssemblies.xml</c>.
		/// </summary>
		[NotNull]
		protected InstallationDataXml InvokeAttributeInstallersInstance(InstallationDataXml retval)
		{
			// Process each known assembly
			foreach(Assembly assembly in Assemblies)
			{
				// Invoke registration
				try
				{
					foreach(var pair in MapAttributeToInstallers)
					{
						foreach(object attribute in assembly.GetCustomAttributes(pair.Key, false))
						{
							foreach(IInstallAttributes installer in pair.Value)
							{
								try
								{
									// Collect installation data!
									InstallationDataXml data = installer.InstallInstance(this, attribute);
									if(data != null)
										retval.MergeWith(data);
								}
								catch(Exception ex)
								{
									throw new InvalidOperationException(string.Format("Failed to collect the installation data for the attribute of type “{0}” from the assembly “{1}” using the “{2}” installer. {3}", pair.Key.AssemblyQualifiedName, assembly.FullName, installer.GetType().AssemblyQualifiedName, ex.Message), ex);
								}
							}
						}
					}
				}
				catch(Exception ex)
				{
					throw new InvalidOperationException(string.Format("Failed to process the “{0}” assembly. {1}", assembly.FullName, ex.Message), ex);
				}
			}
			return retval;
		}

		/// <summary>
		/// Collects the one-time global registration data from the attribute installers, one that is not per-attribute or per-assembly.
		/// Invoked from <see cref="InvokeAttributeInstallersInstance"/>, don't call manually.
		/// </summary>
		protected void InvokeAttributeInstallersStatic(InstallationDataXml total)
		{
			foreach(IInstallAttributes installer in MapInstallerTypeToInstance.Values)
			{
				InstallationDataXml data = installer.InstallStatic(this);
				if(data != null)
					total.MergeWith(data);
			}
		}

		/// <summary>
		/// Maps the attributes storing the installation data to the objects that process their registration and unregistration.
		/// </summary>
		protected OneToSetMap<Type, IInstallAttributes> MapAttributeToInstallers
		{
			get
			{
				if(myMapAttributeToInstallers == null)
					CollectAttributeInstallers();
				if(myMapAttributeToInstallers == null)
					throw new InvalidOperationException(string.Format("Failed to collect the attribute installers."));
				return myMapAttributeToInstallers;
			}
		}

		/// <summary>
		/// Gets or sets the resolver that allows to get a physical file system path for the given source file system directory root.
		/// TODO: internal because of the temporary Func`2 problems, should be made public when solved.
		/// </summary>
		internal Func<SourceRootXml, DirectoryInfo> ResolveSourceDirRoot
		{
			get
			{
				if(myResolveSourceDirRoot == null)
					throw new InvalidOperationException(string.Format("The installer has not been provided with this information."));
				return myResolveSourceDirRoot;
			}
			set
			{
				if(myResolveSourceDirRoot != null)
					throw new InvalidOperationException(string.Format("This value may be set only once."));
				myResolveSourceDirRoot = value;
			}
		}

		#endregion

		#region Overrides

		~Installer()
		{
			Dispose();
		}

		#endregion

		#region IDisposable Members

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		public void Dispose()
		{
			GC.SuppressFinalize(this);
		}

		#endregion
	}
}
