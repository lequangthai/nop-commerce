using Nop.Core;

namespace Nop.Plugin.Widgets.IkBanner.Domain
{
    /// <summary>
    /// Represents a shipping by weight record
    /// </summary>
    public partial class IkBannerWidgetzone : BaseEntity
    {

        public string WidgetZone { get; set; }

        public bool IsStatic { get; set; }

    }
}