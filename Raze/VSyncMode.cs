namespace GVS
{
    public enum VSyncMode
    {
        /// <summary>
        /// Vertical sync is disabled. Image presentation rate is unlimited.
        /// </summary>
        DISABLED,
        /// <summary>
        /// Vertical sync is enabled. Image presentation waits for the vertical trace period, limiting frame rate to
        /// the monitor´s refresh rate.
        /// </summary>
        ENABLED,
        /// <summary>
        /// Vertical sync is enabled, and waits for the vertical trace period. Frame rate is limited to half of the
        /// monitor's refresh rate.
        /// </summary>
        DOUBLE
    }
}
