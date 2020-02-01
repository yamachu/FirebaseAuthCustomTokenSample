using System.Text.Json.Serialization;
using Example.Function.Helpers;
using Google.Cloud.Firestore;

namespace Example.Function.DataObjects
{
	[FirestoreData]
	public class AuthTemporary
	{
		[JsonConverter(typeof(FirestoreDocumentStringFieldConverter))]
		[FirestoreProperty]
		public string Nonce { get; set; }

		[JsonConverter(typeof(FirestoreDocumentStringFieldConverter))]
		[FirestoreProperty]
		public string State { get; set; }

		public static AuthTemporary GenerateRandomAuthTemporary() => new AuthTemporary
		{
			State = System.Guid.NewGuid().ToString().Replace("-", ""),
			Nonce = System.Guid.NewGuid().ToString().Replace("-", ""),
		};
	}
}