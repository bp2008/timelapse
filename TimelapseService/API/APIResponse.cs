namespace TimelapseService.API
{
	/// <summary>
	/// A response from an API method call.
	/// </summary>
	public class APIResponse
	{
		/// <summary>
		/// The result of the API method. May be null.
		/// </summary>
		public object Result { get; protected set; }
		/// <summary>
		/// An error message from the API method. May be null.
		/// </summary>
		public string Error { get; protected set; }
		public APIResponse() { }
		public static APIResponse OK(object result)
		{
			return new APIResponse() { Result = result };
		}
		public static APIResponse ErrorMessage(string errorMessage)
		{
			return new APIResponse() { Error = errorMessage };
		}
	}
}