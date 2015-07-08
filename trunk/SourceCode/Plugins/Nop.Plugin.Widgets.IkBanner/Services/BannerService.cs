using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nop.Core.Data;
using Nop.Plugin.Widgets.IkBanner.Domain;
using Nop.Services.Events;
using Nop.Core;

namespace Nop.Plugin.Widgets.IkBanner.Services
{
    public partial class BannerService : IBannerService
    {
        #region Fields

        private readonly IRepository<IkBanner.Domain.IkBanner> _ibRepository;
        private readonly IRepository<IkBannerWidgetzone> _ibzoneRepository;
        private readonly IEventPublisher _eventPublisher;
        #endregion

        #region Ctor

        public BannerService(IRepository<IkBanner.Domain.IkBanner> ibRepository, IRepository<IkBannerWidgetzone> ibzoneRepository, IEventPublisher eventPublisher)
        {
            this._ibRepository = ibRepository;
            this._ibzoneRepository = ibzoneRepository;
            this._eventPublisher = eventPublisher;
        }

        #endregion
        
        #region Methods

        public virtual void DeleteBanner(IkBanner.Domain.IkBanner bannerRecord)
        {
            if (bannerRecord == null)
                throw new ArgumentNullException("IkBanner");

            _ibRepository.Delete(bannerRecord);
        }

        public virtual IList<IkBanner.Domain.IkBanner> GetAll(int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = from bp in _ibRepository.Table
                        orderby bp.Id
                        select bp;

            var records = new PagedList<IkBanner.Domain.IkBanner>(query, pageIndex, pageSize);
            return records;
        }

        public virtual IList<IkBanner.Domain.IkBanner> GetAllByWidgetzone(int WidgetzoneId, int categoryId)
        {

            var sql = from bp in _ibRepository.Table
                        where bp.WidgetzoneId == WidgetzoneId
                        select bp;
            var query = sql;
            if (categoryId > 0)
                query = query.Where(x => x.CategoryId == categoryId);
            if (query.Count() == 0) query = sql.Where(x => x.CategoryId == 0);
            
            query = query.OrderBy(x => x.Id);
            return query.ToList();
        }

        public virtual IkBanner.Domain.IkBanner GetById(int Id)
        {
            if (Id == 0)
                return null;

            return _ibRepository.GetById(Id);
        }

        public virtual void InsertBanner(IkBanner.Domain.IkBanner bannerRecord)
        {
            if (bannerRecord == null)
                throw new ArgumentNullException("bannerRecord");

            _ibRepository.Insert(bannerRecord);
            
            //event notification
            _eventPublisher.EntityInserted(bannerRecord);
        }

        public virtual void UpdateBanner(IkBanner.Domain.IkBanner bannerRecord)
        {
            if (bannerRecord == null)
                throw new ArgumentNullException("bannerRecord");

            _ibRepository.Update(bannerRecord);

            //event notification
            _eventPublisher.EntityUpdated(bannerRecord);
        }

        #region BannerPlacement


        public virtual IList<IkBannerWidgetzone> GetAllBannerWidgetzones(int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = from bp in _ibzoneRepository.Table
                        orderby bp.Id
                        select bp;

            var records = new PagedList<IkBannerWidgetzone>(query, pageIndex, pageSize);
            return records;
        }


        public virtual IkBannerWidgetzone GetBannerWidgetzoneById(int WidgetzoneId)
        {
            var query = from bp in _ibzoneRepository.Table
                        where bp.Id == WidgetzoneId
                        select bp;
            return query.FirstOrDefault();
        }

        public virtual IkBannerWidgetzone GetBannerWidgetzoneByZone(string widgetZone)
        {
            var query = from bp in _ibzoneRepository.Table
                        where bp.WidgetZone == widgetZone
                        select bp;
            return query.FirstOrDefault();
        }

        public virtual void DeleteBannerWidgetzone(IkBannerWidgetzone widgetZone)
        {
            if (widgetZone == null)
                throw new ArgumentNullException("widgetZone");

            _ibzoneRepository.Delete(widgetZone);
        }

        public virtual void InsertBannerWidgetzone(IkBannerWidgetzone widgetZone)
        {
            if (widgetZone == null)
                throw new ArgumentNullException("widgetZone");

            _ibzoneRepository.Insert(widgetZone);

            //event notification
            _eventPublisher.EntityInserted(widgetZone);

        }

        public virtual void UpdateBannerWidgetzone(IkBannerWidgetzone widgetZone)
        {
            if (widgetZone == null)
                throw new ArgumentNullException("widgetZone");

            _ibzoneRepository.Update(widgetZone);

            //event notification
            _eventPublisher.EntityUpdated(widgetZone);
        }

        #endregion

        #endregion
    }
}
