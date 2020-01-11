using System;

namespace Example.Function.Helpers
{
	// https://dev.to/amay077/kotlins-scope-functions-in-c-pbn
	static class ObjectExtensions
	{
		// Kotlin: fun <T, R> T.let(block: (T) -> R): R
		public static R Let<T, R>(this T self, Func<T, R> block)
		{
			return block(self);
		}

		// Kotlin: fun <T> T.also(block: (T) -> Unit): T
		public static T Also<T>(this T self, Action<T> block)
		{
			block(self);
			return self;
		}
	}
}