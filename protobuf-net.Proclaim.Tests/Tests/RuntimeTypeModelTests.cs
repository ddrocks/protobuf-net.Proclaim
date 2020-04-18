using System;
using System.Linq;
using NUnit.Framework;
using ProtoBuf.Extensions.SampleData;
using ProtoBuf.Meta;

namespace ProtoBuf.Extensions.Tests
{
	[TestFixture]
	public class RuntimeTypeModelTests
	{
		private RuntimeTypeModel _model;

		[OneTimeSetUp]
		public void Startup()
		{
			_model = RuntimeTypeModel.Create();
			_model.ApplyProclaims(assembly => assembly == GetType().Assembly);
		}

		[Test]
		[TestCase(typeof(AdminNotificationBase), typeof(INotification))]
		[TestCase(typeof(NotificationBase), typeof(INotification))]
		public void RuntimeTypeModel_MatchSubtype(Type type, Type baseType)
		{
			Assert.AreEqual(true, _model[baseType].GetSubtypes().Any(a => a.DerivedType.Type == type));
		}

		[Test]
		[TestCase(typeof(EmailEnvironment), typeof(INotification))]
		public void RuntimeTypeModel_MissingSubtype(Type type, Type baseType)
		{
			Assert.AreEqual(false, _model[baseType].GetSubtypes().Any(a => a.DerivedType.Type == type));
		}
	}
}
