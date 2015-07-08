using Autofac;
using Autofac.Core;
using Nop.Core.Data;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Data;
using Nop.Plugin.Widgets.IkBanner.Data;
using Nop.Plugin.Widgets.IkBanner.Domain;
using Nop.Plugin.Widgets.IkBanner.Services;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Widgets.IkBanner
{
    public class DependencyRegistrar : IDependencyRegistrar
    {
        public virtual void Register(ContainerBuilder builder, ITypeFinder typeFinder)
        {
            builder.RegisterType<BannerService>().As<IBannerService>().InstancePerLifetimeScope();

            //data context
            this.RegisterPluginDataContext<IkBannerObjectContext>(builder, "nop_object_context_widget_banner");

            //override required repository with our custom context
            builder.RegisterType<EfRepository<IkBanner.Domain.IkBanner>>()
                .As<IRepository<IkBanner.Domain.IkBanner>>()
                .WithParameter(ResolvedParameter.ForNamed<IDbContext>("nop_object_context_widget_banner"))
                .InstancePerLifetimeScope();

            builder.RegisterType<EfRepository<IkBannerWidgetzone>>()
                .As<IRepository<IkBannerWidgetzone>>()
                .WithParameter(ResolvedParameter.ForNamed<IDbContext>("nop_object_context_widget_banner"))
                .InstancePerLifetimeScope();
        }

        public int Order
        {
            get { return 1; }
        }
    }
}
