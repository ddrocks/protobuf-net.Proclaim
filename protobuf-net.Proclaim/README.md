# protobuf-net-protobase

An extension to add functionality to define base type includes on derived classes.

## Runtime Installation

You can use the following command in the Package Manager Console:
```ps
Install-Package protobuf-net.IncludeExtension
```

## Basic usage

### 1 First Decorate your classes
'IMessage' in this example is an external interface provided by an API library.

```csharp
[ProtoContract]
[ProtoProclaim(10)] // Proclaim this type as subtype on the interface
class UserMessage : IMessage 
{
	[ProtoMember(1)]
	public int Id { get; set;}

	[ProtoMember(2)]
	public string Name { get; set; }
}

[ProtoContract]
[ProtoProclaim(20, InterfaceType = typeof(IMessage)] // Proclaim this type as subtype on the implicit interface 'IMessage'
class AdminMessage : IMessage, IAdminMessage 
{
	[ProtoMember(1)]
	public int Id { get; set;}

	[ProtoMember(2)]
	public string Name { get; set; }

	[ProtoMember(3)]
	public int Flags { get; set; }
}

[ProtoContract]
[ProtoProclaim(30, typeof(GenericMessage<string>)] // Proclaim a concrete type as subtype on the interface
[ProtoProclaim(31, typeof(GenericMessage<Guid>)]
class GenericMessage<T> : IMessage 
{
	[ProtoMember(1)]
	public int Id { get; set;}

	[ProtoMember(2)]
	public string Name { get; set; }

	[ProtoMember(3)]
	public T Value { get; set; }
}
```
The field number provided in 'ProtoBase' must be unique in the dependency tree.

### 2 Prepare your RuntimeTypeModel

```csharp
// No assembly filter
RuntimeTypeModel.Default.ApplyProclaims();

// Regex assembly name filter 
RuntimeTypeModel.Default.ApplyProclaims(new Regex("^(?!System\\.)"));

// Callback assembly filter
RuntimeTypeModel.Default.ApplyProclaims((assembly) => assembly == GetType().Assembly);
```
All extension calls take another optional parameter for implicit field assignment of base types.
(Default: ImplicitFields.None)

## Important

This must be called before any serialization happens, to link up the hierarchy dependencies.

