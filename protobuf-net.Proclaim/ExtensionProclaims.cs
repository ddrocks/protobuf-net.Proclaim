using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using ProtoBuf.Meta;

namespace ProtoBuf.Extensions
{
	/// <summary>
	/// RuntimeTypeModel extension
	/// </summary>
	public static class ExtensionProclaims
	{
		/// <summary>
		/// Apply include mappings via assembly reflection (<see cref="ProtoProclaimAttribute"/>).<br />
		/// Important: This must be called before any serialization happen.
		/// </summary>
		/// <param name="model">The RuntimeTypeModel</param>
		/// <param name="implicitFields">The default import of base type fields, if nothing is specified.</param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="InvalidDataException"></exception>
		public static void ApplyProclaims(this RuntimeTypeModel model, ImplicitFields implicitFields = ImplicitFields.None)
		{
			model.ApplyProclaims(assembly => true, implicitFields);
		}

		/// <summary>
		/// Apply include mappings via assembly reflection (<see cref="ProtoProclaimAttribute"/>).<br />
		/// Important: This must be called before any serialization happen.
		/// </summary>
		/// <param name="model">The RuntimeTypeModel</param>
		/// <param name="assemblyRegexFilter">The regex assembly filer</param>
		/// <param name="implicitFields">The default import of base type fields, if nothing is specified.</param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="InvalidDataException"></exception>
		public static void ApplyProclaims(this RuntimeTypeModel model, Regex assemblyRegexFilter, ImplicitFields implicitFields = ImplicitFields.None)
		{
			model.ApplyProclaims(assembly =>
				assemblyRegexFilter == null || assemblyRegexFilter.IsMatch(assembly.FullName), implicitFields);
		}

		/// <summary>
		/// Apply include mappings via assembly reflection (<see cref="ProtoProclaimAttribute"/>).<br />
		/// Important: This must be called before any serialization happen.
		/// </summary>
		/// <param name="model">The RuntimeTypeModel</param>
		/// <param name="assemblyFilter">The custom assembly filter</param>
		/// <param name="implicitFields">The default import of base type fields, if nothing is specified.</param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="InvalidDataException"></exception>
		public static void ApplyProclaims(this RuntimeTypeModel model, Func<Assembly, bool> assemblyFilter, ImplicitFields implicitFields = ImplicitFields.None)
		{
			AppDomain.CurrentDomain.GetAssemblies().Where(a => assemblyFilter?.Invoke(a) != false)
				.AsParallel().SelectMany(a =>
					a.GetTypes().Where(t => t.IsClass && t.GetCustomAttributes<ProtoProclaimAttribute>(false).Any()))
				.ForAll(t =>
				{
					foreach (var attr in t.GetCustomAttributes<ProtoProclaimAttribute>(false))
					{
						if (attr.ConcreteType != null)
						{
							if (t.IsGenericType)
							{
								if (!attr.ConcreteType.IsGenericType ||
								    attr.ConcreteType.GetGenericTypeDefinition() != t)
								{
									throw new ArgumentException("Concrete type must be assignable from this type");
								}
							}
							else if (!t.IsAssignableFrom(attr.ConcreteType))
							{
								throw new ArgumentException("Concrete type must be assignable from this type");
							}
						}

						var type = attr.ConcreteType ?? t;

						if (type.BaseType == null)
							throw new ArgumentException("BaseType cannot be null");

						if (type.IsGenericType && type.GenericTypeArguments.Length == 0)
							throw new InvalidDataException($"Cannot add generic type without concrete definition. '{type.FullName}'");

						Type baseType;
						if (attr.InterfaceType != null)
						{
							if (!attr.InterfaceType.IsInterface)
								throw new ArgumentException($"The attributed InterfaceType must be an interface. '{attr.InterfaceType.FullName}'");

							if (!attr.InterfaceType.IsAssignableFrom(type))
								throw new ArgumentException(
									$"BaseType '{attr.InterfaceType.FullName}' must be assignable from type {type.FullName}.");

							baseType = attr.InterfaceType;
						}
						else
						{
							if (type.BaseType == typeof(object))
							{
								baseType = type.GetInterfaces().FirstOrDefault();
								if (baseType == null)
									throw new ArgumentException($"BaseType cannot be of type '{typeof(object).FullName}' and no interfaces are defined.");
							}
							else
							{
								baseType = type.BaseType;
							}
						}

						var metaType = model.Add(type, true);
						if (metaType.BaseType != null && metaType.BaseType.Type != baseType)
							throw new InvalidDataException(
								$"Type '{type.FullName}' has already an assigned base type '{metaType.BaseType.Type.FullName}'");

						MetaType baseMetaType;
						if (baseType.IsClass &&
							baseType.GetCustomAttribute<DataContractAttribute>() == null &&
						    baseType.GetCustomAttribute<ProtoContractAttribute>() == null &&
						    model.GetTypes().OfType<MetaType>().All(a => a.Type != baseType))
						{
							baseMetaType = model.Add(baseType, true);

							void ApplyImplicitFields(BindingFlags bindingFlags)
							{
								var fields = baseType
									.GetProperties(bindingFlags | BindingFlags.Instance | BindingFlags.DeclaredOnly)
									.Cast<MemberInfo>()
									.Concat(baseType.GetFields(
										bindingFlags | BindingFlags.Instance | BindingFlags.DeclaredOnly)).ToList();

								if (fields.Any(a =>
									a.GetCustomAttribute<DataMemberAttribute>() != null ||
									a.GetCustomAttribute<ProtoMemberAttribute>() != null))
								{
									return;
								}

								var fieldNumber = 1;
								foreach (var name in fields.Where(x =>
										x.GetCustomAttribute<IgnoreDataMemberAttribute>() == null &&
										x.GetCustomAttribute<ProtoIgnoreAttribute>() == null && 
										x.GetCustomAttribute<CompilerGeneratedAttribute>() == null)
									.Select(a => a.Name))
								{
									baseMetaType.Add(fieldNumber++, name);
								}
							}

							switch (implicitFields)
							{
								case ImplicitFields.AllPublic:
									ApplyImplicitFields(BindingFlags.Public);
									break;
								case ImplicitFields.AllFields:
									ApplyImplicitFields(BindingFlags.Public | BindingFlags.NonPublic);
									break;
								case ImplicitFields.None:
									// Do noting
									break;
							}
						}
						else
						{
							baseMetaType = model.Add(baseType, true);
						}

						var rootMetaType = baseMetaType;
						while (rootMetaType.BaseType != null)
							rootMetaType = rootMetaType.BaseType;

						void ValidateMetaTypes(MetaType mt)
						{
							if (mt.HasSubtypes)
							{
								if (mt.GetSubtypes().Any(a => a.FieldNumber == attr.FieldNumber))
									throw new InvalidDataException(
										$"Cannot assign field number '{attr.FieldNumber}' for '{type.FullName}' on '{baseType.FullName}'. It's already being assigned to '{mt.Type.FullName}' in the hierarchy.");

								foreach (var en in mt.GetSubtypes())
									ValidateMetaTypes(en.DerivedType);
							}
						}

						ValidateMetaTypes(rootMetaType);
						baseMetaType.AddSubType(attr.FieldNumber, type);
					}
				});
		}
	}
}