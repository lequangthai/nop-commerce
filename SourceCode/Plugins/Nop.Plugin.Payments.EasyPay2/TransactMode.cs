namespace Nop.Plugin.Payments.EasyPay2
{
    /// <summary>
    /// Represents manual payment processor transaction mode
    /// </summary>
    public enum TransactMode
    {
        /// <summary>
        /// Pending
        /// </summary>
        //Pending = 0,
        /// <summary>
        /// Authorize
        /// </summary>
        Authorize = 0,
        /// <summary>
        /// Authorize and capture
        /// </summary>
        AuthorizeAndCapture= 2
    }
}
