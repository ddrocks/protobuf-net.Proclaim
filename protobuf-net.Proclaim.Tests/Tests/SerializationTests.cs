using System.IO;
using NUnit.Framework;
using ProtoBuf.Extensions.SampleData;
using ProtoBuf.Meta;

namespace ProtoBuf.Extensions.Tests
{
	[TestFixture]
	public class SerializationTests
	{
		private RuntimeTypeModel _model;

		[OneTimeSetUp]
		public void Startup()
		{
			_model = RuntimeTypeModel.Create();
			_model.ApplyProclaims(assembly => assembly == GetType().Assembly);
		}

		[Test]
		public void Serialization_ValidNotification()
		{
			INotification example = new UserNotification
			{
				Email = "test@dhq-itsolutions.com",
				Name = "Test",
			};

			using (var ms = new MemoryStream())
			{
				_model.Serialize(ms, example);
				ms.Seek(0, SeekOrigin.Begin);
				var result = (INotification)_model.Deserialize(ms, null, typeof(INotification));
				Assert.IsInstanceOf<UserNotification>(result);
				Assert.IsInstanceOf<EmailEnvironment>(result.Environment);
				Assert.AreEqual(example.Email, result.Email);
				Assert.AreEqual(example.Name, result.Name);
			}
		}

		[Test]
		public void Serialization_ValidGenericNotification()
		{
			INotification example = new GenericUserNotification<string>()
			{
				Email = "test@dhq-itsolutions.com",
				Name = "Test",
				Value = "Special"
			};

			using (var ms = new MemoryStream())
			{
				_model.Serialize(ms, example);
				ms.Seek(0, SeekOrigin.Begin);
				var result = (INotification)_model.Deserialize(ms, null, typeof(INotification));
				Assert.IsInstanceOf<GenericUserNotification<string>>(result);
				Assert.AreEqual(example.Email, result.Email);
				Assert.AreEqual(example.Name, result.Name);
				Assert.AreEqual(((GenericUserNotification<string>) example).Value, (result as GenericUserNotification<string>)?.Value);
			}
		}

		[Test]
		public void Serialization_InvalidNotification()
		{
			INotification example = new InvalidNotification
			{
				Email = "test@dhq-itsolutions.com",
				Name = "Test",
			};

			using (var ms = new MemoryStream())
			{
				_model.Serialize(ms, example);
				ms.Seek(0, SeekOrigin.Begin);
				INotification result = null;
				Assert.Catch(() => { result = (INotification)_model.Deserialize(ms, null, typeof(INotification)); });
				Assert.IsNotInstanceOf<InvalidNotification>(result);
			}
		}

		[Test]
		public void Serialization_UserMessage_ImplicitFields_AllPublic()
		{
			MessageBase example = new UserMessage
			{
				MessageId = 1,
				Password = "password",
				UserName = "ddrocks"
			};

			var model = RuntimeTypeModel.Create();
			model.ApplyProclaims(assembly => assembly == GetType().Assembly, ImplicitFields.AllPublic);

			using (var ms = new MemoryStream())
			{
				model.Serialize(ms, example);
				ms.Seek(0, SeekOrigin.Begin);
				var result = (MessageBase)model.Deserialize(ms, null, typeof(MessageBase));
				Assert.IsInstanceOf<UserMessage>(result);
				Assert.AreEqual(example.MessageId, result.MessageId);
			}
		}

		[Test]
		public void Serialization_UserMessage_ImplicitFields_None()
		{
			MessageBase example = new UserMessage
			{
				MessageId = 1,
				Password = "password",
				UserName = "ddrocks"
			};

			using (var ms = new MemoryStream())
			{
				_model.Serialize(ms, example);
				ms.Seek(0, SeekOrigin.Begin);
				var result = (MessageBase)_model.Deserialize(ms, null, typeof(MessageBase));
				Assert.IsInstanceOf<UserMessage>(result);
				Assert.AreNotEqual(example.MessageId, result.MessageId);
			}
		}
	}
}
