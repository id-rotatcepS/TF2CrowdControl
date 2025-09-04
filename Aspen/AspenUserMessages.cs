namespace ASPEN
{
    /// <summary>
	/// All Software Projects Eventually Need User Message text translation.
    /// Language internationalization (i18n).
	/// </summary>
    public interface AspenUserMessages
    {
        /// </summary>
        /// Processor for user-facing text of any kind - indirection of exact wording that also enables internationalization (i18n) of the application.
        /// also needed by view.
        /// object key allows support for less error-prone keys than strings.
        /// Typical C# implementation is equivalent to string.Format(resourceManager.GetString(key), args)
        /// </summary>
        string Formatted(object key, params object[] args);
        /// <summary>
        /// Get the unformatted string resource used by <see cref="Formatted(object, object[])"/>.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
		string Translated(object key);
	}
}