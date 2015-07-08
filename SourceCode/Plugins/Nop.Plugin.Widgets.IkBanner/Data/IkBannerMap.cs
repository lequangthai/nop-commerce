using System.Data.Entity.ModelConfiguration;
using Nop.Plugin.Widgets.IkBanner.Domain;

namespace Nop.Plugin.Widgets.IkBanner.Data
{
    public partial class IkBannerMap : EntityTypeConfiguration<IkBanner.Domain.IkBanner>
    {
        public IkBannerMap()
        {
            this.ToTable("IkBanner");
            this.HasKey(x => x.Id);

        }
    }
}