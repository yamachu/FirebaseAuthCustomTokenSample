using System.Text.Json.Serialization;
using Example.Function.Helpers;
using Google.Cloud.Firestore;

namespace Example.Function.DataObjects
{
	[FirestoreData]
	public class CustomProviderDocument
	{
		[JsonConverter(typeof(FirestoreDocumentStringFieldConverter))]
		[FirestoreProperty]
		public string Id { get; set; }
	}
}