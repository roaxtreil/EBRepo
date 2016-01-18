namespace TDVayne.Modules
{
    /// <summary>
    ///     The Module Type
    /// </summary>
    internal enum ModuleType
    {
        /// <summary>
        ///     The Module is Executed Every Tick
        /// </summary>
        OnUpdate,

        /// <summary>
        ///     The module is executed after an AA
        /// </summary>
        OnAfterAA,

        /// <summary>
        ///     The module is executed under other conditions.
        /// </summary>
        Other
    }
}