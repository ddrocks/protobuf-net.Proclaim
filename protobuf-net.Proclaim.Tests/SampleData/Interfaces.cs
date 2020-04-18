namespace ProtoBuf.Extensions.SampleData
{
	public interface INotification
	{
		string Name { get; set; }
		string Email { get; set; }
		INotificationEnvironment Environment { get; }
	}

	public interface INotificationEnvironment
	{
		string WebsiteUrl { get; set; }
		string ResourceWebsiteUrl { get; set; }
	}

	public class MessageBase
	{
		public int MessageId { get; set; }
	}
}
