using System.Text.Json.Serialization;
using Example.Function.Helpers;

namespace Example.Function.DataObjects
{
	public class CustomProviderDocument
	{
		[JsonConverter(typeof(FirestoreDocumentStringFieldConverter))]
		public string Id { get; set; }
	}
}