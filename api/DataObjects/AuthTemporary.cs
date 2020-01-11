using System.Text.Json.Serialization;
using Example.Function.Helpers;

namespace Example.Function.DataObjects
{
	public class AuthTemporary
	{
		[JsonConverter(typeof(FirestoreDocumentStringFieldConverter))]
		public string Nonce { get; set; }

		[JsonConverter(typeof(FirestoreDocumentStringFieldConverter))]
		public string State { get; set; }

		public static AuthTemporary GenerateRandomAuthTemporary() => new AuthTemporary
		{
			State = System.Guid.NewGuid().ToString().Replace("-", ""),
			Nonce = System.Guid.NewGuid().ToString().Replace("-", ""),
		};
	}
}