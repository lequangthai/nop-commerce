using System.Data.Entity.ModelConfiguration;
using Nop.Plugin.Widgets.IkBanner.Domain;

namespace Nop.Plugin.Widgets.IkBanner.Data
{
    public partial class IkBannerWidgetzoneMap : EntityTypeConfiguration<IkBannerWidgetzone>
    {
        public IkBannerWidgetzoneMap()
        {
            this.ToTable("IkBannerWidgetzone");
            this.HasKey(x => x.Id);

        }
    }
}