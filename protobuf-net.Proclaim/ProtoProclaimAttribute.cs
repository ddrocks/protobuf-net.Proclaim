using System;

namespace ProtoBuf.Extensions
{
	/// <summary>
	/// Proclaim a type to the base class or interface
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public sealed class ProtoProclaimAttribute : Attribute
	{
		/// <summary>
		/// The interface type being used to add this subtype.<br />
		/// ONLY being used if type is not derived form another class.
		/// </summary>
		public Type InterfaceType { get; set; }

		/// <summary>
		/// A defined concrete type to use
		/// </summary>
		public Type ConcreteType { get; }

		/// <summary>
		/// The field number to reference this subtype.
		/// </summary>
		public int FieldNumber { get; }

		/// <summary>
		/// Adds this type as a subtype to the base type
		/// </summary>
		/// <param name="fieldNumber"></param>
		public ProtoProclaimAttribute(int fieldNumber)
		{
			FieldNumber = fieldNumber;
		}

		/// <summary>
		/// Create a link to the concrete derived type on the base type.<br />
		/// Note: you cannot add multiple base type to one concrete type of this class.
		/// </summary>
		/// <param name="fieldNumber"></param>
		/// <param name="concreteType"></param>
		public ProtoProclaimAttribute(int fieldNumber, Type concreteType)
			: this(fieldNumber)
		{
			ConcreteType = concreteType;
		}
	}
}
