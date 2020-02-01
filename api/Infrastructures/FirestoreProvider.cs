using Google.Cloud.Firestore;

namespace Example.Function.Infrastructures
{
	public interface IFirestoreProvider
	{
		FirestoreDb Instance { get; }
	}

	public class FirestoreProvider : IFirestoreProvider
	{
		public FirestoreProvider(FirestoreDb dbInstance)
		{
			this.Instance = dbInstance;
		}

		public FirestoreDb Instance { get; private set; }
	}
}
