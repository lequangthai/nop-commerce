using Nop.Core;

namespace Nop.Plugin.Widgets.IkBanner.Domain
{
    /// <summary>
    /// Represents a shipping by weight record
    /// </summary>
    public partial class IkBanner : BaseEntity
    {
        /// <summary>
        /// Gets or sets the store identifier
        /// </summary>
        public int StoreId { get; set; }

        public int CategoryId { get; set; }

        public int WidgetzoneId { get; set; }

        public string BannerHtml { get; set; }

    }
}