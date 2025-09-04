namespace ASPEN
{
    /// <summary>
    /// All Software Projects Eventually Need User Settings
    /// </summary>
    public interface AspenUserSettings : AspenConfiguration
	{
		// For, As
		void Set<T>(object key, T val);
	}
}
