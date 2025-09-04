namespace ASPEN
{
    /// <summary>
    /// All Software Projects Eventually Need application Configuration.
    /// Typically embodies a configuration file and/or application launch arguments.
    /// </summary>
    public interface AspenConfiguration
	{
		// Of
		T Get<T>(object key);
	}
}
